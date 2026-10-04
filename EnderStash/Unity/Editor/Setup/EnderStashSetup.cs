using UnityEditor;
using UnityEngine;

/// <summary>Batch-mode step 1: import the game assemblies into the SDK project and enable USE_PUG_OTHER.</summary>
public static class EnderStashSetup
{
    public const string GamePath = @"C:\Program Files (x86)\Steam\steamapps\common\Core Keeper";

    public static void ImportGame()
    {
        PugMod.ImporterWindow.UpdateFromGamePath(PugMod.ImporterSettings.Instance, GamePath, out var added);
        var target = UnityEditor.Build.NamedBuildTarget.Standalone;
        var defines = PlayerSettings.GetScriptingDefineSymbols(target);
        if (!defines.Contains("USE_PUG_OTHER"))
            PlayerSettings.SetScriptingDefineSymbols(target, defines + ";USE_PUG_OTHER");
        AssetDatabase.SaveAssets();
        Debug.Log($"[EnderStashSetup] imported {added.Count} game assemblies");
    }
}
