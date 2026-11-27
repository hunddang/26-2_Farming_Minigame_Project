using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NightCafe;
using UnityEditor;
using UnityEngine;

internal static class CafeMapArt
{
    private const string Pieces = "Assets/Art/MapPieces/";
    private const string Furniture = "Assets/Art/furniture/";

    private static readonly string[] InteriorRooms = { "Security Room", "Lobby", "Cafe Hall", "Storage", "Night Room", "Counter" };
    private static readonly string[] InteriorFloors = { "SECURITY", "LOBBY", "CAFE", "STORAGE", "NIGHT ROOM", "COUNTER" };
    private static readonly string[] Floors = {
        "security-tile", "lobby-wood", "hall-wood", "storage-wood", "staff-tile", "counter-wood",
        "utility-tile", "service-tile", "parking-asphalt", "service-tile", "utility-tile"
    };
    private static readonly string[] Walls = {
        "security-wall", "lobby-wall", "hall-wall", "storage-wall", "staff-wall", "counter-wall",
        "utility-wall", "service-wall", "parking-edge", "service-wall", "utility-wall"
    };

    private static readonly Dictionary<string, string> Props = new Dictionary<string, string>
    {
        { "Security desk", Furniture + "cctv-desk-front" },
        { "Lobby bench", Furniture + "bench-front" },
        { "Plant", Furniture + "plant-front" },
        { "Storage shelf", Furniture + "shelf-front" },
        { "Supply crates", Furniture + "crate-front" },
        { "Storage cabinet", Furniture + "staff-desk-front" },
        { "Bed", Furniture + "bed-iso" },
        { "Staff desk", Furniture + "staff-desk-front" },
        { "Staff table", Pieces + "hall-table" },
        { "Chair", Pieces + "hall-chair" },
        { "Sink unit", Pieces + "steel-sink" },
        { "Cafe exterior wall", Pieces + "parking-vents" },
        { "Cafe exterior windows", Pieces + "parking-window" },
        { "Parked car A", Pieces + "car-silver" },
        { "Parked car B", Pieces + "car-blue" },
        { "Flower bed A", Pieces + "parking-flowers" },
        { "Flower bed B", Pieces + "parking-flowers" },
        { "Service cabinet A", Furniture + "shelf-front" },
        { "Service cabinet B", Furniture + "shelf-iso" },
        { "Service cabinet C", Furniture + "shelf-front" }
    };

    public static void ImportSprites()
    {
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Pieces, "*.png").Concat(Directory.GetFiles(Furniture, "*.png")))
        {
            string assetPath = path.Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (assetPath.StartsWith(Pieces, StringComparison.Ordinal))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = CafeWorld.PixelsPerTile;
            }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    public static void ApplyTable(SpriteRenderer renderer)
        => Style(renderer, Pieces + "hall-table", renderer.size);

    public static void ApplyWorld(CafeWorld world)
    {
        OrganizeWalls(world);
        for (int index = 0; index < InteriorRooms.Length; index++)
        {
            int layer = index + 8;
            var room = world.transform.Find(InteriorRooms[index]);
            var structure = room.Find("Floor & Walls");
            var furniture = room.Find("Furniture");
            ApplyStructure(structure, layer, InteriorFloors[index]);
            StyleFurniture(furniture);

            switch (layer)
            {
                case 8:
                    Patch(structure, layer, "Security rug", "security-rug", 5.0f, 4.85f, 3.45f, 1.8f);
                    Patch(structure, layer, "Monitor bank", "security-screens", 3.45f, .44f, 3.4f, .88f, -2);
                    break;
                case 9:
                    Patch(structure, layer, "Lobby windows", "lobby-window", 2.1f, .47f, 3.4f, .85f, -2);
                    Patch(structure, layer, "Illuminated menu board", "lobby-sign", 9.1f, .52f, 2.45f, .92f, -2);
                    Patch(structure, layer, "Bench plinth", "security-tile", 2, 7.1f, 2.75f, 1.65f, -9, true);
                    TileExisting(furniture, "Lobby planter", "security-tile", new Color32(183, 216, 215, 255));
                    Patch(structure, layer, "Main doorway mat", "entry-mat", 1.0f, 4.5f, 1.28f, 1.5f);
                    Patch(structure, layer, "Upper shortcut mat", "entry-mat", 12.07f, 2.65f, 1.25f, 1.42f);
                    Patch(structure, layer, "Lower shortcut mat", "entry-mat", 12.07f, 6.65f, 1.25f, 1.42f);
                    break;
                case 10:
                    for (int i = 0; i < 3; i++)
                        Patch(structure, layer, $"Hall window {i + 1}", "hall-window", 4.35f + 5.15f * i, .52f, 4.95f, 1.04f, -2);
                    foreach (float row in new[] { 3.75f, 7.25f })
                        Patch(structure, layer, $"Cross-aisle carpet {row}", "hall-carpet", 9.5f, row, 18.25f, .85f, -9, true);
                    foreach (float column in new[] { 2.65f, 7.25f, 11.75f, 16.35f })
                        Patch(structure, layer, $"Long aisle carpet {column}", "hall-carpet", column, 5.7f, 10.1f, .65f, -8, true, 90);
                    var tables = furniture.Cast<Transform>().Where(child => child.name == "Cafe table").ToArray();
                    foreach (var chair in furniture.GetComponentsInChildren<SpriteRenderer>(true).Where(renderer => renderer.name == "Chair"))
                    {
                        var table = tables.OrderBy(child => (child.position - chair.transform.position).sqrMagnitude).First();
                        chair.flipX = chair.transform.position.x > table.position.x;
                    }
                    break;
                case 11:
                    Patch(structure, layer, "Stone storage aisle", "storage-aisle", 5.5f, 4.55f, 10.5f, 1.4f, -9, true);
                    Patch(structure, layer, "Stone entry aisle", "storage-aisle", 5.0f, 6.55f, 1.5f, 2.7f, -9, true);
                    for (int i = 0; i < 3; i++)
                    {
                        Patch(furniture, layer, $"Shelf supplies {i + 1}", "storage-boxes", 2 + 3.5f * i, 1.8f, 1.36f, .43f, 2);
                        Patch(furniture, layer, $"Shelf boxes {i + 1}", "storage-boxes", 2 + 3.5f * i, 2.47f, 1.08f, .35f, 2);
                    }
                    break;
                case 12:
                    Patch(structure, layer, "Left rain window", "staff-window", 2.55f, .54f, 3.12f, 1.04f, -2);
                    Patch(structure, layer, "Right rain window", "staff-window", 8.15f, .54f, 3.12f, 1.04f, -2);
                    Patch(structure, layer, "Cross-aisle runner", "staff-aisle", 5.5f, 4.45f, 10.53f, 1.08f, -9, true);
                    Patch(structure, layer, "Entry runner", "staff-aisle", 5, 4.00f, 1.35f, 7.78f, -8, true);
                    var locker = furniture.Find("Staff locker");
                    locker.GetComponent<SpriteRenderer>().enabled = false;
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 center = locker.position + Vector3.right * (i == 0 ? -.47f : .47f);
                        Art(locker, layer, $"Steel locker {i + 1}", Furniture + "locker-front", center, new Vector2(.92f, 2), 1);
                    }
                    break;
                case 13:
                    Patch(structure, layer, "Bar work tiles", "counter-tile", 7f, 2.0f, 13.65f, 3.9f, -9, true);
                    Patch(structure, layer, "Service entrance tiles", "counter-tile", 1.25f, 6.42f, 2.15f, 5.15f, -9, true);
                    TileExisting(furniture, "Back bar", "back-bar-face");
                    TileExisting(furniture, "Bar counter", "counter-face");
                    Patch(furniture, layer, "Espresso machine", "espresso-machine", 5.62f, .68f, 1.23f, .9f, 2);
                    Patch(furniture, layer, "Pastry display", "pastry-case", 11.75f, 4.21f, 2.45f, 1f, 2);
                    break;
            }
        }
    }

    public static void ApplyRoutes(NightRoutes routes)
    {
        for (int layer = 14; layer <= 18; layer++)
        {
            var room = routes.routeObjects.transform.Find(CafeWorld.RoomNames[layer - 8]);
            ApplyStructure(room, layer, "Floor");
            StyleFurniture(room);
            if (layer is 17 or 18)
            {
                TileExisting(room, "Equipment partition", "equipment-panel");
                float partitionEnd = CafeWorld.Sizes[layer - 8].y - 2;
                Patch(room, layer, "Partition base", "steel-rail", 9, partitionEnd, 12, .27f, 4, true);
                for (int i = 0; i < 3; i++)
                    Patch(room, layer, $"Partition vents {i + 1}", "parking-vents", 5.15f + 3.85f * i, 1.25f, 1.35f, 1.15f, 4);
            }
            if (layer != 16) continue;

            Patch(room, layer, "Paved frontage", "parking-pavers", 2.35f, 5.5f, 1.45f, 10.7f, -9, true);
            Patch(room, layer, "Occupied bay A", "parking-bay", 7, 2, 2.85f, 4.7f, -8, false, 90);
            Patch(room, layer, "Occupied bay B", "parking-bay", 13, 2, 2.85f, 4.7f, -8, false, 90);
            Patch(room, layer, "Open bay A", "parking-bay", 8.7f, 8.3f, 2.35f, 2.15f, -8);
            Patch(room, layer, "Open bay B", "parking-bay", 11.1f, 8.3f, 2.35f, 2.15f, -8);
        }
    }

    private static void OrganizeWalls(CafeWorld world)
    {
        foreach (var wall in world.GetComponentsInChildren<BoxCollider2D>(true).Where(collider => collider.name == "Wall"))
        {
            Vector2 position = wall.transform.position;
            bool vertical = wall.size.x < wall.size.y;
            for (int index = 0; index < InteriorRooms.Length; index++)
            {
                int layer = index + 8;
                Vector2 center = CafeWorld.RoomCenter(layer);
                Vector2 size = CafeWorld.Sizes[index];
                Vector2 edge = vertical ? Vector2.right * size.x / 2 : Vector2.up * size.y / 2;
                if (Mathf.Min((position - center - edge).sqrMagnitude, (position - center + edge).sqrMagnitude) > .001f)
                    continue;
                var parent = world.transform.Find(InteriorRooms[index] + "/Floor & Walls");
                if (wall.transform.parent != parent) wall.transform.SetParent(parent, true);
                wall.gameObject.layer = layer;
                break;
            }
        }
    }

    private static void ApplyStructure(Transform parent, int layer, string floorName)
    {
        int index = layer - 8;
        var size = CafeWorld.Sizes[index];
        var floor = parent.Find(floorName).GetComponent<SpriteRenderer>();
        Style(floor, Pieces + Floors[index], size, true);
        foreach (var wall in parent.Cast<Transform>().Where(child => child.name == "Wall"))
        {
            var renderer = wall.GetComponent<SpriteRenderer>();
            Style(renderer, Pieces + (renderer.size.x < renderer.size.y ? "steel-rail-side" : "steel-rail"), renderer.size, true);
        }
        Patch(parent, layer, "Rear wall facing", Walls[index], size.x / 2, .49f, size.x - .25f, .98f, -4, true);
    }

    private static void StyleFurniture(Transform parent)
    {
        foreach (var renderer in parent.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.name == "Cafe table")
            {
                ApplyTable(renderer);
                continue;
            }
            if (!Props.TryGetValue(renderer.name, out string path)) continue;
            Vector2 size = renderer.size;
            if (renderer.name == "Security desk") size.y = 1.43f;
            if (renderer.name == "Plant") size = new Vector2(1.38f, 2f);
            Style(renderer, path, size);
        }
    }

    private static void TileExisting(Transform parent, string name, string piece, Color? tint = null)
    {
        var renderer = parent.Find(name).GetComponent<SpriteRenderer>();
        Style(renderer, Pieces + piece, renderer.size, true);
        renderer.color = tint ?? Color.white;
    }

    private static void Patch(Transform parent, int layer, string name, string piece, float column, float row,
        float width, float height, int order = -9, bool tiled = false, float angle = 0)
        => Art(parent, layer, name, Pieces + piece, CafeWorld.PlanPoint(layer, column, row),
            new Vector2(width, height), order, tiled, angle);

    private static void Art(Transform parent, int layer, string name, string path, Vector2 position, Vector2 size,
        int order, bool tiled = false, float angle = 0)
    {
        var sprite = parent.Find(name);
        if (sprite == null)
        {
            sprite = new GameObject(name, typeof(SpriteRenderer)).transform;
            sprite.SetParent(parent, false);
        }
        sprite.gameObject.layer = layer;
        sprite.position = position;
        sprite.localRotation = Quaternion.Euler(0, 0, angle);
        var renderer = sprite.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = order;
        Style(renderer, path, size, tiled);
    }

    private static void Style(SpriteRenderer renderer, string path, Vector2 size, bool tiled = false)
    {
        string assetPath = path + ".png";
        renderer.sprite = assetPath.StartsWith(Furniture, StringComparison.Ordinal)
            ? AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().Single()
            : AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (renderer.sprite == null) throw new InvalidOperationException("Missing map sprite: " + assetPath);
        renderer.color = Color.white;
        renderer.drawMode = tiled ? SpriteDrawMode.Tiled : SpriteDrawMode.Sliced;
        renderer.size = size;
    }
}
