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
        var menu = Task(root, "로비 메뉴판", CafeWorld.PlanPoint(9, 7.6f, 1.25f), 9, NightAction.Menu);
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

        var hint = Notice(root, "메뉴판 아래 종이", CafeWorld.PlanPoint(9, 7.6f, 2.15f), 9,
            "지워지지 않는 얼룩은 바로 문지르지 말고, 따뜻한 물을 묻힌 행주로 닦아야 한다.");
        var cup = Task(root, "빈 컵", CafeWorld.PlanPoint(13, 5.2f, 1.6f), 13, NightAction.EmptyCup);
        AddTaskArt(cup, "cup", new Vector2(0.66f, 0.66f));
        content.cupPickup = cup.gameObject;
        var sink = Task(root, "싱크대에서 물 받기", CafeWorld.PlanPoint(13, 12.5f, 2.45f), 13, NightAction.Sink);
        Fixture(sink, "Basin", new Color32(98, 140, 145, 255));
        var warmer = Task(root, "컵 워머", CafeWorld.PlanPoint(13, 11.65f, 1.7f), 13, NightAction.Warmer);
        Fixture(warmer, "Heating plate", new Color32(216, 127, 64, 255));
        var cloth = Task(root, "마른 행주", CafeWorld.PlanPoint(13, 9.1f, 2.55f), 13, NightAction.Cloth);
        AddTaskArt(cloth, "paper", new Vector2(0.64f, 0.48f));
        content.clothPickup = cloth.gameObject;
        content.menuTasks = new[] { menu, hint, cup, sink, warmer, cloth };
        content.lobbyCctv = Group(root, "Lobby CCTV framing", new Vector3(-14, -7, -25));
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
        if (tables.Length != 9 || chairs.Length != 18) throw new InvalidOperationException("The hall plan requires nine tables with two chairs each.");
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
        var hallNotice = Notice(root, "홀 마감 안내", new Vector2(-4, -4.1f), 10,
            "바닥 청소 전 모든 의자를 뒤집어 테이블 위에 올려 주세요.\n의자가 남아 있으면 마감이 완료되지 않은 것으로 처리합니다.");
        var storageNotice = Notice(root, "창고 마감 점검표", new Vector2(16.5f, 7.5f), 11,
            "영업 종료 후 칠판에는 ‘마감 완료’를 표시해 주세요.");
        var board = Task(root, "창고 칠판", CafeWorld.PlanPoint(11, 3.95f, .48f), 11, NightAction.Board);
        content.chalkboard = board.transform;
        Board(board.transform, new Vector2(2.5f, 1.35f));
        content.chalkWriting = Writing(board.transform, "", CafeUi.Paper, 12);
        var chalk = Task(root, "떠 있는 분필 잡기", CafeWorld.PlanPoint(11, 3.9f, 1.5f), 11, NightAction.Chalk);
        var chalkArt = Sprite(chalk.transform, "Chalk", Vector2.zero, new Vector2(0.34f, 0.115f), CafeUi.Paper, 10);
        chalk.ConfigureMarker(chalkArt);
        content.chalk = chalkArt.transform;
        var eraser = Task(root, "칠판지우개", new Vector2(18.5f, 6.5f), 11, NightAction.Eraser);
        var eraserArt = Sprite(eraser.transform, "Eraser", Vector2.zero, new Vector2(0.55f, 0.28f), new Color32(174, 124, 78, 255), 4);
        eraser.ConfigureMarker(eraserArt);
        Sprite(eraser.transform, "Felt", new Vector2(0, -0.10f), new Vector2(0.55f, 0.1f), CafeUi.Ink, 5);
        content.eraserPickup = eraser.gameObject;
        content.shadowTasks = new[] { hallNotice, storageNotice, board, chalk, eraser };
        content.warehouseMark = Group(root, "Warehouse shadow position", new Vector3(17.2f, 8.9f, 0));
        content.shadow = Group(root, "Shadow");
        Shade(content.shadow, out content.shadowHead, out content.shadowEyes, false);
    }

    private enum DoorSide { Left, Right, Top, Bottom }

    private static void CreateRoutes(NightContent content, CafeWorld world)
    {
        var routes = content.gameObject.AddComponent<NightRoutes>();
        content.routes = routes;
        var root = Group(content.transform, "Rooms and Passageways");
        routes.routeObjects = root.gameObject;
        var links = new System.Collections.Generic.List<NightRoutes.Portal>();
        for(int layer=14;layer<=18;layer++)
        {
            var hall = Group(root,CafeWorld.RoomNames[layer-8],CafeWorld.RoomCenter(layer),layer);
            Vector2 size=CafeWorld.Sizes[layer-8];
            var floor=Sprite(hall,"Floor",Vector2.zero,size,new Color32(144,158,153,255),-10,layer==16 ? "floor-wood" : "floor-tile");
            floor.drawMode=SpriteDrawMode.Tiled;
            foreach(int sign in new[]{-1,1})
            {
                Wall(hall,new Vector2(sign*size.x/2,0),new Vector2(0.25f,size.y));
                Wall(hall,new Vector2(0,sign*size.y/2),new Vector2(size.x,0.25f));
            }
            for(int x=-(int)size.x/2+2;x<size.x/2;x+=4)
                Sprite(hall,"Guide light",new Vector2(x,-size.y/2+0.3f),new Vector2(0.6f,0.08f),CafeUi.Amber,3);
            void Obstacle(string name, float col, float row, float width, float height, string art, Color color)
            {
                Vector2 position = CafeWorld.PlanPoint(layer, col + width / 2, row + height / 2) - CafeWorld.RoomCenter(layer);
                var prop = Sprite(hall, name, position, new Vector2(width, height), color, 3, art);
                prop.gameObject.AddComponent<BoxCollider2D>().size = prop.size;
            }
            var metal = new Color32(123, 141, 172, 255);
            if (layer == NightRoutes.PursuitCorridor)
            {
                Obstacle("Service cabinet A", 6, 0, 2, 4, "shelf", metal);
                Obstacle("Service cabinet B", 10, 2, 2, 4, "shelf", metal);
                Obstacle("Service cabinet C", 14, 0, 2, 4, "shelf", metal);
            }
            if (layer is 17 or 18)
                Obstacle("Equipment partition", 3, 0, 12, size.y - 2, "wall", metal);
            if (layer == 16)
            {
                Obstacle("Cafe exterior wall", 1, 0, 2, 1, "wall", metal);
                Obstacle("Cafe exterior windows", 1.2f, 4, 1.8f, 3, "monitor", metal);
                Obstacle("Parked car A", 5, 1, 4, 2, "parked-car", Color.white);
                Obstacle("Parked car B", 11, 1, 4, 2, "parked-car", new Color32(162, 167, 193, 255));
                Obstacle("Flower bed A", 5, 9, 3, 1.8f, "plant", metal);
                Obstacle("Flower bed B", 11, 9, 3, 1.8f, "plant", metal);
            }
        }
        (CafeInteractable item, Vector2 arrival) Portal(int layer, DoorSide side, float offset, int to, bool puzzle)
        {
            Vector2 size = CafeWorld.Sizes[layer - 8];
            const float inset = .54f;
            Vector2 position = side switch
            {
                DoorSide.Left => CafeWorld.PlanPoint(layer, inset, offset),
                DoorSide.Right => CafeWorld.PlanPoint(layer, size.x - inset, offset),
                DoorSide.Top => CafeWorld.PlanPoint(layer, offset, inset),
                _ => CafeWorld.PlanPoint(layer, offset, size.y - inset)
            };
            Vector2 inward = side switch
            {
                DoorSide.Left => Vector2.right,
                DoorSide.Right => Vector2.left,
                DoorSide.Top => Vector2.down,
                _ => Vector2.up
            };
            string title = CafeWorld.RoomNames[to - 8] + (puzzle ? " · 우회문" : " 문");
            var item = Task(root, title, position, layer, NightAction.Travel);
            item.kind = InteractionKind.Door;
            AddTaskArt(item, "door", new Vector2(CafeWorld.DoorWidth, .48f));
            if (side is DoorSide.Left or DoorSide.Right)
                item.transform.GetChild(0).localRotation = Quaternion.Euler(0, 0, 90);
            return (item, position + inward * .85f);
        }
        void Connect(int from, DoorSide sideA, float offsetA, int to, DoorSide sideB, float offsetB, bool puzzle = false)
        {
            var left = Portal(from, sideA, offsetA, to, puzzle);
            var right = Portal(to, sideB, offsetB, from, puzzle);
            left.item.taskIndex = links.Count;
            links.Add(new NightRoutes.Portal { item = left.item, target = right.arrival, paired = right.item, puzzle = puzzle });
            right.item.taskIndex = links.Count;
            links.Add(new NightRoutes.Portal { item = right.item, target = left.arrival, paired = left.item, puzzle = puzzle });
        }

        Connect(8, DoorSide.Right, 3.45f, 9, DoorSide.Left, 4.5f);
        Connect(9, DoorSide.Top, 4.6f, 11, DoorSide.Bottom, 5f);
        Connect(9, DoorSide.Bottom, 4.6f, 12, DoorSide.Top, 5f);
        Connect(9, DoorSide.Right, 2.65f, 15, DoorSide.Left, 3f);
        Connect(15, DoorSide.Right, 3f, 13, DoorSide.Bottom, 1.9f);
        Connect(9, DoorSide.Right, 6.65f, 14, DoorSide.Left, 4f);
        Connect(14, DoorSide.Right, 4f, 10, DoorSide.Left, 7.65f);
        Connect(11, DoorSide.Right, 4.65f, 17, DoorSide.Left, 2f, true);
        Connect(17, DoorSide.Right, 2f, 13, DoorSide.Left, 1.8f, true);
        Connect(12, DoorSide.Right, 4.65f, 18, DoorSide.Left, 2f, true);
        Connect(18, DoorSide.Right, 2f, 10, DoorSide.Left, 2.75f, true);
        Connect(13, DoorSide.Right, 7.15f, 16, DoorSide.Left, 3f);
        Connect(10, DoorSide.Right, 5.65f, 16, DoorSide.Left, 8.3f);
        routes.portals=links.ToArray();
        routes.entrances=links.Select(link=>link.item).ToArray();
        routes.hallDoor=links[10].item;
        routes.detourDoor=links[14].item;
        routes.tentacles = new[] { 6f, 10f, 14f }.Select(column =>
        {
            var arm=Group(root,"Tentacle pivot",CafeWorld.PlanPoint(14, column, 7.6f),14);
            Sprite(arm,"Tentacle",new Vector2(0,1.3f),new Vector2(0.55f,3.1f),new Color32(105,57,81,255),5,"circle");
            for(int i=1;i<=5;i++) Sprite(arm,"Sucker",new Vector2(0.2f,i*0.47f),Vector2.one*0.18f,new Color32(192,103,103,255),6,"circle");
            return arm;
        }).ToArray();
        routes.tentacleWarnings = routes.tentacles.Select(arm =>
            Sprite(root, "Tentacle sweep", new Vector2(arm.position.x, CafeWorld.RoomCenter(14).y),
                new Vector2(2, CafeWorld.Sizes[6].y), new Color32(128, 44, 66, 95), -6)).ToArray();
        foreach (var warning in routes.tentacleWarnings)
        {
            warning.gameObject.layer = 14;
            warning.enabled = false;
        }
        routes.pursuer=Group(root,"Pursuer",new Vector3(80.8f,0,0),15);
        Shade(routes.pursuer,out _,out _,true);
        DressShortcutCorridors(routes);
    }

    internal static void DressShortcutCorridors(NightRoutes routes)
    {
        var guideLight = new Color32(176, 187, 206, 255);
        var guideMark = new Color32(89, 99, 124, 255);
        var lightSpill = new Color(.40f, .46f, .64f, .12f);
        foreach (int layer in new[] { NightRoutes.TentacleCorridor, NightRoutes.PursuitCorridor })
        {
            string roomName = CafeWorld.RoomNames[layer - 8];
            var room = routes.routeObjects.transform.Cast<Transform>()
                .Single(child => child.gameObject.layer == layer && child.Find("Floor") != null);
            room.name = roomName;
            var props = room.Find("Shortcut Signs and Lamps") ?? room.Find("Warning Signs and Lamps");
            room.Find("Floor").GetComponent<SpriteRenderer>().color = new Color32(144, 154, 173, 255);
            if (props != null)
            {
                props.name = "Shortcut Signs and Lamps";
                foreach (var text in props.GetComponentsInChildren<Text>(true))
                {
                    text.text = roomName;
                    text.fontSize = 18;
                    text.rectTransform.anchoredPosition = new Vector2(6, 0);
                }
                foreach (var graphic in props.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (graphic.name is "Warning Light" or "Emergency Lamp" or "Guide Lamp")
                    {
                        graphic.name = "Guide Lamp";
                        graphic.color = guideLight;
                    }
                    else if (graphic.name is "Hazard Stripe" or "Floor Marker")
                    {
                        graphic.name = "Floor Marker";
                        graphic.color = guideMark;
                    }
                    else if (graphic.name == "Light Spill") graphic.color = lightSpill;
                }
                continue;
            }
            props = Group(room, "Shortcut Signs and Lamps");
            Vector2 size = CafeWorld.Bounds(layer).size;
            foreach (int side in new[] { -1, 1 })
            {
                float end = side * (size.x / 2 - 1.75f);
                var sign = Group(props, side < 0 ? "Lobby Approach" : "Far Approach",
                    new Vector3(end, size.y / 2 - 0.72f, 0));
                Sprite(sign, "Signboard", Vector2.zero, new Vector2(2.9f, .72f), new Color32(40, 27, 44, 255), 5);
                Sprite(sign, "Guide Lamp", new Vector2(-1.22f, 0), new Vector2(.14f, .31f), guideLight, 6);
                var text = Writing(sign, roomName, CafeUi.Paper, 18);
                text.rectTransform.sizeDelta = new Vector2(145, 50);
                text.rectTransform.anchoredPosition = new Vector2(6, 0);
                for (int stripe = 0; stripe < 4; stripe++)
                    Sprite(props, "Floor Marker", new Vector2(end, (stripe - 1.5f) * .58f),
                        new Vector2(.24f, .26f), guideMark, -8);
            }
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2 mark = new Vector2(offset * 4, size.y / 2 - .27f);
                Sprite(props, "Guide Lamp", mark, new Vector2(.58f, .12f), guideLight, 5);
                Sprite(props, "Light Spill", mark + Vector2.down * .39f, new Vector2(1.8f, 1.1f), lightSpill, -7, "circle");
            }
        }

        foreach (var link in routes.portals)
        {
            int from = link.item.gameObject.layer;
            int destination = link.paired.gameObject.layer;
            if (!NightRoutes.IsHazardCorridor(destination)) continue;
            var farExit = routes.portals.First(exit =>
                exit.item.gameObject.layer == destination && exit.paired.gameObject.layer != from);
            string exitName = CafeWorld.RoomNames[farExit.paired.gameObject.layer - 8];
            link.item.name = link.item.title = $"{exitName} 지름길";
        }
        foreach (var sprite in routes.pursuer.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite.name != "Eye") sprite.color = new Color32(85, 40, 59, 255);
        }
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
