using PugMod;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WeaponTweaks
{
    /// <summary>
    /// Small weapon behaviour fixes, applied to the projectile prefabs as they are converted (both worlds, so client
    /// prediction and the server agree).
    ///
    /// Burnzooka: vanilla's rocket (BurnzookaProjectile, ProjectileAuthoring.piercesEnemies = true) passes through every
    /// enemy, chipping each with the direct hit, and only explodes (ExplosiveAuthoring, the big damage + napalm) when it
    /// meets an unbreakable wall or its 1.1 s lifetime ends. So the explosion only lands on an enemy standing against a
    /// wall. With piercesEnemiesAmount = 0, ProjectileSystem stops the rocket at the first enemy it hits (moves it to the
    /// hit position and zeroes its health), and the death explosion goes off there.
    /// </summary>
    public sealed class WeaponTweaksMod : IMod
    {
        public const string Name = "WeaponTweaks";
        public const string Version = "1.0.0";

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
        }

        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            if (!em.HasComponent<ObjectDataCD>(entity) || !em.HasComponent<PiercingProjectileCD>(entity)) return;
            if (em.GetComponentData<ObjectDataCD>(entity).objectID != ObjectID.BurnzookaProjectile) return;
            var piercing = em.GetComponentData<PiercingProjectileCD>(entity);
            piercing.piercesEnemiesAmount = 0;
            em.SetComponentData(entity, piercing);
            Debug.Log($"[{Name}] Burnzooka rocket now explodes on the first enemy it hits");
        }

        public void Init() { }
        public void Shutdown() { API.Authoring.OnObjectTypeAdded -= OnObjectTypeAdded; }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
