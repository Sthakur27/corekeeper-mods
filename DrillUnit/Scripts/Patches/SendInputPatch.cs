using HarmonyLib;
using PlayerEquipment;
using Unity.Entities;
using UnityEngine;

namespace DrillUnit.Patches
{
    /// <summary>
    /// After the game's SendClientInputSystem (managed, client) has written this frame's input, and after
    /// Build Tools' own postfix (Priority.Last), edits the local player's ClientInputData before NetCode
    /// sends it: sets or clears <see cref="InputBits.GroundBit"/> / <see cref="InputBits.DigBit"/>, and while riding a Drill Unit drops the
    /// attack buttons unless a mining tool is held. The server and the client's prediction both act on this
    /// input, so a weapon simply does nothing from the seat. ClientInputData is ClientInput as raw uints;
    /// buttonSetMask is the uint at byte 60, checked once against the typed copy (as in Build Tools).
    /// </summary>
    [HarmonyPatch(typeof(SendClientInputSystem), "OnUpdate")]
    public static class SendInputPatch
    {
        private const uint VanillaMask = (1u << 18) - 1;
        private const uint AttackButtons =
            (uint)(CommandInputButtonStateNames.Interact_HeldDown | CommandInputButtonStateNames.Interact_Pressed |
                   CommandInputButtonStateNames.UseOffHand_HeldDown | CommandInputButtonStateNames.UseOffHand_Pressed);
        private const uint SecondButtons =
            (uint)(CommandInputButtonStateNames.SecondInteract_HeldDown | CommandInputButtonStateNames.SecondInteract_Pressed);

        private static int _layoutState; // 0 unchecked, 1 ok, -1 mismatch

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(SendClientInputSystem __instance)
        {
            if (_layoutState < 0) return;
            var main = Manager.main;
            if (main == null || main.player == null) return;
            var em = __instance.EntityManager;
            Entity player = main.player.entity;
            if (player == Entity.Null || !em.Exists(player) || !em.HasComponent<ClientInputData>(player)) return;
            bool riding = DrillUnitMod.IsRiding(em, player, out _);
            if (!riding && DrillUnitMod.Ground == DrillUnitMod.GroundMode.Off) return;

            var data = em.GetComponentData<ClientInputData>(player);
            if (_layoutState == 0)
            {
                if (!em.HasComponent<ClientInputForLatestFrameCD>(player)) return;
                var typed = em.GetComponentData<ClientInputForLatestFrameCD>(player).clientInput;
                if (typed.cameraPosition.x == 0f && typed.mouseOrJoystickWorldPoint.x == 0f) return;
                // Only the vanilla bits: Build Tools' postfix has already added its own high bits to the raw copy.
                bool same = (typed.buttonSetMask & VanillaMask) == (data.dataOffset60 & VanillaMask) &&
                            Unity.Mathematics.math.asuint(typed.cameraPosition.x) == data.dataOffset04 &&
                            Unity.Mathematics.math.asuint(typed.mouseOrJoystickWorldPoint.x) == data.dataOffset40;
                _layoutState = same ? 1 : -1;
                if (_layoutState < 0)
                {
                    Debug.LogError($"[{DrillUnitMod.Name}] Client input layout changed; Drill Pod keys and weapon lock disabled.");
                    return;
                }
            }

            uint mask = data.dataOffset60;
            if (!riding)
            {
                DrillUnitMod.Ground = DrillUnitMod.GroundMode.Off; // off again once the player gets out
                mask &= ~InputBits.ActionBits;
            }
            else
            {
                // The unit only acts while the use button (left click) is held; read it before the weapon lock below.
                bool held = (mask & (uint)CommandInputButtonStateNames.Interact_HeldDown) != 0;
                mask &= ~InputBits.ActionBits;
                if (held)
                    mask |= DrillUnitMod.Ground == DrillUnitMod.GroundMode.Lay ? InputBits.GroundBit
                        : DrillUnitMod.Ground == DrillUnitMod.GroundMode.Dig ? InputBits.DigBit
                        : InputBits.ActionBits;
                if (!HoldsMiningTool(em, player, out bool eatable))
                {
                    mask &= ~AttackButtons;
                    if (!eatable) mask &= ~SecondButtons;
                }
            }
            if (mask == data.dataOffset60) return;
            data.dataOffset60 = mask;
            em.SetComponentData(player, data);
        }

        /// <summary>Pickaxes, drills, the laser drill and shovels work from the seat; food can still be eaten.</summary>
        private static bool HoldsMiningTool(EntityManager em, Entity player, out bool eatable)
        {
            eatable = em.HasComponent<EquipmentSlotCD>(player)
                      && em.GetComponentData<EquipmentSlotCD>(player).slotType == EquipmentSlotType.EatableSlot;
            if (!em.HasComponent<EquippedObjectCD>(player)) return false;
            var id = em.GetComponentData<EquippedObjectCD>(player).containedObject.objectID;
            if (id == ObjectID.None) return false;
            if (id == ObjectID.LaserDrillTool) return true;
            var type = PugDatabase.GetObjectInfo(id).objectType;
            return type == ObjectType.MiningPick || type == ObjectType.DrillTool || type == ObjectType.Shovel;
        }
    }
}
