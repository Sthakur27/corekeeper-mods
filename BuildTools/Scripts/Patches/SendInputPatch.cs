using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace BuildTools.Patches
{
    /// <summary>
    /// After the game's SendClientInputSystem (managed, client) has written this frame's input, adds
    /// the Build Tools bits (<see cref="InputBits"/>) to the local player's ClientInputData, which NetCode
    /// then sends to the server. ClientInputData is ClientInput reinterpreted as raw uints; buttonSetMask
    /// is the uint at byte 60. That offset is checked once against the typed copy in
    /// ClientInputForLatestFrameCD, and the channel stays off if the layout ever changes.
    /// </summary>
    [HarmonyPatch(typeof(SendClientInputSystem), "OnUpdate")]
    public static class SendInputPatch
    {
        /// <summary>Bits to send this frame (set by BuildToolsMod.Update); 0 = nothing.</summary>
        public static uint Bits;

        private static int _layoutState; // 0 unchecked, 1 ok, -1 mismatch

        [HarmonyPostfix]
        public static void Postfix(SendClientInputSystem __instance)
        {
            if (Bits == 0 || _layoutState < 0) return;
            var main = Manager.main;
            if (main == null || main.player == null) return;
            var em = __instance.EntityManager;
            Entity player = main.player.entity;
            if (player == Entity.Null || !em.Exists(player) || !em.HasComponent<ClientInputData>(player)) return;

            var data = em.GetComponentData<ClientInputData>(player);
            if (_layoutState == 0)
            {
                if (!em.HasComponent<ClientInputForLatestFrameCD>(player)) return;
                var typed = em.GetComponentData<ClientInputForLatestFrameCD>(player).clientInput;
                // Wait for a frame where the camera position is known, so the comparison means something.
                if (typed.cameraPosition.x == 0f && typed.mouseOrJoystickWorldPoint.x == 0f) return;
                bool same = typed.buttonSetMask == data.dataOffset60 &&
                            Unity.Mathematics.math.asuint(typed.cameraPosition.x) == data.dataOffset04 &&
                            Unity.Mathematics.math.asuint(typed.mouseOrJoystickWorldPoint.x) == data.dataOffset40;
                _layoutState = same ? 1 : -1;
                if (_layoutState < 0)
                {
                    Debug.LogError($"[{BuildToolsMod.Name}] Client input layout changed (mask {typed.buttonSetMask} vs {data.dataOffset60}); Build Tools keys disabled.");
                    return;
                }
                Debug.Log($"[{BuildToolsMod.Name}] Input channel ready.");
            }

            data.dataOffset60 = (data.dataOffset60 & InputBits.VanillaMask) | Bits;
            em.SetComponentData(player, data);
        }
    }
}
