using System;
using System.IO;
using NightCafe;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class NightCafeBuild
{
    private const string Lobby = "Assets/Scenes/Lobby.unity";
    private const string InGame = "Assets/Scenes/InGame.unity";

    // One-time bootstrap, also callable using Unity -batchmode -executeMethod.
    public static void PrepareAndBuild()
    {
        PrepareScenes();
        ValidateRules();
        BuildWindows();
    }

    [MenuItem("Night Cafe/Rebuild Foundation Scenes")]
    public static void PrepareScenes()
    {
        CafeSceneAuthoring.RebuildScenes();
        ConfigureWindows();
    }

    private static void ConfigureWindows()
    {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        string[] roomLayers = CafeWorld.RoomNames;
        for (int i = 0; i < roomLayers.Length; i++) layers.GetArrayElementAtIndex(8 + i).stringValue = roomLayers[i];
        tags.ApplyModifiedPropertiesWithoutUndo();
        PlayerSettings.companyName = "University Project";
        PlayerSettings.productName = "Night Cafe";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = false;
        PlayerSettings.allowFullscreenSwitch = false;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
        // The flashlight material is created at runtime, so retain its shader explicitly.
        var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
        var spriteShader = Shader.Find("Sprites/Default");
        bool found = false;
        for (int i = 0; i < shaders.arraySize; i++) if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == spriteShader) found = true;
        if (!found)
        {
            shaders.InsertArrayElementAtIndex(shaders.arraySize);
            shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = spriteShader;
            graphics.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    [MenuItem("Night Cafe/Validate All Five Nights")]
    public static void ValidateRules()
    {
        UpdatedDesignValidation.Run();
    }

    [MenuItem("Night Cafe/Build Windows FHD")]
    public static void BuildWindows()
    {
        ConfigureWindows();
        var tuning = AssetDatabase.LoadAssetAtPath<GameTuning>("Assets/Data/DayOneTuning.asset");
        if (tuning == null || tuning.recipe == null || tuning.recipe.Length == 0) throw new InvalidOperationException("Day-one tuning needs a recipe.");
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Windows/NightCafe.exe"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { Lobby, InGame }, locationPathName = output,
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + report.summary.result);
        Debug.Log("NIGHT_CAFE_BUILD_PASS: " + output);
    }
}
