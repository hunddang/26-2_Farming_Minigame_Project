using System;
using System.Linq;
using NightCafe;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Edits the saved objects in place so NightGame's serialized references survive the reorganization.
internal static class NightCafeSceneLayout
{
    public static void OrganizeRooms(CafeWorld world)
    {
        foreach (Transform room in world.transform)
        {
            if (room == world.Player.transform || room.name == "Floor Plan") continue;
            foreach (var child in room.Cast<Transform>().ToArray())
            {
                if (child.name is "Floor & Walls" or "Furniture" or "Interactions" or "Effects") continue;
                string category = child.GetComponent<CafeInteractable>() != null ? "Interactions"
                    : child.name.StartsWith("Coffee", StringComparison.Ordinal) ? "Effects"
                    : child.name is "SECURITY" or "LOBBY" or "CAFE" or "STORAGE" or "NIGHT ROOM" or "COUNTER" or "Wall"
                        ? "Floor & Walls" : "Furniture";
                child.SetParent(Group(room, category), true);
            }
        }
    }

    public static void OrganizeTitle(StartScreen start)
    {
        GroupUi(start.transform, "Navigation", start.buttons.Select(button => button.transform).ToArray());
    }

    public static void Organize(NightGame game)
    {
        Scene scene = game.gameObject.scene;
        var story = game.story;
        var content = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<NightContent>(true)).Single();
        var routes = content.routes;
        var nights = Root(scene, "Night Events");
        content.transform.SetParent(nights, true);
        story.transform.SetParent(nights, true);
        content.name = "02–03 · Menu and Closing";
        story.name = "04–05 · Hunt and Investigation";
        story.huntObjects.name = "04 · Ceiling Hunter";
        story.finalObjects.name = "05 · Last Shift";

        var passageways = Root(scene, "Passages & Doors");
        routes.routeObjects.transform.SetParent(passageways, true);
        var doors = Group(routes.routeObjects.transform, "Doors by Room");
        var arrivals = Group(routes.routeObjects.transform, "Arrivals by Room");
        for (int i = 0; i < routes.portals.Length; i++)
        {
            var portal = routes.portals[i];
            string from = RoomName(portal.item.gameObject.layer);
            portal.item.transform.SetParent(Group(doors, from), true);
            if (portal.arrival == null)
            {
                var destination = Group(arrivals, RoomName(portal.paired.gameObject.layer));
                portal.arrival = Group(destination, $"From {from} · {i:00}");
                portal.arrival.position = portal.target;
                routes.portals[i] = portal;
            }
        }

        var hazards = Group(routes.routeObjects.transform, "Corridor Hazards");
        routes.pursuer.SetParent(hazards, true);
        foreach (var tentacle in routes.tentacles) tentacle.SetParent(hazards, true);
        foreach (var warning in routes.tentacleWarnings) warning.transform.SetParent(hazards, true);

        NameLabel(game.view.toast, "Feedback Message");
        NameLabel(game.view.prompt, "Interaction Prompt");
        NameLabel(game.view.feedTitle, "Camera Channel and Time");
        NameLabel(story.title, "Investigation Title");
        NameLabel(story.body, "Investigation Instructions");
        NameLabel(story.input, "Entered Value");
        for (int i = 0; i < story.choices.Length; i++) story.choices[i].name = $"Choice {i + 1}";

        var ui = game.view.transform;
        GroupUi(ui, "Night 05 UI", story.form.transform, story.cinema.transform);
        ConfigureCctv(game.view.feedImage);

        foreach (var task in story.huntTasks.Concat(story.finalTasks))
        {
            string art = task.storyAction switch
            {
                StoryAction.Extinguisher => "extinguisher",
                StoryAction.Net => "capture-net",
                StoryAction.Anchor => "anchor-pin",
                StoryAction.Badge => "staff-badge",
                StoryAction.ExhaustBox => "exhaust-crate",
                _ => null
            };
            if (art == null) continue;
            var renderer = task.GetComponentInChildren<SpriteRenderer>(true);
            Vector2 size = renderer.size;
            renderer.sprite = CafeSceneAuthoring.Sprite(art);
            renderer.size = size;
        }
    }

    public static void ApplyLook(GameObject[] roots)
    {
        foreach (var root in roots)
        {
            foreach (var label in root.GetComponentsInChildren<Text>(true)) label.color = Remap(label.color);
            foreach (var image in root.GetComponentsInChildren<Image>(true)) image.color = Remap(image.color);
            foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.backgroundColor = CafeUi.Ink;
            foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sprite.color = Remap(sprite.color);
                if (sprite.name == "Wall") sprite.color = new Color32(178, 185, 208, 255);
                if (sprite.name == "Guide light") sprite.color = CafeUi.Amber;
                if (sprite.name == "Tentacle") sprite.color = new Color32(122, 47, 62, 255);
                if (sprite.name == "Sucker") sprite.color = new Color32(199, 128, 129, 255);
                if (sprite.transform.parent != null && sprite.transform.parent.name == "낙하자")
                {
                    if (sprite.name == "Eye") sprite.color = CafeUi.Red;
                    else sprite.color = new Color32(131, 54, 68, 255);
                }
            }
        }
    }

    private static Color Remap(Color color)
    {
        Color32 value = color;
        Color mapped = (value.r, value.g, value.b) switch
        {
            (12, 22, 26) => CafeUi.Ink,
            (227, 232, 220) => CafeUi.Paper,
            (142, 163, 158) => CafeUi.Muted,
            (235, 187, 100) => CafeUi.Amber,
            (102, 200, 173) => CafeUi.Green,
            (235, 112, 98) => CafeUi.Red,
            (23, 37, 41) => new Color32(32, 32, 52, 255),
            (68, 88, 84) => new Color32(73, 80, 108, 255),
            (5, 10, 13) or (4, 8, 10) => CafeUi.Ink,
            _ => color
        };
        mapped.a = color.a;
        return mapped;
    }

    private static void ConfigureCctv(RawImage feed)
    {
        feed.color = new Color32(204, 223, 255, 255);
        const string name = "CCTV Scanlines";
        if (feed.transform.Find(name) != null) return;
        var lines = CafeUiAuthoring.Rect(feed.transform, name, 0, 0, 1280, 720).gameObject.AddComponent<RawImage>();
        lines.texture = CafeSceneAuthoring.Sprite("cctv-scanlines").texture;
        lines.color = new Color(0.05f, 0.065f, 0.13f, 0.22f);
        lines.uvRect = new Rect(0, 0, 160, 90);
        lines.raycastTarget = false;
        lines.transform.SetAsFirstSibling();
    }

    private static void NameLabel(Text text, string name) => text.name = name;
    private static string RoomName(int layer) => CafeWorld.RoomNames[layer - 8];

    private static Transform Root(Scene scene, string name)
    {
        var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        if (existing != null) return existing.transform;
        var group = new GameObject(name);
        SceneManager.MoveGameObjectToScene(group, scene);
        return group.transform;
    }

    private static Transform Group(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing;
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    private static void GroupUi(Transform parent, string name, params Transform[] children)
    {
        if (parent.Find(name) != null) return;
        int index = children.Min(child => child.GetSiblingIndex());
        var group = CafeUiAuthoring.Rect(parent, name, 0, 0, 1920, 1080);
        group.SetSiblingIndex(index);
        foreach (Transform child in children) child.SetParent(group, false);
    }
}
