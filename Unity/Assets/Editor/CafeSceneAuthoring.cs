using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NightCafe;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Optional authoring utility. The saved scenes and prefabs run without this script.
public static class CafeSceneAuthoring
{
    private const string Art = "Assets/Art/NightCafe/";
    private const string Prefabs = "Assets/Prefabs/";
    public static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");

    [MenuItem("Night Cafe/Rebuild Authored Scenes")]
    public static void RebuildScenes()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding scenes.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes before rebuilding.");
        ImportSprites();
        Directory.CreateDirectory(Prefabs);
        Directory.CreateDirectory("Assets/Data");
        Directory.CreateDirectory("Assets/Scenes");
        const string tuningPath = "Assets/Data/DayOneTuning.asset";
        var tuning = AssetDatabase.LoadAssetAtPath<GameTuning>(tuningPath);
        if (tuning == null) { tuning = ScriptableObject.CreateInstance<GameTuning>(); AssetDatabase.CreateAsset(tuning, tuningPath); }
        string backup = Path.GetFullPath("../Artifacts/SceneBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(backup);
        foreach (string file in new[] { "Lobby", "InGame" })
            if (File.Exists("Assets/Scenes/" + file + ".unity")) File.Copy("Assets/Scenes/" + file + ".unity", Path.Combine(backup, file + ".unity"));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = MakeCamera("Start Camera", 13, new Rect(0, 0, 1, 1), true);
        BuildStart();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Lobby.unity");
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var world = BuildWorld();
        var hud = BuildHud();
        var game = new GameObject("Night Shift Controller").AddComponent<NightGame>();
        game.tuning = tuning; game.world = world; game.view = hud;
        var cameras = new GameObject("Cameras").transform;
        // URP preserves pixels outside a partial viewport. Clear the full display
        // first so closing an overlay cannot leave its last frame in those pixels.
        var background = MakeCamera("Screen Background Camera", 5, new Rect(0, 0, 1, 1), false);
        background.transform.SetParent(cameras);
        background.cullingMask = 0; background.depth = -1;
        background.allowHDR = false; background.allowMSAA = false;
        game.mapCamera = MakeCamera("Map Camera", 5, new Rect(0.01f, 0.13f, 0.775f, 0.75f), true);
        game.mapCamera.transform.SetParent(cameras);
        game.mapCamera.transform.position = new Vector3(world.Player.transform.position.x, world.Player.transform.position.y, -30);
        game.mapCamera.gameObject.AddComponent<PlayerFollowCamera>().world = world;
        game.mapCamera.cullingMask = CafeWorld.RoomMask(CafeWorld.SecuritySpawn);
        game.feedCamera = MakeCamera("CCTV Camera", 7, new Rect(0, 0, 1, 1), false);
        game.feedCamera.depth = -2;
        game.feedCamera.transform.SetParent(cameras); game.feedCamera.enabled = false;
        game.sound = game.gameObject.AddComponent<AudioSource>(); game.sound.volume = 0.2f; game.sound.playOnAwake = false;
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/InGame.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Lobby.unity", true), new EditorBuildSettingsScene("Assets/Scenes/InGame.unity", true) };
        AssetDatabase.SaveAssets();
        ValidateScene(world, hud);
        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        Debug.Log("NIGHT_CAFE_AUTHORING_PASS: scenes, art sprites and prefabs serialized.");
    }

    private static void ImportSprites()
    {
        AssetDatabase.Refresh();
        foreach (string file in Directory.GetFiles(Art, "*.png"))
        {
            string path = file.Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64;
            importer.spriteBorder = Path.GetFileNameWithoutExtension(path) is "panel" or "button" or "button-selected" or "paper" ? new Vector4(6,6,6,6) : Vector4.zero;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }
    }

    private static Camera MakeCamera(string name, float size, Rect viewport, bool listener)
    {
        var camera = new GameObject(name, typeof(Camera)).GetComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = size;
        camera.transform.position = new Vector3(0, 0, -30);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = CafeUi.Ink; camera.rect = viewport;
        if (listener) { camera.gameObject.AddComponent<AudioListener>(); camera.tag = "MainCamera"; }
        return camera;
    }

    private static GameObject SavePrefab(GameObject root, string name)
    {
        string path = Prefabs + name + ".prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
        return root;
    }

    private static Image ArtImage(Transform parent, string name, string asset, float x, float y, float width, float height)
    {
        var image = CafeUiAuthoring.Panel(parent, name, x, y, width, height, Color.white);
        image.sprite = Sprite(asset);
        return image;
    }

    private static void StyleUi(Transform root)
    {
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite != null || image.rectTransform.sizeDelta.y < 10 || image.name.EndsWith("Overlay") || image.name.Contains("Flood")) continue;
            image.sprite = Sprite(image.GetComponent<Button>() != null ? "button" : "panel");
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            if (image.GetComponent<Button>() != null)
            {
                var style = image.gameObject.AddComponent<CafeButtonStyle>();
                style.idleSprite = Sprite("button"); style.selectedSprite = Sprite("button-selected");
            }
        }
        root.GetComponent<CafeFont>().overrideFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/NanumGothic-Regular.ttf");
        root.GetComponent<CafeFont>().Refresh();
    }

    private static Transform Overlay(Transform root, string name, float width, float height, out GameObject panel)
    {
        var shade = CafeUiAuthoring.Panel(root, name + " Overlay", 0, 0, 1920, 1080, new Color(0.015f,0.03f,0.04f,0.9f));
        shade.raycastTarget = true; panel = shade.gameObject;
        return CafeUiAuthoring.Panel(shade.transform, name + " Panel", (1920-width)/2, (1080-height)/2, width, height, Color.white).transform;
    }

    private static void OrganizeHud(Transform root)
    {
        var children = root.Cast<Transform>().ToArray();
        var header = CafeUiAuthoring.Rect(root, "Header", 0, 0, 1920, 1080);
        var inventory = CafeUiAuthoring.Rect(root, "Inventory", 0, 0, 1920, 1080);
        var interaction = CafeUiAuthoring.Rect(root, "Interaction Feedback", 0, 0, 1920, 1080);
        var overlays = CafeUiAuthoring.Rect(root, "Overlays", 0, 0, 1920, 1080);
        foreach (var child in children)
        {
            var rect = (RectTransform)child;
            Transform parent;
            if (child.name.EndsWith("Overlay")) parent = overlays;
            else if (child.name == "Shift record") { child.name = "Shift Record"; continue; }
            else if (rect.anchoredPosition.y <= -957) parent = inventory;
            else if (child.name == "Flood tint" || rect.anchoredPosition.y <= -830) parent = interaction;
            else parent = header;
            rect.SetParent(parent, false);
        }
        overlays.SetAsLastSibling();
    }

    private static CafeWorld BuildWorld()
    {
        var source = new CafeWorldAuthoring(true);
        var world = source.Root.gameObject.AddComponent<CafeWorld>();
        world.Cup = source.Cup; world.Flood = source.Flood; world.CoffeeDrops = source.CoffeeDrops;
        world.Player = source.Player; world.Interactables = source.Interactables;
        world.Player.transform.SetParent(world.transform, true);
        world.gameObject.AddComponent<CafeFont>();
        var groups = new Dictionary<int, Transform>();
        foreach (int layer in new[] { 0, 8, 9, 10, 11, 12 })
        {
            string name = layer == 0 ? "Floor Plan" : new[] { "Security Room", "Lobby", "Cafe Hall", "Storage", "Night Room" }[layer-8];
            var group = new GameObject(name).transform; group.SetParent(world.transform, false); groups[layer] = group;
        }
        var renderers = world.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var renderer in renderers)
        {
            string name = renderer.name;
            string key = null;
            if (name is "SECURITY" or "LOBBY" or "STORAGE" or "NIGHT ROOM") key = "floor-tile";
            if (name == "CAFE") key = "floor-wood";
            if (name == "Wall") key = "wall";
            if (name is "Bar counter" or "Back bar") key = "counter";
            if (name == "Security desk") key = "desk";
            if (name == "Cafe table") key = "table";
            if (name == "Chair") key = "chair";
            if (name == "Storage shelf") key = "shelf";
            if (name == "Bed") key = "bed";
            if (name == "Plant") key = "plant";
            if (name == "Player") key = "player-body";
            if (name == "Head") key = "player-head";
            var item = renderer.GetComponent<CafeInteractable>();
            if (item != null)
            {
                key = item.kind switch {
                    InteractionKind.Camera => "monitor", InteractionKind.Note => "note", InteractionKind.RuleBook => "book",
                    InteractionKind.Recipe => "recipe", InteractionKind.Cup => "cup", InteractionKind.Door => "door",
                    InteractionKind.Ingredient => new[] { "milk","chocolate","almond","tapioca","cream" }[(int)item.ingredient],
                    _ => "book" };
                if (item.kind == InteractionKind.Cup)
                    foreach (Transform child in item.transform) child.gameObject.SetActive(false);
                if (item.kind == InteractionKind.Door && renderer.size.y > renderer.size.x)
                {
                    var texture = new GameObject("Door artwork").AddComponent<SpriteRenderer>();
                    texture.transform.SetParent(item.transform, false);
                    texture.sprite = Sprite("door"); texture.drawMode = SpriteDrawMode.Sliced;
                    texture.size = new Vector2(renderer.size.y, renderer.size.x);
                    texture.transform.localRotation = Quaternion.Euler(0,0,90);
                    texture.sortingOrder = renderer.sortingOrder;
                    renderer.enabled = false;
                    item.ConfigureMarker(texture);
                    continue;
                }
            }
            if (key != null)
            {
                var size = renderer.size;
                renderer.sprite = Sprite(key); renderer.color = Color.white;
                renderer.drawMode = key.StartsWith("floor-") ? SpriteDrawMode.Tiled : SpriteDrawMode.Sliced;
                renderer.size = size;
                if (item != null) item.ConfigureMarker(renderer);
            }
            if (name is "Table runner" or "Planter" or "Edge" or "Floor seam") renderer.gameObject.SetActive(false);
        }
        // Serialize the player and common furniture as reusable nested prefabs.
        SavePrefab(world.Player.gameObject, "NightPlayer");
        var table = renderers.First(r => r.name == "Cafe table");
        SavePrefab(table.gameObject, "CafeTable");
        foreach (Transform child in world.transform.Cast<Transform>().ToArray())
        {
            if (groups.ContainsValue(child) || child == world.Player.transform) continue;
            int layer = child.gameObject.layer;
            child.SetParent(groups[groups.ContainsKey(layer) ? layer : 0], true);
        }
        world.GetComponent<CafeFont>().overrideFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/NanumGothic-Regular.ttf");
        world.GetComponent<CafeFont>().Refresh();
        SavePrefab(world.gameObject, "CafeWorld");
        return world;
    }

    private static void ValidateScene(CafeWorld world, NightGameView view)
    {
        if (world.Player == null || world.Cup == null || world.Flood == null || world.Interactables.Count < 15)
            throw new InvalidOperationException("World references are incomplete.");
        if (view.inventoryButtons.Length != 5 || view.cameraButtons.Length != 4 || view.completeBody == null)
            throw new InvalidOperationException("UI references are incomplete.");
        foreach (var text in view.GetComponentsInChildren<Text>(true))
            if (text.font == null) throw new InvalidOperationException("Missing font: " + text.name);
    }

    private static void BuildStart()
    {
            var root = CafeUiAuthoring.Canvas("Start Screen UI").transform;
            root.gameObject.AddComponent<CafeFont>();
            var start = root.gameObject.AddComponent<StartScreen>();
            ArtImage(root, "Cafe illustration", "title-cafe", 0, 0, 1920, 1080);
            CafeUiAuthoring.Panel(root, "Menu background", 0, 0, 760, 1080, new Color(0.047f,0.086f,0.102f,0.3f));
            CafeUiAuthoring.Label(root, "NIGHT CAFÉ  /  FIELD NOTES", 86, 82, 650, 40, 23, CafeUi.Amber);
            CafeUiAuthoring.Label(root, "이상현상\n퇴마 기록", 80, 196, 700, 222, 80);
            CafeUiAuthoring.Label(root, "밤이 깊어지면, 규칙을 지키세요.", 87, 460, 650, 50, 29, CafeUi.Muted);
            CafeUiAuthoring.Label(root, "카메라로 관찰하고, 현장에서 답을 찾으세요.\n야간 근무 첫날 — 끝없이 쏟아지는 아메리카노", 87, 542, 650, 96, 23, CafeUi.Muted);
            start.buttons = new[] {
                CafeUiAuthoring.Button(root, "01     근무 시작", 87, 694, 590, 82, null),
                CafeUiAuthoring.Button(root, "02     조작 안내", 87, 796, 590, 70, null),
                CafeUiAuthoring.Button(root, "03     게임 종료", 87, 884, 590, 70, null) };
            CafeUiAuthoring.Label(root, "W/S 선택    Space / Enter 실행", 87, 1002, 650, 40, 19, CafeUi.Muted);
            CafeUiAuthoring.Label(root, "CCTV NETWORK", 860, 104, 880, 46, 24, CafeUi.Green);
            CafeUiAuthoring.Label(root, "●  야간 모니터링", 1420, 104, 400, 46, 24, CafeUi.Amber, TextAnchor.UpperRight);
            CafeUiAuthoring.Panel(root, "Frame top", 828, 173, 1015, 2, CafeUi.Muted);
            CafeUiAuthoring.Panel(root, "Frame bottom", 828, 881, 1015, 2, CafeUi.Muted);
            CafeUiAuthoring.Label(root, "01  /  카페 · 첫 번째 밤", 860, 914, 920, 60, 28);
            CafeUiAuthoring.Label(root, "FIELD RECORD  001     /     NIGHT CAFÉ", 860, 991, 950, 42, 20, CafeUi.Muted);

            var shade = CafeUiAuthoring.Panel(root, "Help", 0, 0, 1920, 1080, new Color(0.02f, 0.04f, 0.05f, 0.96f));
            shade.raycastTarget = true; start.helpPanel = shade.gameObject;
            CafeUiAuthoring.Label(shade.transform, "근무 조작 안내", 400, 175, 1120, 80, 46, CafeUi.Amber);
            CafeUiAuthoring.Label(shade.transform, "WASD / 방향키     이동 · CCTV 카메라 전환\nSpace                    조사 · 상호작용 · 선택\nShift                      달리기\nF                            손전등 켜기 / 끄기\nTab                        규칙서 열기 / 닫기\n1–5                        소지품 선택 / 선택 해제\nEsc                        뒤로 · 일시정지\n마우스                   CCTV · 메뉴 · 소지품 선택", 400, 300, 1120, 430, 29);
            CafeUiAuthoring.Label(shade.transform, "규칙서와 메모를 읽는 동안에도 시간은 흐릅니다.\nEsc 일시정지 메뉴에서는 시간이 멈춥니다.", 400, 762, 1120, 90, 24, CafeUi.Muted);
            start.closeHelpButton = CafeUiAuthoring.Button(shade.transform, "돌아가기  [Space / Esc]", 1060, 890, 460, 74, null);

            start.helpPanel.SetActive(false);
            StyleUi(root);
            start.buttons[0].GetComponent<CafeButtonStyle>().Select(true);
            SavePrefab(root.gameObject, "StartScreen");
    }

    private static NightGameView BuildHud()
    {

            var canvas = CafeUiAuthoring.Canvas("Night Shift UI");
            var view = canvas.gameObject.AddComponent<NightGameView>();
            canvas.gameObject.AddComponent<CafeFont>();
            view.inventorySlots = new Image[5]; view.inventoryLabels = new Text[5]; view.inventoryButtons = new Button[5];
            var root = canvas.transform;
            CafeUiAuthoring.Panel(root, "Header", 0, 0, 1920, 108, CafeUi.Ink);
            CafeUiAuthoring.Label(root, "NIGHT SHIFT  /  01", 48, 22, 500, 38, 28, CafeUi.Amber);
            CafeUiAuthoring.Label(root, "이상현상 관찰 기록", 48, 65, 500, 28, 18, CafeUi.Muted);
            view.timer = CafeUiAuthoring.Label(root, "", 740, 15, 440, 55, 42, CafeUi.Amber, TextAnchor.MiddleCenter);
            view.timerLine = CafeUiAuthoring.Panel(root, "Timer line", 740, 83, 440, 3, CafeUi.Amber);
            view.location = CafeUiAuthoring.Label(root, "감시실", 1250, 40, 200, 40, 24, CafeUi.Paper, TextAnchor.MiddleRight);
            view.pauseButton = CafeUiAuthoring.Button(root, "일시정지  [Esc]", 1512, 24, 360, 60, null);

            var sidebar = CafeUiAuthoring.Panel(root, "Shift record", 1512, 130, 360, 746, new Color32(23, 37, 41, 255)).transform;
            CafeUiAuthoring.Label(sidebar, "야간 근무", 28, 28, 304, 32, 20, CafeUi.Amber);
            CafeUiAuthoring.Label(sidebar, "첫 번째 밤", 28, 72, 304, 60, 36);
            CafeUiAuthoring.Panel(sidebar, "Divider", 28, 151, 304, 1, new Color32(68, 88, 84, 255));
            CafeUiAuthoring.Label(sidebar, "현재 할 일", 28, 177, 304, 32, 19, CafeUi.Muted);
            view.objective = CafeUiAuthoring.Label(sidebar, "", 28, 223, 304, 122, 27);
            view.status = CafeUiAuthoring.Label(sidebar, "", 28, 370, 304, 90, 20, CafeUi.Green);
            view.progress = CafeUiAuthoring.Label(sidebar, "", 28, 473, 304, 88, 20, CafeUi.Muted);
            view.rulesButton = CafeUiAuthoring.Button(sidebar, "규칙서 열기  [Tab]", 24, 580, 312, 64, null);
            CafeUiAuthoring.Label(sidebar, "WASD 이동 · Shift 달리기\nSpace 조사 · F 손전등\n1–5 재료 선택 / 선택 해제", 28, 666, 308, 75, 17, CafeUi.Muted);

            view.floodWarning = CafeUiAuthoring.Panel(root, "Flood tint", 0, 108, 1490, 825, new Color(0.6f, 0.2f, 0.04f, 0));
            view.prompt = CafeUiAuthoring.Label(root, "", 240, 842, 1020, 50, 25, CafeUi.Amber, TextAnchor.MiddleCenter);
            view.toast = CafeUiAuthoring.Label(root, "", 60, 895, 1400, 48, 22, CafeUi.Paper, TextAnchor.MiddleCenter);
            CafeUiAuthoring.Panel(root, "Inventory bar", 0, 957, 1920, 123, CafeUi.Ink);
            CafeUiAuthoring.Label(root, "소지품", 48, 995, 130, 44, 24, CafeUi.Muted);
            for (int i = 0; i < 5; i++)
            {
                int slot = i;
                var button = CafeUiAuthoring.Button(root, "", 214 + i * 248, 978, 230, 78, null);
                view.inventoryButtons[i] = button;
                view.inventorySlots[i] = button.GetComponent<Image>();
                view.inventoryLabels[i] = CafeUiAuthoring.Label(button.transform, "", 18, 0, 200, 78, 22, null, TextAnchor.MiddleLeft);
            }
            CafeUiAuthoring.Label(root, "선택 후 컵 앞에서 Space\n같은 번호를 누르면 조사 모드", 1512, 986, 370, 64, 19, CafeUi.Muted);

            view.rules = BuildRules(root, view);
            view.reading = BuildReading(root, view);
            view.cctv = BuildCCTV(root, view);
            view.pause = BuildPause(root, view);
            view.failure = BuildResult(root, view, false);
            view.complete = BuildResult(root, view, true);
            view.closeButtons = new[] { view.rules, view.reading, view.cctv }.Select(panel => panel.GetComponentInChildren<Button>(true)).ToArray();
            // CCTV camera selectors precede its close button in hierarchy order.
            view.closeButtons[2] = view.cctv.GetComponentsInChildren<Button>(true).Last();
            for (int i = 0; i < 5; i++)
            {
                var button = view.inventoryButtons[i]; button.name = "Inventory Slot " + (i+1);
                var icon = ArtImage(button.transform, "Ingredient artwork", new[] { "milk","chocolate","almond","tapioca","cream" }[i], 10, 12, 38, 52);
                icon.preserveAspect = true;
                view.inventoryLabels[i].rectTransform.anchoredPosition = new Vector2(56,0);
                view.inventoryLabels[i].rectTransform.sizeDelta = new Vector2(170,78);
                view.inventoryLabels[i].fontSize = 20;
                view.inventoryLabels[i].text = (i+1) + "  " + GameTuning.IngredientName((Ingredient)i) + "  —";
            }
            view.objective.text = "CCTV를 확인하고\n카페를 둘러보세요.";
            view.status.text = "카메라 대기 중";
            view.progress.text = "○ CCTV 확인\n○ 컵 조사\n○ 레시피 열람";
            view.timer.text = "03:00";
            StyleUi(root);
            OrganizeHud(root);
            SavePrefab(canvas.gameObject, "NightGameUI");
            return view;
    }
        private static GameObject BuildRules(Transform root, NightGameView view)
        {
            var body = Overlay(root, "Rules", 1060, 800, out var panel);
            CafeUiAuthoring.Label(body, "STAFF ONLY  /  01", 64, 48, 900, 35, 21, CafeUi.Amber);
            CafeUiAuthoring.Label(body, "야간 근무 규칙서", 64, 110, 900, 65, 42);
            CafeUiAuthoring.Panel(body, "Rule divider", 64, 210, 932, 1, CafeUi.Muted);
            CafeUiAuthoring.Label(body, "01", 64, 260, 100, 50, 28, CafeUi.Amber);
            CafeUiAuthoring.Label(body, "아메리카노는 너무 써서 하늘을 난다.", 150, 260, 800, 70, 30);
            CafeUiAuthoring.Label(body, "02", 64, 380, 100, 50, 28, CafeUi.Amber);
            CafeUiAuthoring.Label(body, "카페모카는 너무 끈적해서 목이 막힌다.", 150, 380, 800, 70, 30);
            CafeUiAuthoring.Label(body, "관찰한 것을 기억할 것.\n레시피는 카페에 보관한다.", 64, 520, 900, 100, 23, CafeUi.Muted);
            CafeUiAuthoring.Label(body, "읽는 동안에도 사건은 계속됩니다.", 64, 676, 680, 40, 21, CafeUi.Amber);
            CafeUiAuthoring.Button(body, "덮기  [Tab / Esc / Space]", 580, 714, 416, 60, null);

            panel.SetActive(false);
            return panel;
        }
        private static GameObject BuildReading(Transform root, NightGameView view)
        {
            var body = Overlay(root, "Reading", 1060, 850, out var panel);
            CafeUiAuthoring.Label(body, "현장 기록", 64, 40, 900, 40, 21, CafeUi.Amber);
            view.readingTitle = CafeUiAuthoring.Label(body, "문서 제목", 64, 100, 932, 65, 38);
            view.readingBody = CafeUiAuthoring.Label(body, "문서 내용", 64, 200, 932, 550, 25);
            CafeUiAuthoring.Button(body, "닫기  [Space / Esc]", 650, 770, 346, 60, null);

            panel.SetActive(false);
            return panel;
        }
        private static GameObject BuildCCTV(Transform root, NightGameView view)
        {
            var body = Overlay(root, "CCTV", 1620, 940, out var panel);
            view.feedTitle = CafeUiAuthoring.Label(body, "", 44, 28, 1000, 50, 30, CafeUi.Green);
            CafeUiAuthoring.Label(body, "LIVE  ●  MIC ON", 1280, 36, 300, 42, 22, CafeUi.Amber, TextAnchor.MiddleRight);
            var image = CafeUiAuthoring.Rect(body, "Live camera feed", 44, 104, 1280, 720).gameObject.AddComponent<RawImage>();
            view.feedImage = image; image.raycastTarget = false;
            view.cameraButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                view.cameraButtons[i] = CafeUiAuthoring.Button(body, "0" + (i + 1) + "  " + CafeWorld.CameraNames[i], 1350, 120 + i * 100, 230, 76, null);
            }
            CafeUiAuthoring.Label(body, "내장 마이크 연결됨\n영상과 현장 소리 수신 중\n\nWASD / 방향키\n카메라 전환", 1350, 570, 235, 200, 21, CafeUi.Muted);
            CafeUiAuthoring.Label(body, "감시실을 떠나면 카메라를 볼 수 없습니다.", 44, 864, 960, 42, 22, CafeUi.Muted);
            CafeUiAuthoring.Button(body, "나가기  [Space / Esc]", 1170, 852, 410, 62, null);


            panel.SetActive(false);
            return panel;
        }
        private static GameObject BuildPause(Transform root, NightGameView view)
        {
            var body = Overlay(root, "Pause", 760, 670, out var panel);

            CafeUiAuthoring.Label(body, "근무 일시정지", 60, 65, 640, 70, 40);
            CafeUiAuthoring.Label(body, "잠시 숨을 고릅니다.", 60, 153, 640, 50, 23, CafeUi.Muted);
            view.pauseButtons = new[] {
                CafeUiAuthoring.Button(body, "계속하기", 60, 260, 640, 76, null),
                CafeUiAuthoring.Button(body, "시작 화면으로", 60, 360, 640, 76, null),
                CafeUiAuthoring.Button(body, "게임 종료", 60, 460, 640, 76, null) };

            CafeUiAuthoring.Label(body, "W/S 선택 · Space 실행 · Esc 계속", 60, 580, 640, 50, 21, CafeUi.Muted);

            panel.SetActive(false);
            return panel;
        }
        private static GameObject BuildResult(Transform root, NightGameView view, bool success)
        {
            var body = Overlay(root, success ? "Complete" : "Failure", 960, 750, out var panel);
            CafeUiAuthoring.Label(body, success ? "SHIFT COMPLETE  /  01" : "INCIDENT LOST  /  01", 60, 55, 840, 45, 25, success ? CafeUi.Green : CafeUi.Red);
            CafeUiAuthoring.Label(body, success ? "첫 번째 밤이 끝났다" : "커피가 가게를 삼켰다", 60, 142, 840, 80, 42);
            var resultBody = CafeUiAuthoring.Label(body, success ? "감시실 문을 닫자, 카페는 다시 조용해졌다.\n\n남은 시간  " + 180 + "초     /     잘못된 투입  " + 0 + "회" : "차오르는 커피 속에서 숨을 쉴 수 없다.\n\n사건 시작 시점부터 다시 시도할 수 있습니다.\n읽었던 규칙서와 레시피 기록은 유지됩니다.", 60, 260, 840, 180, 26, CafeUi.Muted);
            var menuButtons = success ? new[] {
                CafeUiAuthoring.Button(body, "시작 화면으로", 60, 500, 840, 76, null),
                CafeUiAuthoring.Button(body, "게임 종료", 60, 600, 840, 76, null) } : new[] {
                CafeUiAuthoring.Button(body, "사건부터 재도전", 60, 500, 840, 76, null),
                CafeUiAuthoring.Button(body, "시작 화면으로", 60, 600, 840, 76, null) };


            if (success) { view.completeButtons = menuButtons; view.completeBody = resultBody; }
            else view.failureButtons = menuButtons;

            panel.SetActive(false);
            return panel;
        }
}
