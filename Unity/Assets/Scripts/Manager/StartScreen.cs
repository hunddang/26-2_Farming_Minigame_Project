using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightCafe
{
    public sealed class StartScreen : MonoBehaviour
    {
        private Button[] buttons;
        private GameObject help;
        private int selected;
        private bool loading;

        private void Start()
        {
            CafeUi.ConfigureWindow();
            new CafeWorld(false);
            var camera = new GameObject("Start Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 13;
            camera.transform.position = new Vector3(0, 0, -30);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = CafeUi.Ink;
            camera.rect = new Rect(0.43f, 0.19f, 0.53f, 0.64f);
            var root = CafeUi.Canvas("Start interface · 1920×1080").transform;
            CafeUi.Panel(root, "Menu background", 0, 0, 805, 1080, CafeUi.Ink);
            CafeUi.Label(root, "NIGHT CAFÉ  /  FIELD NOTES", 86, 82, 650, 40, 23, CafeUi.Amber);
            CafeUi.Label(root, "이상현상\n퇴마 기록", 80, 196, 700, 222, 80);
            CafeUi.Label(root, "밤이 깊어지면, 규칙을 지키세요.", 87, 460, 650, 50, 29, CafeUi.Muted);
            CafeUi.Label(root, "카메라로 관찰하고, 현장에서 답을 찾으세요.\n야간 근무 첫날 — 끝없이 쏟아지는 아메리카노", 87, 542, 650, 96, 23, CafeUi.Muted);
            buttons = new[] {
                CafeUi.Button(root, "01     근무 시작", 87, 694, 590, 82, Begin),
                CafeUi.Button(root, "02     조작 안내", 87, 796, 590, 70, ShowHelp),
                CafeUi.Button(root, "03     게임 종료", 87, 884, 590, 70, Quit) };
            CafeUi.Label(root, "W/S 선택    Space / Enter 실행", 87, 1002, 650, 40, 19, CafeUi.Muted);
            CafeUi.Label(root, "CCTV NETWORK", 860, 104, 880, 46, 24, CafeUi.Green);
            CafeUi.Label(root, "●  야간 모니터링", 1420, 104, 400, 46, 24, CafeUi.Amber, TextAnchor.UpperRight);
            CafeUi.Panel(root, "Frame top", 828, 173, 1015, 2, CafeUi.Muted);
            CafeUi.Panel(root, "Frame bottom", 828, 881, 1015, 2, CafeUi.Muted);
            CafeUi.Label(root, "01  /  카페 · 첫 번째 밤", 860, 914, 920, 60, 28);
            CafeUi.Label(root, "Windows 64-bit     /     1920 × 1080     /     1일차 프로토타입", 860, 991, 950, 42, 20, CafeUi.Muted);
            MarkSelection();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-foundationSmokeTest") >= 0) StartCoroutine(SmokeStart());
        }

        private IEnumerator SmokeStart()
        {
            yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(FoundationSmokeTest.OutputPath("01-start.png"));
            yield return new WaitForSeconds(0.5f);
            Begin();
        }

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys == null || loading) return;
            if (help != null)
            {
                if (keys.escapeKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame) CloseHelp();
                return;
            }
            if (keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame) selected = (selected + 2) % 3;
            if (keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame) selected = (selected + 1) % 3;
            MarkSelection();
            if (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame) buttons[selected].onClick.Invoke();
        }
        private void MarkSelection()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].GetComponent<Image>().color = i == selected ? CafeUi.Amber : new Color32(30, 46, 49, 255);
                buttons[i].GetComponentInChildren<Text>().color = i == selected ? CafeUi.Ink : CafeUi.Paper;
            }
        }
        public void Begin()
        {
            if (loading) return;
            loading = true;
            SceneManager.LoadScene("InGame");
        }
        private void ShowHelp()
        {
            var root = buttons[0].transform.parent;
            var shade = CafeUi.Panel(root, "Help", 0, 0, 1920, 1080, new Color(0.02f, 0.04f, 0.05f, 0.96f));
            shade.raycastTarget = true; help = shade.gameObject;
            CafeUi.Label(shade.transform, "근무 조작 안내", 400, 175, 1120, 80, 46, CafeUi.Amber);
            CafeUi.Label(shade.transform, "WASD / 방향키     이동 · CCTV 카메라 전환\nSpace                    조사 · 상호작용 · 선택\nShift                      달리기\nF                            손전등 켜기 / 끄기\nTab                        규칙서 열기 / 닫기\n1–5                        소지품 선택 / 선택 해제\nEsc                        뒤로 · 일시정지\n마우스                   CCTV · 메뉴 · 소지품 선택", 400, 300, 1120, 430, 29);
            CafeUi.Label(shade.transform, "규칙서와 메모를 읽는 동안에도 시간은 흐릅니다.\nEsc 일시정지 메뉴에서는 시간이 멈춥니다.", 400, 762, 1120, 90, 24, CafeUi.Muted);
            CafeUi.Button(shade.transform, "돌아가기  [Space / Esc]", 1060, 890, 460, 74, CloseHelp);
        }
        private void CloseHelp() { if (help != null) { help.SetActive(false); Destroy(help); help = null; } }
        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
