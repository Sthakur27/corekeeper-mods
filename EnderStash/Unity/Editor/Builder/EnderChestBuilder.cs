using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Batch-mode step 2 (after EnderStashSetup.ImportGame): builds the Ender Chest prefabs and data blocks in
/// Assets/EnderStash from the textures/SpriteAsset/TextDataBlock written by gen_assets.py, then runs the
/// SDK ModBuilder. Game types are resolved by name and filled through SerializedObject so this file has no
/// compile-time dependency on the game assemblies. Values mirror the vanilla ChestEntity / Chest prefabs.
///
/// Unity.exe -batchmode -projectPath CoreKeeperModSDK -executeMethod EnderChestBuilder.BuildAll -quit
///   (-enderOut &lt;dir&gt; = where the built mod folder goes; default Library/EnderStashBuild)
/// </summary>
public static class EnderChestBuilder
{
    const string Root = "Assets/EnderStash";
    const string ObjectName = "EnderStash_EnderChest"; // = EnderChest.ObjectName in the mod scripts and the TextDataBlock name
    const string GraphicsPath = Root + "/Prefabs/EnderChestGraphics.prefab";
    const string LogicPath = Root + "/Prefabs/EnderChestEntity.prefab";
    const string GraphicalBlockPath = Root + "/Data/GraphicalObjectDataBlock/EnderChestGraphics.asset";
    const string AuthoringBlockPath = Root + "/Data/EntityAuthoringDataBlock/EnderChestEntity.asset";
    const string SettingsPath = Root + "/EnderStash.asset";
    const string LitMaterial = "Packages/dev.pugstorm.mod/Assets/Materials/SpriteObject/UGC SpriteObject Lit.mat";
    const string ShadowMaterial = "Packages/dev.pugstorm.mod/Assets/Materials/SpriteObject/UGC SpriteObject Shadow.mat";

    // Vanilla addresses (data blocks are global, so a mod can point at the game's own).
    static readonly (long lo, long hi) ChestShadowSprite = (-5209509996517173420, 6427741878288745627);
    const int ChestShadowVariantHash = 979642088;
    static readonly (long lo, long hi) ChestPoolParams = (7117093835267264374, 3483163772298863800);

    public static void BuildAll()
    {
        try
        {
            ConfigureTextures();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var spriteAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Root + "/Data/SpriteAsset/EnderChest.asset");
            if (spriteAsset == null) throw new Exception("SpriteAsset missing (run gen_assets.py)");
            var spriteAddress = ReadAddress(spriteAsset);
            Debug.Log($"[EnderChestBuilder] SpriteAsset {spriteAsset.GetType().FullName} address {spriteAddress}");

            var graphics = BuildGraphics(spriteAddress);
            var graphicalBlock = CreateBlock("GraphicalObjectDataBlock", GraphicalBlockPath, so =>
            {
                so.FindProperty("prefab").objectReferenceValue = graphics;
                SetAddress(so.FindProperty("poolParams"), ChestPoolParams);
            });

            var logic = BuildLogic(graphics, ReadAddress(graphicalBlock));
            var authoringBlock = CreateBlock("EntityAuthoringDataBlock", AuthoringBlockPath, so =>
            {
                so.FindProperty("prefab").objectReferenceValue = logic;
            });
            // authoringRef needs the block, the block needs the prefab: patch the saved prefab.
            EditPrefab(LogicPath, go =>
            {
                var so = new SerializedObject(Comp(go, "ObjectAuthoring"));
                SetAddress(so.FindProperty("authoringRef"), ReadAddress(authoringBlock));
                so.ApplyModifiedPropertiesWithoutUndo();
            });

            AssetDatabase.SaveAssets();
            BuildMod();
        }
        catch (Exception e)
        {
            Debug.LogError("[EnderChestBuilder] FAILED: " + e);
            EditorApplication.Exit(1);
        }
    }

    // ------------------------------------------------------------------ textures

    static void ConfigureTextures()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Textures" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.isReadable = false;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            ti.GetSourceTextureWidthAndHeight(out _, out int height);
            // World sprites: feet one pixel above the bottom, like vanilla chests. The inventory icon is
            // drawn centered on its pivot, so it gets a centered pivot (else it sits high in the slot).
            s.spritePivot = path.EndsWith("_icon.png") ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f / height);
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }
    }

    // ------------------------------------------------------------------ graphics prefab (vanilla Chest component)

    static GameObject BuildGraphics((long lo, long hi) spriteAddress)
    {
        var root = new GameObject("EnderChestGraphics");
        try
        {
            var xScaler = Child(root, "XScaler", Vector3.zero);
            var spriteGo = Child(xScaler, "Sprite Object", new Vector3(0f, 0.125f, -0.375f));
            var shadowGo = Child(xScaler, "ShadowSprite", new Vector3(0f, 0.0625f, 0.0625f));
            shadowGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadowGo.layer = 22;
            try { shadowGo.tag = "ExcludeFromSpriteAutoSort"; } catch (Exception) { Debug.LogWarning("[EnderChestBuilder] tag ExcludeFromSpriteAutoSort missing"); }
            var interactGo = Child(root, "Interactable", Vector3.zero);

            var sprite = Add(spriteGo, "SpriteObject");
            Fill(sprite, so =>
            {
                SetAddress(so.FindProperty("m_assetRef"), spriteAddress);
                so.FindProperty("color").colorValue = Color.white;
                so.FindProperty("emissiveColor").colorValue = Color.white;
                so.FindProperty("material").objectReferenceValue = Load<Material>(LitMaterial);
                B(so, "syncAnimation", true);
                B(so, "syncVariant", true);
            });
            var shadow = Add(shadowGo, "SpriteObject");
            Fill(shadow, so =>
            {
                SetAddress(so.FindProperty("m_assetRef"), ChestShadowSprite);
                so.FindProperty("color").colorValue = new Color(1f, 1f, 1f, 0.7019608f);
                so.FindProperty("emissiveColor").colorValue = Color.white;
                so.FindProperty("material").objectReferenceValue = Load<Material>(ShadowMaterial);
                N(so, "m_startVariantHash", ChestShadowVariantHash);
                B(so, "syncAnimation", true);
                B(so, "syncVariant", true);
            });

            var interactable = Add(interactGo, "InteractableObject");
            var chest = Add(root, "Chest");
            var variation = Add(root, "SpriteVariationFromEntityDirection");

            Fill(chest, so =>
            {
                so.FindProperty("XScaler").objectReferenceValue = xScaler.transform;
                so.FindProperty("shadow").objectReferenceValue = shadowGo;
                so.FindProperty("interactable").objectReferenceValue = interactable;
                var list = so.FindProperty("spriteObjects");
                list.arraySize = 2;
                list.GetArrayElementAtIndex(0).objectReferenceValue = sprite;
                list.GetArrayElementAtIndex(1).objectReferenceValue = shadow;
                B(so, "useSharedTransformAnimations", true);
                N(so, "soundOptions.takeDamageSfx.value", 926580047);
                N(so, "soundOptions.deathSfx.value", 1045606711);
                B(so, "showSortAndQuickStackButtons", true);
            });
            Fill(variation, so =>
            {
                var list = so.FindProperty("spritesToRotate");
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = sprite;
                B(so, "reflectSides", true);
            });
            Fill(interactable, so =>
            {
                N(so, "weightMultiplier", 1f);
                N(so, "radius", 1.3f);
                N(so, "ignorePlayerDirectionRadiusPercentage", 1f);
                PersistentCall(so.FindProperty("onUseActions"), chest, "Use");
                PersistentCall(so.FindProperty("onTriggerExitActions"), chest, "OnPlayerLeftChest");
                var sub = so.FindProperty("subInteractingData");
                sub.arraySize = 1;
                var e = sub.GetArrayElementAtIndex(0);
                
                var outlines = e.FindPropertyRelative("optionalSpriteObjectOutlines");
                outlines.arraySize = 1;
                outlines.GetArrayElementAtIndex(0).objectReferenceValue = sprite;
            });

            Directory.CreateDirectory(Path.GetDirectoryName(GraphicsPath));
            return PrefabUtility.SaveAsPrefabAsset(root, GraphicsPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>One persistent void call (as the vanilla prefab wires Chest.Use / OnPlayerLeftChest).</summary>
    static void PersistentCall(SerializedProperty events, Object target, string method)
    {
        events.arraySize = 1;
        var calls = events.GetArrayElementAtIndex(0).FindPropertyRelative("m_PersistentCalls.m_Calls");
        calls.arraySize = 1;
        var c = calls.GetArrayElementAtIndex(0);
        c.FindPropertyRelative("m_Target").objectReferenceValue = target;
        c.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = target.GetType().FullName + ", " + target.GetType().Assembly.GetName().Name;
        c.FindPropertyRelative("m_MethodName").stringValue = method;
        c.FindPropertyRelative("m_Mode").enumValueIndex = 1; // Void
        c.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue = "UnityEngine.Object, UnityEngine";
        c.FindPropertyRelative("m_CallState").enumValueIndex = 2; // RuntimeOnly
    }

    // ------------------------------------------------------------------ logic prefab (ObjectAuthoring, no inventory)

    static GameObject BuildLogic(GameObject graphics, (long lo, long hi) graphicalAddress)
    {
        var go = new GameObject("EnderChestEntity");
        try
        {
            var iconPath = Root + "/Textures/EnderChest_icon.png";
            var all = AssetDatabase.LoadAllAssetsAtPath(iconPath);
            Debug.Log($"[EnderChestBuilder] {iconPath}: {string.Join(", ", all.Select(a => a == null ? "null" : a.GetType().Name + ":" + a.name))}");
            var icon = all.OfType<Sprite>().FirstOrDefault() ?? AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icon == null) throw new Exception("no Sprite in " + iconPath);

            Fill(Add(go, "ObjectAuthoring"), so =>
            {
                so.FindProperty("objectName").stringValue = ObjectName;
                N(so, "initialAmount", 1);
                N(so, "objectType", 800); // PlaceablePrefab
                var tags = so.FindProperty("tags");
                tags.arraySize = 1;
                tags.GetArrayElementAtIndex(0).intValue = 25; // same tag as vanilla chests
                N(so, "rarity", 3); // Epic (purple)
                N(so, "salvageMultiplier", 1f);
                so.FindProperty("graphicalPrefab").objectReferenceValue = graphics;
                SetAddress(so.FindProperty("graphicalRef"), graphicalAddress);
            });
            Fill(Add(go, "InventoryItemAuthoring"), so =>
            {
                // Buy price = round(sellValue * 5 * buyValueMultiplier) = 9999.
                N(so, "sellValue", 2000);
                N(so, "buyValueMultiplier", 0.9999f);
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("smallIcon").objectReferenceValue = icon;
                B(so, "isStackable", true);
                N(so, "craftingTime", 2f);
            });
            Fill(Add(go, "PhysicsShapeAuthoring"), so =>
            {
                N(so, "m_ShapeType", 0); // box
                V(so, "m_PrimitiveCenter", 0f, 0.5f, 0f);
                V(so, "m_PrimitiveSize", 1f, 1f, 1f);
                SetCategories(so.FindProperty("m_Material.m_BelongsToCategories.m_Value"), 0);
                SetCategories(so.FindProperty("m_Material.m_CollidesWithCategories.m_Value"), 0, 2, 4, 15);
            });
            Add(go, "MineableAuthoring");
            Fill(Add(go, "HealthAuthoring"), so =>
            {
                N(so, "startHealth", 2);
                N(so, "maxHealth", 2);
                N(so, "normalizedOverrideStartHealth", 1f);
                N(so, "maxHealthMultiplier", 1f);
                B(so, "hasHealthRegeneration", true);
                N(so, "healthIncreasePercentPerFiveSeconds", 100);
                N(so, "healDelayAfterLeavingCombat", 5f);
            });
            Fill(Add(go, "PlaceableObjectAuthoring"), so =>
            {
                V(so, "prefabTileSize", 1, 1);
                B(so, "canBePlacedOnAnyWalkableTile", true);
            });
            Add(go, "AnimationAuthoring");
            Add(go, "IgnoreVertexOffsetsAuthoring");
            Fill(Add(go, "GhostAuthoringComponent"), so =>
            {
                N(so, "DefaultGhostMode", 0);
                N(so, "SupportedGhostModes", 3);
                N(so, "OptimizationMode", 1);
                N(so, "Importance", 1);
            });
            Add(go, "StateAuthoring");
            Fill(Add(go, "IdleStateAuthoring"), so => B(so, "playIdleAnimation", true));
            Add(go, "TookDamageStateAuthoring");
            Add(go, "DeathStateAuthoring");
            Fill(Add(go, "DamageReductionAuthoring"), so =>
            {
                N(so, "reductionMultiplier", 1f);
                N(so, "maxDamagePerHit", 1);
            });
            Fill(Add(go, "RotationAuthoring"), so => V(so, "initialDirection", 0, 0, -1));
            Add(go, "LinkedEntityGroupAuthoring");
            Add(go, "LocalInteractableAuthoring");
            Add(go, "AlwaysDropOneAuthoring");
            Fill(Add(go, "LocalizationAuthoring"), so => so.FindProperty("termKey").stringValue = ObjectName);

            Directory.CreateDirectory(Path.GetDirectoryName(LogicPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, LogicPath);
            // NetCode ghost id = the prefab's own guid (what the Ghost inspector would write).
            EditPrefab(LogicPath, p => Fill(Comp(p, "GhostAuthoringComponent"),
                so => so.FindProperty("prefabId").stringValue = AssetDatabase.AssetPathToGUID(LogicPath)));
            return AssetDatabase.LoadAssetAtPath<GameObject>(LogicPath);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    static void SetCategories(SerializedProperty value, params int[] on)
    {
        for (int i = 0; i < 32; i++)
        {
            var p = value.FindPropertyRelative($"Category{i:00}");
            if (p != null) p.boolValue = on.Contains(i);
        }
    }

    // ------------------------------------------------------------------ data blocks + build

    static ScriptableObject CreateBlock(string typeName, string path, Action<SerializedObject> fill)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);
        var block = ScriptableObject.CreateInstance(FindType(typeName, typeof(ScriptableObject)));
        AssetDatabase.CreateAsset(block, path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        block = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        var so = new SerializedObject(block);
        fill(so);
        // ScriptableData addresses are the asset guid; set it if the importer did not.
        var addr = so.FindProperty("m_address");
        if (addr.FindPropertyRelative("m_low").longValue == 0 && addr.FindPropertyRelative("m_high").longValue == 0)
            SetAddress(addr, GuidAddress(AssetDatabase.AssetPathToGUID(path)));
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(block);
        AssetDatabase.SaveAssets();
        Debug.Log($"[EnderChestBuilder] {typeName} at {path}, address {ReadAddress(block)}");
        return block;
    }

    static void BuildMod()
    {
        var settings = AssetDatabase.LoadAssetAtPath<ModBuilderSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<ModBuilderSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        var meta = settings.metadata;
        meta.guid = "9e4c2a7b1f3d48c6a05e8b7d2c91f4a6"; // EnderStash's manifest guid
        meta.name = "EnderStash";
        meta.displayName = "Ender Stash";
        meta.accessesExtraAssemblies = true;
        meta.requiredOn = PugMod.ModMetadata.ModExistsOn.ClientAndServer;
        if (meta.files == null) meta.files = new System.Collections.Generic.List<PugMod.ModFile>();
        if (meta.dependencies == null) meta.dependencies = new System.Collections.Generic.List<PugMod.ModMetadata.Dependency>();
        settings.metadata = meta;
        settings.modPath = Root;
        if (settings.assets == null) settings.assets = new System.Collections.Generic.List<ModBuilderSettings.ModAsset>();
        settings.buildBundles = true;
        settings.buildLinux = true;
        settings.cacheBundles = false;
        settings.forceReimport = true;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-enderOut");
        string outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.GetFullPath("Library/EnderStashBuild");
        bool ok = false;
        PugMod.ModBuilder.BuildMod(settings, outDir, r => ok = r);
        if (!ok) throw new Exception("ModBuilder.BuildMod failed");
        Debug.Log("[EnderChestBuilder] built mod into " + outDir);
    }

    // ------------------------------------------------------------------ helpers

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
            case SerializedPropertyType.Integer: p.longValue = (long)v; break;
            case SerializedPropertyType.Boolean: p.boolValue = v != 0; break;
            default: throw new Exception($"{p.propertyPath}: unexpected {p.propertyType}");
        }
    }

    /// <summary>Vector3 / Vector2Int / float3 / int2: set the x, y, z children.</summary>
    static void V(SerializedObject so, string path, params double[] xyz)
    {
        var p = Prop(so, path);
        string[] axes = { "x", "y", "z" };
        for (int i = 0; i < xyz.Length; i++)
        {
            var c = p.FindPropertyRelative(axes[i]);
            if (c == null) throw new Exception($"{path} has no {axes[i]} ({p.propertyType})");
            SetNum(c, xyz[i]);
        }
    }

    static Type FindType(string name, Type baseType)
    {
        var matches = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); } })
            .Where(t => t.Name == name && baseType.IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
        if (matches.Count == 0) throw new Exception("type not found: " + name);
        if (matches.Count > 1) Debug.Log($"[EnderChestBuilder] {name}: {string.Join(", ", matches.Select(t => t.AssemblyQualifiedName))}; using first");
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

    static GameObject Child(GameObject parent, string name, Vector3 localPos)
    {
        var c = new GameObject(name);
        c.transform.SetParent(parent.transform, false);
        c.transform.localPosition = localPos;
        return c;
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
