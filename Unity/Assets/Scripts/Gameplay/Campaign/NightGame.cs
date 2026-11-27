using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightCafe
{
    public sealed partial class NightGame : MonoBehaviour
    {
        public GameTuning tuning;
        public GameTuning Tuning => tuning;
        public ShiftSession Session { get; private set; }
        [Header("Scene references")]
        public CafeWorld world;
        public NightGameView view;
        public CafeWorld World => world;
        public CafeInteractable CurrentTarget => nearest;
        public bool InputBlocked => InventoryOpen || overlay != Overlay.None || pouring || storyBusy || Phase == ShiftPhase.Failed;
        private enum Overlay { None, Cctv, Rules, Reading, Detour, StoryForm, Pause, Failure, Complete }
        private Overlay overlay;
        private GameObject modal;
        public Camera feedCamera;
        public Camera[] roomCameras;
        public Camera ActiveRoomCamera { get; private set; }
        public Camera mapCamera;
        public const string DeskNote = "이상한 게 보이면 직접 확인해 주세요.\n감시실에서만 CCTV를 볼 수 있어요.\n\nWASD · 지도 클릭  카메라 선택\nSpace  조사     Shift  달리기\nTab  인벤토리     Esc  닫기\nF  장비한 손전등 전원";
        private RenderTexture feed;
        private Text prompt, toast;
        private Image floodWarning;
        private Button[] cameraButtons;
        private Text feedTitle;
        private CafeInteractable nearest;
        private int cameraIndex, menuIndex;
        private Button[] menuButtons;
        private float toastUntil, pulseClock, clatterAt;
        private bool pouring;
        private float cupAnimationTime;
        public AudioSource sound;
        private AudioClip clatter;
        private float floodAtResolution;

        private void Start()
        {
            CafeUi.ConfigureWindow();
            if (tuning == null || world == null || view == null || view.inventory == null || mapCamera == null || feedCamera == null ||
                roomCameras == null || roomCameras.Length != CafeWorld.CameraRooms.Length || sound == null)
            {
                Debug.LogError("NightGame: assign tuning, world, view, cameras and sound in the scene Inspector.", this);
                enabled = false;
                return;
            }
            Session = new ShiftSession(tuning);
            World.Player.Game = this;
            feed = new RenderTexture(1280, 720, 16) { name = "CCTV Live Feed" };
            feedCamera.targetTexture = feed;
            feedCamera.enabled = false;
            view.feedImage.texture = null;
            prompt = view.prompt; toast = view.toast;
            floodWarning = view.floodWarning;
            cameraButtons = view.cameraButtons; feedTitle = view.feedTitle;
            view.Bind(this);
            clatter = MakeClatter();
            BeginShift();
            RefreshInventory();
            Toast("감시실 책상에서 손전등을 챙기고 CCTV를 확인하자.", 7);
            if (Day == 1 && Array.IndexOf(Environment.GetCommandLineArgs(), "-foundationSmokeTest") >= 0)
                new GameObject("Foundation Smoke Test").AddComponent<FoundationSmokeTest>().game = this;
        }

        private void Update()
        {
            if (Session == null) return;
            if (HandleInventoryInput()) return;
            if (Day >= 4) { UpdateAdvanced(); return; }
            pulseClock += Time.deltaTime;
            // Inventory input returns before simulation so the world stays frozen behind it.
            if (overlay != Overlay.Pause)
            {
                if (laterShift == null) Session.Tick(Time.deltaTime);
                else
                {
                    laterShift.Tick(Time.deltaTime);
                    if (Routes != null && (Phase == ShiftPhase.Surveillance || Phase == ShiftPhase.Incident || Phase == ShiftPhase.Resolved))
                    {
                        string warning = Routes.Tick(World.Player, message => laterShift.Fail(message, NightFailure.Passage),
                            suppressAttacks: Phase == ShiftPhase.Resolved);
                        if (warning.Length != 0) Toast(warning);
                    }
                }
            }
            if (Phase == ShiftPhase.Failed && overlay != Overlay.Failure && (laterShift == null ? AnimateFirstFailure() : AnimateLaterFailure())) ShowEnd(false);
            if (Phase == ShiftPhase.Complete && overlay != Overlay.Complete) ShowEnd(true);
            HandleInput();
            if (!InputBlocked)
            {
                Vector2 pos = World.Player.PlanPosition;
                if (laterShift != null) UpdateLaterShiftAt(pos);
                else if (CafeWorld.RoomLayer(pos) == 10 && Session.Phase == ShiftPhase.Surveillance)
                {
                    Session.StartIncident();
                    cupAnimationTime = 0;
                    Toast("컵이 회전하며 커피를 쏟아내기 시작했다!", 5);
                }
                if (laterShift == null && CafeWorld.RoomLayer(pos) == 8) Session.ReturnToSecurity();
                FindNearest();
            }
            else if (nearest != null) { nearest.Highlight(false); nearest = null; }
            if (laterShift == null) AnimateWorld();
            else nightContent.Refresh(laterShift, World.Player.PlanPosition, overlay == Overlay.Cctv);
            RefreshLocation();
            RefreshPrompt();
            RefreshInventory();
        }

        private void LateUpdate() => UpdateSight();

        private void HandleInput()
        {
            var keys = Keyboard.current;
            if (keys == null || pouring || storyBusy || (Phase == ShiftPhase.Failed && overlay != Overlay.Failure)) return;
            if (keys.escapeKey.wasPressedThisFrame)
            {
                if (overlay == Overlay.None) Pause();
                else if (overlay == Overlay.Pause || overlay == Overlay.Reading || overlay == Overlay.Rules || overlay == Overlay.Cctv || overlay == Overlay.Detour || overlay == Overlay.StoryForm) CloseOverlay();
                return;
            }
            if (overlay == Overlay.Cctv)
            {
                int count = roomCameras.Length;
                if (keys.aKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame) SelectCamera((cameraIndex + count - 1) % count);
                if (keys.dKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame) SelectCamera((cameraIndex + 1) % count);
                if (keys.spaceKey.wasPressedThisFrame) CloseOverlay();
                return;
            }
            if (overlay == Overlay.Pause || overlay == Overlay.Failure || overlay == Overlay.Complete)
            {
                MenuInput(); return;
            }
            if (overlay == Overlay.Reading || overlay == Overlay.Rules) return;
            if (overlay == Overlay.StoryForm) { HandleStoryForm(keys); return; }
            if (overlay == Overlay.Detour)
            {
                HandleDetourInput(keys);
                return;
            }
            if (Day == 4 && keys.eKey.wasPressedThisFrame && Fourth.Chasing) { UseSpray(); return; }
            if (keys.spaceKey.wasPressedThisFrame && nearest != null) Interact(nearest);
            else if (keys.eKey.wasPressedThisFrame && nearest != null && nearest.nightAction == NightAction.Chair)
                UseNightTask(nearest, true);
        }

        private void FindNearest()
        {
            if (nearest != null) nearest.Highlight(false);
            nearest = null;
            float closest = tuning.interactionDistance;
            Vector2 player = World.Player.PlanPosition;
            foreach (var item in World.Interactables)
            {
                if (!item.isActiveAndEnabled) continue;
                float distance = Vector2.Distance(player, (Vector2)item.transform.position);
                if (distance >= closest) continue;
                // Walls/counters cannot be interacted through. The target door itself is allowed.
                var hits = Physics2D.LinecastAll(player, item.transform.position);
                bool blocked = false;
                foreach (var hit in hits)
                    if (hit.collider.gameObject != World.Player.gameObject && hit.collider.gameObject != item.gameObject && !hit.collider.isTrigger)
                    { blocked = true; break; }
                if (blocked) continue;
                closest = distance; nearest = item;
            }
            if (nearest != null) nearest.Highlight(true);
        }

        public void Interact(CafeInteractable item)
        {
            if (InputBlocked || item == null || !item.isActiveAndEnabled) return;
            if (TryRoute(item)) return;
            switch (item.kind)
            {
                case InteractionKind.Camera:
                    if (CafeWorld.RoomLayer(World.Player.PlanPosition) == 8) OpenCctv();
                    break;
                case InteractionKind.Story: UseStoryTask(item); break;
                case InteractionKind.NightTask:
                    if(item.nightAction==NightAction.Microwave) StartCoroutine(HeatMilk());
                    else UseNightTask(item);
                    break;
                case InteractionKind.RuleBook:
                    if (TryPickUp(item, InventoryItem.RuleBook))
                    {
                        ownsRuleBook = true;
                        OpenRules();
                    }
                    break;
                case InteractionKind.Recipe: OpenRecipe(); break;
                case InteractionKind.Flashlight:
                    if (TryPickUp(item, InventoryItem.Flashlight))
                    {
                        ownsFlashlight = true;
                        Toast("손전등을 챙겼다. Tab을 눌러 인벤토리에서 우클릭해 장비하자.");
                    }
                    break;
                case InteractionKind.Ingredient:
                    if (Session.Collect(item.ingredient)) Toast(item.title + " 획득. Tab으로 인벤토리를 열고 우클릭해 손에 들 수 있다.");
                    else Toast(Session.Phase == ShiftPhase.Incident ? "이미 가지고 있는 재료다." : "지금은 재료가 필요하지 않다.");
                    break;
                case InteractionKind.Cup:
                    if (Session.Phase == ShiftPhase.Resolved) { Toast("커피가 더는 나오지 않는다. 감시실로 돌아가자."); break; }
                    if (!Session.CupInspected)
                    {
                        Session.CupInspected = true;
                        Toast("쓴 커피 향. 우유도 시럽도 없는 에스프레소다. 컵 안에서 커피가 끝없이 솟는다.", 7);
                    }
                    else if (Session.SelectedSlot < 0 || !Session.Inventory[Session.SelectedSlot]) Toast("뜨겁고 쓴 에스프레소다. 주변의 종이와 규칙서를 살펴보자.", 5);
                    else StartCoroutine(Pour());
                    break;
                case InteractionKind.Door:
                    // Doors stay open; do not close on top of the player's collider.
                    if (!item.isOpen) { item.SetDoor(true); Toast(item.title + "을 열었다.", 2); }
                    else Toast("열려 있는 문이다.", 2);
                    break;
                default: Read(item.title, item.description); break;
            }
        }

        private bool TryPickUp(CafeInteractable item, InventoryItem inventoryItem)
        {
            if (!TryReturnItem(inventoryItem, -1))
            {
                Toast("인벤토리에 빈칸이 없다.");
                return false;
            }
            item.gameObject.SetActive(false);
            return true;
        }

        private IEnumerator Pour()
        {
            pouring = true;
            Toast("팔을 뻗어 " + GameTuning.IngredientName((Ingredient)Session.SelectedSlot) + "를 넣는 중…", 1);
            float reachTime = 0;
            while (reachTime < 0.8f)
            {
                reachTime += Time.deltaTime;
                World.Player.Reach(Mathf.Clamp01(reachTime / 0.8f));
                yield return null;
            }
            World.Player.Reach(0);
            if (Session.Phase == ShiftPhase.Incident)
            {
                bool correct = Session.TryPour();
                if (!ReturnHeldItem()) SetHeldItem(HeldItem, handReturnSlot);
                if (Session.Phase == ShiftPhase.Resolved)
                {
                    floodAtResolution = Mathf.Clamp01(1 - Session.Remaining / tuning.incidentSeconds);
                    Toast("흐름이 멈췄다. 커피가 카페모카로 변하고 가루처럼 흩어진다…", 5);
                }
                else if (correct) Toast("재료가 섞였다. 흐름은 아직 멈추지 않았다.", 4);
                else { sound.PlayOneShot(clatter); Toast("컵이 커피를 쏟아냈다! 시간이 줄었다. 투입 순서가 초기화됐다.", 5); }
            }
            pouring = false;
        }

        private void OpenRecipe()
        {
            Session.RecipeRead = true;
            string order = string.Join("  →  ", Array.ConvertAll(tuning.recipe, GameTuning.IngredientName));
            Read("접힌 카페 레시피", "CAFÉ MOCHA\n\n에스프레소에 초콜릿 소스를 먼저 녹이고 데운 우유를 섞는다.\n\n★ " + order + " ★\n\n사장 메모: 순서가 바뀌면 맛이 달라져요.\n\n우유는 전자레인지에서 90~120초 데운다.\n재료는 카운터 제조 공간에 보관.\n엉뚱한 재료는 넣지 말 것.");
        }

        private void OpenOverlay(Overlay next, GameObject panel)
        {
            CloseOverlay();
            overlay = next;
            modal = panel;
            modal.SetActive(true);
        }

        public void CloseOverlay()
        {
            CloseInventory();
            if(story != null && story.form != null) story.form.SetActive(false);
            if (overlay == Overlay.Detour)
            {
                nightContent.detourGrid.SetActive(false);
                view.readingBody.gameObject.SetActive(true);
            }
            if (feedCamera != null) feedCamera.enabled = false;
            StopRoomCamera();
            if (view != null && view.feedImage != null) view.feedImage.texture = null;
            if (modal != null) modal.SetActive(false);
            modal = null; overlay = Overlay.None;
            menuButtons = null;
            Time.timeScale = 1;
        }

        private void OpenRules()
        {
            if (Day >= 4) { OpenAdvancedNotes(); return; }
            if (laterShift != null)
            {
                view.readingTitle.text = "전 알바생의 노트";
                view.readingBody.text = Day == 2
                    ? "문구가 드러난 동안 뒤를 한 번에 돌아보지 마십시오.\n방향을 바꿀 때는 옆을 먼저 보십시오.\n\n붉은 흔적은 따뜻하게 불린 뒤 닦으십시오."
                    : "빈자리에 손님이 보이면 의자를 빼 주십시오.\n분필이 혼자 움직이더라도 칠판의 글씨를 지우지 마십시오.\n두 문장 모두 지웠다가 덧쓴 자국이 남아 있다.\n\n홀 연결 복도는 CCTV 지도에서 확인할 수 있습니다.";
                view.readingBody.text += "\n\nTab 인벤토리";
                OpenOverlay(Overlay.Rules, view.reading);
                return;
            }
            OpenOverlay(Overlay.Rules, view.rules);
        }

        private void Read(string title, string text)
        {
            view.readingTitle.text = title;
            view.readingBody.text = text;
            OpenOverlay(Overlay.Reading, view.reading);
        }

        private void OpenCctv()
        {
            OpenOverlay(Overlay.Cctv, view.cctv);
            view.feedImage.texture = feed;
            SelectCamera(CafeWorld.CameraChannel(8));
        }

        public void SelectCamera(int index)
        {
            if (overlay != Overlay.Cctv || index < 0 || index >= roomCameras.Length) return;
            StopRoomCamera();
            cameraIndex = index;
            ActiveRoomCamera = roomCameras[index];
            ActiveRoomCamera.targetTexture = feed;
            ActiveRoomCamera.enabled = true;
            int room = CafeWorld.CameraRooms[index];
            feedTitle.text = CameraHeading();
            for (int i = 0; i < cameraButtons.Length; i++) cameraButtons[i].GetComponent<CafeButtonStyle>().Select(i == index);
            if (Day >= 4) ObserveStoryCamera(index);
            if (laterShift == null && room == 10) Session.CameraViewed = true;
            if (laterShift != null && (Day == 2 ? room == 9 : room == (laterShift.Warehouse ? 11 : 10)))
                laterShift.Viewed = true;
        }

        private string CameraHeading() => $"CAM {cameraIndex + 1:00}  /  {CafeWorld.RoomNames[CafeWorld.CameraRooms[cameraIndex] - 8]}";

        private void StopRoomCamera()
        {
            if (ActiveRoomCamera == null) return;
            ActiveRoomCamera.enabled = false;
            ActiveRoomCamera.targetTexture = null;
            ActiveRoomCamera = null;
        }

        public void Pause()
        {
            if (InputBlocked) return;
            OpenOverlay(Overlay.Pause, view.pause);
            Time.timeScale = 0;
            menuButtons = view.pauseButtons;
            menuIndex = 0; MarkMenu();
        }

        private void ShowEnd(bool success)
        {
            OpenOverlay(success ? Overlay.Complete : Overlay.Failure, success ? view.complete : view.failure);
            if (success) view.completeBody.text = "감시실 문을 닫자, 카페는 다시 조용해졌다.\n\n남은 시간  " + Mathf.CeilToInt(Session.Remaining) + "초     /     잘못된 투입  " + Session.Mistakes + "회";
            RefreshShiftResult(success);
            if(Day>=4) RefreshAdvancedResult(success);
            menuButtons = success ? view.completeButtons : view.failureButtons;
            menuIndex = 0; MarkMenu();
        }

        public void Retry()
        {
            if(Day>=4) { RetryAdvanced(); return; }
            if (laterShift != null)
            {
                LoadShift(Day);
                return;
            }
            StopAllCoroutines(); pouring = false;
            failureStarted=-1;World.Player.transform.localScale=Vector3.one;
            World.Player.Reach(0);
            CloseOverlay(); Session.RetryIncident(); World.Player.Teleport(CafeWorld.IncidentSpawn);
            cupAnimationTime = 0;
            foreach (var item in World.Interactables) if (item.kind == InteractionKind.Door) item.SetDoor(true);
            Toast("사건 시작 시점으로 돌아왔다. 재료와 컵, 커피 수위가 초기화됐다.", 5);
        }

        private void MenuInput()
        {
            var keys = Keyboard.current;
            if (menuButtons == null) return;
            if (keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame) menuIndex = (menuIndex + menuButtons.Length - 1) % menuButtons.Length;
            if (keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame) menuIndex = (menuIndex + 1) % menuButtons.Length;
            MarkMenu();
            if (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame) menuButtons[menuIndex].onClick.Invoke();
        }
        private void MarkMenu()
        {
            for (int i = 0; i < menuButtons.Length; i++) menuButtons[i].GetComponent<CafeButtonStyle>().Select(i == menuIndex);
        }

        private void AnimateWorld()
        {
            bool alive = Session.Phase == ShiftPhase.Surveillance || Session.Phase == ShiftPhase.Incident;
            World.Cup.position = World.CupOrigin + Vector3.up * (alive ? Mathf.Sin(pulseClock * 2.1f) * 0.25f : 0);
            bool hasCupFrame = World.PlayCupFrame(cupAnimationTime, Session.Phase);
            World.Cup.rotation = Quaternion.Euler(0, 0, alive && !hasCupFrame ? pulseClock * (Session.SurgeRemaining > 0 ? 230 : 35) : 0);
            cupAnimationTime += Time.deltaTime * (Session.SurgeRemaining > 0 ? 2.5f : 1);
            for (int i = 0; i < World.CoffeeDrops.Length; i++)
            {
                var drop = World.CoffeeDrops[i];
                bool dust=Session.Phase==ShiftPhase.Resolved;
                drop.gameObject.SetActive(Session.Phase == ShiftPhase.Incident || dust);
                float travel = Mathf.Repeat(pulseClock * (Session.SurgeRemaining > 0 ? 2 : 0.8f) + i * 0.17f, 1);
                float angle = i * 2.4f + pulseClock;
                drop.position = World.Cup.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * travel * 1.8f;
                drop.localScale = Vector3.one * (1 - travel * 0.6f);
                if(dust)
                {
                    float amount=1-Session.Flood;
                    drop.position=World.CupOrigin+new Vector3(Mathf.Cos(i*2.4f)*(1+amount),Mathf.Sin(i*2.4f)+amount*3,0);
                    drop.localScale=Vector3.one*(1-amount)*2;
                    drop.GetComponent<SpriteRenderer>().color=new Color(0.18f,0.065f,0.025f,1-amount);
                }
                else drop.GetComponent<SpriteRenderer>().color=new Color32(159,97,48,255);
            }
            float flood = Session.Phase == ShiftPhase.Resolved ? Session.Flood * floodAtResolution : Session.Phase == ShiftPhase.Complete ? 0 : Session.Flood;
            if(Session.Phase==ShiftPhase.Failed) flood=1;
            World.Flood.color = Session.Phase == ShiftPhase.Resolved ? new Color(0.18f,0.065f,0.025f,flood*0.72f) : new Color(0.28f, 0.12f, 0.03f, flood * 0.72f);
            floodWarning.color = new Color(0.55f, 0.12f, 0.03f, Mathf.Max(0, flood - 0.65f) * (0.3f + 0.12f * Mathf.Sin(pulseClock * 4)));
            if (alive && pulseClock >= clatterAt && ((overlay == Overlay.Cctv && cameraIndex == 0) || Vector2.Distance(World.Player.transform.position, World.Cup.position) < 10))
            {
                sound.PlayOneShot(clatter, overlay == Overlay.Cctv ? 0.7f : 0.4f);
                clatterAt = pulseClock + (Session.SurgeRemaining > 0 ? 0.4f : 2.2f);
            }
        }
        private bool AnimateFirstFailure()
        {
            if(failureStarted<0) { failureStarted=Time.unscaledTime;CloseOverlay();sound.PlayOneShot(clatter,0.5f); }
            float amount=Mathf.Clamp01((Time.unscaledTime-failureStarted)/tuning.cinematicSeconds);
            World.Player.transform.localScale=new Vector3(1-amount*0.6f,1-amount*0.9f,1);
            return amount>=1;
        }

        private void RefreshLocation()
        {
            mapCamera.cullingMask = CafeWorld.RoomMask(World.Player.PlanPosition);
        }

        private void RefreshPrompt()
        {
            prompt.text = !InputBlocked && nearest != null ? "[Space]  " + nearest.title + (nearest.kind == InteractionKind.Cup && Session.SelectedSlot >= 0 ? "  ·  선택 재료 사용" : "") : "";
            if (!InputBlocked && nearest != null && nearest.nightAction == NightAction.Chair)
                prompt.text = "[Space] 의자 정리   /   [E] 의자 빼 주기";
            if (Time.unscaledTime > toastUntil) toast.text = "";
        }

        private void Toast(string text, float duration = 4) { toast.text = text; toastUntil = Time.unscaledTime + duration; }
        private static AudioClip MakeClatter()
        {
            const int rate = 22050;
            var samples = new float[rate / 3];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                samples[i] = (Mathf.Sin(t * 3700 * Mathf.PI * 2) + Mathf.Sin(t * 2390 * Mathf.PI * 2) * 0.4f) * Mathf.Exp(-t * 35) * 0.4f;
            }
            var clip = AudioClip.Create("Ceramic clatter", samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        public void ReturnToLobby() { CloseOverlay(); SceneManager.LoadScene("Lobby"); }
        public static void Quit()
        {
            Time.timeScale = 1;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void OnDisable() => CloseInventory();

        private void OnDestroy()
        {
            Time.timeScale = 1;
            StopRoomCamera();
            if (feedCamera != null) { feedCamera.enabled = false; feedCamera.targetTexture = null; }
            if (view != null && view.feedImage != null) view.feedImage.texture = null;
            if (feed != null) { feed.Release(); Destroy(feed); }
            if (clatter != null) Destroy(clatter);
            if (scratch != null) Destroy(scratch);
            if(sightMaterial != null) Destroy(sightMaterial);
        }
    }
}
