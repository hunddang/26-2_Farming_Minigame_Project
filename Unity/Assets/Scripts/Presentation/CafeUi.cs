using UnityEngine;

namespace NightCafe
{
    public static class CafeUi
    {
        public static readonly Color Ink = new Color32(16, 17, 31, 255);
        public static readonly Color Paper = new Color32(230, 228, 235, 255);
        public static readonly Color Muted = new Color32(164, 172, 195, 255);
        public static readonly Color Amber = new Color32(225, 177, 159, 255);
        public static readonly Color Green = new Color32(150, 204, 222, 255);
        public static readonly Color Red = new Color32(237, 116, 123, 255);
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
