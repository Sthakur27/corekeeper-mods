using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Builds the Wormhole Potion mod objects from vanilla authoring components only:
/// - WormholePotion_Potion: a cast item with the Recall Idol's cast values, 10 Gold Bars.
/// - WormholePotion_Marker: a copy of the vanilla WayPointMapMarker prefab (values from WormholePotionVanilla.txt,
///   written by gen_assets.py from dump_vanilla.py), minus DontSerializeAuthoring so it is saved with the world.
///   Its MapMarkerAuthoring type is Waypoint, so the map lets players at a waypoint teleport to it.
///
/// Networked prefabs are named WormholePotion_*: NetCode derives the ghost type from the prefab name, and a prefab
/// named like a vanilla one would share its ghost type (Runic Storage 0.7.37 lesson).
/// </summary>
public static class WormholePotionBuilder
{
    const string Root = "Assets/WormholePotion";
    const string PotionName = "WormholePotion_Potion";
    const string MarkerName = "WormholePotion_Marker";
    // The vanilla waypoint map marker is DontSerialize (waypoints re-create it); ours must be saved.
    static readonly HashSet<string> SkipMarker = new HashSet<string> { "#Transform", "DontSerializeAuthoring" };

    public static void BuildAll()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var vanilla = Vanilla.Load(Path.Combine(Directory.GetCurrentDirectory(), "WormholePotionVanilla.txt"));
            var icon = ConfigureIcon();
            BuildPotion(vanilla, icon);
            BuildMarker(vanilla, icon);
            AssetDatabase.SaveAssets();
            BuildMod("WormholePotion", "b7e3c2a19d4f4e8a8c5b6a0f1e2d3c40");
        }
        catch (Exception e) { Debug.LogError("[WormholePotionBuilder] " + e); EditorApplication.Exit(1); }
    }

    static Sprite ConfigureIcon()
    {
        string iconPath = Root + "/Textures/WormholePotion_icon.png";
        var ti = (TextureImporter)AssetImporter.GetAtPath(iconPath);
        ti.textureShape = TextureImporterShape.Texture2D;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 16;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        var ts = new TextureImporterSettings(); ti.ReadTextureSettings(ts);
        ts.spriteAlignment = (int)SpriteAlignment.Center;
        ts.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(ts); ti.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(iconPath).OfType<Sprite>().First();
    }

    // ------------------------------------------------------------------ potion

    static void BuildPotion(Vanilla vanilla, Sprite icon)
    {
        string path = Root + "/Prefabs/WormholePotion_Potion.prefab";
        var idol = vanilla.Prefabs["Potion"];
        var data = idol.First(b => b.cls == "EntityMonoBehaviourData");
        var cast = idol.First(b => b.cls == "CastItemAuthoring");
        var go = new GameObject("WormholePotion_Potion");
        try
        {
            Fill(Add(go, "ObjectAuthoring"), so =>
            {
                so.FindProperty("objectName").stringValue = PotionName;
                N(so, "initialAmount", 1);
                N(so, "objectType", data.Num("objectInfo.objectType"));
                N(so, "rarity", data.Num("objectInfo.rarity"));
                N(so, "salvageMultiplier", 1);
                SetTags(so, data);
            });
            Fill(Add(go, "InventoryItemAuthoring"), so =>
            {
                N(so, "sellValue", -1); N(so, "buyValueMultiplier", 1);
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("smallIcon").objectReferenceValue = icon;
                B(so, "isStackable", true);
                var list = so.FindProperty("requiredObjectsToCraft"); list.arraySize = 1;
                list.GetArrayElementAtIndex(0).FindPropertyRelative("objectName").stringValue = "GoldBar";
                list.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 10;
            });
            Fill(Add(go, "CastItemAuthoring"), so =>
            {
                N(so, "castTime", cast.Num("castTime"));
                N(so, "useType", cast.Num("useType"));
                B(so, "allowHoldToRepeat", false);
            });
            Fill(Add(go, "LocalizationAuthoring"), so => so.FindProperty("termKey").stringValue = PotionName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally { Object.DestroyImmediate(go); }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var block = CreateBlock("EntityAuthoringDataBlock", Root + "/Data/EntityAuthoringDataBlock/WormholePotion_Potion.asset",
            so => so.FindProperty("prefab").objectReferenceValue = prefab);
        EditPrefab(path, p => Fill(Comp(p, "ObjectAuthoring"), so => SetAddress(so.FindProperty("authoringRef"), ReadAddress(block))));
        Debug.Log("[WormholePotionBuilder] potion built");
    }

    // ------------------------------------------------------------------ return marker

    static void BuildMarker(Vanilla vanilla, Sprite icon)
    {
        string name = "WormholePotion_MarkerEntity";
        string path = Root + "/Prefabs/" + name + ".prefab";
        var go = new GameObject(name);
        int missing = 0;
        try
        {
            var refs = new List<(Component comp, string path, string target)>();
            foreach (var block in vanilla.Prefabs["Marker"])
            {
                if (SkipMarker.Contains(block.cls)) continue;
                if (block.cls == "EntityMonoBehaviourData")
                {
                    AddMarkerObjectAuthoring(go, block, icon);
                    continue;
                }
                var comp = Add(go, block.cls);
                var so = new SerializedObject(comp);
                foreach (var line in block.lines)
                {
                    if (line.kind == 'R') { refs.Add((comp, line.path, line.value)); continue; }
                    var p = so.FindProperty(line.path);
                    if (p == null) { missing++; Debug.LogWarning($"[WormholePotionBuilder] {name}.{block.cls}: no property {line.path}"); continue; }
                    if (line.kind == 'A') p.arraySize = int.Parse(line.value);
                    else if (line.kind == 'S') p.stringValue = line.value;
                    else if (long.TryParse(line.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long whole)
                             && p.propertyType == SerializedPropertyType.Integer) p.longValue = whole;
                    else SetNum(p, double.Parse(line.value, CultureInfo.InvariantCulture));
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var (comp, propPath, target) in refs)
            {
                if (string.IsNullOrEmpty(target) || target == "EntityMonoBehaviourData" || SkipMarker.Contains(target)) continue;
                Fill(comp, so => so.FindProperty(propPath).objectReferenceValue =
                    target == "#GameObject" ? (Object)go : go.GetComponent(FindType(target, typeof(Component))));
            }
            Fill(Add(go, "LocalizationAuthoring"), so => so.FindProperty("termKey").stringValue = MarkerName);
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
        Debug.Log("[WormholePotionBuilder] marker built");
    }

    static void AddMarkerObjectAuthoring(GameObject go, Vanilla.Block block, Sprite icon)
    {
        Fill(Add(go, "ObjectAuthoring"), so =>
        {
            so.FindProperty("objectName").stringValue = MarkerName;
            N(so, "initialAmount", 1);
            N(so, "variation", 0);
            N(so, "objectType", block.Num("objectInfo.objectType"));
            N(so, "rarity", block.Num("objectInfo.rarity"));
            N(so, "salvageMultiplier", 1);
            SetTags(so, block);
            so.FindProperty("graphicalPrefab").objectReferenceValue = null;
        });
        Fill(Add(go, "InventoryItemAuthoring"), so =>
        {
            N(so, "sellValue", -1); N(so, "buyValueMultiplier", 1);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("smallIcon").objectReferenceValue = icon;
            B(so, "isStackable", false);
            so.FindProperty("requiredObjectsToCraft").arraySize = 0;
        });
    }

    static void SetTags(SerializedObject so, Vanilla.Block block)
    {
        var tags = so.FindProperty("tags");
        int n = (int)block.Num("objectInfo.tags");
        tags.arraySize = n;
        for (int i = 0; i < n; i++) tags.GetArrayElementAtIndex(i).intValue = (int)block.Num($"objectInfo.tags.Array.data[{i}]");
    }

    /// <summary>Flattened vanilla prefab values: per prefab key, components in order, each with its property lines.</summary>
    sealed class Vanilla
    {
        public struct Line { public char kind; public string path, value; }
        public sealed class Block
        {
            public string cls; public List<Line> lines = new List<Line>();
            public double Num(string path)
            {
                foreach (var l in lines)
                    if (l.path == path) return l.kind == 'A' ? int.Parse(l.value) : double.Parse(l.value, CultureInfo.InvariantCulture);
                throw new Exception($"{cls}: no vanilla value {path}");
            }
            public long Long(string path)
            {
                foreach (var l in lines)
                    if (l.path == path) return long.Parse(l.value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                throw new Exception($"{cls}: no vanilla value {path}");
            }
        }
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
                if (f[0] == "C")
                {
                    if (!v.Prefabs.TryGetValue(f[1], out var list)) v.Prefabs[f[1]] = list = new List<Block>();
                    list.Add(cur = new Block { cls = f[2] });
                }
                else cur.lines.Add(new Line { kind = f[0][0], path = f[1], value = f.Length > 2 ? f[2] : "" });
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
        var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-wormholeOut");
        string output = i >= 0 ? args[i + 1] : Path.GetFullPath("Library/WormholePotionBuild");
        bool ok = false; PugMod.ModBuilder.BuildMod(settings, output, r => ok = r);
        if (!ok) throw new Exception("Build failed: " + name);
        Debug.Log("[WormholePotionBuilder] Built " + name + " into " + output);
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
        if (matches.Count > 1) Debug.Log($"[WormholePotionBuilder] {name}: {string.Join(", ", matches.Select(t => t.AssemblyQualifiedName))}; using first");
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
