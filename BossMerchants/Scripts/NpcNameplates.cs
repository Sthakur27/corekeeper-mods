using HarmonyLib;
using UnityEngine;

namespace BossMerchants
{
    /// <summary>One reusable native-font label per pooled NPC graphic, including vanilla merchants.</summary>
    public sealed class NpcNameplate : MonoBehaviour
    {
        private const float VisibilityRange = 8f;
        private NPC _npc;
        private PugText _text;
        private string _term;
        private bool _needsText;
        private static bool _renderFailed;

        public void Begin(NPC npc)
        {
            _npc = npc;
            var merchant = Catalog.ByMerchantId(npc.objectData.objectID);
            _term = "Items/" + (merchant == null ? npc.objectData.objectID.ToString() : merchant.MerchantName);
            _needsText = true;
            enabled = true;
        }

        public void End()
        {
            if (_text != null) _text.gameObject.SetActive(false);
            _npc = null;
            enabled = false;
        }

        private void LateUpdate()
        {
            if (_renderFailed) { End(); return; }
            try { UpdateLabel(); }
            catch (System.Exception)
            {
                // Fail closed for this session: never retry a broken renderer every frame.
                _renderFailed = true;
                End();
                Debug.LogError("[More NPCs] Name labels disabled after a rendering error.");
            }
        }

        private void UpdateLabel()
        {
            var player = Manager.main == null ? null : Manager.main.player;
            bool show = _npc != null && player != null && !Manager.prefs.hideInGameUI && !Manager.ui.isShowingMap;
            if (show)
            {
                // Ground-plane squared distance: no square root, allocations, queries or networking.
                var offset = transform.position - player.transform.position;
                show = offset.x * offset.x + offset.z * offset.z <= VisibilityRange * VisibilityRange;
            }
            if (!show)
            {
                if (_text != null && _text.gameObject.activeSelf) _text.gameObject.SetActive(false);
                return;
            }
            // Defer text creation/rendering until this graphic is close enough to need a label.
            var template = player.nameText;
            if (template == null) return;
            if (_text == null)
            {
                // Native text depends on serialized font/language lists as well as its style.
                var go = Object.Instantiate(template.gameObject, transform, false);
                go.name = "MoreNPCs_Name";
                go.layer = template.gameObject.layer;
                go.transform.SetParent(transform, false);
                // Outside animated/flipped children: names do not wobble or mirror with the walk cycle.
                go.transform.localPosition = new Vector3(0f, 2.8f, 0f);
                go.transform.rotation = template.transform.rotation;
                _text = go.GetComponent<PugText>();
                _text.Clear(false, true);
                for (int i = go.transform.childCount - 1; i >= 0; i--)
                    Object.Destroy(go.transform.GetChild(i).gameObject);
                _text.formatFields = new string[0];
                if (_text.languagesToForceLatinFont == null)
                    _text.languagesToForceLatinFont = new System.Collections.Generic.List<SystemLanguage>();
                _text.renderOnStart = false;
                _text.checkForProfanity = false;
                _text.maxWidth = 0f;
                _text.style = template.style.GetCopy();
                _text.style.fontFace = TextManager.FontFace.boldSmall;
                _text.style.horizontalAlignment = PugTextStyle.HorizontalAlignment.center;
                _text.style.maskInteraction = SpriteMaskInteraction.None;
                _text.style.outline = PugTextStyle.Outline.top | PugTextStyle.Outline.bottom
                    | PugTextStyle.Outline.left | PugTextStyle.Outline.right;
                _text.style.outlineColor = Color.black;
                _text.SetTempColor(Color.white);
                _text.localize = true;
                _text.freeResourcesOnDisable = false;
                _text.DisableAutoInactivationOnStart();
            }
            if (_needsText)
            {
                _text.Render(_term);
                _needsText = false;
            }
            if (!_text.gameObject.activeSelf) _text.gameObject.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(NPC), "OnSpawn")]
    public static class NpcNameplateSpawnPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NPC __instance)
        {
            var nameplate = __instance.GetComponent<NpcNameplate>();
            if (nameplate == null) nameplate = __instance.gameObject.AddComponent<NpcNameplate>();
            nameplate.Begin(__instance);
        }
    }

    [HarmonyPatch(typeof(NPC), "OnDespawn")]
    public static class NpcNameplateDespawnPatch
    {
        [HarmonyPrefix]
        public static void Prefix(NPC __instance)
        {
            var nameplate = __instance.GetComponent<NpcNameplate>();
            if (nameplate != null) nameplate.End();
        }
    }
}
