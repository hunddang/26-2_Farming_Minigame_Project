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
        private static readonly Color Wood = new Color32(100, 82, 66, 255);
        private static readonly Color Teal = new Color32(64, 117, 112, 255);

        // Keep the floor plan visible, but reveal objects only in the player's room.
        // CCTV uses its own mask, so leaving security really loses remote observation.
        public static int RoomLayer(Vector2 position) => CafeWorld.RoomLayer(position);
        public static int RoomMask(Vector2 position) => CafeWorld.RoomMask(position);

        public CafeWorldAuthoring(bool withPlayer)
        {
            Root = new GameObject("Cafe World").transform;
            for (int i=0;i<6;i++)
            {
                int layer = 8+i;
                Vector2 center = CafeWorld.Centers[i], size = CafeWorld.Sizes[i];
                Floor(i == 2 ? "CAFE" : i == 5 ? "COUNTER" : new[] { "SECURITY", "LOBBY", "CAFE", "STORAGE", "NIGHT ROOM" }[i], center, size, Color.white);
                Wall(center + Vector2.up*size.y/2, new Vector2(size.x,0.25f));
                Wall(center - Vector2.up*size.y/2, new Vector2(size.x,0.25f));
                Wall(center - Vector2.right*size.x/2, new Vector2(0.25f,size.y));
                Wall(center + Vector2.right*size.x/2, new Vector2(0.25f,size.y));
                Label(CafeWorld.RoomNames[i],center + new Vector2(0,-size.y/2+0.7f),24,true);
            }
            Vector2 At(int room, float column, float row) => CafeWorld.PlanPoint(room, column, row);
            Furniture("Security desk", At(8, 3.5f, 1.4f), new Vector2(5, .9f), Wood);
            Item(InteractionKind.Camera, "CCTV 단말기", At(8, 3.4f, 1.97f), new Vector2(1.05f, .71f), Teal);
            Item(InteractionKind.RuleBook, "전 알바생의 노트", At(8, 1.6f, 1.97f), new Vector2(.7f, .8f), CafeUi.Paper);

            Furniture("Lobby bench", At(9, 2, 7.1f), new Vector2(2, 1), Wood);
            Furniture("Lobby planter", At(9, 9.5f, 4.6f), new Vector2(2, 2), new Color32(63, 77, 91, 255));
            Plant(At(9, 9.5f, 4.6f));

            Furniture("Back bar", At(13, 9, .65f), new Vector2(9.5f, 1), Wood);
            Furniture("Bar counter", At(13, 8.4f, 4.35f), new Vector2(10.6f, 1), Wood);
            Furniture("Sink unit", At(13, 13.4f, 2.45f), new Vector2(1, 2.8f), Teal);
            Furniture("Register", At(13, 7.4f, 4.96f), new Vector2(1.05f, .28f), Teal);
            for (int i = 0; i < 5; i++)
                Item(InteractionKind.Ingredient, GameTuning.IngredientName((Ingredient)i),
                    At(13, 4 + 1.7f * i, 2.15f), new Vector2(.65f, .75f), Color.white).ingredient = (Ingredient)i;
            var microwave = Item(InteractionKind.NightTask, "전자레인지 · 우유 데우기",
                At(13, 11.65f, 3.2f), new Vector2(.9f, .7f), Teal);
            microwave.nightAction = NightAction.Microwave;

            foreach (float row in new[] { 2f, 5.5f, 9f })
            foreach (float column in new[] { 5f, 9.5f, 14f })
            {
                Vector2 pos = At(10, column, row);
                Furniture("Cafe table", pos, new Vector2(1.25f, 1.85f), Wood);
                Furniture("Chair", pos + Vector2.left * 1.05f, new Vector2(.8f, .78f), Teal);
                Furniture("Chair", pos + Vector2.right * 1.05f, new Vector2(.8f, .78f), Teal);
            }
            Cup = Item(InteractionKind.Cup, "공중에 뜬 에스프레소", At(10, 9.5f, 3.75f), Vector2.one * .65f, CafeUi.Paper).transform;
            Item(InteractionKind.Recipe, "카페모카 레시피", At(10, 2.45f, 6.5f), new Vector2(.7f, .9f), CafeUi.Paper);
            Flood = Shape("Coffee flood", CafeWorld.RoomCenter(10), CafeWorld.Sizes[2], new Color(.25f, .1f, .03f, 0), 3, false, Root);
            Flood.gameObject.layer = 10;
            for (int i = 0; i < CoffeeDrops.Length; i++)
            {
                CoffeeDrops[i] = Shape("Coffee droplet", Cup.position, Vector2.one * .12f, new Color32(159, 97, 48, 255), 5, true, Root).transform;
                CoffeeDrops[i].gameObject.layer = 10;
                CoffeeDrops[i].gameObject.SetActive(false);
            }

            foreach (float column in new[] { 2f, 5.5f, 9f })
                Furniture("Storage shelf", At(11, column, 2), new Vector2(2, 2), Wood);
            Furniture("Supply crates", At(11, 2, 6), new Vector2(2, 1), Wood);
            Furniture("Storage cabinet", At(11, 7.5f, 6), new Vector2(2, 1), Teal);

            Furniture("Bed", At(12, 2.5f, 2.85f), new Vector2(2.8f, 2), Teal);
            Furniture("Staff desk", At(12, 8, 2.2f), new Vector2(3, 1), Wood);
            Furniture("Staff locker", At(12, 2, 6.1f), new Vector2(2, 2), Teal);
            Furniture("Staff table", At(12, 7.6f, 6.05f), new Vector2(2, 1), Wood);
            if (withPlayer)
            {
                var player = new GameObject("Player");
                player.transform.position = CafeWorld.SecuritySpawn;
                player.AddComponent<Rigidbody2D>();
                player.AddComponent<CircleCollider2D>();
                Player = player.AddComponent<NightPlayer>();
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

        private void Floor(string name, Vector2 pos, Vector2 size, Color color) => Shape(name, pos, size, color, -10, false, Root).gameObject.layer = RoomLayer(pos);
        private void Wall(Vector2 pos, Vector2 size)
        {
            var wall = Shape("Wall", pos, size, new Color32(114, 132, 122, 255), 4, false, Root);
            wall.gameObject.layer = RoomLayer(pos - (pos - CafeWorld.RoomCenter(NearestRoom(pos))).normalized * 0.2f);
            wall.gameObject.AddComponent<BoxCollider2D>().size = size;
        }
        private static int NearestRoom(Vector2 position)
        {
            int room = 8; float closest = float.MaxValue;
            for(int i=8;i<=13;i++) { float d=(position-CafeWorld.RoomCenter(i)).sqrMagnitude; if(d<closest){closest=d;room=i;} }
            return room;
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

    }
}
