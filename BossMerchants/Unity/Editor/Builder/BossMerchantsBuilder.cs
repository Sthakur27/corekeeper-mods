using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Builds Boss Merchants: per theme one merchant NPC prefab and one idol prefab, each a copy of a vanilla prefab
/// (values from BossMerchantsVanilla.txt, written by gen_assets.py from dump_vanilla.py) with ObjectAuthoring (+
/// InventoryItemAuthoring for the idol) in place of the vanilla EntityMonoBehaviourData.
///
/// Graphics are the vanilla merchant's / idol's (GraphicalObjectDataBlock address copied from the vanilla
/// objectInfo); the mod swaps our sheets in at runtime (Scripts/Visuals.cs). For a SpriteAsset-based base (Void
/// Merchant) a SpriteAssetSkin "bossMerchant_&lt;Key&gt;_skin" targeting the vanilla SpriteAsset is built here.
/// The merchant's MerchantAuthoring list is emptied: the runtime fills it from the mod's catalog.
///
/// Every networked prefab is named BossMerchants_*Entity: NetCode derives the ghost type from the prefab name, so a
/// vanilla name would share the vanilla ghost type (AGENTS.md, Runic Storage lesson).
/// </summary>
public static class BossMerchantsBuilder
{
    const string Root = "Assets/BossMerchants";

    public static void BuildAll()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var vanilla = Vanilla.Load(Path.Combine(Directory.GetCurrentDirectory(), "BossMerchantsVanilla.txt"));
            ConfigureTextures();
            foreach (var t in vanilla.Themes)
            {
                if (t.kind == "spriteAsset") BuildSkin(t, vanilla.SkinSlots.Where(s => s.key == t.key).ToList());
                BuildEntity(vanilla, t.merchantBase, "BossMerchants_" + t.key + "Merchant", "BossMerchants_" + t.key + "MerchantEntity", null, null);
                BuildEntity(vanilla, t.idolBase, "BossMerchants_" + t.key + "Idol", "BossMerchants_" + t.key + "IdolEntity",
                    Sprite("bossMerchant_" + t.key + "Idol_icon"), Sprite("bossMerchant_" + t.key + "Idol_smallIcon"));
            }
            AssetDatabase.SaveAssets();
            BuildMod("BossMerchants", "b055e7c4a1d24f6e8a3c9d1e5f7a2b60");
        }
        catch (Exception e) { Debug.LogError("[BossMerchantsBuilder] " + e); EditorApplication.Exit(1); }
    }

    // ------------------------------------------------------------------ textures + skins

    static void ConfigureTextures()
    {
        foreach (var path in Directory.GetFiles(Root + "/Textures", "*.png"))
        {
            var p = path.Replace('\\', '/');
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            bool icon = p.EndsWith("_icon.png") || p.EndsWith("_smallIcon.png");
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.textureType = icon ? TextureImporterType.Sprite : TextureImporterType.Default;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            if (icon)
            {
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 16;
                var ts = new TextureImporterSettings(); ti.ReadTextureSettings(ts);
                ts.spriteAlignment = (int)SpriteAlignment.Center;
                ts.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(ts);
            }
            ti.SaveAndReimport();
        }
    }

    static Texture2D Tex(string name) => Load<Texture2D>(Root + "/Textures/" + name + ".png");
    static Sprite Sprite(string name)
    {
        var s = AssetDatabase.LoadAllAssetsAtPath(Root + "/Textures/" + name + ".png").OfType<Sprite>().FirstOrDefault();
        if (s == null) throw new Exception("missing sprite " + name);
        return s;
    }

    /// <summary>One replacement per vanilla animation (guid); the idle sheet is also the static replacement.</summary>
    static void BuildSkin(Vanilla.Theme t, List<Vanilla.SkinSlot> slots)
    {
        if (slots.Count == 0) throw new Exception(t.key + ": spriteAsset base without skin slots");
        var target = (slots[0].lo, slots[0].hi);
        var idle = slots.FirstOrDefault(s => s.texture.EndsWith("_idle")) ?? slots[0];
        CreateBlock("SpriteAssetSkin", Root + "/Data/SpriteAssetSkin/bossMerchant_" + t.key + "_skin.asset", so =>
        {
            SetAddress(so.FindProperty("m_targetAssetRef"), target);
            SetReplacement(so.FindProperty("m_staticReplacementData"), "", idle);
            var list = so.FindProperty("m_replacementData");
            list.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++) SetReplacement(list.GetArrayElementAtIndex(i), slots[i].guid, slots[i]);
        });
    }

    static void SetReplacement(SerializedProperty data, string guid, Vanilla.SkinSlot s)
    {
        data.FindPropertyRelative("m_guid").stringValue = guid;
        var d = data.FindPropertyRelative("m_spriteData");
        d.FindPropertyRelative("texture").objectReferenceValue = Tex(s.texture);
        d.FindPropertyRelative("emissiveTexture").objectReferenceValue = null;
        d.FindPropertyRelative("normalTexture").objectReferenceValue = null;
        d.FindPropertyRelative("pivot").vector2Value = new Vector2(s.px, s.py);
        d.FindPropertyRelative("positionalData").arraySize = 0;
        B(d.FindPropertyRelative("inheritPivot"), true);
        data.FindPropertyRelative("m_variants").arraySize = 0;
    }

    // ------------------------------------------------------------------ entity prefabs

    static void BuildEntity(Vanilla vanilla, string vanillaPrefab, string objectName, string prefabName, Sprite icon, Sprite smallIcon)
    {
        if (!vanilla.Prefabs.TryGetValue(vanillaPrefab, out var blocks)) throw new Exception("no vanilla dump for " + vanillaPrefab);
        string path = Root + "/Prefabs/" + prefabName + ".prefab";
        var go = new GameObject(prefabName);
        int missing = 0;
        try
        {
            var refs = new List<(Component comp, string path, string target)>();
            foreach (var block in blocks)
            {
                if (block.cls == "#Transform") continue;
                if (block.cls.StartsWith("#")) throw new Exception($"{vanillaPrefab}: unexpected non-MonoBehaviour {block.cls}");
                if (block.cls == "EntityMonoBehaviourData")
                {
                    AddObjectAuthoring(go, block, objectName, icon, smallIcon);
                    continue;
                }
                var comp = Add(go, block.cls);
                var so = new SerializedObject(comp);
                foreach (var line in block.lines)
                {
                    if (line.kind == 'R') { refs.Add((comp, line.path, line.value)); continue; }
                    var p = so.FindProperty(line.path);
                    if (p == null) { missing++; Debug.LogWarning($"[BossMerchantsBuilder] {prefabName}.{block.cls}: no property {line.path}"); continue; }
                    if (line.kind == 'A') p.arraySize = int.Parse(line.value);
                    else if (line.kind == 'S') p.stringValue = line.value;
                    else if (long.TryParse(line.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long whole)
                             && p.propertyType == SerializedPropertyType.Integer) p.longValue = whole;
                    else SetNum(p, ParseNum(line.value));
                }
                // The catalog fills the list at runtime (Scripts/MerchantStock.cs).
                if (block.cls == "MerchantAuthoring") so.FindProperty("items").arraySize = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var (comp, propPath, target) in refs)
            {
                if (string.IsNullOrEmpty(target) || target == "EntityMonoBehaviourData") continue;
                Fill(comp, so => so.FindProperty(propPath).objectReferenceValue =
                    target == "#GameObject" ? (Object)go : go.GetComponent(FindType(target, typeof(Component))));
            }
            Fill(Add(go, "LocalizationAuthoring"), so => so.FindProperty("termKey").stringValue = objectName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally { Object.DestroyImmediate(go); }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var authoringBlock = CreateBlock("EntityAuthoringDataBlock", Root + "/Data/EntityAuthoringDataBlock/" + prefabName + ".asset",
            so => so.FindProperty("prefab").objectReferenceValue = prefab);
        EditPrefab(path, p =>
        {
            Fill(Comp(p, "ObjectAuthoring"), so => SetAddress(so.FindProperty("authoringRef"), ReadAddress(authoringBlock)));
            // NetCode ghost id = the prefab's own guid (what the Ghost inspector would write).
            Fill(Comp(p, "GhostAuthoringComponent"), so => so.FindProperty("prefabId").stringValue = AssetDatabase.AssetPathToGUID(path));
        });
        if (missing > 0) throw new Exception($"{prefabName}: {missing} vanilla properties not found (see warnings)");
        Debug.Log($"[BossMerchantsBuilder] {prefabName} from {vanillaPrefab}");
    }

    static void AddObjectAuthoring(GameObject go, Vanilla.Block block, string objectName, Sprite icon, Sprite smallIcon)
    {
        string Raw(string p) => block.lines.First(l => l.path == "objectInfo." + p).value;
        double V(string p) => double.Parse(Raw(p), CultureInfo.InvariantCulture);
        long L(string p) => long.Parse(Raw("prefabInfo.graphicalRef.m_address." + p), CultureInfo.InvariantCulture);
        int tagCount = int.Parse(Raw("tags"), CultureInfo.InvariantCulture);
        Fill(Add(go, "ObjectAuthoring"), so =>
        {
            so.FindProperty("objectName").stringValue = objectName;
            N(so, "initialAmount", V("initialAmount"));
            N(so, "variation", V("variation"));
            N(so, "variationIsDynamic", V("variationIsDynamic"));
            N(so, "variationToToggleTo", V("variationToToggleTo"));
            N(so, "objectType", V("objectType"));
            var tags = so.FindProperty("tags"); tags.arraySize = tagCount;
            for (int i = 0; i < tagCount; i++) tags.GetArrayElementAtIndex(i).intValue = (int)V($"tags.Array.data[{i}]");
            N(so, "rarity", V("rarity"));
            N(so, "salvageMultiplier", V("salvageMultiplier"));
            so.FindProperty("graphicalPrefab").objectReferenceValue = null;
            SetAddress(so.FindProperty("graphicalRef"), (L("m_low"), L("m_high")));
        });
        if (icon == null) return; // merchants are not items
        Fill(Add(go, "InventoryItemAuthoring"), so =>
        {
            N(so, "sellValue", 100);
            N(so, "buyValueMultiplier", 1);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("smallIcon").objectReferenceValue = smallIcon;
            B(so, "isStackable", V("isStackable") != 0);
            N(so, "craftingTime", 0);
            so.FindProperty("requiredObjectsToCraft").arraySize = 0;
        });
    }

    /// <summary>Flattened vanilla prefab values plus the theme table from gen_assets.py.</summary>
    sealed class Vanilla
    {
        public struct Line { public char kind; public string path, value; }
        public sealed class Block { public string cls; public List<Line> lines = new List<Line>(); }
        public sealed class Theme { public string key, merchantBase, idolBase, kind; }
        public sealed class SkinSlot { public string key, texture, guid; public float px, py; public long lo, hi; }
        public readonly Dictionary<string, List<Block>> Prefabs = new Dictionary<string, List<Block>>();
        public readonly List<Theme> Themes = new List<Theme>();
        public readonly List<SkinSlot> SkinSlots = new List<SkinSlot>();

        public static Vanilla Load(string file)
        {
            if (!File.Exists(file)) throw new Exception("missing " + file + " (run gen_assets.py)");
            var v = new Vanilla();
            Block cur = null;
            float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
            foreach (var raw in File.ReadAllLines(file))
            {
                if (raw.Length == 0) continue;
                var f = raw.Split('\t');
                switch (f[0])
                {
                    case "T": v.Themes.Add(new Theme { key = f[1], merchantBase = f[2], idolBase = f[3], kind = f[4] }); break;
                    case "K":
                        v.SkinSlots.Add(new SkinSlot { key = f[1], texture = f[2], guid = f[3], px = F(f[4]), py = F(f[5]),
                            lo = long.Parse(f[6], CultureInfo.InvariantCulture), hi = long.Parse(f[7], CultureInfo.InvariantCulture) });
                        break;
                    case "C":
                        if (!v.Prefabs.TryGetValue(f[1], out var list)) v.Prefabs[f[1]] = list = new List<Block>();
                        list.Add(cur = new Block { cls = f[2] });
                        break;
                    default:
                        cur.lines.Add(new Line { kind = f[0][0], path = f[1], value = f.Length > 2 ? f[2] : "" });
                        break;
                }
            }
            return v;
        }
    }

    // ------------------------------------------------------------------ build + helpers (as SolariteDrillBuilder)

    static ScriptableObject CreateBlock(string typeName, string path, Action<SerializedObject> fill)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var block = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        if (block == null) { block = ScriptableObject.CreateInstance(FindType(typeName, typeof(ScriptableObject))); AssetDatabase.CreateAsset(block, path); }
        Fill(block, so => { fill(so); SetAddress(so.FindProperty("m_address"), GuidAddress(AssetDatabase.AssetPathToGUID(path))); });
        EditorUtility.SetDirty(block); AssetDatabase.SaveAssets(); return block;
    }

    static void BuildMod(string name, string guid)
    {
        string path = Root + "/" + name + ".asset";
        var settings = AssetDatabase.LoadAssetAtPath<ModBuilderSettings>(path);
        if (settings == null) { settings = ScriptableObject.CreateInstance<ModBuilderSettings>(); AssetDatabase.CreateAsset(settings, path); }
        var meta = settings.metadata;
        meta.guid = guid; meta.name = name; meta.displayName = name;
        meta.accessesExtraAssemblies = true; meta.requiredOn = PugMod.ModMetadata.ModExistsOn.ClientAndServer;
        meta.files = new List<PugMod.ModFile>(); meta.dependencies = new List<PugMod.ModMetadata.Dependency>();
        settings.metadata = meta; settings.modPath = Root; settings.assets = new List<ModBuilderSettings.ModAsset>();
        settings.buildBundles = true; settings.buildLinux = true; settings.cacheBundles = false; settings.forceReimport = true;
        EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
        var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-merchantsOut");
        string output = i >= 0 ? args[i + 1] : Path.GetFullPath("Library/BossMerchantsBuild");
        bool ok = false; PugMod.ModBuilder.BuildMod(settings, output, r => ok = r);
        if (!ok) throw new Exception("Build failed: " + name);
        Debug.Log("[BossMerchantsBuilder] Built " + name + " into " + output);
    }

    static SerializedProperty Prop(SerializedObject so, string path)
    {
        var p = so.FindProperty(path);
        if (p == null) throw new Exception($"{so.targetObject.GetType().Name} has no property {path}");
        return p;
    }

    static void B(SerializedObject so, string path, bool v) => B(Prop(so, path), v);

    static void B(SerializedProperty p, bool v)
    {
        if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v; else p.intValue = v ? 1 : 0;
    }

    static void N(SerializedObject so, string path, double v) => SetNum(Prop(so, path), v);

    /// <summary>Python float repr: also inf / -inf / nan (e.g. Rigidbody angular damping, inertia tensor).</summary>
    static double ParseNum(string s)
    {
        switch (s)
        {
            case "inf": return double.PositiveInfinity;
            case "-inf": return double.NegativeInfinity;
            case "nan": return double.NaN;
            default: return double.Parse(s, CultureInfo.InvariantCulture);
        }
    }

    static void SetNum(SerializedProperty p, double v)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.Float: p.doubleValue = v; break;
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.Integer: p.longValue = (long)v; break;
            case SerializedPropertyType.Boolean: p.boolValue = v != 0; break;
            default: throw new Exception($"{p.propertyPath}: unexpected {p.propertyType}");
        }
    }

    static Type FindType(string name, Type baseType)
    {
        var matches = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); } })
            .Where(t => t.Name == name && baseType.IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
        if (matches.Count == 0) throw new Exception("type not found: " + name);
        if (matches.Count > 1) Debug.Log($"[BossMerchantsBuilder] {name}: {string.Join(", ", matches.Select(t => t.AssemblyQualifiedName))}; using first");
        return matches[0];
    }

    static Component Add(GameObject go, string typeName) => go.AddComponent(FindType(typeName, typeof(Component)));
    static Component Comp(GameObject go, string typeName) => go.GetComponent(FindType(typeName, typeof(Component)));

    static void Fill(Object o, Action<SerializedObject> fill)
    {
        var so = new SerializedObject(o);
        fill(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EditPrefab(string path, Action<GameObject> edit)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static T Load<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) throw new Exception("missing asset " + path);
        return a;
    }

    static (long lo, long hi) ReadAddress(Object block)
    {
        var a = new SerializedObject(block).FindProperty("m_address");
        return (a.FindPropertyRelative("m_low").longValue, a.FindPropertyRelative("m_high").longValue);
    }

    static void SetAddress(SerializedProperty p, (long lo, long hi) a)
    {
        var addr = p.FindPropertyRelative("m_low") != null ? p : p.FindPropertyRelative("m_address");
        addr.FindPropertyRelative("m_low").longValue = a.lo;
        addr.FindPropertyRelative("m_high").longValue = a.hi;
    }

    static (long lo, long hi) GuidAddress(string g)
    {
        uint w1 = Convert.ToUInt32(g.Substring(0, 8), 16);
        uint w2 = Convert.ToUInt32(g.Substring(12, 4) + g.Substring(8, 4), 16);
        var bytes = new byte[16];
        BitConverter.GetBytes(w1).CopyTo(bytes, 0);
        BitConverter.GetBytes(w2).CopyTo(bytes, 4);
        for (int i = 0; i < 8; i++) bytes[8 + i] = Convert.ToByte(g.Substring(16 + 2 * i, 2), 16);
        return (BitConverter.ToInt64(bytes, 0), BitConverter.ToInt64(bytes, 8));
    }
}
