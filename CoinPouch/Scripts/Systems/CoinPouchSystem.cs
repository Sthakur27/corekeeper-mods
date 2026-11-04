using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace CoinPouch.Systems
{
    /// <summary>
    /// Server, every <see cref="Interval"/> ticks per player:
    ///  - characters from saves made before the mod (or player entities kept in a world save) get the
    ///    coin slot, its inventory range and its coin-only requirement;
    ///  - the coin slot stays locked, so quick stack to nearby chests leaves it alone (pickup and buying
    ///    ignore locks);
    ///  - Ancient Coins in the inventory (picked up while the slot was at 9999, sold for, taken from a
    ///    chest, dragged out) move into the coin slot, up to <see cref="CoinLayout.MaxCoins"/>.
    ///    Vanilla code stops stacks at 9999, so only this system fills the slot past that.
    /// Everything is checked read-only first: a read-write GetBuffer waits for every job using
    /// ContainedObjectsBuffer, and in steady state there is nothing to write.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class CoinPouchSystem : PugSimulationSystemBase
    {
        private const int Interval = 6;

        private EntityQuery _players;
        private int _tick;
        private bool _warnedRange;

        protected override void OnCreate()
        {
            base.OnCreate();
            _players = GetEntityQuery(
                ComponentType.ReadOnly<PlayerGhost>(),
                ComponentType.ReadOnly<ContainedObjectsBuffer>(),
                ComponentType.ReadOnly<InventoryBuffer>());
            RequireForUpdate(_players);
        }

        protected override void OnUpdate()
        {
            if (!CoinLayout.Ready || ++_tick < Interval) return;
            _tick = 0;
            var entities = _players.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity e = entities[i];
                EnsureLayout(e);
                if (!LayoutOk(e)) continue;
                EnsureLocked(e);
                if (NeedsSweep(e)) Sweep(e);
            }
            entities.Dispose();
        }

        private void EnsureLayout(Entity e)
        {
            int slot = CoinLayout.Slot, index = CoinLayout.InventoryIndex;
            if (EntityManager.GetBuffer<ContainedObjectsBuffer>(e, true).Length <= slot)
            {
                var contained = EntityManager.GetBuffer<ContainedObjectsBuffer>(e);
                while (contained.Length <= slot) contained.Add(default);
            }
            if (EntityManager.GetBuffer<InventoryBuffer>(e, true).Length == index)
                EntityManager.GetBuffer<InventoryBuffer>(e).Add(CoinLayout.Range(slot));
            if (!EntityManager.HasBuffer<InventorySlotRequirementBuffer>(e)) return;
            var requirements = EntityManager.GetBuffer<InventorySlotRequirementBuffer>(e, true);
            for (int r = 0; r < requirements.Length; r++)
                if (CoinLayout.IsRequirement(requirements[r])) return;
            EntityManager.GetBuffer<InventorySlotRequirementBuffer>(e).Add(CoinLayout.Requirement(index));
        }

        private bool LayoutOk(Entity e)
        {
            var inventories = EntityManager.GetBuffer<InventoryBuffer>(e, true);
            if (inventories.Length > CoinLayout.InventoryIndex && inventories[CoinLayout.InventoryIndex].startIndex == CoinLayout.Slot) return true;
            if (!_warnedRange)
            {
                _warnedRange = true;
                Debug.LogWarning($"[{CoinPouchMod.Name}] A player's inventory range {CoinLayout.InventoryIndex} is not the coin slot (another mod changed the player inventories); coin slot inactive for that player.");
            }
            return false;
        }

        private void EnsureLocked(Entity e)
        {
            if (!EntityManager.HasBuffer<LockedObjectsBuffer>(e)) return;
            var locked = EntityManager.GetBuffer<LockedObjectsBuffer>(e, true);
            if (locked.Length > CoinLayout.Slot && locked[CoinLayout.Slot].Value) return;
            var rw = EntityManager.GetBuffer<LockedObjectsBuffer>(e);
            while (rw.Length <= CoinLayout.Slot) rw.Add(default);
            rw[CoinLayout.Slot] = new LockedObjectsBuffer { Value = true };
        }

        private bool NeedsSweep(Entity e)
        {
            var contained = EntityManager.GetBuffer<ContainedObjectsBuffer>(e, true);
            ObjectDataCD pouch = contained[CoinLayout.Slot].objectData;
            if (pouch.objectID != ObjectID.None && (pouch.objectID != ObjectID.AncientCoin || pouch.amount >= CoinLayout.MaxCoins)) return false;
            InventoryBuffer main = EntityManager.GetBuffer<InventoryBuffer>(e, true)[0];
            int end = System.Math.Min(contained.Length, main.startIndex + main.size);
            for (int i = main.startIndex; i < end; i++)
                if (contained[i].objectData.objectID == ObjectID.AncientCoin) return true;
            return false;
        }

        private void Sweep(Entity e)
        {
            InventoryBuffer main = EntityManager.GetBuffer<InventoryBuffer>(e, true)[0];
            var contained = EntityManager.GetBuffer<ContainedObjectsBuffer>(e);
            bool hasLocks = EntityManager.HasBuffer<LockedObjectsBuffer>(e);
            int slot = CoinLayout.Slot;
            int end = System.Math.Min(contained.Length, main.startIndex + main.size);
            for (int i = main.startIndex; i < end; i++)
            {
                ContainedObjectsBuffer from = contained[i];
                if (from.objectData.objectID != ObjectID.AncientCoin) continue;
                ContainedObjectsBuffer to = contained[slot];
                int room = to.objectData.objectID == ObjectID.None ? CoinLayout.MaxCoins : CoinLayout.MaxCoins - to.objectData.amount;
                if (room <= 0) break;
                int move = System.Math.Min(room, from.objectData.amount);
                if (to.objectData.objectID == ObjectID.None)
                {
                    to = from;
                    to.objectData.amount = 0;
                }
                to.objectData.amount += move;
                from.objectData.amount -= move;
                contained[slot] = to;
                contained[i] = from.objectData.amount > 0 ? from : default;
                if (from.objectData.amount <= 0 && hasLocks)
                {
                    // Vanilla clears a slot's lock when it becomes empty (InventoryUtility.UpdateLockedObjects).
                    var locked = EntityManager.GetBuffer<LockedObjectsBuffer>(e);
                    if (i < locked.Length && locked[i].Value) locked[i] = default;
                    contained = EntityManager.GetBuffer<ContainedObjectsBuffer>(e);
                }
            }
        }
    }
}
