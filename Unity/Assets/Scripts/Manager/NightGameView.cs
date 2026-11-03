using UnityEngine;
using UnityEngine.UI;

namespace NightCafe
{
    // All presentation objects are authored and serialized in the HUD prefab.
    public sealed class NightGameView : MonoBehaviour
    {
        [Header("HUD")]
        public Text objective, status, timer, prompt, toast, location, progress;
        public Image timerLine, floodWarning;
        public Text[] inventoryLabels;
        public Image[] inventorySlots;
        public Button[] inventoryButtons;
        public Button pauseButton, rulesButton;

        [Header("Authored overlays (inactive until opened)")]
        public GameObject rules, reading, cctv, pause, failure, complete;
        public Button[] closeButtons;
        public Text readingTitle, readingBody, completeBody;
        public Text feedTitle;
        public RawImage feedImage;
        public Button[] cameraButtons, pauseButtons, failureButtons, completeButtons;

        public void Bind(NightGame game)
        {
            pauseButton.onClick.AddListener(game.Pause);
            rulesButton.onClick.AddListener(game.OpenRules);
            foreach (var button in closeButtons) button.onClick.AddListener(game.CloseOverlay);
            for (int i = 0; i < inventoryButtons.Length; i++)
            {
                int slot = i;
                inventoryButtons[i].onClick.AddListener(() => game.SelectSlot(slot));
            }
            for (int i = 0; i < cameraButtons.Length; i++)
            {
                int camera = i;
                cameraButtons[i].onClick.AddListener(() => game.SelectCamera(camera));
            }
            pauseButtons[0].onClick.AddListener(game.CloseOverlay);
            pauseButtons[1].onClick.AddListener(game.ReturnToLobby);
            pauseButtons[2].onClick.AddListener(NightGame.Quit);
            failureButtons[0].onClick.AddListener(game.Retry);
            failureButtons[1].onClick.AddListener(game.ReturnToLobby);
            completeButtons[0].onClick.AddListener(game.ReturnToLobby);
            completeButtons[1].onClick.AddListener(NightGame.Quit);
            HideOverlays();
        }

        public void HideOverlays()
        {
            foreach (var panel in new[] { rules, reading, cctv, pause, failure, complete })
                panel.SetActive(false);
        }
    }
}
