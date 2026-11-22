using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal sealed class NightCafeWorkspace : EditorWindow
{
    [MenuItem("Night Cafe/편집 도구", priority = 0)]
    private static void Open() => GetWindow<NightCafeWorkspace>("Night Cafe");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("씬", EditorStyles.boldLabel);
        SceneButton("시작 화면", "Lobby");
        SceneButton("게임 씬", "InGame");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("프리팹 · 설정", EditorStyles.boldLabel);
        AssetButton("방과 가구", "Assets/Prefabs/CafeWorld.prefab", true);
        AssetButton("플레이어", "Assets/Prefabs/NightPlayer.prefab", true);
        AssetButton("시작 UI", "Assets/Prefabs/StartScreen.prefab", true);
        AssetButton("HUD와 팝업", "Assets/Prefabs/NightGameUI.prefab", true);
        AssetButton("이동 속도 · 밤별 시간 · 암호", "Assets/Data/DayOneTuning.asset");
        AssetButton("픽셀 아트 폴더", "Assets/Art/NightCafe");
        AssetButton("컵 애니메이션 프레임", "Assets/Art/NightCafe/FloatingCup/floating-cup.png");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("게임 코드", EditorStyles.boldLabel);
        ScriptButton("플레이어 조작", "Character/NightPlayer.cs");
        ScriptButton("1일차 레시피와 범람", "Gameplay/Rules/ShiftSession.cs");
        ScriptButton("2–3일차 사건", "Gameplay/Rules/LaterShift.cs");
        ScriptButton("4–5일차 사건", "Gameplay/Rules/FinalNights.cs");
        ScriptButton("밤 전환 · 흐름", "Gameplay/Campaign/NightGame.Campaign.cs");
        ScriptButton("문 연결 · 복도", "World/NightRoutes.cs");
        ScriptButton("UI 연결", "Presentation/NightGameView.cs");
    }

    private static void SceneButton(string label, string scene)
    {
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button(label) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene($"Assets/Scenes/{scene}.unity");
        }
    }

    private static void ScriptButton(string label, string relativePath)
        => AssetButton(label, Path.Combine("Assets/Scripts", relativePath).Replace('\\', '/'), true);

    private static void AssetButton(string label, string path, bool open = false)
    {
        if (!GUILayout.Button(label)) return;
        var asset = AssetDatabase.LoadMainAssetAtPath(path);
        if (asset == null) return;
        if (open) AssetDatabase.OpenAsset(asset);
        else
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
