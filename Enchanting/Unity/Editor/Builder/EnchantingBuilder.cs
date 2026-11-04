using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Builds the Enchanting Table: one entity prefab copied from the vanilla Upgrade Forge (values from
/// EnchantingVanilla.txt, written by gen_assets.py from dump_vanilla.py) with ObjectAuthoring/InventoryItemAuthoring
/// in place of the vanilla EntityMonoBehaviourData, our icon and recipe. Graphics are the vanilla forge's
/// (GraphicalObjectDataBlock address), so the table opens the vanilla Upgrade Forge UI; the mod's scripts turn
/// its button into "Enchant" when the open building is the table.
///
/// The networked prefab is named Enchanting_*: NetCode derives the ghost type from the prefab name, and a prefab
/// named like a vanilla one would share its ghost type (Runic Storage 0.7.37 lesson).
/// </summary>
public static class EnchantingBuilder
{
    const string Root = "Assets/Enchanting";
    const string ObjectName = "Enchanting_Table";
    const string PrefabName = "Enchanting_TableEntity";
    const string ModGuid = "7b1e4c2a9d3f4e6a8c5b0d2e1f3a4b6c"; // = ModManifest.json guid
    static readonly (string item, int amount)[] Recipe = { ("OctarineBar", 20), ("GoldBar", 15) };

    public static void BuildAll()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var vanilla = Vanilla.Load(Path.Combine(Directory.GetCurrentDirectory(), "EnchantingVanilla.txt"));
            ConfigureTextures();
            BuildEntity(vanilla, Sprite("EnchantingTable_icon"), Sprite("EnchantingTable_smallIcon"));
            AssetDatabase.SaveAssets();
            BuildMod("Enchanting", ModGuid);
        }
        catch (Exception e) { Debug.LogError("[EnchantingBuilder] " + e); EditorApplication.Exit(1); }
    }

    static void ConfigureTextures()
    {
        foreach (var path in Directory.GetFiles(Root + "/Textures", "*.png"))
        {
            var p = path.Replace('\\', '/');
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.textureType = TextureImporterType.Sprite;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            var ts = new TextureImporterSettings(); ti.ReadTextureSettings(ts);
            ts.spriteAlignment = (int)SpriteAlignment.Center;
            ts.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(ts);
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
            foreach (var block in vanilla.Blocks)
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
                    if (p == null) { missing++; Debug.LogWarning($"[EnchantingBuilder] {PrefabName}.{block.cls}: no property {line.path}"); continue; }
                    if (line.kind == 'A') p.arraySize = int.Parse(line.value);
                    else if (line.kind == 'S') p.stringValue = line.value;
                    else if (long.TryParse(line.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long whole)
                             && p.propertyType == SerializedPropertyType.Integer) p.longValue = whole;
                    else SetNum(p, double.Parse(line.value, CultureInfo.InvariantCulture));
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            // References between components of this prefab; anything pointing at the vanilla-only
            // EntityMonoBehaviourData stays null (unused by conversion).
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
        var block2 = CreateBlock("EntityAuthoringDataBlock", Root + "/Data/EntityAuthoringDataBlock/" + PrefabName + ".asset",
            so => so.FindProperty("prefab").objectReferenceValue = prefab);
        EditPrefab(path, p =>
        {
            Fill(Comp(p, "ObjectAuthoring"), so => SetAddress(so.FindProperty("authoringRef"), ReadAddress(block2)));
            // NetCode ghost id = the prefab's own guid (what the Ghost inspector would write).
            Fill(Comp(p, "GhostAuthoringComponent"), so => so.FindProperty("prefabId").stringValue = AssetDatabase.AssetPathToGUID(path));
        });
        if (missing > 0) throw new Exception($"{PrefabName}: {missing} vanilla properties not found (see warnings)");
        Debug.Log($"[EnchantingBuilder] {PrefabName} built");
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
            N(so, "rarity", 2); // rare: one step above the forge
            N(so, "salvageMultiplier", V("salvageMultiplier"));
            so.FindProperty("graphicalPrefab").objectReferenceValue = null;
            // The vanilla Upgrade Forge graphics (its CraftingBuilding opens the upgrade UI).
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
            var list = so.FindProperty("requiredObjectsToCraft"); list.arraySize = Recipe.Length;
            for (int i = 0; i < Recipe.Length; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("objectName").stringValue = Recipe[i].item;
                list.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue = Recipe[i].amount;
            }
        });
    }

    /// <summary>Flattened vanilla prefab values: components in order, each with its property lines.</summary>
    sealed class Vanilla
    {
        public struct Line { public char kind; public string path, value; }
        public sealed class Block { public string cls; public List<Line> lines = new List<Line>(); }
        public readonly List<Block> Blocks = new List<Block>();

        public static Vanilla Load(string file)
        {
            if (!File.Exists(file)) throw new Exception("missing " + file + " (run gen_assets.py)");
            var v = new Vanilla();
            Block cur = null;
            foreach (var raw in File.ReadAllLines(file))
            {
                if (raw.Length == 0) continue;
                var f = raw.Split('\t');
                if (f[0] == "C") v.Blocks.Add(cur = new Block { cls = f[1] });
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
        var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-enchantOut");
        string output = i >= 0 ? args[i + 1] : Path.GetFullPath("Library/EnchantingBuild");
        bool ok = false; PugMod.ModBuilder.BuildMod(settings, output, r => ok = r);
        if (!ok) throw new Exception("Build failed: " + name);
        Debug.Log("[EnchantingBuilder] Built " + name + " into " + output);
    }

    static SerializedProperty Prop(SerializedObject so, string path)
    {
        var p = so.FindProperty(path);
        if (p == null) throw new Exception($"{so.targetObject.GetType().Name} has no property {path}");
        return p;
    }

    static void B(SerializedObject so, string path, bool v)
    {
        var p = Prop(so, path);
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
        if (matches.Count > 1) Debug.Log($"[EnchantingBuilder] {name}: {string.Join(", ", matches.Select(t => t.AssemblyQualifiedName))}; using first");
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
