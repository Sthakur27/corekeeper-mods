using HarmonyLib;

namespace AutoLoadouts
{
    /// <summary>
    /// Short text above the player's head, in the game's own emote bubble (the one that says
    /// "Waypoint activated!"): same font, bounce and fade, local to this client.
    /// We spawn a vanilla WaypointActivated emote; while <see cref="Pending"/> is set, the emote's render
    /// call draws our text (unlocalized) instead of that emote's localization term. The emote's
    /// localize flag is restored right after, so the pooled emote stays correct for vanilla use.
    /// </summary>
    public static class EmoteNote
    {
        internal const string HostTerm = "Emotes/WaypointActivated";
        internal static string Pending;

        public static void Show(string text)
        {
            var player = Manager.main != null ? Manager.main.player : null;
            if (player == null) return;
            Pending = text;
            try
            {
                Emote.SpawnEmoteText(player.center, Emote.EmoteType.WaypointActivated,
                    randomizePosition: false, shortBounce: false, dontAllowSpam: false);
            }
            finally
            {
                Pending = null;
            }
        }
    }

    [HarmonyPatch(typeof(PugText), nameof(PugText.Render), new[] { typeof(string), typeof(bool), typeof(bool), typeof(bool) })]
    public static class EmoteNoteRenderPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PugText __instance, string text, bool rewindEffectAnims, bool force, bool activate)
        {
            if (EmoteNote.Pending == null || text != EmoteNote.HostTerm) return true;
            string ours = EmoteNote.Pending;
            EmoteNote.Pending = null;
            bool localize = __instance.localize;
            __instance.localize = false;
            try
            {
                __instance.Render(ours, rewindEffectAnims, force, activate);
            }
            finally
            {
                __instance.localize = localize;
            }
            return false;
        }
    }
}
