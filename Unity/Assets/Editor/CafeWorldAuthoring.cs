using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NightCafe
{
    public sealed class CafeWorldAuthoring
    {
        public readonly List<CafeInteractable> Interactables = new List<CafeInteractable>();
        public Transform Root { get; }
        public Transform Cup { get; private set; }
        public SpriteRenderer Flood { get; private set; }
        public Transform[] CoffeeDrops { get; } = new Transform[8];
        public NightPlayer Player { get; private set; }
        public static readonly Vector2 SecuritySpawn = new Vector2(-14, 6);
        public static readonly Vector2 IncidentSpawn = new Vector2(-3.5f, -4);
        public static readonly Vector2[] CameraPositions = { new Vector2(3, 2), new Vector2(-13, 6), new Vector2(16.5f, 5), new Vector2(16.5f, -5) };
        public static readonly string[] CameraNames = { "카페 홀", "감시실", "창고", "숙직실" };
        private static readonly Color Wood = new Color32(100, 82, 66, 255);
        private static readonly Color Teal = new Color32(64, 117, 112, 255);

        // Keep the floor plan visible, but reveal objects only in the player's room.
        // CCTV uses its own mask, so leaving security really loses remote observation.
        public static int RoomLayer(Vector2 position)
        {
            if (position.x < -7) return position.y > 2 ? 8 : 9;
            if (position.x > 13) return position.y > 1 ? 11 : 12;
            return 10;
        }
        public static int RoomMask(Vector2 position) => 1 | (1 << RoomLayer(position));

        public CafeWorldAuthoring(bool withPlayer)
        {
            Root = new GameObject("Cafe World").transform;
            Floor("SECURITY", new Vector2(-13, 6), new Vector2(12, 8), new Color32(33, 49, 53, 255));
            Floor("LOBBY", new Vector2(-13, -4.5f), new Vector2(12, 13), new Color32(39, 48, 48, 255));
            Floor("CAFE", new Vector2(3.5f, 0), new Vector2(19, 22), new Color32(62, 61, 53, 255));
            Floor("STORAGE", new Vector2(16.5f, 6), new Vector2(7, 10), new Color32(41, 53, 51, 255));
            Floor("NIGHT ROOM", new Vector2(16.5f, -5), new Vector2(7, 12), new Color32(41, 48, 55, 255));
            for (int y = -10; y <= 10; y += 2)
                Shape("Floor seam", new Vector2(3.5f, y), new Vector2(18.8f, 0.025f), new Color(0, 0, 0, 0.12f), -9, false, Root);
            Wall(new Vector2(0.5f, 11), new Vector2(40, 0.35f));
            Wall(new Vector2(0.5f, -11), new Vector2(40, 0.35f));
            Wall(new Vector2(-19, 0), new Vector2(0.35f, 22));
            Wall(new Vector2(20, 0), new Vector2(0.35f, 22));
            HorizontalWall(-19, -14.3f, 2); HorizontalWall(-11.7f, -7, 2);
            VerticalWall(-7, -11, -5.3f); VerticalWall(-7, -2.7f, 11);
            VerticalWall(13, -11, -6.3f); VerticalWall(13, -3.7f, 3.7f); VerticalWall(13, 6.3f, 11);
            HorizontalWall(13, 20, 1);
            Door("감시실 문", new Vector2(-13, 2), new Vector2(2.6f, 0.3f), true);
            Door("카페 문", new Vector2(-7, -4), new Vector2(0.3f, 2.6f));
            Door("창고 문", new Vector2(13, 5), new Vector2(0.3f, 2.6f));
            Door("숙직실 문", new Vector2(13, -5), new Vector2(0.3f, 2.6f));

            Furniture("Security desk", new Vector2(-14, 8.4f), new Vector2(7, 1.4f), Wood);
            var monitor = Item(InteractionKind.Camera, "CCTV 단말기", new Vector2(-15, 7.5f), new Vector2(1.5f, 0.8f), Teal);
            monitor.description = "감시실에서만 카메라를 볼 수 있다.";
            Item(InteractionKind.Note, "사장의 포스트잇", new Vector2(-11.6f, 7.5f), new Vector2(0.55f, 0.55f), CafeUi.Amber);
            Item(InteractionKind.RuleBook, "야간 근무 규칙서", new Vector2(-17, 4.4f), new Vector2(0.75f, 1), CafeUi.Paper);
            Label("감시실", new Vector2(-13, 10), 30);
            Label("로비", new Vector2(-13, -8.5f), 30);
            Label("카페 홀", new Vector2(1, -8.5f), 30);
            Label("창고", new Vector2(16.5f, 9.5f), 26);
            Label("숙직실", new Vector2(16.5f, -9), 26);

            Furniture("Bar counter", new Vector2(5.5f, 4), new Vector2(10, 0.9f), Wood);
            Furniture("Back bar", new Vector2(5.5f, 9.2f), new Vector2(11, 1.3f), Wood);
            Item(InteractionKind.Ingredient, "우유", new Vector2(2, 7.8f), new Vector2(0.7f, 0.9f), CafeUi.Paper).ingredient = Ingredient.Milk;
            Item(InteractionKind.Ingredient, "초콜릿", new Vector2(5, 7.8f), new Vector2(0.8f, 0.7f), new Color32(164, 102, 75, 255)).ingredient = Ingredient.Chocolate;
            Item(InteractionKind.Ingredient, "아몬드", new Vector2(8, 7.8f), new Vector2(0.7f, 0.8f), CafeUi.Amber).ingredient = Ingredient.Almond;
            Item(InteractionKind.Ingredient, "타피오카", new Vector2(3.5f, 5.8f), new Vector2(0.7f, 0.7f), new Color32(125, 110, 140, 255)).ingredient = Ingredient.Tapioca;
            Item(InteractionKind.Ingredient, "쿠킹크림", new Vector2(7, 5.8f), new Vector2(0.7f, 0.8f), new Color32(177, 197, 178, 255)).ingredient = Ingredient.Cream;
            Label("제조 공간", new Vector2(5, 10.1f), 24);

            foreach (var pos in new[] { new Vector2(-2, 0.8f), new Vector2(8.5f, -1.5f), new Vector2(0, -5), new Vector2(8, -6) })
            {
                Furniture("Cafe table", pos, new Vector2(2.2f, 1.35f), Wood);
                Furniture("Chair", pos + Vector2.up * 1.2f, new Vector2(0.8f, 0.7f), Teal);
                Furniture("Chair", pos + Vector2.down * 1.2f, new Vector2(0.8f, 0.7f), Teal);
                Shape("Table runner", pos, new Vector2(0.45f, 1.2f), new Color32(171, 157, 126, 255), 2, false, Root);
            }
            var cup = Item(InteractionKind.Cup, "공중에 뜬 커피 컵", new Vector2(3, 1), new Vector2(0.65f, 0.65f), CafeUi.Paper, true);
            Cup = cup.transform;
            Shape("Coffee", Vector2.zero, new Vector2(0.43f, 0.43f), new Color32(63, 37, 26, 255), 5, true, Cup);
            Shape("Cup handle", new Vector2(0.4f, 0), new Vector2(0.3f, 0.22f), CafeUi.Paper, 4, true, Cup);
            Shape("Anomaly ring", new Vector2(3, 1), new Vector2(1.8f, 0.8f), new Color(0.9f, 0.6f, 0.25f, 0.18f), -1, true, Root).gameObject.layer = 10;
            Item(InteractionKind.Recipe, "바닥에 떨어진 레시피", new Vector2(-3.5f, -2.2f), new Vector2(0.7f, 0.9f), CafeUi.Amber);
            Flood = Shape("Coffee flood", new Vector2(3, 0), new Vector2(19, 22), new Color(0.25f, 0.11f, 0.04f, 0), 3, false, Root);
            Flood.gameObject.layer = 10;
            for (int i = 0; i < CoffeeDrops.Length; i++)
            {
                var drop = Shape("Coffee droplet", new Vector2(3, 1), Vector2.one * 0.12f,
                    new Color32(159, 97, 48, 255), 5, true, Root);
                drop.gameObject.layer = 10;
                drop.gameObject.SetActive(false);
                CoffeeDrops[i] = drop.transform;
            }

            Furniture("Storage shelf", new Vector2(18.5f, 5.8f), new Vector2(1.4f, 6), Wood);
            Item(InteractionKind.Inspect, "창고의 수납장", new Vector2(16.5f, 7), Vector2.one, Teal).description = "예비 컵과 청소 도구가 가지런하다. 재료는 카페 제조 공간에 있다.";
            Furniture("Bed", new Vector2(17, -6), new Vector2(2.2f, 3.7f), Teal);
            Item(InteractionKind.Inspect, "숙직 일지", new Vector2(15, -2.5f), new Vector2(0.7f, 0.8f), CafeUi.Paper).description = "10월 25일. 첫 근무.\n\n사장님은 규칙서를 꼭 읽으라고 했다. 커피 냄새가 유난히 짙다.";
            Plant(new Vector2(-17.5f, -9)); Plant(new Vector2(11.4f, 9.3f)); Plant(new Vector2(11.5f, -9));
            if (withPlayer)
            {
                var player = Shape("Player", SecuritySpawn, new Vector2(0.62f, 0.62f), CafeUi.Green, 8, true).gameObject;
                Shape("Head", new Vector2(0, 0.16f), new Vector2(0.35f, 0.35f), CafeUi.Paper, 9, true, player.transform);
                player.AddComponent<Rigidbody2D>();
                player.AddComponent<CircleCollider2D>();
                Player = player.AddComponent<NightPlayer>();
                Player.flashlight = Flashlight(player.transform);
                // Collider uses world dimensions; visuals stay independently sized.
                player.transform.localScale = Vector3.one;
                player.GetComponent<SpriteRenderer>().size = new Vector2(0.62f, 0.62f);
                player.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Sliced;
            }
        }

        private static Sprite Sprite(bool round) => CafeSceneAuthoring.Sprite(round ? "circle" : "pixel");

        public static SpriteRenderer Shape(string name, Vector2 pos, Vector2 size, Color color, int order, bool round = false, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            if (parent != null) go.layer = parent.gameObject.layer;
            go.transform.localPosition = pos;
            // Geometry uses renderer size so children and colliders keep world-unit transforms.
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite(round);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void Floor(string name, Vector2 pos, Vector2 size, Color color) => Shape(name, pos, size, color, -10, false, Root);
        private void Wall(Vector2 pos, Vector2 size)
        {
            var wall = Shape("Wall", pos, size, new Color32(114, 132, 122, 255), 4, false, Root);
            wall.gameObject.AddComponent<BoxCollider2D>().size = size;
        }
        private void HorizontalWall(float left, float right, float y) => Wall(new Vector2((left + right) / 2, y), new Vector2(right - left, 0.25f));
        private void VerticalWall(float x, float bottom, float top) => Wall(new Vector2(x, (bottom + top) / 2), new Vector2(0.25f, top - bottom));
        private void Furniture(string name, Vector2 pos, Vector2 size, Color color)
        {
            var value = Shape(name, pos, size, color, 1, false, Root);
            value.gameObject.layer = RoomLayer(pos);
            value.gameObject.AddComponent<BoxCollider2D>().size = size;
            Shape("Edge", pos + new Vector2(0, size.y * 0.35f), new Vector2(size.x * 0.94f, 0.045f), new Color(1, 1, 1, 0.15f), 2, false, Root).gameObject.layer = RoomLayer(pos);
        }
        private CafeInteractable Item(InteractionKind kind, string title, Vector2 pos, Vector2 size, Color color, bool round = false)
        {
            var visual = Shape(title, pos, size, color, 4, round, Root);
            visual.gameObject.layer = kind == InteractionKind.Door ? 0 : RoomLayer(pos);
            var item = visual.gameObject.AddComponent<CafeInteractable>();
            item.kind = kind; item.title = title; item.ConfigureMarker(visual);
            Interactables.Add(item);
            if (kind == InteractionKind.Ingredient) Label(title, pos + new Vector2(0, -0.7f), 18, true);
            return item;
        }
        private void Door(string title, Vector2 pos, Vector2 size, bool security = false)
        {
            var item = Item(InteractionKind.Door, title, pos, size, Teal);
            item.gameObject.AddComponent<BoxCollider2D>().size = size;
            item.securityDoor = security;
            item.SetDoor(false);
        }
        private void Plant(Vector2 pos)
        {
            Shape("Planter", pos, Vector2.one * 0.8f, Wood, 2, true, Root).gameObject.layer = RoomLayer(pos);
            Shape("Plant", pos, Vector2.one * 1.2f, new Color32(65, 100, 78, 255), 3, true, Root).gameObject.layer = RoomLayer(pos);
        }
        private void Label(string text, Vector2 pos, int size, bool roomOnly = false)
        {
            var go = new GameObject(text + " label", typeof(Canvas));
            go.transform.SetParent(Root, false); go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.018f;
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 6;
            var label = CafeUiAuthoring.Label(go.transform, text, -180, -20, 360, 60, size, CafeUi.Muted, TextAnchor.MiddleCenter);
            if (roomOnly) { go.layer = RoomLayer(pos); label.gameObject.layer = go.layer; }
            label.GetComponent<RectTransform>().localRotation = Quaternion.identity;
        }

        public static Transform Flashlight(Transform parent)
        {
            var beam = Shape("Flashlight", new Vector2(0, 2.5f), new Vector2(4.2f, 5), new Color(1, 0.9f, 0.6f, 0.22f), 7, false, parent);
            beam.sprite = CafeSceneAuthoring.Sprite("flashlight");
            var pivot = new GameObject("Flashlight Pivot").transform;
            pivot.SetParent(parent, false);
            beam.transform.SetParent(pivot, false);
            return pivot;
        }
    }
}
