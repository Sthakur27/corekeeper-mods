using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace BossMerchants
{
    /// <summary>
    /// Client-only presentation metadata. Native MapUI owns pooling, position refresh, visibility and removal.
    /// No ghost-prefab changes, server writes, extra network messages, or independent position tracker.
    /// </summary>
    public static class MerchantMapMarkers
    {
        private static MapUI _registeredMap;
        private static readonly ObjectID[] VanillaMerchants =
        {
            ObjectID.CavelingMerchant, ObjectID.SlimeMerchant, ObjectID.FishingMerchant,
            ObjectID.SeasonalMerchant, ObjectID.CrystalMerchant, ObjectID.VoidMerchant
        };

        // Same associations as the game's SpawnMerchantSystem.GetRequiredObjectForMerchant.
        private static ObjectID VanillaIdol(ObjectID merchant)
        {
            switch (merchant)
            {
                case ObjectID.CavelingMerchant: return ObjectID.MysteriousIdol;
                case ObjectID.SlimeMerchant: return ObjectID.SlimeOil;
                case ObjectID.FishingMerchant: return ObjectID.PileOfChum;
                case ObjectID.SeasonalMerchant: return ObjectID.Calendar;
                case ObjectID.CrystalMerchant: return ObjectID.CrystalMerchantSpawnItem;
                case ObjectID.VoidMerchant: return ObjectID.VoidMerchantIdol;
                default: return ObjectID.None;
            }
        }

        internal static bool Supports(ObjectID id) => VanillaIdol(id) != ObjectID.None || Catalog.ByMerchantId(id) != null;

        internal static bool IsReady(MapUI map) => map != null && _registeredMap == map;

        internal static bool Register(MapUI map)
        {
            if (map == null) return false;
            Catalog.Resolve();
            bool complete = true;
            foreach (var id in VanillaMerchants)
            {
                // Preserve any marker metadata already supplied by the game.
                if (map.uniqueMarkerInfo.Exists(entry => entry.id == id)) continue;
                if (!PugDatabase.TryGetObjectInfo(VanillaIdol(id), out var item) || item.icon == null)
                {
                    complete = false;
                    continue;
                }
                map.uniqueMarkerInfo.Add(new MapUI.UniqueMarkerInfo
                {
                    id = id,
                    hoverString = "Items/" + id,
                    largeMapIcon = item.icon,
                    miniMapIcon = item.smallIcon != null ? item.smallIcon : item.icon
                });
            }
            foreach (var merchant in Catalog.Merchants)
            {
                if (merchant.MerchantId == ObjectID.None || merchant.IdolId == ObjectID.None
                    || !PugDatabase.TryGetObjectInfo(merchant.IdolId, out var idol) || idol.icon == null)
                {
                    complete = false;
                    continue;
                }

                var info = new MapUI.UniqueMarkerInfo
                {
                    id = merchant.MerchantId,
                    hoverString = "Items/" + merchant.MerchantName,
                    largeMapIcon = idol.icon,
                    miniMapIcon = idol.smallIcon != null ? idol.smallIcon : idol.icon
                };
                int index = map.uniqueMarkerInfo.FindIndex(entry => entry.id == merchant.MerchantId);
                if (index < 0) map.uniqueMarkerInfo.Add(info);
                else map.uniqueMarkerInfo[index] = info;
            }
            if (complete) _registeredMap = map;
            return complete;
        }
    }

    [HarmonyPatch(typeof(MapUI), nameof(MapUI.Init))]
    public static class MerchantMapInitPatch
    {
        [HarmonyPostfix]
        public static void Postfix(MapUI __instance) => MerchantMapMarkers.Register(__instance);
    }

    /// <summary>
    /// Attach MapMarkerCD to newly received merchants, including old saves and ghosts re-entering sync range.
    /// A MerchantCD order-version gate skips entity snapshots in steady state. Queries are read-only;
    /// the only structural write is once per received merchant. Disabled/destroyed NPCs are left alone.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MerchantMapMarkerSystem : SystemBase
    {
        private EntityQuery _unmarked;
        private int _lastOrder;
        private bool _hasOrder;
        private MapUI _map;
        private double _nextRegistrationAttempt;
        private int _registrationAttempts;

        protected override void OnCreate()
        {
            _unmarked = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<MerchantCD>(), ComponentType.ReadOnly<ObjectDataCD>() },
                None = new[] { ComponentType.ReadOnly<MapMarkerCD>(), ComponentType.ReadOnly<EntityDestroyedCD>() }
            });
        }

        protected override void OnUpdate()
        {
            var map = Manager.ui == null ? null : Manager.ui.mapUI;
            if (map == null) return;
            if (_map != map)
            {
                _map = map;
                _registrationAttempts = 0;
                _nextRegistrationAttempt = 0;
                _hasOrder = false;
            }
            if (!MerchantMapMarkers.IsReady(map))
            {
                // Init normally registers everything. Bound retries for unusual asset-loading order.
                if (_registrationAttempts >= 30 || World.Time.ElapsedTime < _nextRegistrationAttempt) return;
                _registrationAttempts++;
                _nextRegistrationAttempt = World.Time.ElapsedTime + 1;
                if (!MerchantMapMarkers.Register(map)) return;
            }

            int order = EntityManager.GetComponentOrderVersion<MerchantCD>();
            if (_hasOrder && order == _lastOrder) return;
            _hasOrder = true;
            if (!_unmarked.IsEmpty)
            {
                using (var entities = _unmarked.ToEntityArray(Allocator.Temp))
                {
                    foreach (var entity in entities)
                    {
                        var id = EntityManager.GetComponentData<ObjectDataCD>(entity).objectID;
                        if (!MerchantMapMarkers.Supports(id)) continue;
                        EntityManager.AddComponentData(entity, new MapMarkerCD
                        {
                            mapMarkerType = MapMarkerType.UniqueScene,
                            uniqueMarkerId = id
                        });
                    }
                }
            }
            // Our additions also change the order version; don't rescan because of our own writes.
            _lastOrder = EntityManager.GetComponentOrderVersion<MerchantCD>();
        }
    }
}
