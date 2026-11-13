using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StatsHud
{
    /// <summary>
    /// Shows or hides the buff/debuff icon grid under the health bar (ConditionsContainerUI) and the minion counter
    /// under it (MinionCountUI). Only their visibility changes: the buffs keep working and come back as they were.
    ///
    /// Buff grid: vanilla ConditionsContainerUI.LateUpdate calls container.SetActive(true) every frame, so switching it
    /// off once does nothing (2026-11-13). <see cref="BuffGridLateUpdatePatch"/> skips that LateUpdate while hidden.
    /// Minion counter: its own Update re-activates its container every frame, so the component is disabled too.
    /// Applied only when the wanted state differs from the current one (one bool compare per frame otherwise).
    /// </summary>
    internal static class BuffIcons
    {
        /// <summary>Wanted visibility of the buff grid (read by the LateUpdate patch every frame).</summary>
        internal static bool Hidden => StatsHudMod.ShowBuffIcons != null && !StatsHudMod.ShowBuffIcons.Value;

        private const float FindInterval = 2f;

        private static ConditionsContainerUI[] _buffs;
        private static MinionCountUI _minions;
        private static float _nextFind;
        private static bool _applied = true; // what the UI currently shows

        /// <summary>Hides the grid's icon container; also the grid object itself when the container is not set.</summary>
        private static bool AnyDestroyed()
        {
            if (_buffs == null) return false;
            foreach (var grid in _buffs) if (grid == null) { _buffs = null; return true; }
            return false;
        }

        private static void SetGrid(ConditionsContainerUI grid, bool show)
        {
            if (grid == null) return;
            if (grid.container != null && grid.container != grid.gameObject) grid.container.SetActive(show);
            else grid.gameObject.SetActive(show);
        }

        public static void Update()
        {
            bool want = StatsHudMod.ShowBuffIcons == null || StatsHudMod.ShowBuffIcons.Value;
            if (_buffs == null || _minions == null || AnyDestroyed())
            {
                // Not found yet (title screen) or rebuilt after a scene change: look again, but not every frame.
                if (Time.unscaledTime < _nextFind) return;
                _nextFind = Time.unscaledTime + FindInterval;
                if (_minions == null) _minions = Object.FindFirstObjectByType<MinionCountUI>(FindObjectsInactive.Include);
                if (_buffs == null)
                {
                    // Every buff grid in the scene: the HUD one is the one the minion counter points at, but the
                    // first one found by type was not it (Sid 2026-11-13: the minion counter hid, the buffs stayed).
                    var all = Object.FindObjectsByType<ConditionsContainerUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (all.Length > 0) _buffs = all;
                }
                if (_buffs == null && _minions == null) return;
                var hud = _minions != null ? _minions.conditionsContainerUI : null;
                Debug.Log($"[{StatsHudMod.Name}] buff icons found: grids={(_buffs != null ? _buffs.Length : 0)} "
                    + $"minionCounter={(_minions != null)} hudGrid={(hud != null ? hud.name : "none")}");
                _applied = !want; // force the first apply on the new objects
            }
            if (want == _applied) return;
            _applied = want;
            Debug.Log($"[{StatsHudMod.Name}] buff icons {(want ? "shown" : "hidden")}");
            if (_buffs != null)
                foreach (var grid in _buffs) SetGrid(grid, want); // immediate; the patch keeps it that way
            if (_minions != null)
            {
                _minions.enabled = want; // its Update turns the counter back on when there are minions
                if (!want)
                {
                    if (_minions.container != null) _minions.container.SetActive(false);
                    if (_minions.boxColl != null) _minions.boxColl.enabled = false;
                }
            }
        }
    }

    /// <summary>
    /// While the buff icons are hidden, vanilla's LateUpdate (which re-activates the container every frame) is skipped
    /// and the container stays off; nothing flips on and off each frame. Shown: vanilla runs unchanged.
    /// </summary>
    [HarmonyPatch(typeof(ConditionsContainerUI), "LateUpdate")]
    public static class BuffGridLateUpdatePatch
    {
        public static bool Prefix(ConditionsContainerUI __instance)
        {
            if (!BuffIcons.Hidden) return true;
            if (__instance.container != null && __instance.container.activeSelf) __instance.container.SetActive(false);
            return false;
        }
    }
}
