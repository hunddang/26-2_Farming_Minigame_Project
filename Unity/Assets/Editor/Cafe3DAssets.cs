using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Original, editable low-poly models. All meshes/materials/prefabs are saved assets.
public static class Cafe3DAssets
{
    public const string Root = "Assets/Art/NightCafe3D";
    private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    public static Material Mat(string name) => materials.TryGetValue(name, out var value) ? value : AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + name + ".mat");

    public static void BuildAll()
    {
        foreach (string folder in new[] { "Materials", "Models", "Meshes" }) Directory.CreateDirectory(Root + "/" + folder);
        AssetDatabase.Refresh();
        Material("Walnut", "614433", 0, 0.28f);
        Material("Wood edge", "AB7A50", 0, 0.34f);
        Material("Charcoal", "202D32", 0.28f, 0.35f);
        Material("Metal", "687879", 0.65f, 0.48f);
        Material("Brass", "B58A4B", 0.65f, 0.52f);
        Material("Teal", "406E69", 0, 0.3f);
        Material("Dark teal", "203D41", 0, 0.2f);
        Material("Ceramic", "ECE4D1", 0.05f, 0.6f);
        Material("Paper", "DACCAC", 0, 0.16f);
        Material("Ink", "394B4C", 0, 0.12f);
        Material("Coffee", "32170C", 0.05f, 0.85f);
        Material("Chocolate", "5F3429", 0, 0.3f);
        Material("Milk blue", "659FA6", 0, 0.32f);
        Material("Almond", "C79B61", 0, 0.25f);
        Material("Tapioca", "3E2B37", 0, 0.38f);
        Material("Cream", "C1CEAE", 0, 0.3f);
        Material("Leaf", "417C59", 0, 0.18f);
        Material("Leaf light", "6F9364", 0, 0.18f);
        Material("Terracotta", "9B6150", 0, 0.2f);
        Material("Skin", "C7A17D", 0, 0.28f);
        Material("Hair", "292323", 0, 0.15f);
        Material("Uniform", "487D78", 0, 0.28f);
        Material("Trousers", "263642", 0, 0.2f);
        Material("Wall", "72847A", 0, 0.23f);
        Material("Wall trim", "344F50", 0, 0.26f);
        Material("Monitor glow", "75B7A3", 0.12f, 0.5f, 0.6f);
        Material("Warm light", "F1BF72", 0, 0.3f, 1.8f);
        Material("Camera red", "E17454", 0, 0.25f, 0.9f);
        for (int i = 0; i < 6; i++) Material("Floor wood " + i, new[] { "66523F", "705A45", "78624C", "604C3B", "6C5845", "7B654D" }[i], 0, 0.32f);
        for (int i = 0; i < 4; i++) Material("Floor tile " + i, new[] { "536261", "5B6967", "596464", "616D6B" }[i], 0, 0.25f);
        var flood = Material("Flood", "35170C", 0.1f, 0.94f);
        flood.SetFloat("_Surface", 1); flood.SetFloat("_Blend", 0);
        flood.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); flood.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        flood.SetFloat("_ZWrite", 0); flood.SetOverrideTag("RenderType", "Transparent");
        flood.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); flood.renderQueue = 3000;
        foreach (var name in new[] { "Table", "Chair", "Desk", "Counter", "Shelf", "Bed", "Plant", "Monitor", "Book", "Note", "Recipe", "Cup", "Milk", "Chocolate", "Almond", "Tapioca", "Cream", "Door", "Player", "SecurityCamera", "Pendant", "CoffeeMachine" })
        {
            var model = new GameObject(name);
            BuildModel(model.transform, name);
            PrefabUtility.SaveAsPrefabAsset(model, Root + "/Models/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(model);
        }
        AssetDatabase.SaveAssets();
    }

    private static Material Material(string name, string hex, float metallic, float smoothness, float emission = 0)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        mat.SetColor("_BaseColor", color); mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", smoothness);
        mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * emission);
        materials[name] = mat; EditorUtility.SetDirty(mat); return mat;
    }

    public static Transform Group(Transform parent, string name, Vector3 position = default)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
    }
    public static Transform Box(Transform parent, string name, Vector3 position, Vector3 scale, string material)
        => Primitive(parent, name, PrimitiveType.Cube, position, scale, material);
    public static Transform Primitive(Transform parent, string name, PrimitiveType kind, Vector3 position, Vector3 scale, string material)
    {
        var go = GameObject.CreatePrimitive(kind); go.name = name;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = Mat(material);
        renderer.receiveShadows = true; return go.transform;
    }
    public static Transform Model(Transform parent, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + name + ".prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); go.name = name + " Model"; return go.transform;
    }
    public static void SetLayer(Transform root, int layer)
    {
        if (root.gameObject.layer != CafeWorldLayer) root.gameObject.layer = layer;
        foreach (Transform child in root) SetLayer(child, layer);
    }
    private const int CafeWorldLayer = 13;

    private static void Legs(Transform parent, float width, float depth, float height, string material = "Charcoal")
    {
        foreach (float x in new[] { -1f, 1f }) foreach (float z in new[] { -1f, 1f })
            Box(parent, "Leg", new Vector3(x * width * 0.4f, height / 2, z * depth * 0.36f), new Vector3(0.075f, height, 0.075f), material);
    }
    private static void Plinth(Transform p, string material)
    {
        Box(p, "Pantry stand", new Vector3(0, 0.40f, 0), new Vector3(0.58f, 0.8f, 0.48f), "Dark teal");
        Box(p, "Tray lip", new Vector3(0, 0.81f, 0), new Vector3(0.66f, 0.04f, 0.55f), "Brass");
        Box(p, "Label plate", new Vector3(0, 0.62f, -0.246f), new Vector3(0.34f, 0.16f, 0.016f), material);
    }
    private static void Book(Transform p, float y, string cover)
    {
        Box(p, "Pages", new Vector3(0, y + 0.04f, 0), new Vector3(0.50f, 0.075f, 0.68f), "Paper");
        foreach (float offset in new[] { 0f, 0.085f }) Box(p, "Cover", new Vector3(0, y + offset, 0), new Vector3(0.54f, 0.018f, 0.72f), cover);
        Box(p, "Spine", new Vector3(-0.26f, y + 0.045f, 0), new Vector3(0.04f, 0.1f, 0.72f), cover);
        Box(p, "Cover stripe", new Vector3(0.06f, y + 0.1f, 0), new Vector3(0.26f, 0.01f, 0.035f), "Brass");
    }
    private static void BuildModel(Transform p, string name)
    {
        switch (name)
        {
            case "Table":
            case "Desk":
                Legs(p, 1, 1, 0.89f);
                Box(p, "Apron", new Vector3(0, 0.81f, 0), new Vector3(0.93f, 0.14f, 0.87f), "Walnut");
                Box(p, "Solid wood top", new Vector3(0, 0.96f, 0), new Vector3(1, 0.13f, 1), "Walnut");
                Box(p, "Inlaid edge", new Vector3(0, 0.967f, -0.487f), new Vector3(0.98f, 0.045f, 0.025f), "Wood edge");
                if (name == "Table") Box(p, "Linen runner", new Vector3(0, 1.032f, 0), new Vector3(0.23f, 0.008f, 0.94f), "Paper");
                break;
            case "Chair":
                Legs(p, 0.85f, 0.85f, 0.45f);
                Box(p, "Upholstered seat", new Vector3(0, 0.49f, 0), new Vector3(0.86f, 0.15f, 0.82f), "Teal");
                Box(p, "Back", new Vector3(0, 0.87f, 0.37f), new Vector3(0.86f, 0.68f, 0.13f), "Teal");
                break;
            case "Counter":
                Box(p, "Cabinet", new Vector3(0, 0.5f, 0), new Vector3(0.96f, 1, 0.92f), "Dark teal");
                for (int i = 0; i < 8; i++) Box(p, "Front fluting", new Vector3(-0.43f + i * 0.123f, 0.52f, -0.47f), new Vector3(0.013f, 0.83f, 0.02f), "Teal");
                Box(p, "Brass kickplate", new Vector3(0, 0.085f, -0.48f), new Vector3(0.96f, 0.08f, 0.025f), "Brass");
                Box(p, "Walnut worktop", new Vector3(0, 1.07f, 0), new Vector3(1, 0.14f, 1), "Walnut");
                break;
            case "Shelf":
                foreach (float x in new[] { -0.45f, 0.45f }) Box(p, "Upright", new Vector3(x, 1.25f, 0), new Vector3(0.08f, 2.5f, 0.98f), "Charcoal");
                for (int i = 0; i < 4; i++)
                {
                    float y = 0.22f + i * 0.67f;
                    Box(p, "Shelf", new Vector3(0, y, 0), new Vector3(1, 0.07f, 1), "Walnut");
                    Box(p, "Stock box", new Vector3(0.06f, y + 0.22f, 0.1f), new Vector3(0.63f, 0.38f, 0.60f), i % 2 == 0 ? "Almond" : "Paper");
                }
                break;
            case "Bed":
                Box(p, "Frame", new Vector3(0, 0.26f, 0), new Vector3(1, 0.42f, 1), "Walnut");
                Box(p, "Mattress", new Vector3(0, 0.54f, 0), new Vector3(0.94f, 0.20f, 0.95f), "Ceramic");
                Box(p, "Blanket", new Vector3(0, 0.655f, -0.13f), new Vector3(0.95f, 0.08f, 0.68f), "Teal");
                Box(p, "Pillow", new Vector3(0, 0.72f, 0.32f), new Vector3(0.70f, 0.18f, 0.22f), "Paper");
                Box(p, "Headboard", new Vector3(0, 0.65f, 0.48f), new Vector3(1, 1.1f, 0.06f), "Wood edge");
                break;
            case "Plant":
                Lathe(p, "Faceted pot", new[] { new Vector2(0.25f, 0), new Vector2(0.36f, 0.52f), new Vector2(0.38f, 0.56f), new Vector2(0.29f, 0.56f) }, "Terracotta", 12);
                for (int i = 0; i < 9; i++)
                {
                    float a = i * 2.4f;
                    var leaf = Primitive(p, "Leaf", PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.23f, 0.95f + i % 3 * 0.12f, Mathf.Sin(a) * 0.23f), new Vector3(0.22f, 0.95f, 0.34f), i % 2 == 0 ? "Leaf" : "Leaf light");
                    leaf.localRotation = Quaternion.Euler(Mathf.Sin(a) * 28, i * 137, Mathf.Cos(a) * 28);
                }
                break;
            case "Monitor":
                Legs(p, 1.6f, 0.8f, 0.91f);
                Box(p, "Terminal table", new Vector3(0, 0.95f, 0.1f), new Vector3(1.65f, 0.1f, 0.9f), "Walnut");
                Box(p, "CRT foot", new Vector3(0, 1.04f, 0.05f), new Vector3(0.64f, 0.1f, 0.48f), "Charcoal");
                Box(p, "CRT casing", new Vector3(0, 1.42f, 0.12f), new Vector3(1.22f, 0.74f, 0.66f), "Charcoal");
                Box(p, "Convex glass", new Vector3(-0.05f, 1.43f, -0.222f), new Vector3(0.95f, 0.55f, 0.025f), "Monitor glow");
                for (int i = 0; i < 4; i++) Box(p, "Video grid", new Vector3(-0.29f + (i % 2) * 0.48f, 1.29f + (i / 2) * 0.28f, -0.24f), new Vector3(0.43f, 0.23f, 0.012f), "Dark teal");
                Box(p, "Power LED", new Vector3(0.51f, 1.16f, -0.224f), new Vector3(0.035f, 0.035f, 0.014f), "Camera red");
                Box(p, "Keyboard", new Vector3(0, 1.02f, -0.37f), new Vector3(0.91f, 0.045f, 0.19f), "Metal");
                break;
            case "Book":
            case "Note":
                Legs(p, 0.75f, 0.75f, 0.73f);
                Box(p, "Reading table", new Vector3(0, 0.76f, 0), new Vector3(0.85f, 0.08f, 0.88f), "Walnut");
                if (name == "Book") Book(p, 0.81f, "Dark teal");
                else
                {
                    Box(p, "Note paper", new Vector3(0, 0.81f, 0), new Vector3(0.46f, 0.012f, 0.5f), "Almond");
                    for (int i = 0; i < 4; i++) Box(p, "Handwriting", new Vector3(0, 0.82f, -0.13f + i * 0.08f), new Vector3(i == 3 ? 0.18f : 0.31f, 0.006f, 0.014f), "Ink");
                }
                break;
            case "Recipe":
                Box(p, "Folded paper", new Vector3(0, 0.035f, 0), new Vector3(0.64f, 0.018f, 0.82f), "Paper");
                Box(p, "Coffee stain", new Vector3(0.15f, 0.046f, 0.18f), new Vector3(0.16f, 0.003f, 0.14f), "Almond");
                for (int i = 0; i < 6; i++) Box(p, "Recipe line", new Vector3(-0.03f, 0.048f, -0.24f + i * 0.085f), new Vector3(i % 3 == 0 ? 0.29f : 0.42f, 0.004f, 0.017f), "Ink");
                break;
            case "Cup":
                Lathe(p, "Hollow porcelain", new[] { new Vector2(0, 0), new Vector2(0.21f, 0), new Vector2(0.26f, 0.05f), new Vector2(0.30f, 0.44f), new Vector2(0.267f, 0.45f), new Vector2(0.225f, 0.08f), new Vector2(0, 0.08f) }, "Ceramic", 24);
                Primitive(p, "Coffee surface", PrimitiveType.Cylinder, new Vector3(0, 0.38f, 0), new Vector3(0.52f, 0.015f, 0.52f), "Coffee");
                Torus(p, "Porcelain handle", new Vector3(0.33f, 0.23f, 0), 0.16f, 0.044f, "Ceramic");
                break;
            case "Milk":
            case "Cream":
                Plinth(p, name == "Milk" ? "Milk blue" : "Cream");
                Box(p, "Carton", new Vector3(0, 1.12f, 0), new Vector3(0.34f, 0.59f, 0.32f), "Ceramic");
                Box(p, "Printed band", new Vector3(0, 1.07f, -0.166f), new Vector3(0.33f, 0.2f, 0.01f), name == "Milk" ? "Milk blue" : "Cream");
                var roof = Box(p, "Folded carton top", new Vector3(0, 1.44f, 0), new Vector3(0.245f, 0.245f, 0.3f), name == "Milk" ? "Milk blue" : "Cream"); roof.localRotation = Quaternion.Euler(0, 0, 45);
                Box(p, "Sealed ridge", new Vector3(0, 1.61f, 0), new Vector3(0.035f, 0.08f, 0.3f), "Ceramic");
                break;
            case "Chocolate":
                Plinth(p, "Chocolate");
                Box(p, "Wrapper", new Vector3(0, 1.05f, 0), new Vector3(0.43f, 0.43f, 0.18f), "Chocolate");
                Box(p, "Gold label", new Vector3(0, 1.02f, -0.095f), new Vector3(0.38f, 0.21f, 0.015f), "Brass");
                for (int i = 0; i < 3; i++) Box(p, "Chocolate square", new Vector3(-0.14f + i * 0.14f, 1.30f, 0), new Vector3(0.12f, 0.12f, 0.16f), "Chocolate");
                break;
            case "Almond":
            case "Tapioca":
                Plinth(p, name);
                Lathe(p, "Pantry jar", new[] { new Vector2(0.19f, 0.83f), new Vector2(0.22f, 0.88f), new Vector2(0.22f, 1.28f), new Vector2(0.19f, 1.34f), new Vector2(0, 1.34f) }, name, 16);
                Primitive(p, "Screw lid", PrimitiveType.Cylinder, new Vector3(0, 1.37f, 0), new Vector3(0.44f, 0.045f, 0.44f), "Metal");
                Box(p, "Jar label", new Vector3(0, 1.08f, -0.222f), new Vector3(0.27f, 0.22f, 0.015f), "Paper");
                for (int i = 0; i < 3; i++) Primitive(p, "Ingredient motif", PrimitiveType.Sphere, new Vector3(-0.08f + i * 0.08f, 1.08f, -0.235f), new Vector3(0.055f, name == "Almond" ? 0.11f : 0.055f, 0.018f), name);
                break;
            case "Door":
                var hinge = Group(p, "Hinge", new Vector3(-0.5f, 0, 0));
                Box(hinge, "Lower door", new Vector3(0.5f, 0.55f, 0), new Vector3(0.96f, 1.1f, 0.14f), "Teal");
                Box(hinge, "Brass push plate", new Vector3(0.83f, 0.88f, -0.08f), new Vector3(0.04f, 0.27f, 0.025f), "Brass");
                var upper = Group(hinge, "CCTV upper door"); upper.gameObject.layer = 13;
                Box(upper, "Door panel", new Vector3(0.5f, 1.86f, 0), new Vector3(0.96f, 1.52f, 0.14f), "Dark teal").gameObject.layer = 13;
                Box(upper, "Frosted window", new Vector3(0.5f, 1.95f, -0.08f), new Vector3(0.64f, 0.73f, 0.025f), "Teal").gameObject.layer = 13;
                foreach (float x in new[] { -0.53f, 0.53f })
                    Box(p, "Door jamb", new Vector3(x, 0.55f, 0), new Vector3(0.065f, 1.1f, 0.21f), "Wood edge");
                break;
            case "Player":
                var body = Group(p, "Body");
                Box(body, "Jacket", new Vector3(0, 1.0f, 0), new Vector3(0.48f, 0.60f, 0.29f), "Uniform");
                Box(body, "Apron", new Vector3(0, 0.89f, 0.162f), new Vector3(0.38f, 0.65f, 0.035f), "Dark teal");
                Box(body, "Name badge", new Vector3(-0.10f, 1.15f, 0.19f), new Vector3(0.12f, 0.055f, 0.014f), "Paper");
                Primitive(body, "Head", PrimitiveType.Sphere, new Vector3(0, 1.52f, 0), new Vector3(0.38f, 0.44f, 0.36f), "Skin");
                Primitive(body, "Hair", PrimitiveType.Sphere, new Vector3(0, 1.68f, -0.025f), new Vector3(0.40f, 0.24f, 0.37f), "Hair");
                for (int i = 0; i < 2; i++)
                {
                    var leg = Group(body, i == 0 ? "Left leg" : "Right leg", new Vector3(i == 0 ? -0.125f : 0.125f, 0.70f, 0));
                    Box(leg, "Trouser leg", new Vector3(0, -0.28f, 0), new Vector3(0.18f, 0.55f, 0.23f), "Trousers");
                    Box(leg, "Shoe", new Vector3(0, -0.61f, 0.045f), new Vector3(0.20f, 0.16f, 0.34f), "Charcoal");
                    var arm = Group(body, i == 0 ? "Left arm" : "Reach arm", new Vector3(i == 0 ? -0.31f : 0.31f, 1.23f, 0));
                    Box(arm, "Sleeve", new Vector3(0, -0.21f, 0), new Vector3(0.15f, 0.43f, 0.20f), "Uniform");
                    Box(arm, "Hand", new Vector3(0, -0.47f, 0), new Vector3(0.14f, 0.16f, 0.16f), "Skin");
                }
                break;
            case "SecurityCamera":
                Box(p, "Mounting plate", new Vector3(0, 0, -0.12f), new Vector3(0.24f, 0.24f, 0.08f), "Metal");
                Box(p, "Camera housing", new Vector3(0, -0.08f, 0.13f), new Vector3(0.29f, 0.22f, 0.49f), "Ceramic");
                Primitive(p, "Lens", PrimitiveType.Sphere, new Vector3(0, -0.08f, 0.385f), new Vector3(0.14f, 0.14f, 0.06f), "Charcoal");
                Box(p, "Recording LED", new Vector3(0.10f, -0.14f, 0.385f), new Vector3(0.035f, 0.035f, 0.02f), "Camera red");
                break;
            case "Pendant":
                Box(p, "Cable", new Vector3(0, 0.35f, 0), new Vector3(0.025f, 0.7f, 0.025f), "Charcoal");
                Lathe(p, "Shade", new[] { new Vector2(0.46f, -0.10f), new Vector2(0.17f, 0.22f), new Vector2(0.09f, 0.23f) }, "Charcoal", 16);
                Primitive(p, "Diffuser", PrimitiveType.Cylinder, new Vector3(0, -0.11f, 0), new Vector3(0.83f, 0.025f, 0.83f), "Warm light");
                break;
            case "CoffeeMachine":
                Box(p, "Espresso machine", new Vector3(0, 0.34f, 0), new Vector3(1.45f, 0.68f, 0.68f), "Metal");
                Box(p, "Black front", new Vector3(0, 0.33f, -0.355f), new Vector3(1.25f, 0.43f, 0.05f), "Charcoal");
                for (int i = 0; i < 2; i++)
                {
                    Box(p, "Group head", new Vector3(-0.36f + i * 0.72f, 0.39f, -0.43f), new Vector3(0.22f, 0.16f, 0.20f), "Brass");
                    Box(p, "Portafilter", new Vector3(-0.36f + i * 0.72f, 0.33f, -0.62f), new Vector3(0.10f, 0.08f, 0.35f), "Charcoal");
                }
                Box(p, "Drip tray", new Vector3(0, 0.04f, -0.39f), new Vector3(1.45f, 0.08f, 0.42f), "Charcoal");
                break;
        }
    }

    private static Transform Lathe(Transform parent, string name, Vector2[] profile, string material, int sides)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int y = 0; y < profile.Length; y++) for (int i = 0; i <= sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides;
            vertices.Add(new Vector3(Mathf.Cos(a) * profile[y].x, profile[y].y, Mathf.Sin(a) * profile[y].x));
        }
        for (int y = 0; y < profile.Length - 1; y++) for (int i = 0; i < sides; i++)
        {
            int a = y * (sides + 1) + i, b = a + sides + 1;
            triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
        }
        return Mesh(parent, name, vertices.ToArray(), triangles.ToArray(), material);
    }
    private static void Torus(Transform parent, string name, Vector3 position, float radius, float tube, string material)
    {
        const int rings = 24, sides = 8;
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int i = 0; i <= rings; i++) for (int j = 0; j <= sides; j++)
        {
            float a = i * Mathf.PI * 2 / rings, b = j * Mathf.PI * 2 / sides;
            vertices.Add(position + new Vector3(Mathf.Cos(a) * (radius + Mathf.Cos(b) * tube), Mathf.Sin(a) * (radius + Mathf.Cos(b) * tube), Mathf.Sin(b) * tube));
        }
        for (int i = 0; i < rings; i++) for (int j = 0; j < sides; j++)
        {
            int a = i * (sides + 1) + j, b = a + sides + 1;
            triangles.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });
        }
        Mesh(parent, name, vertices.ToArray(), triangles.ToArray(), material);
    }
    private static Transform Mesh(Transform parent, string name, Vector3[] vertices, int[] triangles, string material)
    {
        var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = Root + "/Meshes/" + parent.root.name + "-" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
        var go = Group(parent, name); go.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.gameObject.AddComponent<MeshRenderer>().sharedMaterial = Mat(material); return go;
    }
}
