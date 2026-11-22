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
        public static readonly Vector2 IncidentSpawn = CafeWorld.IncidentSpawn;
        public static readonly Vector2[] CameraPositions = { new Vector2(3, 2), new Vector2(-13, 6), new Vector2(16.5f, 5), new Vector2(16.5f, -5) };
        public static readonly string[] CameraNames = { "카페 홀", "감시실", "창고", "숙직실" };
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
            Furniture("Security desk", new Vector2(-14,8.4f),new Vector2(6,0.8f),Wood);
            Item(InteractionKind.Camera,"CCTV 단말기",new Vector2(-15,7.5f),new Vector2(1.1f,0.7f),Teal);
            Item(InteractionKind.Note,"사장의 포스트잇",new Vector2(-11.7f,7.5f),Vector2.one*0.55f,CafeUi.Amber);
            Item(InteractionKind.RuleBook,"전 알바생의 노트",new Vector2(-17,6),new Vector2(0.7f,0.9f),CafeUi.Paper);
            Furniture("Bar counter",new Vector2(3,9),new Vector2(9,0.7f),Wood);
            Furniture("Back bar",new Vector2(3,13),new Vector2(10,0.8f),Wood);
            for(int i=0;i<5;i++)
                Item(InteractionKind.Ingredient,GameTuning.IngredientName((Ingredient)i),new Vector2(-1+2*i,11.5f),new Vector2(0.65f,0.75f),Color.white).ingredient=(Ingredient)i;
            var microwave = Item(InteractionKind.NightTask,"전자레인지 · 우유 데우기",new Vector2(8.5f,11),new Vector2(0.9f,0.7f),Teal);
            microwave.nightAction = NightAction.Microwave;
            foreach(var pos in new[] {new Vector2(-2,-5.5f),new Vector2(7,-5.5f),new Vector2(-2,-10),new Vector2(7,-10)})
            {
                Furniture("Cafe table",pos,new Vector2(2.2f,1.35f),Wood);
                Furniture("Chair",pos+Vector2.up*1.2f,new Vector2(0.8f,0.7f),Teal);
                Furniture("Chair",pos-Vector2.up*1.2f,new Vector2(0.8f,0.7f),Teal);
            }
            Cup = Item(InteractionKind.Cup,"공중에 뜬 에스프레소",new Vector2(3,-6),Vector2.one*0.65f,CafeUi.Paper).transform;
            Item(InteractionKind.Recipe,"카페모카 레시피",new Vector2(-4,-9),new Vector2(0.7f,0.9f),CafeUi.Paper);
            Flood = Shape("Coffee flood",new Vector2(3,-8),new Vector2(19,11),new Color(0.25f,0.1f,0.03f,0),3,false,Root);
            Flood.gameObject.layer=10;
            for(int i=0;i<CoffeeDrops.Length;i++)
            {
                CoffeeDrops[i] = Shape("Coffee droplet",new Vector2(3,-6),Vector2.one*0.12f,new Color32(159,97,48,255),5,true,Root).transform;
                CoffeeDrops[i].gameObject.layer=10; CoffeeDrops[i].gameObject.SetActive(false);
            }
            Furniture("Storage shelf",new Vector2(23.5f,7),new Vector2(1.2f,4),Wood);
            Furniture("Bed",new Vector2(22,-7),new Vector2(2.2f,3.7f),Teal);
            Plant(new Vector2(-19,-10)); Plant(new Vector2(11,-12));
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
