using Inventory;
using PlayerState;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace WormholePotion
{
    // Casting's item-ID switch is inside a Burst job; Harmony cannot intercept it.
    // Handle our own cast before that job and use its existing replicated "in use"
    // flag to prevent the vanilla switch running. Same flow as a Recall Idol: a
    // three-second cast, then vanilla's teleport sequence to the Core. On the server
    // the potion is consumed and a one-use waypoint marker is left where it was drunk.
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PlayerStateSystemGroup))]
    [UpdateBefore(typeof(UpdatePlayerStateSystem))]
    [UpdateBefore(typeof(ChangePlayerStateSystem))]
    public partial class PotionCastSystem : PugSimulationSystemBase
    {
        private EntityQuery players, time, rate, changes, bankQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            // Read-only: writes happen only while a potion is being cast, through EntityManager.
            players = GetEntityQuery(ComponentType.ReadOnly<Simulate>(), ComponentType.ReadOnly<CastingStateCD>(),
                ComponentType.ReadOnly<PlayerStateCD>(), ComponentType.ReadOnly<EquippedObjectCD>(),
                ComponentType.ReadOnly<HealthCD>(), ComponentType.ReadOnly<PlayerMovementCD>(),
                ComponentType.ReadOnly<TeleportingStateCD>(), ComponentType.ReadOnly<SummarizedConditionEffectsBuffer>(),
                ComponentType.ReadOnly<AnimationBuffer>(), ComponentType.ReadOnly<AnimationBufferPointer>(),
                ComponentType.ReadOnly<LocalTransform>());
            time = GetEntityQuery(ComponentType.ReadOnly<NetworkTime>());
            rate = GetEntityQuery(ComponentType.ReadOnly<ClientServerTickRate>());
            changes = GetEntityQuery(ComponentType.ReadWrite<InventoryChangeBuffer>());
            bankQuery = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            RequireForUpdate(time);
            RequireForUpdate(rate);
        }
        protected override void OnUpdate()
        {
            var potion = WormholePotionMod.Potion;
            if (potion == ObjectID.None) return;
            var tick = time.GetSingleton<NetworkTime>().ServerTick;
            uint tickRate = (uint)rate.GetSingleton<ClientServerTickRate>().SimulationTickRate;
            if (!tick.IsValid || tickRate == 0) return;
            bool server = World.IsServer();
            using (var entities = players.ToEntityArray(Allocator.Temp))
            foreach (var entity in entities)
            {
                var state = EntityManager.GetComponentData<PlayerStateCD>(entity);
                if (!state.HasAnyState(PlayerStateEnum.Casting)) continue;
                var cast = EntityManager.GetComponentData<CastingStateCD>(entity);
                if (cast.objectData.objectID != potion) continue;
                // Leave death, stun and already-requested transitions to the game.
                if (state.nextState != PlayerStateEnum.Null && state.nextState != PlayerStateEnum.Casting) continue;
                if (!cast.itemIsInProcessOfBeingUsed) Animate(entity, -1518581387, tick);
                cast.itemIsInProcessOfBeingUsed = true;
                var equipped = EntityManager.GetComponentData<EquippedObjectCD>(entity);
                var health = EntityManager.GetComponentData<HealthCD>(entity);
                int maxHealth = health.GetMaxHealthWithConditions(EntityManager.GetBuffer<SummarizedConditionEffectsBuffer>(entity, true));
                // Same damage pushback as vanilla Casting.UpdateState.
                if (cast.previousHealth > health.health && cast.previousMaxHealth == maxHealth && maxHealth > 0)
                {
                    uint pushback = (uint)math.round((float)(cast.previousHealth - health.health) / maxHealth * 3f / tickRate);
                    uint elapsed = (uint)cast.castTimer.GetElapsedTicks(tick);
                    cast.castTimer.startTick.Add(math.min(pushback, elapsed));
                }
                cast.previousHealth = health.health;
                cast.previousMaxHealth = maxHealth;
                // Check identity even on completion: a last-tick slot switch must not
                // use up an item that is no longer held.
                bool changed = equipped.containedObject.objectData.objectID != potion
                    || equipped.equippedSlotIndex != cast.inventoryIndexOnCast;
                bool moving = math.length(EntityManager.GetComponentData<PlayerMovementCD>(entity).targetMovementVelocity) > 0.1f;
                if (!WormholePotionMod.Active || changed || moving)
                {
                    state.SetNextState(PlayerStateEnum.Walk);
                    Animate(entity, -1065991089, tick);
                }
                else if (cast.castTimer.IsTimerElapsed(tick))
                {
                    var teleport = EntityManager.GetComponentData<TeleportingStateCD>(entity);
                    teleport.targetPosition = PlayerControllerBurstableStatics.PLAYER_SPAWN_POSITION;
                    EntityManager.SetComponentData(entity, teleport);
                    state.SetNextState(PlayerStateEnum.Teleporting, nextStateLocked: true);
                    if (server) Complete(entity, equipped);
                }
                EntityManager.SetComponentData(entity, cast);
                EntityManager.SetComponentData(entity, state);
            }
        }
        private void Complete(Entity player, EquippedObjectCD equipped)
        {
            float3 origin = EntityManager.GetComponentData<LocalTransform>(player).Position;
            bool godMode = EntityManager.HasComponent<GodModeCD>(player) && EntityManager.IsComponentEnabled<GodModeCD>(player);
            if (!changes.IsEmpty)
                EntityManager.GetBuffer<InventoryChangeBuffer>(changes.GetSingletonEntity()).Add(new InventoryChangeBuffer
                {
                    inventoryChangeData = Create.ConsumeEntityAt(player, equipped.equippedSlotIndex, 1, destroy: true, godMode,
                        origin, equipped.containedObject.variation),
                    playerEntity = player
                });
            if (!bankQuery.IsEmpty)
                ReturnMarkers.Spawn(World, player, origin, bankQuery.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob);
        }
        private void Animate(Entity entity, int trigger, NetworkTick tick)
        {
            var pointer = EntityManager.GetComponentData<AnimationBufferPointer>(entity);
            PlayerController.PlayAnimationTrigger(trigger, tick, EntityManager.GetBuffer<AnimationBuffer>(entity), ref pointer);
            EntityManager.SetComponentData(entity, pointer);
        }
    }
}
