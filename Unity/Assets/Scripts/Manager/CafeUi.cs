using UnityEngine;

namespace NightCafe
{
    public static class CafeUi
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
