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
        private DateTime shiftClockStart;
        private bool tentacleMemoryPlayed, pursuitMemoryPlayed;
        private int recordCause;
        private string recordPeople = "";
        public bool StoryBusy => storyBusy;

        private void InitializeSight()
        {
            if (story == null) return;
            sightMaterial = new Material(story.darknessMaterial);
            story.darkness.material = sightMaterial;
            UpdateSight();
            for (int i = 0; i < story.choices.Length; i++)
            {
                int selection = i;
                story.choices[i].onClick.AddListener(() => ConfirmForm(selection));
            }
        }
        private void UpdateSight()
        {
            if (sightMaterial == null) return;
            var player = World.Player;
            int room = CafeWorld.RoomLayer(player.PlanPosition);
            story.darkness.enabled = room != CafeWorld.SecurityRoomLayer;
            if (!story.darkness.enabled) return;

            Vector2 face = World.Player.Facing;
            sightMaterial.SetVector("_Facing", new Vector4(face.x, face.y, 0, 0));
            sightMaterial.SetFloat("_On", player.FlashlightOn ? 1 : 0);

            var lamp = Routes?.securityDoorLamp;
            if (lamp == null || !lamp.gameObject.activeInHierarchy || lamp.gameObject.layer != room)
            {
                sightMaterial.SetVector("_DoorLamp", Vector4.zero);
                return;
            }
            Vector2 delta = (Vector2)lamp.position - player.PlanPosition;
            float height = mapCamera.orthographicSize * 2f;
            sightMaterial.SetVector("_DoorLamp", new Vector4(
                .5f + delta.x / (height * mapCamera.aspect), .5f + delta.y / height, 1f, 0f));
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
                shiftClockStart = DateTime.Now;
                SyncFinalObjects();
            }
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
            RefreshLocation(); RefreshPrompt(); RefreshInventory();
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
        private static void SetLayer(Transform root, int room)
        {
            foreach (var part in root.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = room;
        }
    }
}
