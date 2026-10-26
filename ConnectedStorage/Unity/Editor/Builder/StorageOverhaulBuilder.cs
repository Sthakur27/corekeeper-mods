using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class StorageOverhaulBuilder {
    public static void Build() {
        try {
            const string root="Assets/ConnectedStorage";
            const string path=root+"/SidsStorageAssets.asset";
            var settings=AssetDatabase.LoadAssetAtPath<ModBuilderSettings>(path);
            if(settings==null){settings=ScriptableObject.CreateInstance<ModBuilderSettings>();AssetDatabase.CreateAsset(settings,path);}
            var meta=settings.metadata;
            meta.guid="5d1a0c3e9b7f4e21a8c6d4b2f0e19a73";meta.name="SidsStorageAssets";meta.displayName=meta.name;
            meta.accessesExtraAssemblies=true;meta.requiredOn=PugMod.ModMetadata.ModExistsOn.ClientAndServer;
            meta.files=new List<PugMod.ModFile>();meta.dependencies=new List<PugMod.ModMetadata.Dependency>();
            settings.metadata=meta;settings.modPath=root;settings.assets=new List<ModBuilderSettings.ModAsset>();
            settings.buildBundles=true;settings.buildLinux=true;settings.cacheBundles=false;settings.forceReimport=true;
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-storageOut");
            string output=i>=0?args[i+1]:Path.GetFullPath("Library/StorageOverhaulBuild");
            bool ok=false;PugMod.ModBuilder.BuildMod(settings,output,r=>ok=r);
            if(!ok)throw new Exception("Combined storage bundle build failed.");
            Debug.Log("[StorageOverhaulBuilder] BUILD PASS "+output);
        } catch(Exception e){Debug.LogError(e);EditorApplication.Exit(1);}
    }
}
public sealed class StorageOverhaulAssets : PugMod.IPugModBuilderProcessor {
    public void Execute(ModBuilderSettings settings,string output,List<string> paths) {
        if(settings.metadata.name!="SidsStorageAssets")return;
        foreach(string root in new[]{"Assets/EnderStash","Assets/MagicMirror","Assets/ConnectedStorageBrowser"}) {
            if(!AssetDatabase.IsValidFolder(root))throw new Exception("Missing combined bundle source: "+root);
            foreach(string path in AssetDatabase.FindAssets("t:Object",new[]{root}).Select(AssetDatabase.GUIDToAssetPath))
                if(!Directory.Exists(path)&&!path.EndsWith(".unity")&&!path.EndsWith(".cs")&&!path.EndsWith(".asmdef")&&AssetDatabase.GetMainAssetTypeAtPath(path)!=typeof(ModBuilderSettings)&&!paths.Contains(path))paths.Add(path);
        }
    }
}
