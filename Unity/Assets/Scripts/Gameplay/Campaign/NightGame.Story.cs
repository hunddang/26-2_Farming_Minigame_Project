using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NightCafe
{
    public sealed partial class NightGame
    {
        public NightStoryContent story;
        public FourthShift Fourth { get; private set; }
        public FifthShift Fifth { get; private set; }
        private bool storyBusy, endingStarted, securityReleaseShown;
        private Material sightMaterial;
        private Vector2 secondChaseStart;
        private int previousRoom;
        private float monsterGrace, powderUntil;
        private bool tentacleMemoryPlayed, pursuitMemoryPlayed;
        private int recordCause;
        private string recordPeople = "";
        public bool StoryBusy => storyBusy;

        private void InitializeSight()
        {
            if (story == null) return;
            sightMaterial = new Material(story.darknessMaterial);
            story.darkness.material = sightMaterial;
            foreach (var beam in World.Player.flashlight.GetComponentsInChildren<SpriteRenderer>()) beam.enabled = false;
            for (int i = 0; i < story.choices.Length; i++)
            {
                int selection = i;
                story.choices[i].onClick.AddListener(() => ConfirmForm(selection));
            }
        }
        private void UpdateSight()
        {
            if (sightMaterial == null) return;
            Vector2 face = World.Player.Facing;
            sightMaterial.SetVector("_Facing", new Vector4(face.x, face.y, 0, 0));
            sightMaterial.SetFloat("_On", World.Player.FlashlightOn ? 1 : 0);
        }
        private IEnumerator HeatMilk()
        {
            if (Day != 1 || Session.Phase != ShiftPhase.Incident || !Session.Inventory[(int)Ingredient.Milk])
            { Toast("데울 우유를 먼저 챙겨야 한다."); yield break; }
            pouring = true;
            Toast("우유를 전자레인지에 넣었다. 표시창이 90초에서 빠르게 줄어든다.", tuning.heatMilkSeconds + 1);
            yield return new WaitForSeconds(tuning.heatMilkSeconds);
            Session.HeatMilk(); pouring = false; Toast("따뜻하게 데운 우유를 챙겼다.");
        }
        private void BeginAdvanced()
        {
            foreach (var slot in inventorySlots)
                foreach (var icon in slot.GetComponentsInChildren<Image>())
                    if (icon != slot) icon.enabled = false;
            World.Flood.gameObject.SetActive(false); World.Cup.gameObject.SetActive(false);
            foreach (var drop in World.CoffeeDrops) drop.gameObject.SetActive(false);
            foreach (var item in World.Interactables)
                if (item.kind == InteractionKind.Ingredient || item.kind == InteractionKind.Recipe || item.nightAction == NightAction.Microwave)
                    item.gameObject.SetActive(false);
            story.huntObjects.SetActive(Day == 4); story.finalObjects.SetActive(Day == 5);
            story.spirit.gameObject.SetActive(false); story.memoryActor.gameObject.SetActive(false);
            story.trap.gameObject.SetActive(false); story.closedNet.SetActive(false); story.powder.SetActive(false);
            foreach (var marker in story.trapMarkers) marker.gameObject.SetActive(false);
            World.Interactables.AddRange(Day == 4 ? story.huntTasks : story.finalTasks);
            if (Day == 4)
            {
                Fourth = new FourthShift(tuning.fourthNightSeconds);
                scratch=MakeScratch();
                story.hunter.gameObject.SetActive(false);
            }
            else
            {
                Fifth = new FifthShift(tuning);
                SyncFinalObjects();
            }
            nightContent.controlHelp.text = "WASD 이동 · Shift 달리기\nSpace 조사 · F 손전등" +
                (Day == 4 ? "\nE 소화기 분사" : "\n숫자 입력 · Enter 확정");
            nightContent.itemHelp.text = Day == 4 ? "소화기는 정면의 가까운 대상에 분사합니다." : "1–5 소지한 기록 다시 읽기";
            view.nightHeader.text = $"NIGHT SHIFT  /  {Day:00}";
            view.nightTitle.text = Day == 4 ? "네 번째 밤" : "다섯 번째 밤";
            previousRoom = 8;
        }
        private void UpdateAdvanced()
        {
            pulseClock += Time.deltaTime;
            if (overlay != Overlay.Pause)
            {
                if (Day == 4) Fourth.Tick(Time.deltaTime); else Fifth.Tick(Time.deltaTime);
                if (!storyBusy && (Phase == ShiftPhase.Incident || Phase == ShiftPhase.Resolved))
                {
                    string warning = Routes.Tick(World.Player,
                        reason => { if (Day == 4) Fourth.Fail(reason); else Fifth.Capture(reason); },
                        suppressAttacks: Phase == ShiftPhase.Resolved || Fifth?.Identified == true,
                        tentaclesReleased: Fifth?.TentaclesReleased == true,
                        smokeCleared: Fifth?.SmokeCleared == true,
                        tentacleHit: Day == 5 ? (Action)(() => { Fifth.Wrong("전선에 몸이 닿았다."); RefreshCracks(); }) : null);
                    if (warning.Length > 0) Toast(warning);
                }
            }
            if (Phase == ShiftPhase.Failed && !endingStarted)
            {
                endingStarted = true; StartCoroutine(FailureMovie());
            }
            if (Phase == ShiftPhase.Complete && !endingStarted)
            {
                endingStarted = true;
                if (Day == 5) StartCoroutine(HappyMovie()); else ShowEnd(true);
            }
            HandleInput();
            if (!InputBlocked)
            {
                if (Day == 4) UpdateHunt(); else UpdateFinale();
                FindNearest();
            }
            else if (nearest != null) { nearest.Highlight(false); nearest = null; }
            if (Day == 4) RefreshHuntObjects(); else SyncFinalObjects();
            RefreshAdvancedHud(); RefreshLocation(); RefreshPrompt();
        }
        private void UseStoryTask(CafeInteractable item)
        {
            if (Day == 4) UseHuntTask(item); else if (Day == 5) UseFinalTask(item);
        }
        private bool BeforeStoryTravel(CafeInteractable item)
        {
            if (item.nightAction != NightAction.Travel) return false;
            if (Day == 5 && Fifth.Frozen && CafeWorld.RoomLayer(World.Player.PlanPosition) == 8 && !Fifth.CanLeaveSecurity)
            { Toast("감시실을 더 탐색하세요. 근무표·사고 기록·숙직실 CCTV를 확인해야 한다."); return true; }
            if (Day == 5 && Fifth.Stage == FinaleStage.Escort)
            {
                if (Vector2.Distance(story.spirit.position, World.Player.PlanPosition) > 2.3f || !World.Player.FlashlightOn)
                { Toast("손전등을 비추며 영혼이 따라올 때까지 기다리자."); return true; }
            }
            return false;
        }
        private void ObserveStoryCamera(int index)
        {
            int room = CafeWorld.CameraRooms[index];
            if (Day == 4)
            {
                if (Fourth.Stage == HuntStage.Leak) Fourth.Observe(room);
                else if (Fourth.Stage == HuntStage.Search && room == CafeWorld.RoomLayer(story.hunter.position))
                { Fourth.Observe(room); Toast("낙하자는 " + CafeWorld.RoomNames[room - 8] + "에 있다. 그 구역에 머물러 있다."); }
            }
            else if (Fifth.Frozen && room == 12) Fifth.ObserveIdentity();
            story.feedEffect.SetActive(Day == 5 && Fifth.Stage == FinaleStage.Blackout && index < CafeWorld.BlackoutChannels);
            story.feedCaption.text = "NO SIGNAL\n\n카메라 전원이 끊겼다. 해당 장소의 숫자를 직접 확인해야 한다.";
            RefreshCracks();
        }
        private void RefreshCracks()
        {
            int amount = Day == 5 && Fifth != null ? Fifth.Cracks : 0;
            for (int i = 0; i < story.cracks.Length; i++) story.cracks[i].gameObject.SetActive(i < amount * 3);
            story.handprint.SetActive(amount>=1);
            story.faceOutline.SetActive(amount>=2);
        }
        private void RefreshAdvancedHud()
        {
            bool frozen = Day == 5 && Fifth.Frozen;
            float remaining = Day == 4 ? Fourth.Remaining : Fifth.Remaining;
            timer.gameObject.SetActive(true); timerLine.gameObject.SetActive(!frozen && Phase == ShiftPhase.Incident);
            timer.text = frozen ? FinaleClock() : $"{Mathf.CeilToInt(remaining) / 60:00}:{Mathf.CeilToInt(remaining) % 60:00}";
            timer.color = frozen ? CafeUi.Muted : remaining < 45 ? CafeUi.Red : CafeUi.Amber;
            timerLine.rectTransform.sizeDelta = new Vector2(timerLineWidth * remaining / (Day == 4 ? tuning.fourthNightSeconds : tuning.fifthNightSeconds), 3);
            if (Day == 4)
            {
                objective.text = Fourth.Stage switch
                {
                    HuntStage.Leak => "CCTV의 소리와\n로비 누수를 확인하세요.", HuntStage.FirstChase => "소화기를 찾아\n정면에 분사하세요.",
                    HuntStage.ReadNotes => "전임자의 노트를\n확인하세요.", HuntStage.Equipment => "창고에서 포획 장비를\n챙기세요.",
                    HuntStage.SecondChase => "방의 반대편이나\n출구로 도망치세요.", HuntStage.InstallTrap => "노란 표식 중 한 곳에\n함정을 설치하세요.",
                    HuntStage.Search => "CCTV에서 낙하자를\n찾아 현장으로 가세요.", HuntStage.ThirdChase => "낙하자를 함정으로\n유인하세요.", _ => "감시실로 돌아가세요."
                };
                status.text = Fourth.Chasing ? "발소리가 따라온다" : "천장이 조용하다";
                progress.text = $"소화기 {Fourth.Charges}/2\n그물 {(Fourth.HasNet ? "●" : "○")}  고정구 {(Fourth.HasAnchor ? "●" : "○")}";
                inventoryLabels[0].text = Fourth.HasExtinguisher ? $"소화기 {Fourth.Charges}/2" : "소화기 —";
                inventoryLabels[1].text = Fourth.HasNet ? "그물" : "그물 —"; inventoryLabels[2].text = Fourth.HasAnchor ? "고정구" : "고정구 —";
                inventoryLabels[3].text = Fourth.TrapRoom >= 0 ? "설치 · " + CafeWorld.RoomNames[Fourth.TrapRoom - 8] : "함정 —";
                inventoryLabels[4].text = "전임자의 노트";
            }
            else
            {
                objective.text = Fifth.Stage switch
                {
                    FinaleStage.Blackout => "네 곳의 숫자를 찾아\n컴퓨터를 복구하세요.", FinaleStage.Return => "감시실로 돌아가세요.",
                    FinaleStage.Escort => "손전등으로 비추며\n남은 사람과 함께\n출구로 이동하세요.",
                    FinaleStage.Happy => "마지막 근무 종료", FinaleStage.Captured => "화면 안에 갇혔다", _ => "마지막 근무의\n원인을 조사하세요."
                };
                status.text = frozen ? $"시간 정지\nCCTV 균열  {Fifth.Cracks}/3" : "꺼진 CCTV";
                progress.text = $"{(Fifth.Identified ? "●" : "○")} 직원 신원\n{(Fifth.TentaclesReleased ? "●" : "○")} 잠긴 문\n{(Fifth.SmokeCleared ? "●" : "○")} 배기와 연기\n{(Fifth.RecordCorrected ? "●" : "○")} 사고 기록";
                string[] names = { "근무표", "사고 기록", "수리 요청", "정비도", "A의 쪽지" };
                bool[] found = { Fifth.RosterRead, Fifth.AccidentRead, Fifth.RepairRequestRead, Fifth.DiagramRead, Fifth.HasNote };
                for (int i = 0; i < 5; i++) inventoryLabels[i].text = $"{i + 1} {names[i]}" + (found[i] ? "" : " —");
            }
        }
        private static void SetLayer(Transform root, int room)
        {
            foreach (var part in root.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = room;
        }
    }
}
