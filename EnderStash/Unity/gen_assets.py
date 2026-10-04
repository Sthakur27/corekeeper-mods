"""Writes the Ender Chest source assets (textures, SpriteAsset, TextDataBlock) into the Mod SDK project.
Run by build_bundle.py; EnderChestBuilder (Unity, batch mode) then builds the prefabs and bundles."""
import os, re, struct, shutil, sys, zipfile
SDK = sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\Sid\CoreKeeperModSDK"
MOD = os.path.join(SDK, "Assets", "EnderStash")
ART = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Art")
# Stable guids so rebuilds keep the same addresses (saves never store these, but keep it tidy).
G = {
 "EnderChest.png": "6e1d0a3b9c7f4e21a5b8c3d2e1f00a01",
 "EnderChest_side.png": "6e1d0a3b9c7f4e21a5b8c3d2e1f00a02",
 "EnderChest_up.png": "6e1d0a3b9c7f4e21a5b8c3d2e1f00a03",
 "EnderChest_emissive.png": "6e1d0a3b9c7f4e21a5b8c3d2e1f00a04",
 "EnderChest_side_emissive.png": "6e1d0a3b9c7f4e21a5b8c3d2e1f00a05",
 "EnderChest_up_emissive.png": "6e1d0a3b9c7f4e21a5b8c3d2e1f00a06",
 "EnderChest_icon.png": "6e1d0a3b9c7f4e21a5b8c3d2e1f00a07",  # copy of EnderChest.png, centered pivot
 "SpriteAsset": "6e1d0a3b9c7f4e21a5b8c3d2e1f00b01",
 "Text": "6e1d0a3b9c7f4e21a5b8c3d2e1f00b02",
}
def addr(g):
    w1 = int(g[0:8], 16); w2 = int(g[12:16] + g[8:12], 16)
    lo, hi = struct.unpack('<qq', struct.pack('<II', w1, w2) + bytes.fromhex(g[16:32]))
    return lo, hi
# self-check against the SDK example (guid -> address pairs seen in Workbench data)
assert addr("c9acb5f7e6e71244ab86af757aced4a1") == (1316430874294400503, -6785571713184725333), addr("c9acb5f7e6e71244ab86af757aced4a1")

for d in ["Textures", "Data/SpriteAsset", "Data/TextDataBlock/Items", "Data/EntityAuthoringDataBlock", "Data/GraphicalObjectDataBlock", "Prefabs"]:
    os.makedirs(os.path.join(MOD, d), exist_ok=True)

# Texture import settings (sprite, point filter, pivot 1px above the bottom) are applied by
# EnderChestBuilder through the TextureImporter API; the meta only pins the guid.
for fn, guid in G.items():
    if not fn.endswith(".png"): continue
    src = os.path.join(ART, "EnderChest.png" if fn == "EnderChest_icon.png" else fn); dst = os.path.join(MOD, "Textures", fn)
    shutil.copy2(src, dst)
    if not os.path.exists(dst + ".meta"):
        open(dst + ".meta", "w", encoding="utf-8", newline="\n").write("fileFormatVersion: 2\nguid: %s\n" % guid)

def tref(fn): return "{fileID: 2800000, guid: %s, type: 3}" % G[fn]
lo, hi = addr(G["SpriteAsset"])
sprite_asset = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: -217761678, guid: 292700ef68995bdb2163e35989fc7eb0, type: 3}}
  m_Name: EnderChest
  m_EditorClassIdentifier: 
  m_overload:
    m_address:
      m_low: 0
      m_high: 0
  m_address:
    m_low: {lo}
    m_high: {hi}
  m_dynamicCollections:
    m_list: []
"""
for k in ["m_defaultPrimaryGradientMap", "m_defaultSecondaryGradientMap", "m_defaultTertiaryGradientMap", "m_defaultPrimaryGradientMapRef", "m_defaultSecondaryGradientMapRef", "m_defaultTertiaryGradientMapRef"]:
    sprite_asset += f"  {k}:\n    m_address:\n      m_low: 0\n      m_high: 0\n"
def sdata(fn, em, py, indent):
    p = " " * indent
    return (f"texture: {tref(fn)}\n{p}emissiveTexture: {tref(em)}\n{p}normalTexture: {{fileID: 0}}\n"
            f"{p}pivot: {{x: 0.5, y: {py}}}\n{p}positionalData: []\n{p}inheritPivot: 1\n")
sprite_asset += "  m_editorHideEmissive: 0\n"
sprite_asset += "  m_staticSpriteData:\n    " + sdata("EnderChest.png", "EnderChest_emissive.png", 0.0625, 4)
sprite_asset += "  m_staticVariants:\n"
sprite_asset += "  - " + sdata("EnderChest_side.png", "EnderChest_side_emissive.png", 1 / 19, 4)
sprite_asset += "  - " + sdata("EnderChest_up.png", "EnderChest_up_emissive.png", 0.0625, 4)
sprite_asset += "  m_animations: []\n  m_events: []\n  m_positionalData: []\n  references:\n    version: 2\n    RefIds: []\n"
p = os.path.join(MOD, "Data", "SpriteAsset", "EnderChest.asset")
open(p, "w", encoding="utf-8", newline="\n").write(sprite_asset)
open(p + ".meta", "w", encoding="utf-8", newline="\n").write(f"fileFormatVersion: 2\nguid: {G['SpriteAsset']}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

# Text: copy the example, swap name/address/strings. Term = Items/EnderStash_EnderChest.
TITLE = "Ender Chest"
DESC = "Opens your personal Ender Stash: 40 slots shared by every Ender Chest, in every world. Your stash is never dropped on death."
# The SDK example item text carries the language keys of every game language; reuse it as the template.
with zipfile.ZipFile(os.path.join(SDK, "Assets", "Examples.zip")) as z:
    t = z.read("Examples/ContentCreation/Workbench/Data/TextDataBlock/Items/MyNewWorkbench1.asset").decode("utf-8")
t = t.replace("m_Name: MyNewWorkbench1", "m_Name: EnderStash_EnderChest")
lo, hi = addr(G["Text"])
t = re.sub(r"(  m_address:\n    m_low: )-?\d+(\n    m_high: )-?\d+", lambda m: f"{m.group(1)}{lo}{m.group(2)}{hi}", t, count=1)
t = t.replace("title: MyNewWorkbench1", "title: " + TITLE).replace("description: A workbench crafted from chaos.", "description: " + DESC)
assert "MyNewWorkbench" not in t
p = os.path.join(MOD, "Data", "TextDataBlock", "Items", "EnderStash_EnderChest.asset")
open(p, "w", encoding="utf-8", newline="\n").write(t)
open(p + ".meta", "w", encoding="utf-8", newline="\n").write(f"fileFormatVersion: 2\nguid: {G['Text']}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
print("assets written to", MOD)
