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
    private static int checks;

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
        string[] roomLayers = { "Security Room", "Lobby Room", "Cafe Room", "Storage Room", "Night Room" };
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

    [MenuItem("Night Cafe/Validate Day One Rules")]
    public static void ValidateRules()
    {
        checks = 0;
        var tuning = ScriptableObject.CreateInstance<GameTuning>();
        var session = new ShiftSession(tuning);
        Assert(session.Phase == ShiftPhase.Surveillance && session.Remaining == 180, "Initial surveillance state");
        session.Tick(60); Assert(session.Remaining == 180, "No incident timer in surveillance");
        Assert(!session.Collect(Ingredient.Milk), "No ingredient collection before incident");
        session.StartIncident(); session.Tick(10); Assert(session.Remaining == 170, "Incident timer");
        session.StartIncident(); Assert(session.Remaining == 170, "Incident cannot reset itself");
        Assert(session.Collect(Ingredient.Milk), "Ingredient collection");
        Assert(!session.Collect(Ingredient.Milk), "No duplicate inventory");
        Assert(session.TryPour() && session.RecipeStep == 1 && session.Phase == ShiftPhase.Incident, "Partial recipe is not failure");
        Assert(!session.Inventory[0] && session.SelectedSlot == -1, "Pour consumes selection");
        session.Collect(Ingredient.Almond); Assert(!session.TryPour(), "Wrong ingredient");
        Assert(session.Mistakes == 1 && session.RecipeStep == 0 && session.Remaining == 150, "Mistake resets progress and deducts time");
        session.Tick(6); Assert(session.Remaining == 139 && session.SurgeRemaining == 0, "Surge splits time accurately across its end");
        session.Collect(Ingredient.Milk); Assert(session.TryPour(), "Used ingredients can be reacquired");
        session.Collect(Ingredient.Chocolate); Assert(session.TryPour() && session.Phase == ShiftPhase.Resolved, "Correct recipe resolves");
        session.ReturnToSecurity(); Assert(session.Phase == ShiftPhase.Resolved, "Return waits for automatic cleanup");
        session.Tick(4); Assert(session.CleanupRemaining == 0 && session.Remaining == 139, "Cleanup without incident timer");
        session.ReturnToSecurity(); Assert(session.Phase == ShiftPhase.Complete, "Return completes day");
        session = new ShiftSession(tuning); session.StartIncident(); session.Collect(Ingredient.Chocolate);
        Assert(!session.TryPour() && session.RecipeStep == 0, "Wrong ingredient order");
        session.CupInspected = session.RecipeRead = true; session.Tick(999);
        Assert(session.Phase == ShiftPhase.Failed && session.Remaining == 0, "Timeout fails without negative time");
        session.RetryIncident();
        Assert(session.Phase == ShiftPhase.Incident && session.Remaining == 180 && session.RecipeStep == 0 && session.Mistakes == 0 && session.SelectedSlot == -1, "Retry resets incident state");
        Assert(session.CupInspected && session.RecipeRead, "Retry preserves read knowledge");
        Assert(Array.TrueForAll(session.Inventory, value => !value), "Retry clears inventory");
        session.Select(99); Assert(session.SelectedSlot == -1, "Invalid selection ignored");
        session.Collect(Ingredient.Milk); session.Select(0); Assert(session.SelectedSlot == -1, "Same hotkey deselects");
        session.Tick(179); session.Collect(Ingredient.Cream); session.TryPour();
        Assert(session.Phase == ShiftPhase.Failed, "Penalty can cause failure");
        UnityEngine.Object.DestroyImmediate(tuning);
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Artifacts/rule-tests.txt"));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, "PASS: " + checks + " day-one state/rule checks.\n");
        Debug.Log("NIGHT_CAFE_RULE_TESTS_PASS: " + checks);
    }

    private static void Assert(bool condition, string label)
    {
        checks++;
        if (!condition) throw new InvalidOperationException("Night Cafe validation failed: " + label);
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
