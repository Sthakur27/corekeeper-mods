using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BossMerchants.Systems
{
    /// <summary>
    /// Server-side safety net for boss merchants that already live in a save (and the prefab): every few
    /// seconds, keep each merchant's list equal to its catalog stock and its inventory big enough
    /// (<see cref="MerchantStock"/>). When items are newly added to a live merchant, its restock timer
    /// (ObjectDataCD.amount, counted down by MerchantBuyInventorySystem) is zeroed so the stock shows up now.
    /// Read-only in steady state: write access is only taken when a buffer actually changes.
    ///
    /// Boss summons (<see cref="Catalog.Entry.unlockedBy"/>) are listed once their boss is beaten: it is in the
    /// world's KilledEnemiesBuffer (UpdateHealthFromBufferSystem records every boss kill there, server side) or,
    /// for the three Titans, a connected player holds its soul (Potion Seller's rule).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class MerchantStockSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 3f;
        // Staggered start so the merchant mods' timers don't share a tick (Potion Seller 1.3, Fish Seller 0.x).
        private float _timer = 2.1f;
        private EntityQuery _merchants;
        private EntityQuery _killed;
        private EntityQuery _souls;
        private readonly HashSet<ObjectID> _beaten = new HashSet<ObjectID>();
        private readonly HashSet<ObjectID> _loggedUnlock = new HashSet<ObjectID>();
        private System.Func<ObjectID, bool> _isBeaten;

        protected override void OnCreate()
        {
            base.OnCreate();
            _isBeaten = _beaten.Contains;
            _merchants = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MerchantCD>(),
                    ComponentType.ReadOnly<ObjectDataCD>(),
                    ComponentType.ReadOnly<MerchantItemInfoBuffer>(),
                    ComponentType.ReadOnly<ContainedObjectsBuffer>(),
                    ComponentType.ReadOnly<InventoryBuffer>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
            _killed = GetEntityQuery(ComponentType.ReadOnly<KilledEnemiesBuffer>());
            _souls = GetEntityQuery(ComponentType.ReadOnly<PlayerGhost>(), ComponentType.ReadOnly<CollectedSoulsBuffer>());
        }

        /// <summary>The soul a player gets for beating each Titan (SoulID.None for other bosses).</summary>
        private static SoulID SoulFor(ObjectID boss)
        {
            switch (boss)
            {
                case ObjectID.BirdBoss: return SoulID.SoulOfAzeos;
                case ObjectID.OctopusBoss: return SoulID.SoulOfOmoroth;
                case ObjectID.ScarabBoss: return SoulID.SoulOfScarab;
                default: return SoulID.None;
            }
        }

        private void RefreshBeaten()
        {
            _beaten.Clear();
            if (_killed.TryGetSingletonBuffer<KilledEnemiesBuffer>(out var killed, true))
                for (int i = 0; i < killed.Length; i++) _beaten.Add(killed[i].objectData.objectID);

            if (!_souls.IsEmpty)
            {
                var players = _souls.ToEntityArray(Allocator.Temp);
                for (int p = 0; p < players.Length; p++)
                {
                    var souls = EntityManager.GetBuffer<CollectedSoulsBuffer>(players[p], true);
                    for (int s = 0; s < souls.Length; s++)
                    {
                        switch (souls[s].soulId)
                        {
                            case SoulID.SoulOfAzeos: _beaten.Add(ObjectID.BirdBoss); break;
                            case SoulID.SoulOfOmoroth: _beaten.Add(ObjectID.OctopusBoss); break;
                            case SoulID.SoulOfScarab: _beaten.Add(ObjectID.ScarabBoss); break;
                        }
                    }
                }
                players.Dispose();
            }
        }

        private void LogUnlocks(Catalog.Merchant merchant)
        {
            foreach (var entry in merchant.Stock)
                if (entry.unlockedBy != ObjectID.None && _beaten.Contains(entry.unlockedBy) && _loggedUnlock.Add(entry.id))
                    Debug.Log($"[{BossMerchantsMod.Name}] {entry.unlockedBy} beaten{(SoulFor(entry.unlockedBy) != SoulID.None ? " (kill or soul)" : "")}: {merchant.Key} merchant sells {entry.id}.");
        }

        protected override void OnUpdate()
        {
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f) return;
            _timer = IntervalSeconds;
            if (_merchants.IsEmpty) return;
            Catalog.Resolve();
            RefreshBeaten();

            int stock = BossMerchantsConfig.Stock;
            var entities = _merchants.ToEntityArray(Allocator.Temp);
            var objects = _merchants.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var merchant = Catalog.ByMerchantId(objects[i].objectID);
                if (merchant == null) continue;
                var e = entities[i];
                bool isPrefab = EntityManager.HasComponent<Prefab>(e);
                // The prefab never lists boss summons; live merchants list the unlocked ones.
                if (!MerchantStock.Apply(EntityManager, e, merchant, stock, isPrefab ? null : _isBeaten, out bool addedItems)) continue;
                if (addedItems && !isPrefab)
                {
                    var od = EntityManager.GetComponentData<ObjectDataCD>(e);
                    od.amount = 0; // restock on the next MerchantBuyInventorySystem tick
                    EntityManager.SetComponentData(e, od);
                    LogUnlocks(merchant);
                }
                Debug.Log($"[{BossMerchantsMod.Name}] {merchant.Key} merchant {(isPrefab ? "prefab" : e.ToString())}: {EntityManager.GetBuffer<MerchantItemInfoBuffer>(e, true).Length} items, stock {stock}{(addedItems && !isPrefab ? ", restock forced" : "")}.");
            }
            entities.Dispose();
            objects.Dispose();
        }
    }
}
