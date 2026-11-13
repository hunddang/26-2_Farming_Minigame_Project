using System;
using System.Linq;
using NightCafe;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class NightCampaignAuthoring
{
    public static void InstallValidateAndBuild()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        Attach(UnityEngine.Object.FindFirstObjectByType<NightGame>());
        EditorSceneManager.SaveScene(scene);
        NightCafeBuild.ValidateRules();
        NightCafeBuild.BuildWindows();
    }

    public static void Attach(NightGame game)
    {
        var serialized = new SerializedObject(game);
        var field = serialized.FindProperty("nightContent");
        var content = field.objectReferenceValue as NightContent;
        if (content == null)
        {
            content = new GameObject("Night 02–03 Content").AddComponent<NightContent>();
            field.objectReferenceValue = content;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            BindLabels(game.view);
            CreateMenuNight(content);
            CreateShadowNight(content, game.World);
            content.menuNight.SetActive(false);
            content.shadowNight.SetActive(false);
        }
        if (content.routes == null) CreateRoutes(content, game.World);
        if (content.scratchHand == null) CreateScratchHand(content);
        if (content.detourGrid == null) CreateDetourGrid(content, game.view);
        var hudLabels = game.view.GetComponentsInChildren<Text>(true);
        content.controlHelp = hudLabels.First(t => t.text.StartsWith("WASD 이동 · Shift 달리기"));
        content.itemHelp = hudLabels.First(t => t.text.StartsWith("선택 후 컵 앞에서 Space"));
        game.view.GetComponent<CafeFont>().Refresh();
    }

    private static void CreateDetourGrid(NightContent content, NightGameView view)
    {
        var grid = CafeUiAuthoring.Rect(view.readingBody.transform.parent, "Waste disposal puzzle", 64, 190, 932, 565);
        content.detourGrid = grid.gameObject;
        CafeUiAuthoring.Label(grid, "봉투를 밀어 처리 칸에 놓으세요.", 0, 0, 932, 36, 27);
        CafeUiAuthoring.Label(grid, "WASD / 방향키   이동     R   초기화", 0, 43, 932, 36, 22, CafeUi.Muted);
        content.detourTiles = new Image[36];
        content.detourSymbols = new Text[36];
        CafeUiAuthoring.Panel(grid, "Grid backing", 232, 89, 468, 438, CafeUi.Ink);
        for (int i = 0; i < 36; i++)
        {
            var tile = CafeUiAuthoring.Panel(grid, $"Tile {i / 6 + 1},{i % 6 + 1}", 246 + i % 6 * 74, 103 + i / 6 * 69, 70, 65, CafeUi.Ink);
            content.detourTiles[i] = tile;
            content.detourSymbols[i] = CafeUiAuthoring.Label(tile.transform, "", 0, 0, 70, 65, 33, CafeUi.Paper, TextAnchor.MiddleCenter);
        }
        CafeUiAuthoring.Label(grid, "●  플레이어       <color=#EBBB64>◆</color>  쓰레기봉투       <color=#66C8AD>○</color>  처리 칸",
            0, 534, 932, 30, 22, CafeUi.Paper, TextAnchor.MiddleCenter);
        content.detourGrid.SetActive(false);
    }

    private static void BindLabels(NightGameView view)
    {
        Text Find(GameObject parent, string value) => parent.GetComponentsInChildren<Text>(true).First(t => t.text == value);
        view.nightHeader = Find(view.gameObject, "NIGHT SHIFT  /  01");
        view.nightTitle = Find(view.gameObject, "첫 번째 밤");
        view.completeHeader = Find(view.complete, "SHIFT COMPLETE  /  01");
        view.completeTitle = Find(view.complete, "첫 번째 밤이 끝났다");
        view.failureHeader = Find(view.failure, "INCIDENT LOST  /  01");
        view.failureTitle = Find(view.failure, "커피가 가게를 삼켰다");
        view.failureBody = view.failure.GetComponentsInChildren<Text>(true).First(t => t.text.StartsWith("차오르는 커피"));
        PrefabUtility.RecordPrefabInstancePropertyModifications(view);
    }

    private static void CreateMenuNight(NightContent content)
    {
        var root = Group(content.transform, "Night 02 · Lobby menu");
        content.menuNight = root.gameObject;
        var menu = Task(root, "로비 메뉴판", new Vector2(-10.1f, -1.7f), 9, NightAction.Menu);
        Board(menu.transform, new Vector2(2.9f, 1.5f));
        content.menuBoard = menu;
        content.menuWriting = Writing(menu.transform, "…  /  …  /  …", new Color32(232, 79, 61, 255), 18);
        var smear = Group(menu.transform, "Obscured writing on CCTV");
        content.menuSmear = smear.gameObject;
        for (int i = 0; i < 12; i++)
        {
            var mark = Sprite(smear, "Unreadable stain", new Vector2(-1.1f + i * 0.2f, Mathf.Sin(i * 5.3f) * 0.084f),
                new Vector2(0.17f, 0.052f + i % 3 * 0.034f), i % 4 == 0 ? new Color32(188, 74, 53, 255) : new Color32(111, 46, 41, 255), 9);
            mark.transform.localRotation = Quaternion.Euler(0, 0, -25 + i * 17 % 50);
        }

        var hint = Notice(root, "메뉴판 아래 종이", new Vector2(-12.2f, -1.55f), 9,
            "지워지지 않는 얼룩은 바로 문지르지 말고, 따뜻한 물을 묻힌 행주로 닦아야 한다.");
        var cup = Task(root, "빈 컵", new Vector2(2, 6.8f), 10, NightAction.EmptyCup);
        AddTaskArt(cup, "cup", new Vector2(0.66f, 0.66f));
        content.cupPickup = cup.gameObject;
        var sink = Task(root, "싱크대에서 물 받기", new Vector2(4.3f, 7.4f), 10, NightAction.Sink);
        Fixture(sink, "Basin", new Color32(98, 140, 145, 255));
        var warmer = Task(root, "컵 워머", new Vector2(6.4f, 7.4f), 10, NightAction.Warmer);
        Fixture(warmer, "Heating plate", new Color32(216, 127, 64, 255));
        var cloth = Task(root, "마른 행주", new Vector2(8.5f, 6.7f), 10, NightAction.Cloth);
        AddTaskArt(cloth, "paper", new Vector2(0.64f, 0.48f));
        content.clothPickup = cloth.gameObject;
        content.menuTasks = new[] { menu, hint, cup, sink, warmer, cloth };
        content.lobbyCctv = Group(root, "Lobby CCTV framing", new Vector3(-13, -4, -25));
    }

    private static void CreateScratchHand(NightContent content)
    {
        var hand = Group(content.menuNight.transform, "Nails at the player's back");
        content.scratchHand = hand;
        var skin = new Color32(141, 101, 108, 255);
        Sprite(hand, "Palm", Vector2.zero, new Vector2(1.5f, 1.7f), skin, 20, "circle");
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * 0.35f;
            float reach = 1.1f - Mathf.Abs(i - 1.5f) * 0.14f;
            Sprite(hand, "Finger", new Vector2(x, 0.55f + reach / 2), new Vector2(0.28f, reach), skin, 20, "circle");
            Sprite(hand, "Nail", new Vector2(x, 0.66f + reach), new Vector2(0.16f, 0.42f), CafeUi.Paper, 21, "circle");
        }
        hand.gameObject.SetActive(false);
    }

    private static void CreateShadowNight(NightContent content, CafeWorld world)
    {
        var root = Group(content.transform, "Night 03 · Closing shift");
        content.shadowNight = root.gameObject;
        var hall = world.transform.Find("Cafe Hall");
        var tables = hall.GetComponentsInChildren<Transform>().Where(t => t.name == "Cafe table").ToArray();
        var chairs = hall.GetComponentsInChildren<Transform>().Where(t => t.name == "Chair" && t.GetComponent<Collider2D>() != null).ToArray();
        if (tables.Length != 4 || chairs.Length != 8) throw new InvalidOperationException("Expected four hall tables and eight chairs.");
        content.seats = chairs.Select((chair, i) =>
        {
            var item = chair.GetComponent<CafeInteractable>();
            if (item == null) item = chair.gameObject.AddComponent<CafeInteractable>();
            item.kind = InteractionKind.NightTask;
            item.nightAction = NightAction.Chair;
            item.taskIndex = i;
            item.title = "마감 전 의자";
            item.ConfigureMarker(chair.GetComponent<SpriteRenderer>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            return new NightContent.Seat
            {
                chair = item,
                table = tables.OrderBy(t => (t.position - chair.position).sqrMagnitude).First()
            };
        }).ToArray();
        var hallNotice = Notice(root, "홀 마감 안내", new Vector2(-4.85f, -2.4f), 10,
            "바닥 청소 전 모든 의자를 뒤집어 테이블 위에 올려 주세요.\n의자가 남아 있으면 마감이 완료되지 않은 것으로 처리합니다.");
        var storageNotice = Notice(root, "창고 마감 점검표", new Vector2(14.1f, 7.15f), 11,
            "영업 종료 후 칠판에는 ‘마감 완료’를 표시해 주세요.");
        var board = Task(root, "창고 칠판", new Vector2(15.45f, 9.35f), 11, NightAction.Board);
        content.chalkboard = board.transform;
        Board(board.transform, new Vector2(2.5f, 1.35f));
        content.chalkWriting = Writing(board.transform, "", CafeUi.Paper, 12);
        var chalk = Task(root, "떠 있는 분필 잡기", new Vector2(16.55f, 8.65f), 11, NightAction.Chalk);
        var chalkArt = Sprite(chalk.transform, "Chalk", Vector2.zero, new Vector2(0.34f, 0.115f), CafeUi.Paper, 10);
        chalk.ConfigureMarker(chalkArt);
        content.chalk = chalkArt.transform;
        var eraser = Task(root, "칠판지우개", new Vector2(15.8f, 6.65f), 11, NightAction.Eraser);
        var eraserArt = Sprite(eraser.transform, "Eraser", Vector2.zero, new Vector2(0.55f, 0.28f), new Color32(174, 124, 78, 255), 4);
        eraser.ConfigureMarker(eraserArt);
        Sprite(eraser.transform, "Felt", new Vector2(0, -0.10f), new Vector2(0.55f, 0.1f), CafeUi.Ink, 5);
        content.eraserPickup = eraser.gameObject;
        content.shadowTasks = new[] { hallNotice, storageNotice, board, chalk, eraser };
        content.warehouseMark = Group(root, "Warehouse shadow position", new Vector3(14.5f, 8.85f, 0));
        content.shadow = Group(root, "Shadow");
        Shade(content.shadow, out content.shadowHead, out content.shadowEyes, false);
    }

    private static void CreateRoutes(NightContent content, CafeWorld world)
    {
        var routes = content.gameObject.AddComponent<NightRoutes>();
        content.routes = routes;
        var root = Group(content.transform, "Night 03 · Passageways");
        routes.routeObjects = root.gameObject;
        CafeInteractable Door(Vector2 position) => world.Interactables.Where(i => i.kind == InteractionKind.Door)
            .OrderBy(i => ((Vector2)i.transform.position - position).sqrMagnitude).First();
        routes.hallDoor = Door(new Vector2(-7, -4));
        routes.detourDoor = Door(new Vector2(13, 5));

        CafeInteractable Portal(string title, float x, float y, int layer, NightAction action, int index)
        {
            var portal = Task(root, title, new Vector2(x, y), layer, action);
            portal.taskIndex = index;
            AddTaskArt(portal, "door", new Vector2(1.08f, 0.38f));
            Sprite(portal.transform, "Marker", new Vector2(0, 0.26f), new Vector2(0.56f, 0.10f), CafeUi.Amber, 5);
            return portal;
        }

        Transform Corridor(string name, float x, int layer)
        {
            var hall = Group(root, name, new Vector3(x, 0, 0), layer);
            var floor = Sprite(hall, "Floor", Vector2.zero, new Vector2(3.4f, 18.5f), new Color32(158, 169, 167, 255), -10, "floor-tile");
            floor.drawMode = SpriteDrawMode.Tiled;
            foreach (int sign in new[] { -1, 1 })
            {
                Wall(hall, new Vector2(1.62f * sign, 0), new Vector2(0.22f, 18.5f));
                Wall(hall, new Vector2(0, 9.2f * sign), new Vector2(3.4f, 0.22f));
            }
            for (int y = -6; y <= 6; y += 6)
                Sprite(hall, "Guide light", new Vector2(1.45f, y), new Vector2(0.1f, 0.64f), CafeUi.Amber, 5);
            return hall;
        }

        var tentacleHall = Corridor("Tentacle passage", 40, 14);
        var pursuitHall = Corridor("Pursuit passage", 48, 15);
        routes.entrances = new[]
        {
            Portal("추격 복도 · 카운터 방향", -11.5f, -7.8f, 9, NightAction.ChaseEntrance, 0),
            Portal("추격 복도 · 로비 방향", -0.3f, 7.45f, 10, NightAction.ChaseEntrance, 1),
            Portal("창고로 가는 외부 통로", -14.4f, -9.2f, 9, NightAction.ServiceDoor, 0),
            Portal("로비로 가는 외부 통로", 15f, 3.6f, 11, NightAction.ServiceDoor, 1),
            Portal("로비 출구", 40, -8.2f, 14, NightAction.TentacleExit, 0),
            Portal("홀 출구", 40, 8.2f, 14, NightAction.TentacleExit, 1),
            Portal("로비 출구", 48, -8.2f, 15, NightAction.ChaseExit, 0),
            Portal("카운터 출구", 48, 8.2f, 15, NightAction.ChaseExit, 1)
        };
        routes.tentacles = new[] { -3.8f, 0, 3.8f }.Select(y =>
        {
            var arm = Group(tentacleHall, "Tentacle pivot", new Vector3(-1.38f, y, 0));
            Sprite(arm, "Tentacle", new Vector2(0.11f, 1.37f), new Vector2(0.52f, 2.95f), new Color32(105, 57, 81, 255), 5, "circle");
            for (int i = 1; i <= 5; i++)
                Sprite(arm, "Sucker", new Vector2(0.23f, i * 0.44f), Vector2.one * (0.22f - 0.018f * i), new Color32(192, 103, 103, 255), 6, "circle");
            return arm;
        }).ToArray();
        routes.tentacleCamera = Group(tentacleHall, "Passage CCTV framing", new Vector3(0, 0, -25));
        routes.pursuer = Group(pursuitHall, "Pursuer");
        Shade(routes.pursuer, out _, out _, true);
        root.gameObject.SetActive(false);
    }

    private static void Shade(Transform root, out Transform head, out Transform eyes, bool pursuit)
    {
        var dark = new Color32(22, 24, 31, 255);
        Sprite(root, "Body", Vector2.zero, new Vector2(0.64f, 0.9f), dark, 6, "circle");
        foreach (float x in new[] { -0.34f, 0.34f })
            Sprite(root, "Arm", new Vector2(x, -0.05f), new Vector2(0.25f, 0.65f), dark, 6, "circle");
        head = Group(root, "Head tilt", new Vector3(0, 0.29f, 0));
        Sprite(head, "Head", Vector2.zero, new Vector2(0.56f, 0.53f), dark, 7, "circle");
        eyes = Group(head, "Eyes");
        foreach (float x in new[] { -0.12f, 0.12f })
            Sprite(eyes, "Eye", new Vector2(x, 0.025f), new Vector2(0.085f, 0.07f), pursuit ? CafeUi.Red : CafeUi.Amber, 8);
        eyes.gameObject.SetActive(pursuit);
    }

    private static CafeInteractable Task(Transform parent, string name, Vector2 position, int layer, NightAction action)
    {
        var item = Group(parent, name, position, layer).gameObject.AddComponent<CafeInteractable>();
        item.kind = InteractionKind.NightTask;
        item.title = name;
        item.nightAction = action;
        return item;
    }

    private static CafeInteractable Notice(Transform parent, string name, Vector2 position, int layer, string text)
    {
        var item = Task(parent, name, position, layer, NightAction.None);
        item.kind = InteractionKind.Inspect;
        item.description = text;
        AddTaskArt(item, "note", new Vector2(0.62f, 0.72f));
        return item;
    }

    private static void AddTaskArt(CafeInteractable item, string art, Vector2 size)
    {
        item.ConfigureMarker(Sprite(item.transform, art, Vector2.zero, size, Color.white, 4, art));
    }

    private static void Fixture(CafeInteractable item, string name, Color color)
    {
        item.ConfigureMarker(Sprite(item.transform, "Housing", Vector2.zero, new Vector2(0.71f, 0.58f), new Color32(65, 82, 83, 255), 4));
        Sprite(item.transform, name, new Vector2(0, -0.02f), new Vector2(0.49f, 0.35f), color, 5);
    }

    private static void Board(Transform parent, Vector2 size)
    {
        var frame = Sprite(parent, "Frame", Vector2.zero, size, new Color32(132, 102, 73, 255), 7);
        parent.GetComponent<CafeInteractable>().ConfigureMarker(frame);
        Sprite(parent, "Slate", Vector2.zero, size - new Vector2(0.15f, 0.15f), new Color32(43, 60, 59, 255), 8);
    }

    private static Text Writing(Transform parent, string text, Color color, int size)
    {
        var canvas = new GameObject("Writing", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
        canvas.transform.SetParent(parent, false);
        canvas.transform.localScale = Vector3.one * 0.018f;
        canvas.gameObject.layer = parent.gameObject.layer;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 9;
        var label = CafeUiAuthoring.Label(canvas.transform, text, 0, 0, 128, 58, size, color, TextAnchor.MiddleCenter);
        var rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/NanumGothic-Regular.ttf");
        label.gameObject.layer = parent.gameObject.layer;
        return label;
    }

    private static void Wall(Transform parent, Vector2 position, Vector2 size)
    {
        var wall = Sprite(parent, "Wall", position, size, Color.white, 4, "wall");
        wall.gameObject.AddComponent<BoxCollider2D>().size = size;
    }

    private static SpriteRenderer Sprite(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order, string art = "pixel")
    {
        var sprite = CafeWorldAuthoring.Shape(name, position, size, color, order, false, parent);
        sprite.sprite = CafeSceneAuthoring.Sprite(art);
        sprite.size = size;
        return sprite;
    }

    private static Transform Group(Transform parent, string name, Vector3 position = default, int? layer = null)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        group.localPosition = position;
        group.gameObject.layer = layer ?? parent.gameObject.layer;
        return group;
    }
}
