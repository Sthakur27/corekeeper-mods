using PugMod;
using UnityEngine;

namespace FuelPower
{
    // Observation-only until the craft transaction and persistence hooks have been verified in game.
    public sealed class FuelPowerMod : IMod
    {
        public const string Name = "FuelPowerPrototype";
        public void EarlyInit()
        {
            Debug.Log("[" + Name + "] Observation-only prototype. No fuel is consumed; vanilla crafting is unchanged.");
        }
        public void Init() { }
        public void Update() { }
        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
    }
}
