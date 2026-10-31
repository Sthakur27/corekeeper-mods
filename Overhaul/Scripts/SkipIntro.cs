using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace SidsOverhaul
{
    /// <summary>
    /// Skips the studio logos at game start.
    /// LoadingScene.ShowLogo is the coroutine that fades each logo in and out; we hide the logo and hand back
    /// an empty coroutine so loading carries straight on. Always on.
    /// </summary>
    [HarmonyPatch(typeof(LoadingScene), "ShowLogo", new[] { typeof(SpriteRenderer) })]
    public static class SkipIntro
    {
        [HarmonyPrefix]
        public static bool Prefix(SpriteRenderer logo, ref IEnumerator __result)
        {
            if (logo != null) logo.gameObject.SetActive(false);
            __result = Empty();
            return false;
        }

        private static IEnumerator Empty()
        {
            yield break;
        }
    }
}
