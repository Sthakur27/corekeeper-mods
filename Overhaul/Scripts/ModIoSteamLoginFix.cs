using System;
using HarmonyLib;
using ModIOBrowser;
using Steamworks;
using UnityEngine;

namespace SidsOverhaul
{
    /// <summary>
    /// Fixes the in-game mod.io "Sign in with Steam" button hanging on "waiting for response".
    /// Vanilla RadicalMainMenuOption_OpenMods.Awake hands mod.io a ticket delegate that starts a
    /// coroutine on the Mods menu option. Since the mod list menu was added, opening the mod.io browser
    /// pops the main menu, so that object is inactive and StartCoroutine throws; the request is never sent.
    /// We re-register a delegate that needs no MonoBehaviour: mod.io's SubmitSteamAuthenticationRequest
    /// already marshals the callback back to the main thread through its MonoDispatcher.
    /// </summary>
    [HarmonyPatch(typeof(RadicalMainMenuOption_OpenMods), "Awake")]
    public static class ModIoSteamLoginFix
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!(Manager.platform.platformImpl is SteamPlatform)) return;
            // Queued on mod.io's dispatcher after vanilla's own registration, so ours wins.
            Browser.SetupSteamAuthenticationOption(GetTicket);
        }

        private static void GetTicket(Action<string> callback)
        {
            SteamUser.RequestEncryptedAppTicketAsync().ContinueWith(task =>
            {
                if (!task.IsCompletedSuccessfully || task.Result == null)
                {
                    Debug.LogError($"[{SidsOverhaulMod.Name}] failed to get encrypted Steam app ticket: {task.Status}");
                    callback(null);
                    return;
                }
                byte[] ticket = task.Result;
                callback(ModIO.Util.Utility.EncodeEncryptedSteamAppTicket(ticket, (uint)ticket.Length));
            });
        }
    }
}
