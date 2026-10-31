using PlayerEquipment;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace BuildTools.Systems
{
    /// <summary>
    /// Applies the tool size the player wants (the "size" field of <see cref="InputBits"/>) to the held
    /// tool's slot in the game's PlacementSizeByEquipmentTypeBuffer. Runs in both worlds right after the
    /// equipment update, so the client's prediction shows the new size the same frame and the server
    /// agrees one round trip later. The client decides the value (remembered size, size keys, and R,
    /// which it mirrors), so vanilla's own R cycling simply lands on the same number.
    /// </summary>
    [UpdateInGroup(typeof(EquipmentUpdateSystemGroup))]
    [UpdateAfter(typeof(EquipmentUpdateSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class ToolSizeSystem : PugSimulationSystemBase
    {
        private EntityQuery _players;

        protected override void OnCreate()
        {
            base.OnCreate();
            _players = GetEntityQuery(ComponentType.ReadOnly<EquipmentSlotCD>(), ComponentType.ReadOnly<ClientInput>(),
                ComponentType.ReadWrite<PlacementSizeByEquipmentTypeBuffer>());
        }

        protected override void OnUpdate()
        {
            bool server = World.IsServer();
            Entity localPlayer = Entity.Null;
            if (!server)
            {
                var main = Manager.main;
                if (main == null || main.player == null) return;
                localPlayer = main.player.entity;
            }

            var players = _players.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (!server && player != localPlayer) continue;
                var slot = EntityManager.GetComponentData<EquipmentSlotCD>(player).slotType;
                if (!ToolSizes.HasSize(slot)) continue;
                var bits = InputBits.Decode(EntityManager.GetComponentData<ClientInput>(player).buttonSetMask);
                if (!bits.Present || bits.Size == InputBits.NoSize) continue;

                var buffer = EntityManager.GetBuffer<PlacementSizeByEquipmentTypeBuffer>(player);
                if (buffer.Length < PlacementSizeByEquipmentTypeBuffer.EquipmentWithPlacementSize) continue;
                ref var element = ref buffer.GetElementForEquipment(slot);
                if (element.sizeVariationToPlace != bits.Size) element.sizeVariationToPlace = (byte)bits.Size;
            }
            players.Dispose();
        }
    }
}
