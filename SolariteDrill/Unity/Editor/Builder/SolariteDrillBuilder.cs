using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Builds the Solarite Drill: four entity prefabs (one per direction, like the vanilla drills), each a copy of
/// the vanilla Scarlet Drill prefab for that direction (values from SolariteDrillVanilla.txt, written by
/// gen_assets.py from dump_vanilla.py) with ObjectAuthoring/InventoryItemAuthoring in place of the vanilla
/// EntityMonoBehaviourData, 3x mining damage and the Solarite recipe. Graphics are the vanilla drill's
/// (GraphicalObjectDataBlock address), re-skinned at runtime with the two SpriteAssetSkins built here, which
/// target the vanilla drill SpriteAssets exactly like the Tin Drill's skins do.
///
/// Every networked prefab is named SolariteDrill_*: NetCode derives the ghost type from the prefab name, and a
/// prefab named like a vanilla one (DrillForwardEntity) would share its ghost type and never enter the ghost
/// collection, keeping GhostCollectionSystem busy every frame (Runic Storage 0.7.37 lesson).
/// </summary>
public static class SolariteDrillBuilder
{
    const string Root = "Assets/SolariteDrill";
    const string ObjectName = "SolariteDrill_Drill";
    const int DamageMultiplier = 3; // vs. the Scarlet Drill
    // Vanilla variation per direction (DirectionBasedOnVariationAuthoring): Forward 0, Right 1, Backward 2, Left 3.
    static readonly string[] Directions = { "Forward", "Right", "Backward", "Left" };
    // Vanilla drill SpriteAssets the skins target (same as the Tin Drill skins drillCrude / drillBaseCrude).
    static readonly (long lo, long hi) DrillAsset = (6432503404751252551, 6649026345659046154);
    static readonly (long lo, long hi) DrillBaseAsset = (-6366884559295650734, 2711081489943583545);

    public static void BuildAll()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var vanilla = Vanilla.Load(Path.Combine(Directory.GetCurrentDirectory(), "SolariteDrillVanilla.txt"));
            ConfigureTextures();
            BuildSkins();
            var icon = Sprite("SolariteDrill_icon");
            var smallIcon = Sprite("SolariteDrill_smallIcon");
            for (int variation = 0; variation < Directions.Length; variation++)
                BuildEntity(vanilla, Directions[variation], variation, icon, smallIcon);
            AssetDatabase.SaveAssets();
            BuildMod("SolariteDrill", "7c1e5d0a2b3f4e6a9d8c1b0a5f3e2d17");
        }
        catch (Exception e) { Debug.LogError("[SolariteDrillBuilder] " + e); EditorApplication.Exit(1); }
    }

    // ------------------------------------------------------------------ textures + skins

    static void ConfigureTextures()
    {
        foreach (var path in Directory.GetFiles(Root + "/Textures", "*.png"))
        {
            var p = path.Replace('\\', '/');
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            bool icon = p.Contains("_icon") || p.Contains("_smallIcon");
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.textureType = icon ? TextureImporterType.Sprite : TextureImporterType.Default;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
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
    static Sprite Sprite(string name) => AssetDatabase.LoadAllAssetsAtPath(Root + "/Textures/" + name + ".png").OfType<Sprite>().First();

    static void BuildSkins()
    {
        // Pivots, slots and replacement guids mirror the vanilla Tin Drill skins (see Unity/extract_reference.py).
        const float body = 0.2222222238779068f, side = 0.2857142984867096f;
        CreateBlock("SpriteAssetSkin", Root + "/Data/SpriteAssetSkin/SolariteDrill_DrillSkin.asset", so =>
        {
            SetAddress(so.FindProperty("m_targetAssetRef"), DrillAsset);
            SetReplacement(so.FindProperty("m_staticReplacementData"), "", ("solariteDrill_inactive_up", .5f, 0f));
            var list = so.FindProperty("m_replacementData");
            list.arraySize = 2;
            SetReplacement(list.GetArrayElementAtIndex(0), "83c5d8c3-d99c-49a2-99d6-848c78c17d18",
                ("solariteDrill_inactive", .5f, body), ("solariteDrill_inactive_side", side, 0f), ("solariteDrill_inactive_up", .5f, 0f));
            SetReplacement(list.GetArrayElementAtIndex(1), "45f939c1-0ee3-447c-ac6a-b1df55d9f88f",
                ("solariteDrill_active", .5f, body), ("solariteDrill_active_side", side, 0f), ("solariteDrill_active_up", .5f, 0f));
        });
        CreateBlock("SpriteAssetSkin", Root + "/Data/SpriteAssetSkin/SolariteDrill_BaseSkin.asset", so =>
        {
            SetAddress(so.FindProperty("m_targetAssetRef"), DrillBaseAsset);
            SetReplacement(so.FindProperty("m_staticReplacementData"), "",
                ("solariteDrillBase_idle", .5f, 0f), ("solariteDrillBase_idle_side", .5f, 0f),
                ("solariteDrillBase_idle_side2", .5f, 0f), ("solariteDrillBase_idle_up", .5f, 0f));
            so.FindProperty("m_replacementData").arraySize = 0;
        });
    }

    /// <summary>First sprite is m_spriteData, the rest are m_variants.</summary>
    static void SetReplacement(SerializedProperty data, string guid, params (string tex, float px, float py)[] sprites)
    {
        data.FindPropertyRelative("m_guid").stringValue = guid;
        SetSpriteData(data.FindPropertyRelative("m_spriteData"), sprites[0]);
        var variants = data.FindPropertyRelative("m_variants");
        variants.arraySize = sprites.Length - 1;
        for (int i = 1; i < sprites.Length; i++) SetSpriteData(variants.GetArrayElementAtIndex(i - 1), sprites[i]);
    }

    static void SetSpriteData(SerializedProperty d, (string tex, float px, float py) s)
    {
        d.FindPropertyRelative("texture").objectReferenceValue = Tex(s.tex);
        d.FindPropertyRelative("emissiveTexture").objectReferenceValue = null;
        d.FindPropertyRelative("normalTexture").objectReferenceValue = null;
        d.FindPropertyRelative("pivot").vector2Value = new Vector2(s.px, s.py);
        d.FindPropertyRelative("positionalData").arraySize = 0;
        B(d.FindPropertyRelative("inheritPivot"), true);
    }

    // ------------------------------------------------------------------ entity prefabs

    static void BuildEntity(Vanilla vanilla, string direction, int variation, Sprite icon, Sprite smallIcon)
    {
        string name = "SolariteDrill_" + direction + "Entity";
        string path = Root + "/Prefabs/" + name + ".prefab";
        var go = new GameObject(name);
        int missing = 0;
        try
        {
            var refs = new List<(Component comp, string path, string target)>();
            foreach (var block in vanilla.Prefabs[direction])
            {
                if (block.cls == "#Transform") continue;
                if (block.cls == "EntityMonoBehaviourData")
                {
                    AddObjectAuthoring(go, block, variation, icon, smallIcon);
                    continue;
                }
                var comp = Add(go, block.cls);
                var so = new SerializedObject(comp);
                foreach (var line in block.lines)
                {
                    if (line.kind == 'R') { refs.Add((comp, line.path, line.value)); continue; }
                    var p = so.FindProperty(line.path);
                    if (p == null) { missing++; Debug.LogWarning($"[SolariteDrillBuilder] {name}.{block.cls}: no property {line.path}"); continue; }
                    if (line.kind == 'A') p.arraySize = int.Parse(line.value);
                    else if (line.kind == 'S') p.stringValue = line.value;
                    else if (long.TryParse(line.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long whole)
                             && p.propertyType == SerializedPropertyType.Integer) p.longValue = whole;
                    else SetNum(p, double.Parse(line.value, CultureInfo.InvariantCulture));
                }
                if (block.cls == "AutomatedMinerAuthoring")
                    so.FindProperty("damage").intValue *= DamageMultiplier;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            // References between components of this prefab (e.g. HealthAuthoring.level -> AreaLevelAuthoring).
            // Anything pointing at the vanilla-only EntityMonoBehaviourData stays null (unused by conversion).
            foreach (var (comp, propPath, target) in refs)
            {
                if (string.IsNullOrEmpty(target) || target == "EntityMonoBehaviourData") continue;
                Fill(comp, so => so.FindProperty(propPath).objectReferenceValue =
                    target == "#GameObject" ? (Object)go : go.GetComponent(FindType(target, typeof(Component))));
            }
            Fill(Add(go, "LocalizationAuthoring"), so => so.FindProperty("termKey").stringValue = ObjectName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally { Object.DestroyImmediate(go); }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var block2 = CreateBlock("EntityAuthoringDataBlock", Root + "/Data/EntityAuthoringDataBlock/" + name + ".asset",
            so => so.FindProperty("prefab").objectReferenceValue = prefab);
        EditPrefab(path, p =>
        {
            Fill(Comp(p, "ObjectAuthoring"), so => SetAddress(so.FindProperty("authoringRef"), ReadAddress(block2)));
            // NetCode ghost id = the prefab's own guid (what the Ghost inspector would write).
            Fill(Comp(p, "GhostAuthoringComponent"), so => so.FindProperty("prefabId").stringValue = AssetDatabase.AssetPathToGUID(path));
        });
        if (missing > 0) throw new Exception($"{name}: {missing} vanilla properties not found (see warnings)");
        Debug.Log($"[SolariteDrillBuilder] {name} variation {variation}");
    }

    static void AddObjectAuthoring(GameObject go, Vanilla.Block block, int variation, Sprite icon, Sprite smallIcon)
    {
        double V(string p) => double.Parse(block.lines.First(l => l.path == "objectInfo." + p).value, CultureInfo.InvariantCulture);
        if ((int)V("variation") != variation) throw new Exception($"{go.name}: vanilla variation {V("variation")} != {variation}");
        Fill(Add(go, "ObjectAuthoring"), so =>
        {
            so.FindProperty("objectName").stringValue = ObjectName;
            N(so, "initialAmount", V("initialAmount"));
            N(so, "variation", variation);
            N(so, "variationIsDynamic", V("variationIsDynamic"));
            N(so, "variationToToggleTo", V("variationToToggleTo"));
            N(so, "objectType", V("objectType"));
            var tags = so.FindProperty("tags"); tags.arraySize = 1; tags.GetArrayElementAtIndex(0).intValue = (int)V("tags.Array.data[0]");
            N(so, "rarity", V("rarity"));
            N(so, "salvageMultiplier", V("salvageMultiplier"));
            so.FindProperty("graphicalPrefab").objectReferenceValue = null;
            // The vanilla drill graphics (shared pool with Tin/Scarlet drills); reskinned by DrillReskinPatch.
            long L(string p) => long.Parse(block.lines.First(l => l.path == "objectInfo.prefabInfo.graphicalRef.m_address." + p).value, CultureInfo.InvariantCulture);
            SetAddress(so.FindProperty("graphicalRef"), (L("m_low"), L("m_high")));
        });
        Fill(Add(go, "InventoryItemAuthoring"), so =>
        {
            N(so, "sellValue", V("sellValue"));
            N(so, "buyValueMultiplier", V("buyValueMultiplier"));
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("smallIcon").objectReferenceValue = smallIcon;
            B(so, "isStackable", V("isStackable") != 0);
            N(so, "craftingTime", V("craftingTime"));
            // Scarlet Drill: 10 Iron + 10 Scarlet Bars. One tier pattern up: 10 Galaxite + 10 Solarite Bars.
            var list = so.FindProperty("requiredObjectsToCraft"); list.arraySize = 2;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("objectName").stringValue = "GalaxiteBar";
            list.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 10;
            list.GetArrayElementAtIndex(1).FindPropertyRelative("objectName").stringValue = "SolariteBar";
            list.GetArrayElementAtIndex(1).FindPropertyRelative("amount").intValue = 10;
        });
    }

    /// <summary>Flattened vanilla prefab values: per direction, components in order, each with its property lines.</summary>
    sealed class Vanilla
    {
        public struct Line { public char kind; public string path, value; }
        public sealed class Block { public string cls; public List<Line> lines = new List<Line>(); }
        public readonly Dictionary<string, List<Block>> Prefabs = new Dictionary<string, List<Block>>();

        public static Vanilla Load(string file)
        {
            if (!File.Exists(file)) throw new Exception("missing " + file + " (run gen_assets.py)");
            var v = new Vanilla();
            Block cur = null;
            foreach (var raw in File.ReadAllLines(file))
            {
                if (raw.Length == 0) continue;
                var f = raw.Split('\t');
                switch (f[0])
                {
                    case "B": break;
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

    // ------------------------------------------------------------------ build + helpers (as MagicMirrorBuilder)

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
        var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-drillOut");
        string output = i >= 0 ? args[i + 1] : Path.GetFullPath("Library/SolariteDrillBuild");
        bool ok = false; PugMod.ModBuilder.BuildMod(settings, output, r => ok = r);
        if (!ok) throw new Exception("Build failed: " + name);
        Debug.Log("[SolariteDrillBuilder] Built " + name + " into " + output);
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
        if (matches.Count > 1) Debug.Log($"[SolariteDrillBuilder] {name}: {string.Join(", ", matches.Select(t => t.AssemblyQualifiedName))}; using first");
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
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
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

    /// <summary>Sets a DataBlockRef (has m_address) or a raw address property.</summary>
    static void SetAddress(SerializedProperty p, (long lo, long hi) a)
    {
        var addr = p.FindPropertyRelative("m_low") != null ? p : p.FindPropertyRelative("m_address");
        addr.FindPropertyRelative("m_low").longValue = a.lo;
        addr.FindPropertyRelative("m_high").longValue = a.hi;
    }

    /// <summary>Same mapping the SDK example data uses (guid text -> Hash128 words).</summary>
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
