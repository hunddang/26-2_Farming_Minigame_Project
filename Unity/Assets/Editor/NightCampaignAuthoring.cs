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
        if (content.menuSmear == null) RefineBoards(content);
        var hudLabels = game.view.GetComponentsInChildren<Text>(true);
        content.controlHelp = hudLabels.First(t => t.text.StartsWith("WASD 이동 · Shift 달리기"));
        content.itemHelp = hudLabels.First(t => t.text.StartsWith("선택 후 컵 앞에서 Space"));
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

    private static void RefineBoards(NightContent content)
    {
        void Feet(Transform board, float halfWidth, float height)
        {
            foreach (float x in new[] { -halfWidth, halfWidth })
                Cafe3DAssets.Box(board, "Board foot", new Vector3(x, height * 0.5f, 0), new Vector3(0.16f, height, 0.48f), "Walnut");
            Cafe3DAssets.SetLayer(board, board.gameObject.layer);
        }
        Feet(content.menuBoard.transform, 1.13f, 0.66f);
        Feet(content.chalkboard, 0.99f, 0.77f);
        content.menuWriting.characterSize = 0.074f;
        content.chalkWriting.characterSize = 0.039f;
        content.chalkboard.position = new Vector3(15.45f, 0, 9.35f);
        content.warehouseMark.position = new Vector3(14.5f, 0, 8.85f);
        content.chalk.parent.position = new Vector3(16.55f, 0, 8.65f);
        content.eraserPickup.transform.position = new Vector3(15.8f, 0, 6.65f);
        content.shadowTasks.First(t => t.title == "창고 마감 점검표").transform.position = new Vector3(14.1f, 0, 7.15f);
        foreach (var notice in content.menuTasks.Concat(content.shadowTasks).Where(t => t.kind == InteractionKind.Inspect))
        {
            Stand(notice.transform, "Walnut");
            Cafe3DAssets.SetLayer(notice.transform, notice.gameObject.layer);
        }

        var smear = Cafe3DAssets.Group(content.menuBoard.transform, "Obscured writing on CCTV");
        content.menuSmear = smear.gameObject;
        for (int i = 0; i < 12; i++)
        {
            var mark = Cafe3DAssets.Box(smear, "Unreadable stain",
                new Vector3(-1.1f + i * 0.2f, 1.53f + Mathf.Sin(i * 5.3f) * 0.084f, -0.099f),
                new Vector3(0.17f, 0.052f + i % 3 * 0.034f, 0.012f), i % 4 == 0 ? "Terracotta" : "Chocolate");
            mark.localRotation = Quaternion.Euler(0, 0, -25 + i * 17 % 50);
        }
        Cafe3DAssets.SetLayer(smear, 9);
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
        var root = Cafe3DAssets.Group(content.transform, "Night 02 · Lobby menu");
        content.menuNight = root.gameObject;
        var menu = Task(root, "로비 메뉴판", -10.1f, -1.7f, 9, NightAction.Menu);
        menu.transform.localRotation = Quaternion.Euler(0, 0, 0);
        Board(menu.transform, 2.9f, 1.8f);
        content.menuBoard = menu;
        content.menuWriting = Writing(menu.transform, new Vector3(0, 1.55f, -0.12f), "…  /  …  /  …", new Color(0.9f, 0.15f, 0.12f), 0.16f);
        var hint = Notice(root, "메뉴판 아래 종이", -12.2f, -1.55f, 9,
            "지워지지 않는 얼룩은 바로 문지르지 말고, 따뜻한 물을 묻힌 행주로 닦아야 한다.");
        var cup = Task(root, "빈 컵", 2, 6.8f, 10, NightAction.EmptyCup);
        Stand(cup.transform, "Ceramic");
        Cafe3DAssets.Model(cup.transform, "Cup").localPosition = Vector3.up * 0.75f;
        content.cupPickup = cup.gameObject;
        var sink = Task(root, "싱크대에서 물 받기", 4.3f, 7.4f, 10, NightAction.Sink);
        Stand(sink.transform, "Metal");
        Cafe3DAssets.Box(sink.transform, "Faucet", new Vector3(0.16f, 0.87f, 0.19f), new Vector3(0.07f, 0.28f, 0.07f), "Metal");
        Cafe3DAssets.Box(sink.transform, "Basin", new Vector3(0, 0.76f, 0), new Vector3(0.48f, 0.06f, 0.34f), "Dark teal");
        var warmer = Task(root, "컵 워머", 6.4f, 7.4f, 10, NightAction.Warmer);
        Stand(warmer.transform, "Charcoal");
        Cafe3DAssets.Box(warmer.transform, "Heated plate", new Vector3(0, 0.8f, 0), new Vector3(0.4f, 0.08f, 0.32f), "Warm light");
        var cloth = Task(root, "마른 행주", 8.5f, 6.7f, 10, NightAction.Cloth);
        Stand(cloth.transform, "Walnut");
        Cafe3DAssets.Box(cloth.transform, "Folded cloth", new Vector3(0, 0.8f, 0), new Vector3(0.45f, 0.1f, 0.34f), "Paper");
        content.clothPickup = cloth.gameObject;
        content.menuTasks = new[] { menu, hint, cup, sink, warmer, cloth };
        content.lobbyCctv = Cafe3DAssets.Group(root, "Lobby CCTV lens", new Vector3(-15.4f, 3.4f, -7.9f));
        content.lobbyCctv.LookAt(menu.transform.position + Vector3.up * 1.25f);
        SetRoomLayers(content.menuTasks);
    }

    private static void CreateScratchHand(NightContent content)
    {
        var hand = Cafe3DAssets.Group(content.menuNight.transform, "Nails at the player's back");
        content.scratchHand = hand;
        Cafe3DAssets.Primitive(hand, "Palm", PrimitiveType.Sphere, Vector3.zero,
            new Vector3(0.41f, 0.44f, 0.13f), "Tapioca");
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * 0.096f;
            float reach = 0.35f - Mathf.Abs(i - 1.5f) * 0.042f;
            Cafe3DAssets.Primitive(hand, "Finger", PrimitiveType.Sphere, new Vector3(x, 0.16f + reach / 2, 0),
                new Vector3(0.083f, reach, 0.09f), "Tapioca");
            Cafe3DAssets.Primitive(hand, "Nail", PrimitiveType.Sphere, new Vector3(x, 0.17f + reach, -0.035f),
                new Vector3(0.04f, 0.18f, 0.037f), "Ceramic");
        }
        hand.gameObject.SetActive(false);
    }

    private static void CreateShadowNight(NightContent content, CafeWorld world)
    {
        var root = Cafe3DAssets.Group(content.transform, "Night 03 · Closing shift");
        content.shadowNight = root.gameObject;
        var hall = world.transform.Find("Cafe Hall");
        var tables = hall.GetComponentsInChildren<Transform>().Where(t => t.name == "Cafe table").ToArray();
        var chairs = hall.GetComponentsInChildren<Transform>().Where(t => t.name == "Chair" && t.GetComponent<Collider>() != null).ToArray();
        if (tables.Length == 0 || chairs.Length == 0) throw new InvalidOperationException("No 3D hall tables or chairs found.");
        content.seats = chairs.Select((chair, i) =>
        {
            var item = chair.GetComponent<CafeInteractable>();
            if (item == null) item = chair.gameObject.AddComponent<CafeInteractable>();
            item.kind = InteractionKind.NightTask;
            item.nightAction = NightAction.Chair;
            item.taskIndex = i;
            item.title = "마감 전 의자";
            return new NightContent.Seat
            {
                chair = item,
                table = tables.OrderBy(t => (t.position - chair.position).sqrMagnitude).First()
            };
        }).ToArray();

        var hallNotice = Notice(root, "홀 마감 안내", -4.85f, -2.4f, 10,
            "바닥 청소 전 모든 의자를 뒤집어 테이블 위에 올려 주세요.\n의자가 남아 있으면 마감이 완료되지 않은 것으로 처리합니다.");
        var storageNotice = Notice(root, "창고 마감 점검표", 14.65f, 7.35f, 11,
            "영업 종료 후 칠판에는 ‘마감 완료’를 표시해 주세요.");
        var board = Task(root, "창고 칠판", 16.3f, 8.5f, 11, NightAction.Board);
        content.chalkboard = board.transform;
        Board(board.transform, 2.5f, 1.6f);
        content.chalkWriting = Writing(board.transform, new Vector3(0, 1.55f, -0.12f), "", new Color(0.9f, 0.91f, 0.77f), 0.1f);
        var chalk = Task(root, "떠 있는 분필 잡기", 17.65f, 8.05f, 11, NightAction.Chalk);
        content.chalk = Cafe3DAssets.Box(chalk.transform, "Chalk", new Vector3(0, 0.75f, 0), new Vector3(0.22f, 0.065f, 0.065f), "Ceramic");
        var eraser = Task(root, "칠판지우개", 17.55f, 6.65f, 11, NightAction.Eraser);
        Stand(eraser.transform, "Walnut");
        Cafe3DAssets.Box(eraser.transform, "Eraser", new Vector3(0, 0.8f, 0), new Vector3(0.35f, 0.09f, 0.15f), "Paper");
        content.eraserPickup = eraser.gameObject;
        content.shadowTasks = new[] { hallNotice, storageNotice, board, chalk, eraser };
        content.warehouseMark = Cafe3DAssets.Group(root, "Warehouse shadow position", new Vector3(15.7f, 0, 7.8f));

        content.shadow = Cafe3DAssets.Group(root, "Shadow");
        Cafe3DAssets.Box(content.shadow, "Torso", new Vector3(0, 0.92f, 0), new Vector3(0.6f, 0.72f, 0.35f), "Charcoal");
        foreach (float x in new[] { -0.19f, 0.19f })
            Cafe3DAssets.Box(content.shadow, "Leg", new Vector3(x, 0.34f, 0), new Vector3(0.22f, 0.68f, 0.25f), "Charcoal");
        foreach (float x in new[] { -0.42f, 0.42f })
            Cafe3DAssets.Box(content.shadow, "Arm", new Vector3(x, 0.86f, 0), new Vector3(0.23f, 0.7f, 0.23f), "Charcoal");
        content.shadowHead = Cafe3DAssets.Group(content.shadow, "Head tilt", new Vector3(0, 1.55f, 0));
        Cafe3DAssets.Primitive(content.shadowHead, "Head", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.57f, 0.66f, 0.49f), "Charcoal");
        content.shadowEyes = Cafe3DAssets.Group(content.shadowHead, "Eye flash");
        foreach (float x in new[] { -0.12f, 0.12f })
            Cafe3DAssets.Box(content.shadowEyes, "Eye", new Vector3(x, 0.035f, -0.23f), new Vector3(0.105f, 0.045f, 0.07f), "Warm light");
        content.shadowEyes.gameObject.SetActive(false);
        SetRoomLayers(content.shadowTasks);
    }

    private static void CreateRoutes(NightContent content, CafeWorld world)
    {
        var routes = content.gameObject.AddComponent<NightRoutes>();
        content.routes = routes;
        var root = Cafe3DAssets.Group(content.transform, "Night 03 · Passageways");
        routes.routeObjects = root.gameObject;
        CafeInteractable Door(float x, float z) => world.Interactables.Where(i => i.kind == InteractionKind.Door)
            .OrderBy(i => (CafeWorld.PlanPosition(i.transform.position) - new Vector2(x, z)).sqrMagnitude).First();
        routes.hallDoor = Door(-7, -4);
        routes.detourDoor = Door(13, 5);

        CafeInteractable Portal(Transform parent, string title, float x, float z, int layer, NightAction action, int index)
        {
            var portal = Task(parent, title, x, z, layer, action);
            portal.taskIndex = index;
            Cafe3DAssets.Box(portal.transform, "Door", new Vector3(0, 0.93f, 0.22f), new Vector3(1.08f, 1.86f, 0.18f), "Dark teal");
            Cafe3DAssets.Box(portal.transform, "Marker", new Vector3(0, 1.65f, 0.11f), new Vector3(0.62f, 0.11f, 0.06f), "Warm light");
            Cafe3DAssets.SetLayer(portal.transform, layer);
            return portal;
        }

        Transform Corridor(string name, float x, int layer)
        {
            var hall = Cafe3DAssets.Group(root, name, new Vector3(x, 0, 0));
            Cafe3DAssets.Box(hall, "Floor", new Vector3(0, -0.13f, 0), new Vector3(3.4f, 0.25f, 18.5f), "Floor tile 0");
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Cafe3DAssets.Box(hall, "Side wall", new Vector3(1.62f * sign, 0.65f, 0), new Vector3(0.22f, 1.3f, 18.5f), "Wall")
                    .gameObject.AddComponent<BoxCollider>();
                Cafe3DAssets.Box(hall, "End wall", new Vector3(0, 0.65f, 9.2f * sign), new Vector3(3.4f, 1.3f, 0.22f), "Wall")
                    .gameObject.AddComponent<BoxCollider>();
            }
            for (int z = -6; z <= 6; z += 6)
                Cafe3DAssets.Box(hall, "Guide light", new Vector3(1.45f, 0.8f, z), new Vector3(0.1f, 0.13f, 0.64f), "Warm light");
            Cafe3DAssets.SetLayer(hall, layer);
            return hall;
        }

        var tentacleHall = Corridor("Tentacle passage", 40, 14);
        var chaseHall = Corridor("Pursuit passage", 48, 15);
        routes.entrances = new[]
        {
            Portal(root, "추격 복도 · 카운터 방향", -11.5f, -7.8f, 9, NightAction.ChaseEntrance, 0),
            Portal(root, "추격 복도 · 로비 방향", -0.3f, 7.45f, 10, NightAction.ChaseEntrance, 1),
            Portal(root, "창고로 가는 외부 통로", -14.4f, -9.2f, 9, NightAction.ServiceDoor, 0),
            Portal(root, "로비로 가는 외부 통로", 15f, 3.6f, 11, NightAction.ServiceDoor, 1),
            Portal(root, "로비 출구", 40, -8.2f, 14, NightAction.TentacleExit, 0),
            Portal(root, "홀 출구", 40, 8.2f, 14, NightAction.TentacleExit, 1),
            Portal(root, "로비 출구", 48, -8.2f, 15, NightAction.ChaseExit, 0),
            Portal(root, "카운터 출구", 48, 8.2f, 15, NightAction.ChaseExit, 1)
        };
        routes.tentacles = new[] { -3.8f, 0, 3.8f }.Select(z =>
        {
            var arm = Cafe3DAssets.Group(tentacleHall, "Tentacle pivot", new Vector3(-1.38f, 0.72f, z));
            Cafe3DAssets.Primitive(arm, "Tentacle", PrimitiveType.Sphere, new Vector3(0, 1.26f, 0),
                new Vector3(0.42f, 2.85f, 0.54f), "Tapioca");
            for (int i = 1; i <= 4; i++)
                Cafe3DAssets.Primitive(arm, "Sucker", PrimitiveType.Sphere, new Vector3(0.18f, i * 0.5f, 0),
                    new Vector3(0.16f, 0.23f, 0.23f), "Terracotta");
            Cafe3DAssets.SetLayer(arm, 14);
            return arm;
        }).ToArray();
        routes.tentacleCamera = Cafe3DAssets.Group(tentacleHall, "Passage CCTV lens", new Vector3(0, 3.55f, -8.75f));
        routes.tentacleCamera.LookAt(new Vector3(40, 0.15f, 1.1f));
        routes.pursuer = Cafe3DAssets.Group(chaseHall, "Pursuer");
        Cafe3DAssets.Primitive(routes.pursuer, "Body", PrimitiveType.Sphere, new Vector3(0, 0.99f, 0),
            new Vector3(0.89f, 1.9f, 0.61f), "Charcoal");
        Cafe3DAssets.Primitive(routes.pursuer, "Head", PrimitiveType.Sphere, new Vector3(0, 1.93f, 0),
            new Vector3(0.73f, 0.74f, 0.65f), "Charcoal");
        for (int sign = -1; sign <= 1; sign += 2)
        {
            Cafe3DAssets.Box(routes.pursuer, "Arm", new Vector3(0.48f * sign, 0.99f, 0), new Vector3(0.22f, 1.32f, 0.27f), "Charcoal");
            Cafe3DAssets.Box(routes.pursuer, "Eye", new Vector3(0.13f * sign, 1.99f, 0.30f), new Vector3(0.15f, 0.075f, 0.08f), "Camera red");
        }
        Cafe3DAssets.SetLayer(routes.pursuer, 15);
        root.gameObject.SetActive(false);
    }

    private static CafeInteractable Task(Transform parent, string name, float x, float z, int layer, NightAction action)
    {
        var t = Cafe3DAssets.Group(parent, name, new Vector3(x, 0, z));
        t.gameObject.layer = layer;
        var item = t.gameObject.AddComponent<CafeInteractable>();
        item.kind = InteractionKind.NightTask;
        item.title = name;
        item.nightAction = action;
        return item;
    }

    private static CafeInteractable Notice(Transform parent, string name, float x, float z, int layer, string text)
    {
        var item = Task(parent, name, x, z, layer, NightAction.None);
        item.kind = InteractionKind.Inspect;
        item.description = text;
        Cafe3DAssets.Model(item.transform, "Note").localPosition = Vector3.up * 0.75f;
        return item;
    }

    private static void Board(Transform parent, float width, float height)
    {
        Cafe3DAssets.Box(parent, "Frame", new Vector3(0, 1.5f, 0), new Vector3(width, height, 0.15f), "Walnut");
        Cafe3DAssets.Box(parent, "Slate", new Vector3(0, 1.5f, -0.079f), new Vector3(width - 0.15f, height - 0.15f, 0.026f), "Charcoal");
    }

    private static void Stand(Transform parent, string surface)
    {
        Cafe3DAssets.Box(parent, "Stand", new Vector3(0, 0.35f, 0), new Vector3(0.57f, 0.7f, 0.52f), "Dark teal");
        Cafe3DAssets.Box(parent, "Surface", new Vector3(0, 0.73f, 0), new Vector3(0.65f, 0.06f, 0.58f), surface);
    }

    private static TextMesh Writing(Transform parent, Vector3 position, string text, Color color, float size)
    {
        var label = Cafe3DAssets.Group(parent, "Writing", position).gameObject.AddComponent<TextMesh>();
        label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/NanumGothic-Regular.ttf");
        label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 64;
        label.characterSize = size;
        label.text = text;
        label.color = color;
        return label;
    }

    private static void SetRoomLayers(CafeInteractable[] tasks)
    {
        foreach (var item in tasks) Cafe3DAssets.SetLayer(item.transform, item.gameObject.layer);
    }
}
