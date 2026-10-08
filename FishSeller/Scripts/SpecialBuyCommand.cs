using System.Globalization;
using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using CoreLib.Submodule.Command.Util;
using Inventory;
using PugMod;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace FishSeller
{
    /// <summary>
    /// Server side of buying a Legendary item (see <see cref="Patches.SpecialBuy"/>):
    /// "/fishsellerbuy &lt;objectID&gt; &lt;slot&gt;". Finds a merchant within reach of the player whose slot
    /// holds that item, checks the player has the coins in the main inventory, then queues the same kind
    /// of inventory changes vanilla code uses: consume the coins, move one item to the player (dropped at
    /// their feet if the inventory is full).
    /// </summary>
    public sealed class SpecialBuyCommand : IServerCommandHandler
    {
        private const float MaxDistance = 12f;

        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            var world = API.Server.World;
            if (world == null || !world.IsCreated) return new CommandOutput("Fish Seller: server world not available.", CommandStatus.Error);
            if (parameters.Length < 2
                || !int.TryParse(parameters[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawId)
                || !int.TryParse(parameters[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int slot))
                return new CommandOutput("Fish Seller: usage /fishsellerbuy <objectID> <slot> (sent by the shop window).", CommandStatus.Error);

            var id = (ObjectID)rawId;
            int price = ShopItems.SpecialPrice(id);
            if (price <= 0) return new CommandOutput("Fish Seller: that item is bought the normal way.", CommandStatus.Warning);

            Entity player = sender.GetPlayerEntity();
            var em = world.EntityManager;
            if (player == Entity.Null || !em.Exists(player) || !em.HasComponent<LocalTransform>(player)) return new CommandOutput("Fish Seller: no player entity.", CommandStatus.Error);
            float3 pos = em.GetComponentData<LocalTransform>(player).Position;

            Entity merchant = FindMerchant(em, pos, id, slot);
            if (merchant == Entity.Null) return new CommandOutput("Fish Seller: sold out.", CommandStatus.Hint);

            var inventories = em.GetBuffer<InventoryBuffer>(player);
            if (inventories.Length == 0) return new CommandOutput("Fish Seller: no inventory.", CommandStatus.Error);
            var main = inventories[0];
            var contained = em.GetBuffer<ContainedObjectsBuffer>(player);
            int coins = 0;
            for (int i = main.startIndex; i < main.startIndex + main.size && i < contained.Length; i++)
                if (contained[i].objectID == ObjectID.AncientCoin) coins += contained[i].amount;
            if (coins < price) return new CommandOutput($"Fish Seller: {id} costs {price} coins, you have {coins}.", CommandStatus.Hint);

            var query = em.CreateEntityQuery(ComponentType.ReadWrite<InventoryChangeBuffer>());
            if (query.IsEmptyIgnoreFilter) return new CommandOutput("Fish Seller: inventory system not ready.", CommandStatus.Error);
            var changes = em.GetBuffer<InventoryChangeBuffer>(query.GetSingletonEntity());
            changes.Add(new InventoryChangeBuffer { playerEntity = player, inventoryChangeData = Create.ConsumeObjectType(player, ObjectID.AncientCoin, price) });
            changes.Add(new InventoryChangeBuffer
            {
                playerEntity = player,
                inventoryChangeData = Create.MoveOrDropAmount(merchant, slot, player, -1, main.startIndex + main.size, 1, pos)
            });
            UnityEngine.Debug.Log($"[{FishSellerMod.Name}] {player} bought {id} for {price} coins (merchant slot {slot}).");
            return new CommandOutput($"Bought {id} for {price} coins.", CommandStatus.Info);
        }

        private static Entity FindMerchant(EntityManager em, float3 pos, ObjectID id, int slot)
        {
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<MerchantCD>(), ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<ContainedObjectsBuffer>());
            var merchants = query.ToEntityArray(Allocator.Temp);
            Entity found = Entity.Null;
            float best = MaxDistance * MaxDistance;
            foreach (var m in merchants)
            {
                var buffer = em.GetBuffer<ContainedObjectsBuffer>(m);
                if (slot < 0 || slot >= buffer.Length || buffer[slot].objectID != id || buffer[slot].amount < 1) continue;
                float d = math.distancesq(em.GetComponentData<LocalTransform>(m).Position, pos);
                if (d <= best) { best = d; found = m; }
            }
            merchants.Dispose();
            return found;
        }

        public string GetDescription() => "Used by the merchant window to buy Legendary items (Fish Seller).";

        public string[] GetTriggerNames() => new[] { Patches.SpecialBuy.Trigger };
    }
}
