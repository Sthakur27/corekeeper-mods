using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Builds the Drill Unit: one entity prefab copied from the vanilla Speeder Boat (values from DrillUnitVanilla.txt,
/// written by gen_assets.py from dump_vanilla.py) with ObjectAuthoring/InventoryItemAuthoring in place of the vanilla
/// EntityMonoBehaviourData, placeable on land as well as water, a faster BoatAuthoring and the Rift Statue recipe
/// (300 Pandorium + 100 Solarite Bars). Graphics are the vanilla boat's (GraphicalObjectDataBlock address); the
/// mod gives it the Speeder Boat skin at runtime.
///
/// The networked prefab is named DrillUnit_*: NetCode derives the ghost type from the prefab name, and a prefab named
/// like a vanilla one would share its ghost type (Runic Storage 0.7.37 lesson).
/// </summary>
public static class DrillUnitBuilder
{
    const string Root = "Assets/DrillUnit";
    const string ObjectName = "DrillUnit_Unit";
    const string PrefabName = "DrillUnit_UnitEntity";
    const double SpeedMultiplier = 1.6; // Speeder Boat 1.3, Boat 1.0

    public static void BuildAll()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var vanilla = Vanilla.Load(Path.Combine(Directory.GetCurrentDirectory(), "DrillUnitVanilla.txt"));
            ConfigureTextures();
            BuildEntity(vanilla, Sprite("DrillUnit_icon"), Sprite("DrillUnit_smallIcon"));
            AssetDatabase.SaveAssets();
            BuildMod("DrillUnit", "d1a1e7b0c3f84e2a9b6d5c4e3f2a1b09");
        }
        catch (Exception e) { Debug.LogError("[DrillUnitBuilder] " + e); EditorApplication.Exit(1); }
    }

    static void ConfigureTextures()
    {
        foreach (var path in Directory.GetFiles(Root + "/Textures", "*.png"))
        {
            var p = path.Replace('\\', '/');
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            // Icons are sprites; the two boat skin sheets stay plain textures for SpriteSheetSkin.SetSkin.
            bool icon = p.Contains("_icon") || p.Contains("_smallIcon");
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.textureType = icon ? TextureImporterType.Sprite : TextureImporterType.Default;
            ti.isReadable = !icon;
            ti.npotScale = TextureImporterNPOTScale.None;
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

    static Sprite Sprite(string name) => AssetDatabase.LoadAllAssetsAtPath(Root + "/Textures/" + name + ".png").OfType<Sprite>().First();

    static void BuildEntity(Vanilla vanilla, Sprite icon, Sprite smallIcon)
    {
        string path = Root + "/Prefabs/" + PrefabName + ".prefab";
        var go = new GameObject(PrefabName);
        int missing = 0;
        try
        {
            var refs = new List<(Component comp, string path, string target)>();
            foreach (var block in vanilla.Prefabs["Main"])
            {
                if (block.cls == "#Transform") continue;
                if (block.cls == "EntityMonoBehaviourData")
                {
                    AddObjectAuthoring(go, block, icon, smallIcon);
                    continue;
                }
                var comp = Add(go, block.cls);
                var so = new SerializedObject(comp);
                foreach (var line in block.lines)
                {
                    if (line.kind == 'R') { refs.Add((comp, line.path, line.value)); continue; }
                    var p = so.FindProperty(line.path);
                    if (p == null) { missing++; Debug.LogWarning($"[DrillUnitBuilder] {PrefabName}.{block.cls}: no property {line.path}"); continue; }
                    if (line.kind == 'A') p.arraySize = int.Parse(line.value);
                    else if (line.kind == 'S') p.stringValue = line.value;
                    else if (long.TryParse(line.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long whole)
                             && p.propertyType == SerializedPropertyType.Integer) p.longValue = whole;
                    else SetNum(p, double.Parse(line.value, CultureInfo.InvariantCulture));
                }
                if (block.cls == "BoatAuthoring")
                    SetNum(Prop(so, "speedMultiplier"), SpeedMultiplier);
                if (block.cls == "PlaceableObjectAuthoring")
                {
                    // Vanilla boats go on water only (canBePlacedOnObjects = Water). The unit also goes on any walkable tile.
                    B(so, "canBePlacedOnAnyWalkableTile", true);
                    B(so, "canBePlacedOnWater", true);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
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
        var entityBlock = CreateBlock("EntityAuthoringDataBlock", Root + "/Data/EntityAuthoringDataBlock/" + PrefabName + ".asset",
            so => so.FindProperty("prefab").objectReferenceValue = prefab);
        EditPrefab(path, p =>
        {
            Fill(Comp(p, "ObjectAuthoring"), so => SetAddress(so.FindProperty("authoringRef"), ReadAddress(entityBlock)));
            Fill(Comp(p, "GhostAuthoringComponent"), so => so.FindProperty("prefabId").stringValue = AssetDatabase.AssetPathToGUID(path));
        });
        if (missing > 0) throw new Exception($"{PrefabName}: {missing} vanilla properties not found (see warnings)");
        Debug.Log($"[DrillUnitBuilder] {PrefabName} built");
    }

    static void AddObjectAuthoring(GameObject go, Vanilla.Block block, Sprite icon, Sprite smallIcon)
    {
        double V(string p) => double.Parse(block.lines.First(l => l.path == "objectInfo." + p).value, CultureInfo.InvariantCulture);
        Fill(Add(go, "ObjectAuthoring"), so =>
        {
            so.FindProperty("objectName").stringValue = ObjectName;
            N(so, "initialAmount", V("initialAmount"));
            N(so, "variation", 0);
            N(so, "variationIsDynamic", V("variationIsDynamic"));
            N(so, "variationToToggleTo", V("variationToToggleTo"));
            N(so, "objectType", V("objectType"));
            var tags = so.FindProperty("tags"); tags.arraySize = 1; tags.GetArrayElementAtIndex(0).intValue = (int)V("tags.Array.data[0]");
            N(so, "rarity", 3); // Epic: end-game craft
            N(so, "salvageMultiplier", V("salvageMultiplier"));
            so.FindProperty("graphicalPrefab").objectReferenceValue = null;
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
            var list = so.FindProperty("requiredObjectsToCraft"); list.arraySize = 2;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("objectName").stringValue = "PandoriumBar";
            list.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 300;
            list.GetArrayElementAtIndex(1).FindPropertyRelative("objectName").stringValue = "SolariteBar";
            list.GetArrayElementAtIndex(1).FindPropertyRelative("amount").intValue = 100;
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
        var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-unitOut");
        string output = i >= 0 ? args[i + 1] : Path.GetFullPath("Library/DrillUnitBuild");
        bool ok = false; PugMod.ModBuilder.BuildMod(settings, output, r => ok = r);
        if (!ok) throw new Exception("Build failed: " + name);
        Debug.Log("[DrillUnitBuilder] Built " + name + " into " + output);
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
        if (matches.Count > 1) Debug.Log($"[DrillUnitBuilder] {name}: {string.Join(", ", matches.Select(t => t.AssemblyQualifiedName))}; using first");
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
