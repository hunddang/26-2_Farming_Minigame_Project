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
        private LaterShift laterShift;
        private AudioClip scratch;
        private float failureStarted = -1;
        public int Day { get; private set; } = 1;
        public LaterShift Later => laterShift;
        public ShiftPhase Phase => laterShift?.Phase ?? Session?.Phase ?? ShiftPhase.Surveillance;
        private NightRoutes Routes => Day == 3 ? nightContent.routes : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetCampaignEntry() => pendingDay = 1;

        private void BeginShift()
        {
            Day = pendingDay;
            pendingDay = 1;
            if (nightContent == null) Day = 1;
            if (view.nightHeader != null) view.nightHeader.text = $"NIGHT SHIFT  /  {Day:00}";
            if (view.nightTitle != null) view.nightTitle.text = Day == 1 ? "첫 번째 밤" : Day == 2 ? "두 번째 밤" : "세 번째 밤";
            view.completeButtons[0].GetComponentInChildren<Text>().text =
                Day < 3 && nightContent != null ? "다음 밤 시작" : "시작 화면으로";
            if (Day == 1) return;

            laterShift = new LaterShift(Day, Day == 2 ? tuning.secondNightSeconds : tuning.thirdNightSeconds,
                Day == 3 ? nightContent.seats.Length : 0);
            nightContent.Activate(laterShift, World);
            if (Day == 2)
            {
                cameraButtons[3].GetComponentInChildren<Text>().text = "04  로비";
                scratch = MakeScratch();
            }
            else if (Routes != null) cameraButtons[3].GetComponentInChildren<Text>().text = "04  촉수 복도";
            nightContent.controlHelp.text = "WASD 이동 · Shift 달리기\nSpace 조사 · F 손전등" +
                (Day == 3 ? "\nE 의자 빼 주기" : "");
            nightContent.itemHelp.text = Day == 2 ? "컵과 행주는 조사하면 챙깁니다." : "지우개를 챙긴 뒤 칠판을 조사하세요.";
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (i >= (Day == 2 ? 2 : 1))
                {
                    inventorySlots[i].gameObject.SetActive(false);
                    continue;
                }
                inventorySlots[i].GetComponent<Button>().interactable = false;
                foreach (var icon in inventorySlots[i].GetComponentsInChildren<Image>())
                    if (icon != inventorySlots[i]) icon.enabled = false;
            }
        }

        public void FinishShift()
        {
            if (Phase == ShiftPhase.Complete && Day < 3 && nightContent != null) LoadShift(Day + 1);
            else ReturnToLobby();
        }

        private void LoadShift(int day)
        {
            CloseOverlay();
            pendingDay = day;
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

        private string ShiftCameraName(int index)
        {
            if (index == 3)
            {
                if (Day == 2) return "로비";
                if (Routes != null) return "촉수 복도";
            }
            return CafeWorld.CameraNames[index];
        }

        private void ViewShiftCamera(int index)
        {
            if (index == 3 && Routes != null)
            {
                Routes.ViewTentacles(feedCamera);
                return;
            }
            if (Day != 2 || index != 3)
            {
                World.ViewCctv(feedCamera, index);
                return;
            }
            feedCamera.transform.SetPositionAndRotation(nightContent.lobbyCctv.position, nightContent.lobbyCctv.rotation);
            feedCamera.orthographicSize = 7;
            feedCamera.cullingMask = CafeWorld.RoomMask(nightContent.lobbyCctv.position);
        }

        private bool TryRoute(CafeInteractable item)
        {
            if (Routes == null) return false;
            if (Routes.IsDetourDoor(item) && !item.isOpen)
            {
                view.readingTitle.text = "쓰레기 처리 우회문";
                OpenOverlay(Overlay.Detour, view.reading);
                view.readingBody.gameObject.SetActive(false);
                nightContent.detourGrid.SetActive(true);
                RefreshDetour();
                return true;
            }
            if (!Routes.Enter(item, World.Player)) return false;
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
                Toast("쓰레기봉투를 모두 치웠다. 창고와 카운터 사이의 우회문이 열렸다.");
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
                    : "홀과 창고가 조용해졌다. 준비된 세 번의 야간 근무를 마쳤다.";
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

        private void RefreshLaterHud()
        {
            var phase = laterShift.Phase;
            bool running = phase == ShiftPhase.Surveillance || phase == ShiftPhase.Incident;
            timer.gameObject.SetActive(running);
            timerLine.gameObject.SetActive(running);
            int seconds = Mathf.CeilToInt(laterShift.Remaining);
            timer.text = $"{seconds / 60:00}:{seconds % 60:00}";
            timer.color = seconds < 45 ? CafeUi.Red : CafeUi.Amber;
            timerLine.rectTransform.sizeDelta = new Vector2(timerLineWidth * laterShift.Remaining / laterShift.Duration, 3);
            objective.text = phase switch
            {
                ShiftPhase.Surveillance => "CCTV로 이상을 살피고\n현장을 확인하세요.",
                ShiftPhase.Incident => Day == 2 ? "메뉴판의 붉은 흔적을\n조사하세요." :
                    laterShift.Warehouse ? "창고의 그림자를\n확인하세요." : "홀의 그림자와\n마감 안내를 살피세요.",
                ShiftPhase.Resolved => "감시실로\n돌아가세요.",
                ShiftPhase.Complete => "오늘의 근무 완료",
                _ => "사건 해결 실패"
            };
            status.text = running ? $"남은 이상현상  {laterShift.RemainingIncidents}" : "근무 기록";
            progress.text = (laterShift.Viewed ? "●" : "○") + " CCTV 확인";
            if (Day == 2)
            {
                if (laterShift.RestrictTurning) status.text += "\n뒤를 한 번에 돌지 마세요.";
                progress.text += laterShift.MenuInspected ? "\n● 메뉴판 조사" : "\n○ 메뉴판 조사";
                inventoryLabels[0].text = laterShift.Water switch
                {
                    WaterState.Empty => "빈 컵", WaterState.Cold => "찬물이 든 컵", WaterState.Hot => "데운 물이 든 컵", _ => "컵 —"
                };
                inventoryLabels[1].text = laterShift.WetCloth ? "젖은 행주" : laterShift.HasCloth ? "마른 행주" : "행주 —";
            }
            else
            {
                progress.text += laterShift.Warehouse ? "\n● 홀 정리 완료" : $"\n○ 남은 의자  {laterShift.ChairsLeft}";
                inventoryLabels[0].text = laterShift.HasEraser ? "칠판지우개" : "칠판지우개 —";
                inventoryLabels[1].text = "";
            }
            for (int i = 2; i < inventoryLabels.Length; i++) inventoryLabels[i].text = "";
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
