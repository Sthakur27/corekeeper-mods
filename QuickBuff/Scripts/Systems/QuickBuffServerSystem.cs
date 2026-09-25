using System.Collections.Generic;
using Inventory;
using PlayerState;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace QuickBuff.Systems
{
    public struct QuickBuffResult
    {
        /// <summary>Distinct buff items found in the inventory.</summary>
        public int candidates;
        /// <summary>Items actually consumed this press.</summary>
        public int consumed;
        /// <summary>Items skipped because every buff they give is still active for long enough.</summary>
        public int skippedActive;
        /// <summary>Set when the request was rejected because the last one was too recent.</summary>
        public bool onCooldown;
        /// <summary>Non-null when nothing could be done at all (missing components, dead player...).</summary>
        public string error;
    }

    /// <summary>
    /// Server-world worker for the quick buff. It never runs on its own; <see cref="Consume"/> is
    /// called from the command handler on the server main thread.
    ///
    /// For each distinct (objectID, variation) in the player's main inventory that is an Eatable
    /// and yields at least one timed, non-permanent condition when consumed, it:
    ///   1. enqueues Inventory.Create.ConsumeEntityAt(player, slot, 1, destroy: true, dontConsume: godMode)
    ///      into the InventoryChangeBuffer singleton, exactly like EatableSlot.EatItem does, so the
    ///      stack decrement, slot lock reset and replication go through the vanilla inventory path;
    ///   2. applies the item's effects with the same helpers EatableSlotConsumeResultEvaluationSystem
    ///      uses (ConditionUIExtensions.GetConditionsOnConsume, EntityUtility.AddOrRefreshCondition,
    ///      PlayerController.HealPlayer / AddManaToPlayer / AddHunger, HealthChangeBuffer);
    ///   3. pushes the vanilla eat / drink effect event into the player's GhostEffectEventBuffer.
    /// The vanilla evaluation system can't be reused directly because it reads the *equipped* item
    /// (EquippedObjectCD), not an arbitrary inventory slot.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class QuickBuffServerSystem : PugSimulationSystemBase
    {
        /// <summary>Server-side guard between requests per player, in seconds (vanilla eat cooldown).</summary>
        private const float RequestCooldownSeconds = 0.4f;

        private ComponentLookup<FlowerCD> _flowerLookup;
        private ComponentLookup<FishCD> _fishLookup;
        private BufferLookup<GivesConditionsWhenConsumedBuffer> _givesConditionsLookup;

        private EntityQuery _databaseQuery;
        private EntityQuery _conditionsTableQuery;
        private EntityQuery _tickRateQuery;
        private EntityQuery _changeBufferQuery;
        private EntityQuery _healthChangeQuery;

        private readonly Dictionary<Entity, NetworkTick> _lastRequestTick = new Dictionary<Entity, NetworkTick>();

        protected override void OnCreate()
        {
            base.OnCreate();
            _flowerLookup = GetComponentLookup<FlowerCD>(true);
            _fishLookup = GetComponentLookup<FishCD>(true);
            _givesConditionsLookup = GetBufferLookup<GivesConditionsWhenConsumedBuffer>(true);

            _databaseQuery = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            _conditionsTableQuery = GetEntityQuery(ComponentType.ReadOnly<ConditionsTableCD>());
            _tickRateQuery = GetEntityQuery(ComponentType.ReadOnly<ClientServerTickRate>());
            _changeBufferQuery = GetEntityQuery(ComponentType.ReadWrite<InventoryChangeBuffer>());
            _healthChangeQuery = GetEntityQuery(ComponentType.ReadWrite<HealthChangeBuffer>());

            // Nothing to do per tick; all work happens in Consume().
            Enabled = false;
        }

        public QuickBuffResult Consume(Entity player, bool skipActive, float skipSeconds)
        {
            var result = new QuickBuffResult();
            var em = EntityManager;

            if (!em.Exists(player)
                || !em.HasBuffer<ContainedObjectsBuffer>(player)
                || !em.HasBuffer<ConditionsBuffer>(player)
                || !em.HasBuffer<SummarizedConditionsBuffer>(player)
                || !em.HasBuffer<SummarizedConditionEffectsBuffer>(player)
                || !em.HasComponent<HealthCD>(player)
                || !em.HasComponent<ManaCD>(player)
                || !em.HasComponent<HungerCD>(player)
                || !em.HasComponent<PlayerStateCD>(player))
            {
                result.error = "player entity is missing inventory or condition data.";
                return result;
            }
            if (_databaseQuery.IsEmptyIgnoreFilter || _conditionsTableQuery.IsEmptyIgnoreFilter || _changeBufferQuery.IsEmptyIgnoreFilter)
            {
                result.error = "world is not ready.";
                return result;
            }

            var playerState = em.GetComponentData<PlayerStateCD>(player);
            if (playerState.HasAnyState(PlayerStateEnum.Death))
            {
                result.error = "you are dead.";
                return result;
            }

            if (!_tickRateQuery.TryGetSingleton(out ClientServerTickRate tickRateData))
                tickRateData.ResolveDefaults();
            uint tickRate = (uint)tickRateData.SimulationTickRate;
            if (tickRate == 0) tickRate = 60;
            NetworkTick currentTick = GetServerTick();

            if (_lastRequestTick.TryGetValue(player, out NetworkTick lastTick) && lastTick.IsValid && currentTick.IsValid
                && currentTick.TicksSince(lastTick) < (int)(RequestCooldownSeconds * tickRate))
            {
                result.onCooldown = true;
                return result;
            }
            _lastRequestTick[player] = currentTick;

            _flowerLookup.Update(this);
            _fishLookup.Update(this);
            _givesConditionsLookup.Update(this);

            PugDatabase.DatabaseBankCD databaseBank = _databaseQuery.GetSingleton<PugDatabase.DatabaseBankCD>();
            ConditionsTableCD conditionsTable = _conditionsTableQuery.GetSingleton<ConditionsTableCD>();
            Entity changeBufferEntity = _changeBufferQuery.GetSingletonEntity();
            Entity healthChangeEntity = _healthChangeQuery.IsEmptyIgnoreFilter ? Entity.Null : _healthChangeQuery.GetSingletonEntity();

            bool godMode = em.HasComponent<GodModeCD>(player) && em.IsComponentEnabled<GodModeCD>(player);
            float3 position = em.HasComponent<LocalTransform>(player) ? em.GetComponentData<LocalTransform>(player).Position : float3.zero;

            // Main inventory range (hotbar + bag); equipment and other slots are excluded.
            DynamicBuffer<ContainedObjectsBuffer> contained = em.GetBuffer<ContainedObjectsBuffer>(player, true);
            int start = 0;
            int end = contained.Length;
            if (em.HasBuffer<InventoryBuffer>(player))
            {
                DynamicBuffer<InventoryBuffer> inventories = em.GetBuffer<InventoryBuffer>(player, true);
                if (inventories.Length > 0)
                {
                    start = math.clamp(inventories[0].startIndex, 0, contained.Length);
                    end = math.clamp(start + inventories[0].size, start, contained.Length);
                }
            }

            // Snapshot the slots first: applying effects touches other buffers on the same entity.
            var slots = new List<KeyValuePair<int, ObjectDataCD>>();
            var seen = new HashSet<long>();
            for (int i = start; i < end; i++)
            {
                ObjectDataCD item = contained[i].objectData;
                if (item.objectID == ObjectID.None || item.amount < 1) continue;
                long key = ((long)item.objectID << 32) | (uint)item.variation;
                if (!seen.Add(key)) continue;
                slots.Add(new KeyValuePair<int, ObjectDataCD>(i, item));
            }

            foreach (var slot in slots)
            {
                int index = slot.Key;
                ObjectDataCD item = slot.Value;

                if (PugDatabase.GetEntityObjectInfo(item.objectID, databaseBank.databaseBankBlob).objectType != ObjectType.Eatable) continue;
                Entity prefab = PugDatabase.GetPrimaryPrefabEntity(item.objectID, databaseBank.databaseBankBlob, item.variation);
                if (prefab == Entity.Null) continue;
                if (em.HasComponent<PetCandyCD>(prefab) || em.HasComponent<CattleCD>(prefab)) continue;

                bool isCooked = em.HasComponent<CookedFoodCD>(prefab);
                bool isPotion = em.HasComponent<PotionCD>(prefab);

                // Same ingredient expansion as the vanilla evaluation system.
                var ingredients = new FixedList64Bytes<ObjectDataCD>();
                ingredients.Add(item);
                if (isCooked)
                {
                    ingredients.Add(new ObjectDataCD { objectID = CookedFoodCD.GetPrimaryIngredientFromVariation(item.variation), amount = 1 });
                    ingredients.Add(new ObjectDataCD { objectID = CookedFoodCD.GetSecondaryIngredientFromVariation(item.variation), amount = 1 });
                }

                DynamicBuffer<SummarizedConditionsBuffer> summarizedConditions = em.GetBuffer<SummarizedConditionsBuffer>(player);
                NativeArray<ConditionData> conditions = ConditionUIExtensions.GetConditionsOnConsume(
                    item, ingredients, isCooked, player, databaseBank, conditionsTable,
                    _flowerLookup, _fishLookup, _givesConditionsLookup, summarizedConditions, Allocator.Temp);

                try
                {
                    if (!GivesBuff(conditions, conditionsTable)) continue; // hunger-only food, no-effect items, bombs, seeds...
                    result.candidates++;

                    if (skipActive && AllBuffsActive(conditions, conditionsTable, em.GetBuffer<ConditionsBuffer>(player, true), currentTick, tickRate, skipSeconds))
                    {
                        result.skippedActive++;
                        continue;
                    }

                    // 1. Consume through the vanilla inventory path (same call as EatableSlot.EatItem).
                    DynamicBuffer<InventoryChangeBuffer> changes = em.GetBuffer<InventoryChangeBuffer>(changeBufferEntity);
                    changes.Add(new InventoryChangeBuffer
                    {
                        playerEntity = player,
                        inventoryChangeData = Create.ConsumeEntityAt(player, index, 1, destroy: true, godMode, position, item.variation)
                    });

                    // 2. Apply effects (mirror of EatableSlotConsumeResultEvaluationSystem.Execute).
                    ApplyEffects(player, item, conditions, conditionsTable, currentTick, tickRate, healthChangeEntity);

                    // 3. Eat / drink feedback.
                    PlayEffect(player, item.objectID, isPotion, position, currentTick);

                    result.consumed++;
                }
                finally
                {
                    conditions.Dispose();
                }
            }

            return result;
        }

        /// <summary>Instant effects handled by the evaluation system's switch; they are not buffs.</summary>
        private static bool IsInstant(ConditionID id)
        {
            return id == ConditionID.HealthAddition
                || id == ConditionID.HealthAdditionPercentage
                || id == ConditionID.ManaAdditionPercentage
                || id == ConditionID.HealthReduction
                || id == ConditionID.HungerAddition;
        }

        private static bool IsBuff(ConditionData data, ConditionsTableCD table)
        {
            if (data.conditionID == ConditionID.None || IsInstant(data.conditionID)) return false;
            if (data.duration <= 0f) return false;
            ConditionInfoBlob info = table.GetConditionInfo(data.conditionID);
            return !info.isPermanent;
        }

        private static bool GivesBuff(NativeArray<ConditionData> conditions, ConditionsTableCD table)
        {
            for (int i = 0; i < conditions.Length; i++)
                if (IsBuff(conditions[i], table)) return true;
            return false;
        }

        /// <summary>True when every buff the item gives is already active with at least skipSeconds left.</summary>
        private static bool AllBuffsActive(NativeArray<ConditionData> conditions, ConditionsTableCD table, DynamicBuffer<ConditionsBuffer> active,
            NetworkTick currentTick, uint tickRate, float skipSeconds)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                if (!IsBuff(conditions[i], table)) continue;
                float remaining = RemainingSeconds(conditions[i].conditionID, active, currentTick, tickRate);
                if (remaining < skipSeconds) return false;
            }
            return true;
        }

        private static float RemainingSeconds(ConditionID id, DynamicBuffer<ConditionsBuffer> active, NetworkTick currentTick, uint tickRate)
        {
            for (int i = 0; i < active.Length; i++)
            {
                Condition condition = active[i].condition;
                if (condition.conditionData.conditionID != id || condition.toBeRemoved) continue;
                if (!condition.removeTick.IsValid || !currentTick.IsValid) return float.MaxValue; // no expiry
                int ticksLeft = condition.removeTick.TicksSince(currentTick);
                return ticksLeft <= 0 ? 0f : ticksLeft / (float)tickRate;
            }
            return -1f; // not active
        }

        private void ApplyEffects(Entity player, ObjectDataCD item, NativeArray<ConditionData> conditions, ConditionsTableCD conditionsTable,
            NetworkTick currentTick, uint tickRate, Entity healthChangeEntity)
        {
            var em = EntityManager;
            HealthCD health = em.GetComponentData<HealthCD>(player);
            ManaCD mana = em.GetComponentData<ManaCD>(player);
            HungerCD hunger = em.GetComponentData<HungerCD>(player);
            PlayerStateCD playerState = em.GetComponentData<PlayerStateCD>(player);
            DynamicBuffer<SummarizedConditionEffectsBuffer> summarizedEffects = em.GetBuffer<SummarizedConditionEffectsBuffer>(player);
            DynamicBuffer<SummarizedConditionsBuffer> summarizedConditions = em.GetBuffer<SummarizedConditionsBuffer>(player);
            DynamicBuffer<ConditionsBuffer> conditionsBuffer = em.GetBuffer<ConditionsBuffer>(player);
            ObjectID objectID = item.objectID;

            for (int i = 0; i < conditions.Length; i++)
            {
                ConditionData data = conditions[i];
                switch (data.conditionID)
                {
                    case ConditionID.HealthAddition:
                        PlayerController.HealPlayer(data.value, ref health, in playerState, in summarizedEffects);
                        continue;
                    case ConditionID.HealthAdditionPercentage:
                    {
                        int maxHealth = health.GetMaxHealthWithConditions(summarizedEffects);
                        int amount = (int)math.round(math.clamp(data.value / 100f * maxHealth, 0f, maxHealth));
                        PlayerController.HealPlayer(amount, ref health, in playerState, in summarizedEffects);
                        if (objectID == ObjectID.HealingPotion || objectID == ObjectID.GreaterHealingPotion)
                        {
                            float potionHot = summarizedConditions[113].value / 10f;
                            if (potionHot > 0f)
                            {
                                EntityUtility.AddOrRefreshCondition(new ConditionData
                                {
                                    conditionID = ConditionID.HealOverTimeFromPotion,
                                    value = (int)math.round(amount * potionHot / 20f),
                                    duration = 20f
                                }, conditionsBuffer, conditionsTable, currentTick, tickRate, summarizedConditions);
                            }
                        }
                        continue;
                    }
                    case ConditionID.ManaAdditionPercentage:
                    {
                        int maxMana = mana.maxMana;
                        PlayerController.AddManaToPlayer((int)math.round(math.clamp(data.value / 100f * maxMana, 0f, maxMana)), ref mana, in playerState, in summarizedEffects);
                        continue;
                    }
                    case ConditionID.HealthReduction:
                        if (healthChangeEntity != Entity.Null)
                        {
                            em.GetBuffer<HealthChangeBuffer>(healthChangeEntity).Add(new HealthChangeBuffer
                            {
                                healthChange = new HealthChange { entity = player, amount = data.value }
                            });
                        }
                        continue;
                    case ConditionID.HungerAddition:
                        PlayerController.AddHunger(data.value, in playerState, ref hunger);
                        continue;
                }

                int lengthBefore = conditionsBuffer.Length;
                EntityUtility.AddOrRefreshCondition(data, conditionsBuffer, conditionsTable, currentTick, tickRate, summarizedConditions);
                bool added = lengthBefore != conditionsBuffer.Length;
                ConditionInfoBlob info = conditionsTable.GetConditionInfo(data.conditionID);
                if ((added || !info.isUnique) && info.effect == ConditionEffect.MaxHealthPermanent)
                    PushEffectEvent(player, new EffectEventCD { effectID = EffectID.EatIncreaseMaxHealthItem, entity = player }, currentTick);
            }

            em.SetComponentData(player, health);
            em.SetComponentData(player, mana);
            em.SetComponentData(player, hunger);
        }

        private void PlayEffect(Entity player, ObjectID objectID, bool isPotion, float3 position, NetworkTick currentTick)
        {
            EffectID effectID;
            if (isPotion) effectID = EffectID.DrinkPotionDefault;
            else if (objectID == ObjectID.Mushroom) effectID = EffectID.EatMushroom;
            else if (objectID == ObjectID.HeartBerry) effectID = EffectID.EatHeartBerry;
            else effectID = EffectID.EatDefault;

            PushEffectEvent(player, new EffectEventCD { effectID = effectID, position1 = position, entity = player }, currentTick);
        }

        private void PushEffectEvent(Entity player, EffectEventCD effect, NetworkTick currentTick)
        {
            var em = EntityManager;
            if (!em.HasBuffer<GhostEffectEventBuffer>(player) || !em.HasComponent<GhostEffectEventBufferPointerCD>(player)) return;
            DynamicBuffer<GhostEffectEventBuffer> buffer = em.GetBuffer<GhostEffectEventBuffer>(player);
            GhostEffectEventBufferPointerCD pointer = em.GetComponentData<GhostEffectEventBufferPointerCD>(player);
            var item = new GhostEffectEventBuffer { Tick = currentTick, value = effect };
            buffer.AddToRingBuffer(ref pointer, in item);
            em.SetComponentData(player, pointer);
        }
    }
}
