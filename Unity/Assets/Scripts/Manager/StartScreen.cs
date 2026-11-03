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
        [Header("Scene references")]
        public Button[] buttons;
        public GameObject helpPanel;
        public Button closeHelpButton;
        [SerializeField] private string gameScene = "InGame";
        private int selected;
        private bool loading;

        private void Start()
        {
            CafeUi.ConfigureWindow();
            helpPanel.SetActive(false);
            buttons[0].onClick.AddListener(Begin);
            buttons[1].onClick.AddListener(ShowHelp);
            buttons[2].onClick.AddListener(Quit);
            closeHelpButton.onClick.AddListener(CloseHelp);
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
            if (helpPanel.activeSelf)
            {
                if (keys.escapeKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame) CloseHelp();
                return;
            }
            if (keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame) selected = (selected + buttons.Length - 1) % buttons.Length;
            if (keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame) selected = (selected + 1) % buttons.Length;
            MarkSelection();
            if (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame) buttons[selected].onClick.Invoke();
        }
        private void MarkSelection()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].GetComponent<CafeButtonStyle>().Select(i == selected);
            }
        }
        public void Begin()
        {
            if (loading) return;
            loading = true;
            SceneManager.LoadScene(gameScene);
        }
        public void ShowHelp() => helpPanel.SetActive(true);
        public void CloseHelp() => helpPanel.SetActive(false);
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
