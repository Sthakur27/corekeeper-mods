using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StorageJoinPreviewBuilder
{
    public static void Build()
    {
        const string root = "Assets/RunicStorageJoinPreview";
        try
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in Directory.GetFiles(root + "/Textures", "*.png"))
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                    if (importer.textureShape == TextureImporterShape.Texture2D &&
                        importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                        importer.textureCompression == TextureImporterCompression.Uncompressed)
                        continue;
                    importer.textureShape = TextureImporterShape.Texture2D;
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            var settings = ScriptableObject.CreateInstance<ModBuilderSettings>();
            var metadata = settings.metadata;
            metadata.name = "RunicStorageJoinPreview";
            metadata.displayName = "Runic Storage Join Preview";
            metadata.guid = "de65c4091f3b403385ed737f8e3b930f";
            metadata.files = new List<PugMod.ModFile>();
            metadata.dependencies = new List<PugMod.ModMetadata.Dependency>();
            settings.metadata = metadata;
            settings.modPath = root;
            settings.assets = new List<ModBuilderSettings.ModAsset>();
            settings.buildBundles = true;
            settings.cacheBundles = false;
            settings.forceReimport = true;

            var args = Environment.GetCommandLineArgs();
            string output = args[Array.IndexOf(args, "-storageOut") + 1];
            bool ok = false;
            PugMod.ModBuilder.BuildMod(settings, output, result => ok = result);
            if (!ok) throw new InvalidOperationException("Preview bundle build failed.");
            Debug.Log("[RunicJoinPreview] Preview bundle built in " + output);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
