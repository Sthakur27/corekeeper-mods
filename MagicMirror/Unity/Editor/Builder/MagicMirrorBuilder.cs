using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Builds an item using only vanilla authoring components, so standalone and
// overhaul bundles never bind a MonoBehaviour to a mod's compiled assembly name.
public static class MagicMirrorBuilder
{
    const string Root = "Assets/MagicMirror";
    const string LogicPath = Root + "/Prefabs/MagicMirror.prefab";
    public static void BuildAll()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string iconPath = Root + "/Textures/MagicMirror_icon.png";
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
            var icon = AssetDatabase.LoadAllAssetsAtPath(iconPath).OfType<Sprite>().First();
            var go = new GameObject("MagicMirror");
            try
            {
                Fill(Add(go, "ObjectAuthoring"), so => {
                    so.FindProperty("objectName").stringValue = "MagicMirror_Mirror";
                    N(so,"initialAmount",1); N(so,"objectType",602); N(so,"rarity",3);
                    N(so,"salvageMultiplier",1);
                    var tags=so.FindProperty("tags"); tags.arraySize=1; tags.GetArrayElementAtIndex(0).intValue=25;
                });
                Fill(Add(go, "InventoryItemAuthoring"), so => {
                    N(so,"sellValue",-1); N(so,"buyValueMultiplier",1);
                    so.FindProperty("icon").objectReferenceValue=icon;
                    so.FindProperty("smallIcon").objectReferenceValue=icon;
                    B(so,"isStackable",false);
                    var list=so.FindProperty("requiredObjectsToCraft");list.arraySize=1;
                    list.GetArrayElementAtIndex(0).FindPropertyRelative("objectName").stringValue="SolariteBar";
                    list.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue=50;
                });
                Fill(Add(go,"CastItemAuthoring"),so=>{
                    N(so,"castTime",3);N(so,"useType",3);
                    B(so,"allowHoldToRepeat",false);
                });
                Fill(Add(go,"LocalizationAuthoring"),so=>so.FindProperty("termKey").stringValue="MagicMirror_Mirror");
                PrefabUtility.SaveAsPrefabAsset(go,LogicPath);
            }
            finally { Object.DestroyImmediate(go); }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(LogicPath);
            var block=CreateBlock("EntityAuthoringDataBlock",Root+"/Data/EntityAuthoringDataBlock/MagicMirror.asset",
                so=>so.FindProperty("prefab").objectReferenceValue=prefab);
            EditPrefab(LogicPath,p=>Fill(Comp(p,"ObjectAuthoring"),so=>SetAddress(so.FindProperty("authoringRef"),ReadAddress(block))));
            AssetDatabase.SaveAssets();
            BuildMod("MagicMirror","f24d9b6e721a4cc4a650c8de17923100");
            // Loader supports one data-block loader per mod GUID. Build one combined
            // bundle containing the Ender Chest and Mirror data for Sid's Overhaul.
            BuildMod("SidsItemAssets","5d1a0c3e9b7f4e21a8c6d4b2f0e19a73");
        }
        catch(Exception e) { Debug.LogError("[MagicMirrorBuilder] "+e); EditorApplication.Exit(1); }
    }
    static ScriptableObject CreateBlock(string typeName,string path,Action<SerializedObject> fill)
    {
        var block=AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        if(block==null) {block=ScriptableObject.CreateInstance(FindType(typeName,typeof(ScriptableObject)));AssetDatabase.CreateAsset(block,path);}
        Fill(block,so=>{fill(so);SetAddress(so.FindProperty("m_address"),GuidAddress(AssetDatabase.AssetPathToGUID(path)));});
        EditorUtility.SetDirty(block);AssetDatabase.SaveAssets();return block;
    }
    static void BuildMod(string name,string guid)
    {
        string path=Root+"/"+name+".asset";
        var settings=AssetDatabase.LoadAssetAtPath<ModBuilderSettings>(path);
        if(settings==null) {settings=ScriptableObject.CreateInstance<ModBuilderSettings>();AssetDatabase.CreateAsset(settings,path);}
        var meta=settings.metadata;
        meta.guid=guid;meta.name=name;meta.displayName=name;
        meta.accessesExtraAssemblies=true;meta.requiredOn=PugMod.ModMetadata.ModExistsOn.ClientAndServer;
        meta.files=new List<PugMod.ModFile>();meta.dependencies=new List<PugMod.ModMetadata.Dependency>();
        settings.metadata=meta;settings.modPath=Root;settings.assets=new List<ModBuilderSettings.ModAsset>();
        settings.buildBundles=true;settings.buildLinux=true;settings.cacheBundles=false;settings.forceReimport=true;
        EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-mirrorOut");
        string output=i>=0?args[i+1]:Path.GetFullPath("Library/MagicMirrorBuild");
        bool ok=false;PugMod.ModBuilder.BuildMod(settings,output,r=>ok=r);
        if(!ok)throw new Exception("Build failed: "+name);
        Debug.Log("[MagicMirrorBuilder] Built "+name+" into "+output);
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

public sealed class MirrorOverhaulAssets : PugMod.IPugModBuilderProcessor
{
    public void Execute(ModBuilderSettings settings, string output, List<string> paths)
    {
        if(settings.metadata.name!="SidsItemAssets")return;
        const string ender="Assets/EnderStash";
        if(!AssetDatabase.IsValidFolder(ender))throw new Exception("Build EnderStash source assets before the combined overhaul bundle.");
        var assets=AssetDatabase.FindAssets("t:Object",new[]{ender}).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p=>!Directory.Exists(p) && !p.EndsWith(".cs") && !p.EndsWith(".asmdef") && AssetDatabase.GetMainAssetTypeAtPath(p)!=typeof(ModBuilderSettings));
        foreach(var asset in assets)if(!paths.Contains(asset))paths.Add(asset);
    }
}
