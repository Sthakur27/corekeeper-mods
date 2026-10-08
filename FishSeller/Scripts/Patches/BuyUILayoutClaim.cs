using System.Linq;
using PugMod;
using UnityEngine;

namespace FishSeller.Patches
{
    /// <summary>
    /// Fish Seller and Potion Seller both widen the merchant buy window, with the same layout code. Two
    /// copies running would each shift the window and stack the offsets, so exactly one owns it: the first
    /// one to ask leaves a marker child on the BuyUI object ("SidsBuyUILayout:&lt;owner&gt;") and every
    /// copy defers to whatever name the marker carries. Works across separate mods and inside the overhaul
    /// (one assembly, two namespaces) without referencing each other. Copies of Potion Seller older than
    /// 1.4.0 do not know the marker, so a standalone Potion Seller always wins (<c>otherModOwns</c>).
    /// </summary>
    public static class BuyUILayoutClaim
    {
        private const string Prefix = "SidsBuyUILayout:";

        private static BuyUI _ui;
        private static string _owner;

        public static bool Owns(BuyUI ui, string me, bool otherModOwns)
        {
            if (otherModOwns || ui == null) return false;
            if (_ui != ui || _owner == null)
            {
                _ui = ui;
                _owner = null;
                foreach (Transform child in ui.transform)
                {
                    if (child.name.StartsWith(Prefix)) { _owner = child.name.Substring(Prefix.Length); break; }
                }
                if (_owner == null)
                {
                    var marker = new GameObject(Prefix + me);
                    marker.transform.SetParent(ui.transform, false);
                    _owner = me;
                    Debug.Log($"[{me}] owns the buy window layout.");
                }
            }
            return _owner == me;
        }

        private static int _standaloneChecked;
        private static bool _standalone;

        /// <summary>True if a separately installed mod with this manifest name is loaded (not the overhaul).</summary>
        public static bool StandaloneModLoaded(string manifestName)
        {
            if (_standaloneChecked == 0)
            {
                _standaloneChecked = 1;
                _standalone = API.ModLoader.LoadedMods.Any(m => m.Metadata.name == manifestName);
            }
            return _standalone;
        }
    }
}
