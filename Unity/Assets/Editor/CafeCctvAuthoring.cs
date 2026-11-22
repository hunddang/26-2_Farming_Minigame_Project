using NightCafe;
using UnityEngine;

internal static class CafeCctvAuthoring
{
    // Schematic of the plan's connections. The nodes keep the plan's room proportions.
    private static readonly Vector2[] Positions =
    {
        new(399, 334), new(399, 64), new(154, 64), new(154, 334),
        new(51, 199), new(154, 199), new(516, 199), new(282, 153),
        new(282, 245), new(282, 64), new(282, 334)
    };

    public static void Build(Transform body, NightGameView view)
    {
        FlatPanel(body, "Monitor bezel", 24, 96, 1104, 635, new Color32(11, 14, 28, 255));
        view.feedTitle = CafeUiAuthoring.Label(body, "", 42, 26, 850, 52, 34, CafeUi.Green);
        CafeUiAuthoring.Label(body, "●  현장 음성", 922, 36, 184, 37, 21, CafeUi.Muted, TextAnchor.MiddleRight);
        var glass = CafeUiAuthoring.Rect(body, "Monitor glass", 36, 106, 1280, 720);
        glass.localScale = Vector3.one * (1080f / 1280);
        view.feedImage = CafeUiAuthoring.Rect(glass, "Live camera feed", 0, 0, 1280, 720)
            .gameObject.AddComponent<UnityEngine.UI.RawImage>();
        view.feedImage.raycastTarget = false;

        CafeUiAuthoring.Label(body, "카페 평면도", 1168, 36, 560, 42, 28, CafeUi.Paper);
        var map = FlatPanel(body, "Camera Map", 1168, 90, 570, 397, new Color32(25, 31, 52, 255)).transform;
        view.cameraButtons = new UnityEngine.UI.Button[CafeWorld.CameraRooms.Length];
        for (int i = 0; i < view.cameraButtons.Length; i++)
        {
            int room = CafeWorld.CameraRooms[i];
            Vector2 roomSize = CafeWorld.Sizes[room - 8];
            float width = Mathf.Clamp(roomSize.x * 6, 94, 124);
            float height = 56 + (roomSize.y - 6) * 1.8f;
            Vector2 pos = Positions[i];
            string title = $"{i + 1:00}\n{CafeWorld.RoomNames[room - 8]}";
            var button = CafeUiAuthoring.Button(map, title, pos.x - width / 2, pos.y - height / 2, width, height, null);
            button.name = $"Map Camera {i + 1:00}";
            var label = button.GetComponentInChildren<UnityEngine.UI.Text>();
            label.fontSize = room is 14 or 15 ? 15 : 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchoredPosition = new Vector2(3, 0);
            label.rectTransform.sizeDelta = new Vector2(width - 6, height);
            view.cameraButtons[i] = button;
        }

        CafeUiAuthoring.Label(body, "지도 클릭 또는 WASD로 카메라 선택", 1168, 501, 568, 32, 20, CafeUi.Muted);
        var note = FlatPanel(body, "Desk Post-it", 1170, 554, 538, 265, new Color32(227, 205, 144, 255));
        note.rectTransform.localRotation = Quaternion.Euler(0, 0, -1.6f);
        FlatPanel(note.transform, "Tape", 194, -8, 150, 16, new Color32(240, 230, 187, 220));
        CafeUiAuthoring.Label(note.transform, "사장이 남긴 포스트잇", 25, 19, 480, 39, 24, CafeUi.Ink);
        CafeUiAuthoring.Label(note.transform, NightGame.DeskNote, 25, 62, 490, 180, 20, CafeUi.Ink);

        CafeUiAuthoring.Label(body, "감시실 단말기  /  외부에서는 열람할 수 없습니다.", 40, 755, 1090, 52, 22, CafeUi.Muted);
        CafeUiAuthoring.Button(body, "단말기 닫기  [Space / Esc]", 1168, 872, 570, 62, null);
    }

    private static UnityEngine.UI.Image FlatPanel(Transform parent, string name, float x, float y, float width, float height, Color color)
    {
        var panel = CafeUiAuthoring.Panel(parent, name, x, y, width, height, color);
        panel.sprite = CafeSceneAuthoring.Sprite("pixel");
        return panel;
    }

    public static void DrawLinks(NightGameView view, NightRoutes.Portal[] portals)
    {
        Transform map = view.cameraButtons[0].transform.parent;
        for (int i = 0; i < portals.Length; i += 2)
        {
            var link = portals[i];
            int first = CafeWorld.CameraChannel(link.item.gameObject.layer);
            int second = CafeWorld.CameraChannel(link.paired.gameObject.layer);
            Vector2 start = Positions[first];
            Vector2 end = Positions[second];
            Vector2 delta = end - start;
            var line = CafeUiAuthoring.Panel(map, "Passage Link", start.x, start.y, delta.magnitude, 2,
                link.puzzle ? new Color32(173, 151, 111, 255) : new Color32(120, 145, 166, 255));
            line.rectTransform.pivot = new Vector2(0, 0.5f);
            line.rectTransform.anchoredPosition = new Vector2(start.x, -start.y);
            line.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg);
            line.transform.SetAsFirstSibling();
        }
    }
}
