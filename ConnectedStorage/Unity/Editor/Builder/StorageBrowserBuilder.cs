using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Imports the MIT-licensed authored Item Browser layout, removing catalogue
// behaviours and persistent events. The output references only game components.
public static class StorageBrowserBuilder
{
    const string Root = "Assets/ConnectedStorageBrowser";
    static Type TypeNamed(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name,false)).First(t=>t!=null);
    static object Field(object target,string name) => target.GetType().GetField(name).GetValue(target);
    static Component Find(GameObject root,string name) => root.GetComponentsInChildren<Component>(true)
        .First(c => c != null && c.GetType().Name == name);
    static void CopyFields(Component from, Component to)
    {
        var src=new SerializedObject(from); var dst=new SerializedObject(to);
        var prop=src.GetIterator();
        if(prop.NextVisible(true)) do {
            if(prop.name != "m_Script" && dst.FindProperty(prop.propertyPath) != null)
                dst.CopyFromSerializedProperty(prop);
        } while(prop.NextVisible(false));
        dst.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Replace(Component old, string type)
    {
        var next=old.gameObject.AddComponent(TypeNamed(type));
        CopyFields(old,next); Object.DestroyImmediate(old);
        var serialized=new SerializedObject(next);
        foreach(var eventName in new[]{"onLeftClick","onRightClick","onInputFieldDone"}) {
            var calls=serialized.FindProperty(eventName+".m_PersistentCalls.m_Calls");
            if(calls != null) calls.ClearArray();
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void Build()
    {
        Directory.CreateDirectory(Root);
        // Imported game DLL GUIDs differ between SDK installations. Script local
        // IDs are stable; resolve those references against this SDK's DLLs.
        var scripts=new Dictionary<long,string>();
        foreach(string dll in Directory.GetFiles("Assets/Plugins","*.dll",SearchOption.AllDirectories))
            foreach(var script in AssetDatabase.LoadAllAssetsAtPath(dll.Replace('\\','/')).OfType<MonoScript>())
                if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script,out string guid,out long id)) scripts[id]=guid;
        string upstream="Assets/ItemBrowserPackage/Prefabs/Browser/ItemBrowserUI.prefab";
        string yaml=File.ReadAllText(upstream);
        yaml=Regex.Replace(yaml,@"m_Script: \{fileID: (-?\d+), guid: ([0-9a-f]+), type: 3\}",m=>{
            long id=long.Parse(m.Groups[1].Value);
            return id!=11500000 && scripts.TryGetValue(id,out var guid)
                ? "m_Script: {fileID: "+id+", guid: "+guid+", type: 3}" : m.Value;
        });
        File.WriteAllText(upstream,yaml);
        AssetDatabase.ImportAsset(upstream,ImportAssetOptions.ForceSynchronousImport);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ItemBrowserPackage/Prefabs/Browser/ItemBrowserUI.prefab");
        var sourceView=Find(source,"ItemsListView");
        var root=Object.Instantiate(sourceView.gameObject);
        root.name="ConnectedStorageBrowser"; root.SetActive(false);
        var view=Find(root,"ItemsListView");
        var list=(Component)Field(view,"objectList"); list.gameObject.name="StorageGrid";
        var size=(Vector2Int)Field(list,"size");
        var metrics=new GameObject("StorageMetrics"); metrics.transform.SetParent(root.transform,false);
        metrics.transform.localPosition=new Vector3(size.x,size.y,(float)Field(list,"spread"));
        metrics.SetActive(false);
        var search=(Component)Field(view,"searchInput"); search.gameObject.name="StorageSearch";
        foreach(var pair in new[]{new[]{"gridLayoutItemPrefab","StorageGridSlot"},new[]{"listLayoutItemPrefab","StorageListSlot"}}) {
            var template=(Component)Field(list,pair[0]);
            var clone=Object.Instantiate(template.gameObject,root.transform);
            clone.name=pair[1]; clone.SetActive(false);
        }
        foreach(var c in root.GetComponentsInChildren<Component>(true).ToArray()) {
            if(c==null || c.GetType().Assembly.GetName().Name!="ItemBrowser") continue;
            string name=c.GetType().Name;
            if(name=="SearchBar") Replace(c,"TextInputField");
            else if(name=="VirtualObjectListSlot") Replace(c,"SlotUIBase");
            else if(TypeNamed("ButtonUIElement").IsAssignableFrom(c.GetType())) {
                if(name=="LayoutButton") c.gameObject.name="StorageLayout";
                if(name=="SortButton") c.gameObject.name="StorageSort";
                if(name=="SortOrderButton") c.gameObject.name="StorageOrder";
                if(name=="ToggleFiltersButton") c.gameObject.name="StorageFilters";
                Replace(c,"ButtonUIElement");
            }
            else Object.DestroyImmediate(c);
        }
        // Remove any references that could execute upstream callbacks.
        foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true)) {
            if(c==null) continue;
            if(c.GetType().Assembly.GetName().Name=="ItemBrowser") throw new Exception("Upstream behaviour remains");
        }
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        var path=Root+"/ConnectedStorageBrowser.prefab";
        PrefabUtility.SaveAsPrefabAsset(root,path);
        File.WriteAllLines(Root+"/hierarchy.txt",root.GetComponentsInChildren<Transform>(true).Select(t=>
            t.name+" pos="+t.localPosition+" active="+t.gameObject.activeSelf));
        Object.DestroyImmediate(root); AssetDatabase.SaveAssets();
        string output=Path.GetFullPath("Library/ConnectedStorageBrowserBuild"); Directory.CreateDirectory(output);
        foreach(var target in new[]{BuildTarget.StandaloneWindows64,BuildTarget.StandaloneLinux64}) {
            string name=target==BuildTarget.StandaloneWindows64?"ConnectedStorageBrowser_Windows.assetbundle":"ConnectedStorageBrowser_Linux.assetbundle";
            var result=BuildPipeline.BuildAssetBundles(output,new[]{new AssetBundleBuild {assetBundleName=name,assetNames=new[]{path}}},BuildAssetBundleOptions.ChunkBasedCompression,target);
            if(result==null) throw new Exception("Browser bundle build failed: "+target);
        }
        Debug.Log("[StorageBrowserBuilder] Exported browser visuals to "+output);
    }
}


