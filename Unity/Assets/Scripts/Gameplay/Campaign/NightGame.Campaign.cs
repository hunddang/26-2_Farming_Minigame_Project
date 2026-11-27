using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightCafe
{
    public sealed partial class NightGame
    {
        [SerializeField] private NightContent nightContent;
        private static int pendingDay = 1;
        private static bool pendingRuleBook;
        private static (bool owned, bool equipped, bool powered) pendingFlashlight;
        private LaterShift laterShift;
        private AudioClip scratch;
        private float failureStarted = -1;
        public int Day { get; private set; } = 1;
        public LaterShift Later => laterShift;
        public ShiftPhase Phase => Fourth?.Phase ?? Fifth?.Phase ?? laterShift?.Phase ?? Session?.Phase ?? ShiftPhase.Surveillance;
        private NightRoutes Routes => nightContent == null ? null : nightContent.routes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetCampaignEntry()
        {
            pendingDay = 1;
            pendingRuleBook = false;
            pendingFlashlight = default;
        }

        private void BeginShift()
        {
            Day = pendingDay;
            ownsRuleBook = pendingRuleBook;
            ownsFlashlight = pendingFlashlight.owned;
            if (pendingFlashlight.equipped) SetEquippedItem(InventoryItem.Flashlight, powerOn: pendingFlashlight.powered);
            ResetCampaignEntry();
            if (nightContent == null) Day = 1;
            view.completeButtons[0].GetComponentInChildren<Text>().text =
                Day < 5 && nightContent != null ? "다음 밤 시작" : "시작 화면으로";
            if (Routes != null) Routes.Activate(World,Day);
            InitializeSight();
            if (World.FlashlightPickup != null) World.FlashlightPickup.gameObject.SetActive(!ownsFlashlight);
            if (ownsRuleBook)
                foreach (var item in World.Interactables)
                    if (item.kind == InteractionKind.RuleBook) item.gameObject.SetActive(false);
            if (Day >= 4) { BeginAdvanced(); return; }
            if (Day == 1) return;

            laterShift = new LaterShift(Day, Day == 2 ? tuning.secondNightSeconds : tuning.thirdNightSeconds,
                Day == 3 ? nightContent.seats.Length : 0);
            nightContent.Activate(laterShift, World);
            if (Day == 2) scratch = MakeScratch();
        }

        public void FinishShift()
        {
            if (Phase == ShiftPhase.Complete && Day < 5 && nightContent != null) LoadShift(Day + 1);
            else ReturnToLobby();
        }

        private void LoadShift(int day)
        {
            CloseOverlay();
            pendingDay = day;
            pendingRuleBook = ownsRuleBook;
            pendingFlashlight = (ownsFlashlight, FlashlightEquipped, World.Player.FlashlightOn);
            SceneManager.LoadScene(gameObject.scene.name);
        }

        private void UpdateLaterShiftAt(Vector2 position)
        {
            if (nightContent.AtFirstSite(laterShift, position) && laterShift.EnterSite())
            {
                if (Day == 2)
                {
                    World.Player.SnapFacingToCardinal();
                    sound.PlayOneShot(scratch, 0.8f);
                    Toast("메뉴판에 붉은 글씨가 드러났다. 뒤를 한 번에 돌아보지 마라.", 6);
                }
                else Toast("빈 의자에서 그림자가 몸을 일으켰다.");
            }
            if (CafeWorld.RoomLayer(position) == 8) laterShift.ReturnToSecurity();
        }

        public Vector2 FilterDirection(Vector2 input, Vector2 facing)
        {
            if (laterShift == null || !laterShift.RestrictTurning) return input.normalized;
            // Opposite inputs have already canceled on each axis. Resolve diagonals
            // to the axis the player was already using.
            if (input.x != 0 && input.y != 0)
                input = Mathf.Abs(facing.x) >= Mathf.Abs(facing.y) ? new Vector2(input.x, 0) : new Vector2(0, input.y);
            if (Vector2.Dot(input, facing) > -0.99f) return input;
            laterShift.Fail("뒤로 돌아서는 순간, 바로 등 뒤에서 손톱이 긁히는 소리가 났다.", NightFailure.Menu);
            return Vector2.zero;
        }

        private void UseNightTask(CafeInteractable item, bool alternate = false)
        {
            if (laterShift == null) return;
            bool wasStacked = item.nightAction == NightAction.Chair && laterShift.Stacked[item.taskIndex];
            string message = laterShift.Act(item.nightAction, item.taskIndex, alternate, nightContent.ChalkIdle);
            if (!wasStacked && item.nightAction == NightAction.Chair && laterShift.Stacked[item.taskIndex])
            {
                item.Highlight(false);
                nightContent.PlaceChair(item.taskIndex);
            }
            if (message.Length != 0) Toast(message);
        }

        private bool TryRoute(CafeInteractable item)
        {
            if (Routes == null) return false;
            if (Day >= 4 && BeforeStoryTravel(item)) return true;
            if (Routes.IsDetourDoor(item) && !item.isOpen)
            {
                Routes.PreparePuzzle(item);
                view.readingTitle.text = "쓰레기 처리 우회문";
                OpenOverlay(Overlay.Detour, view.reading);
                view.readingBody.gameObject.SetActive(false);
                nightContent.detourGrid.SetActive(true);
                RefreshDetour();
                return true;
            }
            if (!Routes.Enter(item, World.Player)) return false;
            int destination = CafeWorld.RoomLayer(World.Player.PlanPosition);
            if(Day==5 && Fifth.Stage==FinaleStage.Escort)
            {
                story.spirit.position=InsideRoom(World.Player.PlanPosition+Vector2.left*0.8f,destination);SetLayer(story.spirit,destination);
            }
            TryCorridorConfrontation(destination);
            if (nearest != null) nearest.Highlight(false);
            nearest = null;
            return true;
        }

        private void HandleDetourInput(Keyboard keys)
        {
            if (keys.spaceKey.wasPressedThisFrame)
            {
                CloseOverlay();
                return;
            }
            if (keys.rKey.wasPressedThisFrame) Routes.Puzzle.Reset();
            else if (keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame) Routes.Puzzle.Move(Vector2Int.down);
            else if (keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame) Routes.Puzzle.Move(Vector2Int.up);
            else if (keys.aKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame) Routes.Puzzle.Move(Vector2Int.left);
            else if (keys.dKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame) Routes.Puzzle.Move(Vector2Int.right);
            if (Routes.Puzzle.Solved)
            {
                Routes.UnlockDetour();
                CloseOverlay();
                Toast("쓰레기봉투를 모두 치웠다. 이 우회문을 열 수 있다.");
            }
            else RefreshDetour();
        }

        private void RefreshDetour()
        {
            for (int i = 0; i < nightContent.detourTiles.Length; i++)
            {
                var cell = Routes.Puzzle.CellAt(i % 6, i / 6);
                var tile = nightContent.detourTiles[i];
                var symbol = nightContent.detourSymbols[i];
                (tile.color, symbol.color, symbol.text) = cell switch
                {
                    DetourCell.Wall => (new Color32(23, 38, 40, 255), CafeUi.Muted, ""),
                    DetourCell.Goal => (new Color32(32, 77, 71, 255), CafeUi.Green, "○"),
                    DetourCell.Bag => (new Color32(102, 73, 38, 255), CafeUi.Amber, "◆"),
                    DetourCell.Delivered => (new Color32(37, 99, 82, 255), CafeUi.Paper, "◆"),
                    DetourCell.Worker => (new Color32(59, 87, 93, 255), CafeUi.Paper, "●"),
                    _ => (new Color32(55, 71, 72, 255), CafeUi.Paper, "")
                };
            }
        }

        private void RefreshShiftResult(bool success)
        {
            if (laterShift == null) return;
            if (success)
            {
                view.completeHeader.text = $"SHIFT COMPLETE  /  {Day:00}";
                view.completeTitle.text = Day == 2 ? "두 번째 밤이 끝났다" : "세 번째 밤이 끝났다";
                view.completeBody.text = Day == 2
                    ? "메뉴판이 조용해졌다. 감시실 모니터에 다음 근무가 표시된다."
                    : "홀과 창고가 조용해졌다. 감시실 모니터에 네 번째 근무가 표시된다.";
                return;
            }
            view.failureHeader.text = $"INCIDENT LOST  /  {Day:00}";
            view.failureTitle.text = laterShift.FailureKind == NightFailure.Menu ? "등 뒤에서 긁는 소리가 났다" : "마감에 실패했다";
            view.failureBody.text = laterShift.Failure + "\n\n재도전하면 이 밤의 감시실부터 시작합니다.";
            view.failureButtons[0].GetComponentInChildren<Text>().text = "이 밤 다시 시작";
        }

        private bool AnimateLaterFailure()
        {
            if (failureStarted < 0)
            {
                failureStarted = Time.unscaledTime;
                CloseOverlay();
                if (laterShift.FailureKind == NightFailure.Menu) sound.PlayOneShot(scratch);
                else sound.PlayOneShot(clatter);
            }
            float amount = Mathf.Clamp01((Time.unscaledTime - failureStarted) / 0.8f);
            floodWarning.color = new Color(0.33f, 0.025f, 0.02f, amount * 0.47f);
            nightContent.AnimateFailure(laterShift.FailureKind, mapCamera.transform, World.Player.transform.position, amount);
            return amount >= 1;
        }


        private static AudioClip MakeScratch()
        {
            const int rate = 22050;
            var samples = new float[rate / 2];
            var noise = new System.Random(47);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Sin(Mathf.PI * i / samples.Length);
                samples[i] = ((float)noise.NextDouble() - 0.5f + Mathf.Sin(t * (2800 + t * 2700))) * envelope * 0.4f;
            }
            var clip = AudioClip.Create("Menu scratch", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
