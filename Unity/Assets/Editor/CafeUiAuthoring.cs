using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEditor;
using System.Linq;

namespace NightCafe
{
    public static class CafeUiAuthoring
    {
        public static readonly Color Ink = new Color32(12, 22, 26, 255);
        public static readonly Color Paper = new Color32(227, 232, 220, 255);
        public static readonly Color Muted = new Color32(142, 163, 158, 255);
        public static readonly Color Amber = new Color32(235, 187, 100, 255);
        public static readonly Color Green = new Color32(102, 200, 173, 255);
        public static readonly Color Red = new Color32(235, 112, 98, 255);
        private static Font font;
        public static Font Font
        {
            get
            {
                if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 28);
                return font;
            }
        }

        public static Canvas Canvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                var module = events.GetComponent<InputSystemUIInputModule>();
                const string path = "Assets/InputSystem_Actions.inputactions";
                module.actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                var references = AssetDatabase.LoadAllAssetsAtPath(path).OfType<InputActionReference>().ToArray();
                module.point = references.First(r => r.action.name == "Point");
                module.leftClick = references.First(r => r.action.name == "Click");
                module.rightClick = references.First(r => r.action.name == "RightClick");
                module.middleClick = references.First(r => r.action.name == "MiddleClick");
                module.scrollWheel = references.First(r => r.action.name == "ScrollWheel");
                // Keyboard menu input is handled by the controllers; do not submit twice.
                module.move = null; module.submit = null; module.cancel = null;
            }
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        public static Image Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(Transform parent, string value, float x, float y, float w, float h,
            int size = 24, Color? color = null, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var text = Rect(parent, value.Length > 0 ? value.Split('\n')[0] : "Dynamic text", x, y, w, h).gameObject.AddComponent<Text>();
            text.font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/NanumGothic-Regular.ttf");
            text.text = value;
            text.fontSize = size;
            text.color = color ?? Paper;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(Transform parent, string text, float x, float y, float w, float h, UnityAction action)
        {
            var background = Panel(parent, text, x, y, w, h, new Color32(35, 54, 56, 255));
            background.raycastTarget = true;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.2f);
            colors.pressedColor = Amber;
            button.colors = colors;
            Label(background.transform, text, 18, 0, w - 36, h, 24, null, TextAnchor.MiddleLeft);
            if (action != null) button.onClick.AddListener(action);
            return button;
        }

        public static void ConfigureWindow()
        {
            Application.targetFrameRate = 60;
            if (!Application.isEditor) Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1;
        }
    }
}
