using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NightCafe;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Cafe3DConversion
{
    private const string WorldPath = "Assets/Prefabs/CafeWorld.prefab";
    private const string GamePath = "Assets/Scenes/InGame.unity";
    private const string LobbyPath = "Assets/Scenes/Lobby.unity";

    [MenuItem("Night Cafe/Convert Existing Game to 3D")]
    public static void Convert()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before conversion.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save your open scenes before conversion.");
        string backup = Path.GetFullPath("../Artifacts/3DBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(backup);
        foreach (string path in new[] { WorldPath, GamePath, LobbyPath, "Assets/Prefabs/NightPlayer.prefab", "Assets/Prefabs/StartScreen.prefab" })
            File.Copy(path, Path.Combine(backup, Path.GetFileName(path)));
        var savedWorld = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPath).GetComponent<CafeWorld>();
        if (savedWorld.CctvMounts == null || savedWorld.CctvMounts.Length != 4) Cafe3DAssets.BuildAll();
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        settings.FindProperty("layers").GetArrayElementAtIndex(13).stringValue = "CCTV Full Walls";
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode3D;
        var root = PrefabUtility.LoadPrefabContents(WorldPath);
        try
        {
            var world = root.GetComponent<CafeWorld>();
            // Safe to repeat: an already converted world is never generated over.
            if (world.CctvMounts == null || world.CctvMounts.Length != 4)
            {
                UnpackChildren(root.transform);
                ConvertWorld(world);
            }
            EnsureCctvArchitecture(world);
            EnsureFloorEdges(world);
            PrefabUtility.SaveAsPrefabAsset(root, WorldPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        EnsureReusableTable();
        var scene = EditorSceneManager.OpenScene(GamePath);
        var game = UnityEngine.Object.FindFirstObjectByType<NightGame>();
        game.mapCamera.orthographic = false; game.mapCamera.fieldOfView = 48;
        game.mapCamera.nearClipPlane = 0.1f; game.mapCamera.farClipPlane = 100;
        var follow = game.mapCamera.GetComponent<PlayerFollowCamera>();
        follow.offset = new Vector3(0, 11, -9); follow.lookHeight = 0.55f;
        var p = game.world.Player.transform.position;
        game.mapCamera.transform.SetPositionAndRotation(p + follow.offset, Quaternion.LookRotation(Vector3.up * follow.lookHeight - follow.offset));
        game.mapCamera.cullingMask = CafeWorld.RoomMask(CafeWorld.SecuritySpawn);
        game.feedCamera.orthographic = false; game.feedCamera.nearClipPlane = 0.08f; game.feedCamera.farClipPlane = 70;
        game.feedCamera.allowHDR = false; game.feedCamera.enabled = false;
        game.world.ViewCctv(game.feedCamera, 0);
        ConfigureFeedMaterial(game.view);
        ConfigureLighting();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        scene = EditorSceneManager.OpenScene(LobbyPath);
        ConfigureLobby(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Validate();
        EditorSceneManager.OpenScene(GamePath);
        Debug.Log("NIGHT_CAFE_3D_CONVERSION_PASS: existing two scenes, room layout, interactions and rules retained.");
    }

    private static void UnpackChildren(Transform root)
    {
        foreach (Transform child in root.Cast<Transform>().ToArray())
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                PrefabUtility.UnpackPrefabInstance(child.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            UnpackChildren(child);
        }
    }

    private static void ConvertWorld(CafeWorld world)
    {
        var renderers = world.GetComponentsInChildren<SpriteRenderer>(true);
        var player = world.Player;
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            if (renderer.GetComponentInParent<NightPlayer>() != null) continue;
            var item = renderer.GetComponent<CafeInteractable>();
            if (item == null && renderer.GetComponentInParent<CafeInteractable>() != null) continue;
            var t = renderer.transform; Vector2 size = renderer.size;
            t.localPosition = CafeWorld.GroundPosition(new Vector2(t.localPosition.x, t.localPosition.y));
            t.localRotation = Quaternion.identity; t.localScale = Vector3.one;
            if (!renderer.gameObject.activeSelf && renderer.name != "Coffee droplet") continue;
            if (item != null) ConvertItem(item, size);
            else if (renderer.name is "CAFE" or "SECURITY" or "LOBBY" or "STORAGE" or "NIGHT ROOM") BuildFloor(t, size, renderer.name == "CAFE");
            else if (renderer.name == "Wall")
            {
                Cafe3DAssets.Box(t, "Cutaway wall", new Vector3(0, 0.54f, 0), new Vector3(size.x, 1.08f, size.y), "Wall");
                Cafe3DAssets.Box(t, "Skirting", new Vector3(0, 0.10f, 0), new Vector3(size.x + 0.035f, 0.19f, size.y + 0.035f), "Wall trim");
                Cafe3DAssets.Box(t, "Wall cap", new Vector3(0, 1.09f, 0), new Vector3(size.x + 0.025f, 0.045f, size.y + 0.025f), "Wood edge");
                Cafe3DAssets.Box(t, "CCTV full wall", new Vector3(0, 2.15f, 0), new Vector3(size.x, 2.14f, size.y), "Wall").gameObject.layer = 13;
            }
            else if (renderer.name == "Coffee flood")
            {
                var surface = Cafe3DAssets.Primitive(t, "Coffee surface", PrimitiveType.Quad, Vector3.zero, Vector3.one, "Flood");
                surface.localRotation = Quaternion.Euler(90, 0, 0); surface.localScale = new Vector3(18.95f, 21.95f, 1);
                world.Flood = surface.GetComponent<MeshRenderer>();
                // Keep the renderer directly on the original flood transform for level animation.
                UnityEngine.Object.DestroyImmediate(renderer);
                t.gameObject.AddComponent<MeshFilter>().sharedMesh = surface.GetComponent<MeshFilter>().sharedMesh;
                world.Flood = t.gameObject.AddComponent<MeshRenderer>(); world.Flood.sharedMaterial = Cafe3DAssets.Mat("Flood");
                t.localRotation = surface.localRotation; t.localScale = surface.localScale;
                UnityEngine.Object.DestroyImmediate(surface.gameObject);
            }
            else if (renderer.name == "Coffee droplet")
                Cafe3DAssets.Primitive(t, "Droplet", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.09f, 0.16f, 0.09f), "Coffee");
            else if (renderer.name == "Anomaly ring")
            {
                t.localPosition += Vector3.up * 0.035f;
                for (int i = 0; i < 16; i++)
                {
                    float a = i * Mathf.PI / 8;
                    Cafe3DAssets.Box(t, "Brass anomaly trace", new Vector3(Mathf.Cos(a) * 0.78f, 0, Mathf.Sin(a) * 0.78f), new Vector3(0.04f, 0.012f, 0.04f), "Brass");
                }
            }
            else
            {
                string model = renderer.name switch { "Security desk" => "Desk", "Bar counter" or "Back bar" => "Counter", "Cafe table" => "Table", "Chair" => "Chair", "Storage shelf" => "Shelf", "Bed" => "Bed", "Plant" => "Plant", _ => null };
                if (model != null)
                {
                    var visual = Cafe3DAssets.Model(t, model);
                    if (model != "Plant") visual.localScale = new Vector3(size.x, 1, size.y);
                    if (model == "Chair" && t.localPosition.z < 0) visual.localRotation = Quaternion.Euler(0, 180, 0);
                }
            }
            Cafe3DAssets.SetLayer(t, t.gameObject.layer);
        }
        foreach (var label in world.GetComponentsInChildren<Canvas>(true))
        {
            var t = label.transform;
            t.localPosition = CafeWorld.GroundPosition(new Vector2(t.localPosition.x, t.localPosition.y), 0.035f);
            t.localRotation = Quaternion.Euler(90, 0, 0);
        }
        // Record footprints before removing mutually exclusive Unity physics components.
        var footprints = world.GetComponentsInChildren<BoxCollider2D>(true).Select(collider => new {
            owner = collider.gameObject, size = collider.size, offset = collider.offset,
            trigger = collider.isTrigger, enabled = collider.enabled,
            height = collider.name == "Wall" || collider.GetComponent<CafeInteractable>()?.kind == InteractionKind.Door ? 3.25f : collider.name switch { "Chair" => 1.22f, "Storage shelf" => 2.5f, "Bed" => 0.9f, _ => 1.14f }
        }).ToArray();
        foreach (var old in world.GetComponentsInChildren<Collider2D>(true)) UnityEngine.Object.DestroyImmediate(old);
        foreach (var old in world.GetComponentsInChildren<Rigidbody2D>(true)) UnityEngine.Object.DestroyImmediate(old);
        foreach (var footprint in footprints)
        {
            var box = footprint.owner.AddComponent<BoxCollider>();
            box.size = new Vector3(footprint.size.x, footprint.height, footprint.size.y);
            box.center = new Vector3(footprint.offset.x, footprint.height / 2, footprint.offset.y);
            box.isTrigger = footprint.trigger; box.enabled = footprint.enabled;
        }
        foreach (var old in renderers) if (old != null) UnityEngine.Object.DestroyImmediate(old);
        foreach (Transform child in player.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        player.transform.localPosition = CafeWorld.GroundPosition(CafeWorld.SecuritySpawn);
        player.transform.localScale = Vector3.one;
        var body = player.GetComponent<Rigidbody>();
        if (body == null) body = player.gameObject.AddComponent<Rigidbody>();
        body.useGravity = false; body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var capsule = player.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = player.gameObject.AddComponent<CapsuleCollider>();
        capsule.radius = 0.31f; capsule.height = 1.75f; capsule.center = new Vector3(0, 0.875f, 0);
        var physics = new PhysicsMaterial("Night player frictionless") { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum };
        string physicsPath = Cafe3DAssets.Root + "/Player.physicsMaterial";
        var savedPhysics = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(physicsPath);
        if (savedPhysics == null) { AssetDatabase.CreateAsset(physics, physicsPath); savedPhysics = physics; }
        else UnityEngine.Object.DestroyImmediate(physics);
        capsule.sharedMaterial = savedPhysics;
        var avatar = Cafe3DAssets.Model(player.transform, "Player"); player.model = avatar.Find("Body");
        player.leftLeg = player.model.Find("Left leg"); player.rightLeg = player.model.Find("Right leg"); player.reachArm = player.model.Find("Reach arm");
        player.flashlight = Cafe3DAssets.Group(player.transform, "Flashlight", new Vector3(0, 1.05f, 0));
        var light = player.flashlight.gameObject.AddComponent<Light>(); light.type = LightType.Spot;
        light.color = new Color(1, 0.89f, 0.68f); light.intensity = 5; light.range = 11; light.spotAngle = 58; light.innerSpotAngle = 28;
        light.shadows = LightShadows.Soft; light.shadowBias = 0.03f;
        player.flashlight.rotation = Quaternion.LookRotation(new Vector3(0, -0.22f, -1));
        PrefabUtility.SaveAsPrefabAsset(player.gameObject, "Assets/Prefabs/NightPlayer.prefab");
        AddCameras(world); AddSetDressing(world);
    }

    private static void EnsureReusableTable()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CafeTable.prefab");
        if (existing.GetComponent<BoxCollider>() != null) return;
        var table = new GameObject("Cafe table");
        var tabletop = Cafe3DAssets.Model(table.transform, "Table"); tabletop.localScale = new Vector3(2.2f, 1, 1.35f);
        var tableCollider = table.AddComponent<BoxCollider>(); tableCollider.center = new Vector3(0, 0.57f, 0); tableCollider.size = new Vector3(2.2f, 1.14f, 1.35f);
        Cafe3DAssets.SetLayer(table.transform, 10);
        PrefabUtility.SaveAsPrefabAsset(table, "Assets/Prefabs/CafeTable.prefab"); UnityEngine.Object.DestroyImmediate(table);
    }

    private static void EnsureCctvArchitecture(CafeWorld world)
    {
        if (world.GetComponentsInChildren<Transform>(true).Any(t => t.name == "CCTV ceiling")) return;
        foreach (var wall in world.GetComponentsInChildren<Transform>(true).Where(t => t.name == "CCTV full wall"))
        {
            wall.localPosition = new Vector3(0, 2.64f, 0);
            wall.localScale = new Vector3(wall.localScale.x, 3.12f, wall.localScale.z);
        }
        foreach (var floor in world.GetComponentsInChildren<Transform>(true))
        {
            Vector2 size = floor.name switch { "CAFE" => new Vector2(19, 22), "SECURITY" => new Vector2(12, 8), "LOBBY" => new Vector2(12, 13), "STORAGE" => new Vector2(7, 10), "NIGHT ROOM" => new Vector2(7, 12), _ => Vector2.zero };
            if (size == Vector2.zero) continue;
            Cafe3DAssets.Box(floor, "CCTV ceiling", new Vector3(0, 4.25f, 0), new Vector3(size.x, 0.10f, size.y), "Wall trim").gameObject.layer = CafeWorld.CctvWallsLayer;
        }
    }

    private static void EnsureFloorEdges(CafeWorld world)
    {
        // The old sprites stopped one unit short of two walls. Fill only that visual
        // border in 3D; wall, door, furniture and interaction coordinates stay intact.
        foreach (var floor in world.GetComponentsInChildren<Transform>(true))
        {
            if (floor.name != "CAFE" && floor.name != "SECURITY") continue;
            if (floor.Find("3D floor edge") != null) continue;
            var edge = Cafe3DAssets.Group(floor, "3D floor edge");
            var ceiling = floor.Find("CCTV ceiling");
            if (floor.name == "CAFE")
            {
                for (int row = 0; row < 2; row++) for (int i = 0; i < 8; i++)
                    Cafe3DAssets.Box(edge, "Edge plank", new Vector3(-10.25f + row * 0.5f, -0.014f, -9.625f + i * 2.75f), new Vector3(0.486f, 0.025f, 2.736f), "Floor wood " + (i + row) % 6);
                ceiling.localPosition = new Vector3(-0.5f, 4.25f, 0); ceiling.localScale = new Vector3(20, 0.1f, 22);
            }
            else
            {
                for (int i = 0; i < 12; i++)
                    Cafe3DAssets.Box(edge, "Edge tile", new Vector3(-5.5f + i, -0.014f, 4.5f), new Vector3(0.986f, 0.025f, 0.986f), "Floor tile " + i % 4);
                ceiling.localPosition = new Vector3(0, 4.25f, 0.5f); ceiling.localScale = new Vector3(12, 0.1f, 9);
            }
        }
    }

    private static void ConvertItem(CafeInteractable item, Vector2 size)
    {
        foreach (Transform child in item.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        string model = item.kind switch
        {
            InteractionKind.Camera => "Monitor", InteractionKind.Note => "Note", InteractionKind.RuleBook => "Book", InteractionKind.Recipe => "Recipe", InteractionKind.Cup => "Cup", InteractionKind.Door => "Door",
            InteractionKind.Ingredient => new[] { "Milk", "Chocolate", "Almond", "Tapioca", "Cream" }[(int)item.ingredient],
            _ => item.title.Contains("수납") ? "Shelf" : "Book"
        };
        var visual = Cafe3DAssets.Model(item.transform, model);
        if (item.kind == InteractionKind.Cup) item.transform.localPosition += Vector3.up * 1.45f;
        if (item.kind == InteractionKind.Door)
        {
            visual.localScale = new Vector3(Mathf.Max(size.x, size.y), 1, 1);
            if (size.y > size.x) visual.localRotation = Quaternion.Euler(0, 90, 0);
            item.doorHinge = visual.Find("Hinge");
        }
        if (item.kind == InteractionKind.Recipe) visual.localRotation = Quaternion.Euler(0, -18, 0);
        Cafe3DAssets.SetLayer(item.transform, item.gameObject.layer);
    }

    private static void BuildFloor(Transform parent, Vector2 size, bool wood)
    {
        Cafe3DAssets.Box(parent, "Foundation", new Vector3(0, -0.13f, 0), new Vector3(size.x, 0.24f, size.y), "Charcoal");
        // Combine planks by material so detailed floors do not cost a draw call per tile.
        var temporary = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var source = temporary.GetComponent<MeshFilter>().sharedMesh;
        int palette = wood ? 6 : 4;
        var lists = Enumerable.Range(0, palette).Select(_ => new List<CombineInstance>()).ToArray();
        float w = wood ? 0.65f : 1, d = wood ? 2.8f : 1;
        int row = 0;
        for (float x = -size.x / 2; x < size.x / 2; x += w, row++)
        {
            int column = 0;
            for (float z = -size.y / 2 - (row % 2) * d / 2; z < size.y / 2; z += d, column++)
            {
                float left = Mathf.Max(-size.y / 2, z), right = Mathf.Min(size.y / 2, z + d), width = Mathf.Min(w, size.x / 2 - x);
                if (right <= left) continue;
                lists[(row * 7 + column * 3) % palette].Add(new CombineInstance { mesh = source, transform = Matrix4x4.TRS(new Vector3(x + width / 2, -0.014f, (left + right) / 2), Quaternion.identity, new Vector3(width - 0.014f, 0.025f, right - left - 0.014f)) });
            }
        }
        // Only the mesh is used; remove the temporary primitive immediately.
        UnityEngine.Object.DestroyImmediate(temporary);
        for (int i = 0; i < palette; i++)
        {
            var mesh = new Mesh { name = parent.name + " floor " + i, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(lists[i].ToArray());
            string path = Cafe3DAssets.Root + "/Meshes/" + mesh.name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); mesh = saved; }
            var t = Cafe3DAssets.Group(parent, wood ? "Walnut planks " + i : "Stone tiles " + i);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = Cafe3DAssets.Mat((wood ? "Floor wood " : "Floor tile ") + i);
        }
    }

    private static void AddCameras(CafeWorld world)
    {
        var rig = Cafe3DAssets.Group(world.transform, "CCTV Mounts"); world.CctvMounts = new Transform[4];
        Vector3[] positions = { new Vector3(-5.7f, 3.05f, -10.4f), new Vector3(-18.5f, 3.05f, 2.6f), new Vector3(13.5f, 3.05f, 1.5f), new Vector3(13.5f, 3.05f, 0.5f) };
        Vector3[] targets = { new Vector3(3, 0.75f, 2.5f), new Vector3(-12.6f, 0.7f, 7), new Vector3(17, 0.8f, 6.5f), new Vector3(17, 0.7f, -5.5f) };
        for (int i = 0; i < 4; i++)
        {
            var mount = Cafe3DAssets.Group(rig, "CAM 0" + (i + 1) + " - " + CafeWorld.CameraNames[i], positions[i]);
            mount.rotation = Quaternion.LookRotation(targets[i] - positions[i]);
            Cafe3DAssets.Model(mount, "SecurityCamera");
            world.CctvMounts[i] = Cafe3DAssets.Group(mount, "Lens viewpoint", new Vector3(0, -0.08f, 0.44f));
            Cafe3DAssets.SetLayer(mount, CafeWorld.RoomLayer(CafeWorld.CameraPositions[i]));
        }
    }

    private static void AddSetDressing(CafeWorld world)
    {
        var decor = Cafe3DAssets.Group(world.transform, "3D Lighting and Details");
        var machine = Cafe3DAssets.Model(decor, "CoffeeMachine"); machine.localPosition = new Vector3(5.5f, 1.15f, 9.1f); Cafe3DAssets.SetLayer(machine, 10);
        Vector3[] points = { new Vector3(-14, 3.15f, 6), new Vector3(-13, 3.15f, -4), new Vector3(3, 3.6f, 0), new Vector3(5.5f, 3.6f, 6.5f), new Vector3(8, 3.6f, -6), new Vector3(-2, 3.6f, -5), new Vector3(16.5f, 3.15f, 6), new Vector3(16.5f, 3.15f, -5) };
        for (int i = 0; i < points.Length; i++)
        {
            var pendant = Cafe3DAssets.Model(decor, "Pendant"); pendant.localPosition = points[i];
            int layer = CafeWorld.RoomLayer(CafeWorld.PlanPosition(points[i])); Cafe3DAssets.SetLayer(pendant, layer);
            var lamp = Cafe3DAssets.Group(pendant, "Warm pool", new Vector3(0, -0.3f, 0)).gameObject.AddComponent<Light>();
            lamp.type = LightType.Point; lamp.color = i == 0 || i == 6 ? new Color(0.62f, 0.85f, 0.84f) : new Color(1, 0.75f, 0.47f);
            lamp.intensity = i == 0 ? 3.5f : 4.5f; lamp.range = i == 0 ? 8 : 10;
            lamp.shadows = LightShadows.None;
        }
        // Rain-dark windows sit within the existing outer walls and add no obstacles.
        foreach (float x in new[] { -2f, 3.5f, 9f })
        {
            var window = Cafe3DAssets.Group(decor, "Rain window", new Vector3(x, 1.93f, 10.79f));
            Cafe3DAssets.Box(window, "Frame", Vector3.zero, new Vector3(3.8f, 1.9f, 0.16f), "Charcoal");
            Cafe3DAssets.Box(window, "Night glass", new Vector3(0, 0, -0.09f), new Vector3(3.55f, 1.63f, 0.025f), "Dark teal");
            Cafe3DAssets.Box(window, "Mullion", new Vector3(0, 0, -0.13f), new Vector3(0.06f, 1.72f, 0.05f), "Metal");
            Cafe3DAssets.SetLayer(window, 10);
        }
    }

    public static CafeWorld InstantiateWorld()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPath);
        if (prefab.GetComponent<CafeWorld>().CctvMounts?.Length != 4) throw new InvalidOperationException("Run Night Cafe/Convert Existing Game to 3D first.");
        return ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<CafeWorld>();
    }

    public static void ConfigureLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.39f, 0.46f, 0.48f);
        RenderSettings.ambientEquatorColor = new Color(0.28f, 0.32f, 0.31f);
        RenderSettings.ambientGroundColor = new Color(0.14f, 0.17f, 0.18f);
        RenderSettings.fog = false; RenderSettings.skybox = null;
        var existing = GameObject.Find("Night fill light");
        var light = existing != null ? existing.GetComponent<Light>() : new GameObject("Night fill light").AddComponent<Light>();
        light.type = LightType.Directional; light.color = new Color(0.76f, 0.85f, 0.92f); light.intensity = 0.65f;
        light.transform.rotation = Quaternion.Euler(55, -28, 0); light.shadows = LightShadows.Soft; light.shadowStrength = 0.55f; light.shadowBias = 0.04f;
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if (pipeline != null) { pipeline.msaaSampleCount = 4; pipeline.shadowDistance = 55; EditorUtility.SetDirty(pipeline); }
    }

    public static void ConfigureFeedMaterial(NightGameView view)
    {
        string path = Cafe3DAssets.Root + "/Materials/CCTV screen.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("NightCafe/CCTV Screen")); AssetDatabase.CreateAsset(mat, path); }
        view.feedImage.material = mat;
        PrefabUtility.RecordPrefabInstancePropertyModifications(view.feedImage);
    }

    public static void ConfigureLobby()
    {
        var start = UnityEngine.Object.FindFirstObjectByType<StartScreen>();
        var artwork = start.transform.Find("Cafe illustration").GetComponent<Image>();
        artwork.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(artwork);
        var background = start.transform.Find("Menu background").GetComponent<Image>();
        background.color = new Color(0.025f, 0.055f, 0.065f, 0.96f); PrefabUtility.RecordPrefabInstancePropertyModifications(background);
        if (UnityEngine.Object.FindFirstObjectByType<CafeWorld>() == null)
        {
            var world = InstantiateWorld(); world.name = "Cafe 3D Title Set";
            world.Player.gameObject.SetActive(false);
        }
        var camera = GameObject.Find("Start Camera").GetComponent<Camera>();
        camera.orthographic = false; camera.fieldOfView = 47; camera.nearClipPlane = 0.1f;
        camera.transform.SetPositionAndRotation(new Vector3(16, 14, -18), Quaternion.LookRotation(new Vector3(0, 0.5f, 2) - new Vector3(16, 14, -18)));
        camera.cullingMask = 1 | (1 << 10); camera.backgroundColor = CafeUi.Ink;
        ConfigureLighting();
    }

    [MenuItem("Night Cafe/Validate 3D Presentation")]
    public static void Validate()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPath);
        var world = prefab.GetComponent<CafeWorld>();
        if (world.GetComponentsInChildren<SpriteRenderer>(true).Length != 0 || world.GetComponentsInChildren<Collider2D>(true).Length != 0 || world.GetComponentsInChildren<Rigidbody2D>(true).Length != 0)
            throw new InvalidOperationException("Legacy 2D world components remain.");
        if (world.CctvMounts.Length != 4 || world.Flood == null || world.Player.GetComponent<Rigidbody>() == null || world.Cup.position.y < 1)
            throw new InvalidOperationException("Missing 3D world references.");
        foreach (var item in world.Interactables)
            if (item == null || item.GetComponentsInChildren<MeshRenderer>(true).Length == 0)
                throw new InvalidOperationException("Missing 3D interaction model.");
        foreach (var renderer in world.GetComponentsInChildren<MeshRenderer>(true))
            if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null || renderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
                throw new InvalidOperationException("Missing 3D material: " + renderer.name);
        NightCafeBuild.ValidateRules();
        Debug.Log("NIGHT_CAFE_3D_VALIDATION_PASS: 3D meshes/physics, four physical CCTV mounts, materials and unchanged day-one rules.");
    }

    public static void ConvertAndBuild()
    {
        Convert(); NightCafeBuild.BuildWindows();
    }
}
