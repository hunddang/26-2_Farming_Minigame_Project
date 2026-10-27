using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightCafe
{
    public sealed class NightGame : MonoBehaviour
    {
        public GameTuning tuning;
        public GameTuning Tuning => tuning;
        public ShiftSession Session { get; private set; }
        public CafeWorld World { get; private set; }
        public CafeInteractable CurrentTarget => nearest;
        public bool InputBlocked => overlay != Overlay.None || pouring;
        private enum Overlay { None, Cctv, Rules, Reading, Pause, Failure, Complete }
        private Overlay overlay;
        private Canvas canvas;
        private GameObject modal;
        private Camera feedCamera;
        private Camera mapCamera;
        private RenderTexture feed;
        private Text objective, status, timer, prompt, toast, location, progress;
        private Image timerLine, floodWarning;
        private readonly Text[] inventoryLabels = new Text[5];
        private readonly Image[] inventorySlots = new Image[5];
        private readonly Button[] cameraButtons = new Button[4];
        private Text feedTitle;
        private CafeInteractable nearest;
        private int cameraIndex, menuIndex;
        private Button[] menuButtons;
        private float toastUntil, pulseClock, clatterAt;
        private bool pouring;
        private AudioSource sound;
        private AudioClip clatter;
        private float floodAtResolution;

        private void Start()
        {
            CafeUi.ConfigureWindow();
            if (tuning == null) tuning = ScriptableObject.CreateInstance<GameTuning>();
            Session = new ShiftSession(tuning);
            World = new CafeWorld(true);
            World.Player.Game = this;
            var cameraObject = new GameObject("Map Camera", typeof(Camera), typeof(AudioListener));
            mapCamera = cameraObject.GetComponent<Camera>();
            cameraObject.tag = "MainCamera";
            mapCamera.orthographic = true; mapCamera.orthographicSize = 12.6f;
            mapCamera.transform.position = new Vector3(0, 0, -30);
            mapCamera.clearFlags = CameraClearFlags.SolidColor; mapCamera.backgroundColor = CafeUi.Ink;
            mapCamera.rect = new Rect(0.01f, 0.13f, 0.775f, 0.75f);
            mapCamera.cullingMask = CafeWorld.RoomMask(CafeWorld.SecuritySpawn);
            feedCamera = new GameObject("CCTV Camera", typeof(Camera)).GetComponent<Camera>();
            feedCamera.orthographic = true; feedCamera.orthographicSize = 7;
            feedCamera.clearFlags = CameraClearFlags.SolidColor; feedCamera.backgroundColor = CafeUi.Ink;
            feed = new RenderTexture(1280, 720, 16);
            feedCamera.targetTexture = feed; feedCamera.enabled = false;
            sound = gameObject.AddComponent<AudioSource>(); sound.volume = 0.2f;
            clatter = MakeClatter();
            BuildHud();
            Toast("감시실에서 근무를 시작했다. 사장이 남긴 메모와 CCTV를 확인하자.", 7);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-foundationSmokeTest") >= 0)
                gameObject.AddComponent<FoundationSmokeTest>().game = this;
        }

        private void BuildHud()
        {
            canvas = CafeUi.Canvas("In-game interface · 1920×1080");
            var root = canvas.transform;
            CafeUi.Panel(root, "Header", 0, 0, 1920, 108, CafeUi.Ink);
            CafeUi.Label(root, "NIGHT SHIFT  /  01", 48, 22, 500, 38, 28, CafeUi.Amber);
            CafeUi.Label(root, "이상현상 관찰 기록", 48, 65, 500, 28, 18, CafeUi.Muted);
            timer = CafeUi.Label(root, "", 740, 15, 440, 55, 42, CafeUi.Amber, TextAnchor.MiddleCenter);
            timerLine = CafeUi.Panel(root, "Timer line", 740, 83, 440, 3, CafeUi.Amber);
            location = CafeUi.Label(root, "감시실", 1250, 40, 200, 40, 24, CafeUi.Paper, TextAnchor.MiddleRight);
            CafeUi.Button(root, "일시정지  [Esc]", 1512, 24, 360, 60, Pause);

            var sidebar = CafeUi.Panel(root, "Shift record", 1512, 130, 360, 746, new Color32(23, 37, 41, 255)).transform;
            CafeUi.Label(sidebar, "야간 근무", 28, 28, 304, 32, 20, CafeUi.Amber);
            CafeUi.Label(sidebar, "첫 번째 밤", 28, 72, 304, 60, 36);
            CafeUi.Panel(sidebar, "Divider", 28, 151, 304, 1, new Color32(68, 88, 84, 255));
            CafeUi.Label(sidebar, "현재 할 일", 28, 177, 304, 32, 19, CafeUi.Muted);
            objective = CafeUi.Label(sidebar, "", 28, 223, 304, 122, 27);
            status = CafeUi.Label(sidebar, "", 28, 370, 304, 90, 20, CafeUi.Green);
            progress = CafeUi.Label(sidebar, "", 28, 473, 304, 88, 20, CafeUi.Muted);
            CafeUi.Button(sidebar, "규칙서 열기  [Tab]", 24, 580, 312, 64, OpenRules);
            CafeUi.Label(sidebar, "WASD 이동 · Shift 달리기\nSpace 조사 · F 손전등\n1–5 재료 선택 / 선택 해제", 28, 666, 308, 75, 17, CafeUi.Muted);

            floodWarning = CafeUi.Panel(root, "Flood tint", 0, 108, 1490, 825, new Color(0.6f, 0.2f, 0.04f, 0));
            prompt = CafeUi.Label(root, "", 240, 842, 1020, 50, 25, CafeUi.Amber, TextAnchor.MiddleCenter);
            toast = CafeUi.Label(root, "", 60, 895, 1400, 48, 22, CafeUi.Paper, TextAnchor.MiddleCenter);
            CafeUi.Panel(root, "Inventory bar", 0, 957, 1920, 123, CafeUi.Ink);
            CafeUi.Label(root, "소지품", 48, 995, 130, 44, 24, CafeUi.Muted);
            for (int i = 0; i < 5; i++)
            {
                int slot = i;
                var button = CafeUi.Button(root, "", 214 + i * 248, 978, 230, 78, () => SelectSlot(slot));
                inventorySlots[i] = button.GetComponent<Image>();
                inventoryLabels[i] = CafeUi.Label(button.transform, "", 18, 0, 200, 78, 22, null, TextAnchor.MiddleLeft);
            }
            CafeUi.Label(root, "선택 후 컵 앞에서 Space\n같은 번호를 누르면 조사 모드", 1512, 986, 370, 64, 19, CafeUi.Muted);
        }

        private void Update()
        {
            if (Session == null) return;
            pulseClock += Time.unscaledDeltaTime;
            // Reading rules, recipes and CCTV never freezes the incident; only Pause does.
            if (overlay != Overlay.Pause) Session.Tick(Time.deltaTime);
            if (Session.Phase == ShiftPhase.Failed && overlay != Overlay.Failure) ShowEnd(false);
            if (Session.Phase == ShiftPhase.Complete && overlay != Overlay.Complete) ShowEnd(true);
            HandleInput();
            if (!InputBlocked)
            {
                Vector2 pos = World.Player.transform.position;
                if (pos.x > -6.5f && pos.x < 13 && Session.Phase == ShiftPhase.Surveillance)
                {
                    Session.StartIncident();
                    Toast("컵이 회전하며 커피를 쏟아내기 시작했다!", 5);
                }
                if (pos.x < -7.5f && pos.y > 2.5f) Session.ReturnToSecurity();
                FindNearest();
            }
            else if (nearest != null) { nearest.Highlight(false); nearest = null; }
            AnimateWorld();
            RefreshHud();
        }

        private void HandleInput()
        {
            var keys = Keyboard.current;
            if (keys == null || pouring) return;
            if (keys.escapeKey.wasPressedThisFrame)
            {
                if (overlay == Overlay.None) Pause();
                else if (overlay == Overlay.Pause || overlay == Overlay.Reading || overlay == Overlay.Rules || overlay == Overlay.Cctv) CloseOverlay();
                return;
            }
            if (keys.tabKey.wasPressedThisFrame && (overlay == Overlay.None || overlay == Overlay.Rules))
            {
                if (overlay == Overlay.Rules) CloseOverlay(); else OpenRules();
                return;
            }
            if (overlay == Overlay.Cctv)
            {
                if (keys.aKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame) SelectCamera((cameraIndex + 3) % 4);
                if (keys.dKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame) SelectCamera((cameraIndex + 1) % 4);
                if (keys.spaceKey.wasPressedThisFrame) CloseOverlay();
                return;
            }
            if (overlay == Overlay.Pause || overlay == Overlay.Failure || overlay == Overlay.Complete)
            {
                MenuInput(); return;
            }
            if (overlay == Overlay.Reading || overlay == Overlay.Rules)
            {
                if (keys.spaceKey.wasPressedThisFrame) CloseOverlay();
                return;
            }
            if (keys.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (keys.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (keys.digit3Key.wasPressedThisFrame) SelectSlot(2);
            if (keys.digit4Key.wasPressedThisFrame) SelectSlot(3);
            if (keys.digit5Key.wasPressedThisFrame) SelectSlot(4);
            if (keys.spaceKey.wasPressedThisFrame && nearest != null) Interact(nearest);
        }

        private void FindNearest()
        {
            if (nearest != null) nearest.Highlight(false);
            nearest = null;
            float closest = tuning.interactionDistance;
            Vector2 player = World.Player.transform.position;
            foreach (var item in World.Interactables)
            {
                float distance = Vector2.Distance(player, item.transform.position);
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
            if (InputBlocked) return;
            switch (item.kind)
            {
                case InteractionKind.Camera: OpenCctv(); break;
                case InteractionKind.Note:
                    Read("사장이 남긴 메모", "오늘부터 야간 근무를 부탁해요.\n\n먼저 CCTV를 둘러보고, 이상한 게 보이면 직접 가서 확인해 주세요. 감시실 밖에서는 카메라를 볼 수 없어요.\n\nWASD / 방향키   이동 · CCTV 카메라 전환\nSpace   조사 / 상호작용\nTab   규칙서 열기 / 닫기\nShift   달리기     F   손전등\n1–5   소지품 선택 / 선택 해제\nEsc   뒤로 / 일시정지\n\n규칙서는 책상에 두었어요. 꼭 챙겨 읽어 주세요."); break;
                case InteractionKind.RuleBook: OpenRules(); break;
                case InteractionKind.Recipe:
                    Session.RecipeRead = true;
                    string order = string.Join("  →  ", Array.ConvertAll(tuning.recipe, GameTuning.IngredientName));
                    Read("접힌 카페 레시피", "CAFÉ MOCHA\n\n아메리카노 베이스에 아래 재료를 하나씩 넣는다.\n\n★ " + order + " ★\n\n사장 메모: 순서가 바뀌면 맛이 달라져요.\n\n재료는 카운터 뒤 제조 공간에 보관.\n엉뚱한 재료는 넣지 말 것."); break;
                case InteractionKind.Ingredient:
                    if (Session.Collect(item.ingredient)) Toast(item.title + "를 소지품에 넣었다. [" + ((int)item.ingredient + 1) + "]로 선택 / 해제.");
                    else Toast(Session.Phase == ShiftPhase.Incident ? "이미 가지고 있는 재료다." : "지금은 재료가 필요하지 않다.");
                    break;
                case InteractionKind.Cup:
                    if (Session.Phase == ShiftPhase.Resolved) { Toast("커피가 더는 나오지 않는다. 감시실로 돌아가자."); break; }
                    if (!Session.CupInspected)
                    {
                        Session.CupInspected = true;
                        Toast("쓴 커피 향. 우유도 시럽도 없는 아메리카노다. 컵 안에서 커피가 끝없이 솟는다.", 7);
                    }
                    else if (Session.SelectedSlot < 0 || !Session.Inventory[Session.SelectedSlot]) Toast("쓴 향의 아메리카노다. 주변의 종이와 규칙서를 살펴보자.", 5);
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
                if (Session.Phase == ShiftPhase.Resolved)
                {
                    floodAtResolution = Mathf.Clamp01(1 - Session.Remaining / tuning.incidentSeconds);
                    Toast("흐름이 멈췄다. 바닥의 커피가 컵 안으로 빨려 들어간다…", 5);
                }
                else if (correct) Toast("재료가 섞였다. 흐름은 아직 멈추지 않았다.", 4);
                else { sound.PlayOneShot(clatter); Toast("컵이 커피를 쏟아냈다! 시간이 줄었다. 투입 순서가 초기화됐다.", 5); }
            }
            pouring = false;
        }

        private void SelectSlot(int index)
        {
            if (InputBlocked) return;
            if (!Session.Inventory[index]) { Toast("아직 가지고 있지 않은 재료다.", 2); return; }
            Session.Select(index);
        }

        private Transform OpenOverlay(Overlay next, float width = 1060, float height = 800)
        {
            CloseOverlay(); overlay = next;
            var shade = CafeUi.Panel(canvas.transform, "Modal shade", 0, 0, 1920, 1080, new Color(0.015f, 0.03f, 0.04f, 0.88f));
            shade.raycastTarget = true; modal = shade.gameObject;
            return CafeUi.Panel(shade.transform, "Modal", (1920 - width) / 2, (1080 - height) / 2, width, height, new Color32(24, 39, 43, 255)).transform;
        }

        public void CloseOverlay()
        {
            if (modal != null) { modal.SetActive(false); Destroy(modal); }
            modal = null; overlay = Overlay.None;
            if (feedCamera != null) feedCamera.enabled = false;
            Time.timeScale = 1;
        }

        public void OpenRules()
        {
            if (overlay != Overlay.None || pouring) return;
            var body = OpenOverlay(Overlay.Rules);
            CafeUi.Label(body, "STAFF ONLY  /  01", 64, 48, 900, 35, 21, CafeUi.Amber);
            CafeUi.Label(body, "야간 근무 규칙서", 64, 110, 900, 65, 42);
            CafeUi.Panel(body, "Rule divider", 64, 210, 932, 1, CafeUi.Muted);
            CafeUi.Label(body, "01", 64, 260, 100, 50, 28, CafeUi.Amber);
            CafeUi.Label(body, "아메리카노는 너무 써서 하늘을 난다.", 150, 260, 800, 70, 30);
            CafeUi.Label(body, "02", 64, 380, 100, 50, 28, CafeUi.Amber);
            CafeUi.Label(body, "카페모카는 너무 끈적해서 목이 막힌다.", 150, 380, 800, 70, 30);
            CafeUi.Label(body, "관찰한 것을 기억할 것.\n레시피는 카페에 보관한다.", 64, 520, 900, 100, 23, CafeUi.Muted);
            CafeUi.Label(body, "읽는 동안에도 사건은 계속됩니다.", 64, 676, 680, 40, 21, CafeUi.Amber);
            CafeUi.Button(body, "덮기  [Tab / Esc / Space]", 580, 714, 416, 60, CloseOverlay);
        }

        private void Read(string title, string text)
        {
            var body = OpenOverlay(Overlay.Reading, 1060, 850);
            CafeUi.Label(body, "현장 기록", 64, 40, 900, 40, 21, CafeUi.Amber);
            CafeUi.Label(body, title, 64, 100, 932, 65, 38);
            CafeUi.Label(body, text, 64, 200, 932, 550, 25);
            CafeUi.Button(body, "닫기  [Space / Esc]", 650, 770, 346, 60, CloseOverlay);
        }

        private void OpenCctv()
        {
            var body = OpenOverlay(Overlay.Cctv, 1620, 940);
            feedTitle = CafeUi.Label(body, "", 44, 28, 1000, 50, 30, CafeUi.Green);
            CafeUi.Label(body, "LIVE  ●  MIC ON", 1280, 36, 300, 42, 22, CafeUi.Amber, TextAnchor.MiddleRight);
            var image = CafeUi.Rect(body, "Live camera feed", 44, 104, 1280, 720).gameObject.AddComponent<RawImage>();
            image.texture = feed; image.raycastTarget = false;
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                cameraButtons[i] = CafeUi.Button(body, "0" + (i + 1) + "  " + CafeWorld.CameraNames[i], 1350, 120 + i * 100, 230, 76, () => SelectCamera(index));
            }
            CafeUi.Label(body, "내장 마이크 연결됨\n영상과 현장 소리 수신 중\n\nWASD / 방향키\n카메라 전환", 1350, 570, 235, 200, 21, CafeUi.Muted);
            CafeUi.Label(body, "감시실을 떠나면 카메라를 볼 수 없습니다.", 44, 864, 960, 42, 22, CafeUi.Muted);
            CafeUi.Button(body, "나가기  [Space / Esc]", 1170, 852, 410, 62, CloseOverlay);
            feedCamera.enabled = true;
            SelectCamera(0);
        }

        private void SelectCamera(int index)
        {
            cameraIndex = index;
            Vector2 pos = CafeWorld.CameraPositions[index];
            feedCamera.transform.position = new Vector3(pos.x, pos.y, -25);
            feedCamera.cullingMask = CafeWorld.RoomMask(pos);
            feedCamera.orthographicSize = index == 0 ? 7 : 5;
            feedTitle.text = "CAM 0" + (index + 1) + "  /  " + CafeWorld.CameraNames[index];
            for (int i = 0; i < 4; i++) cameraButtons[i].GetComponent<Image>().color = i == index ? new Color32(72, 99, 85, 255) : new Color32(35, 54, 56, 255);
            if (index == 0) Session.CameraViewed = true;
        }

        private void Pause()
        {
            if (InputBlocked) return;
            var body = OpenOverlay(Overlay.Pause, 760, 670);
            Time.timeScale = 0;
            CafeUi.Label(body, "근무 일시정지", 60, 65, 640, 70, 40);
            CafeUi.Label(body, "잠시 숨을 고릅니다.", 60, 153, 640, 50, 23, CafeUi.Muted);
            menuButtons = new[] {
                CafeUi.Button(body, "계속하기", 60, 260, 640, 76, CloseOverlay),
                CafeUi.Button(body, "시작 화면으로", 60, 360, 640, 76, ReturnToLobby),
                CafeUi.Button(body, "게임 종료", 60, 460, 640, 76, Quit) };
            menuIndex = 0; MarkMenu();
            CafeUi.Label(body, "W/S 선택 · Space 실행 · Esc 계속", 60, 580, 640, 50, 21, CafeUi.Muted);
        }

        private void ShowEnd(bool success)
        {
            var body = OpenOverlay(success ? Overlay.Complete : Overlay.Failure, 960, 750);
            CafeUi.Label(body, success ? "SHIFT COMPLETE  /  01" : "INCIDENT LOST  /  01", 60, 55, 840, 45, 25, success ? CafeUi.Green : CafeUi.Red);
            CafeUi.Label(body, success ? "첫 번째 밤이 끝났다" : "커피가 가게를 삼켰다", 60, 142, 840, 80, 42);
            CafeUi.Label(body, success ? "감시실 문을 닫자, 카페는 다시 조용해졌다.\n\n남은 시간  " + Mathf.CeilToInt(Session.Remaining) + "초     /     잘못된 투입  " + Session.Mistakes + "회" : "차오르는 커피 속에서 숨을 쉴 수 없다.\n\n사건 시작 시점부터 다시 시도할 수 있습니다.\n읽었던 규칙서와 레시피 기록은 유지됩니다.", 60, 260, 840, 180, 26, CafeUi.Muted);
            menuButtons = success ? new[] {
                CafeUi.Button(body, "시작 화면으로", 60, 500, 840, 76, ReturnToLobby),
                CafeUi.Button(body, "게임 종료", 60, 600, 840, 76, Quit) } : new[] {
                CafeUi.Button(body, "사건부터 재도전", 60, 500, 840, 76, Retry),
                CafeUi.Button(body, "시작 화면으로", 60, 600, 840, 76, ReturnToLobby) };
            menuIndex = 0; MarkMenu();
        }

        public void Retry()
        {
            StopAllCoroutines(); pouring = false;
            World.Player.Reach(0);
            CloseOverlay(); Session.RetryIncident(); World.Player.Teleport(CafeWorld.IncidentSpawn);
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
            for (int i = 0; i < menuButtons.Length; i++) menuButtons[i].GetComponent<Image>().color = i == menuIndex ? new Color32(75, 99, 85, 255) : new Color32(35, 54, 56, 255);
        }

        private void AnimateWorld()
        {
            bool alive = Session.Phase == ShiftPhase.Surveillance || Session.Phase == ShiftPhase.Incident;
            World.Cup.position = new Vector3(3, 1 + (alive ? Mathf.Sin(pulseClock * 2.1f) * 0.25f : 0), 0);
            World.Cup.rotation = Quaternion.Euler(0, 0, alive ? pulseClock * (Session.SurgeRemaining > 0 ? 230 : 35) : 0);
            for (int i = 0; i < World.CoffeeDrops.Length; i++)
            {
                var drop = World.CoffeeDrops[i];
                drop.gameObject.SetActive(Session.Phase == ShiftPhase.Incident);
                float travel = Mathf.Repeat(pulseClock * (Session.SurgeRemaining > 0 ? 2 : 0.8f) + i * 0.17f, 1);
                float angle = i * 2.4f + pulseClock;
                drop.position = World.Cup.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * travel * 1.8f;
                drop.localScale = Vector3.one * (1 - travel * 0.6f);
            }
            float flood = Session.Phase == ShiftPhase.Resolved ? Session.Flood * floodAtResolution : Session.Phase == ShiftPhase.Complete ? 0 : Session.Flood;
            World.Flood.color = new Color(0.28f, 0.12f, 0.03f, flood * 0.72f);
            floodWarning.color = new Color(0.55f, 0.12f, 0.03f, Mathf.Max(0, flood - 0.65f) * (0.3f + 0.12f * Mathf.Sin(pulseClock * 4)));
            if (alive && pulseClock >= clatterAt && ((overlay == Overlay.Cctv && cameraIndex == 0) || Vector2.Distance(World.Player.transform.position, World.Cup.position) < 10))
            {
                sound.PlayOneShot(clatter, overlay == Overlay.Cctv ? 0.7f : 0.4f);
                clatterAt = pulseClock + (Session.SurgeRemaining > 0 ? 0.4f : 2.2f);
            }
        }

        private void RefreshHud()
        {
            var phase = Session.Phase;
            timer.gameObject.SetActive(phase == ShiftPhase.Incident);
            timerLine.gameObject.SetActive(phase == ShiftPhase.Incident);
            int seconds = Mathf.CeilToInt(Session.Remaining);
            timer.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            timer.color = seconds < 45 || Session.SurgeRemaining > 0 ? CafeUi.Red : CafeUi.Amber;
            timerLine.rectTransform.sizeDelta = new Vector2(440 * Session.Remaining / tuning.incidentSeconds, 3);
            objective.text = phase == ShiftPhase.Surveillance ? "CCTV를 확인하고\n카페를 둘러보세요." : phase == ShiftPhase.Incident ? "현장을 조사하고\n커피의 흐름을\n멈추세요." : phase == ShiftPhase.Resolved ? Session.CleanupRemaining > 0 ? "커피가 사라지고\n있습니다…" : "감시실로\n돌아가세요." : phase == ShiftPhase.Complete ? "오늘의 근무 완료" : "사건 해결 실패";
            status.text = phase == ShiftPhase.Surveillance ? "카메라 대기 중" : phase == ShiftPhase.Incident ? "범람 수위  " + Mathf.RoundToInt(Session.Flood * 100) + "%\n" + (Session.SurgeRemaining > 0 ? "커피 범람 가속 중!" : "커피가 차오르고 있습니다.") : phase == ShiftPhase.Resolved ? "현상 해소 · 자동 정리" : "근무 종료";
            progress.text = (Session.CameraViewed ? "●" : "○") + " CCTV 확인\n" + (Session.CupInspected ? "●" : "○") + " 컵 조사\n" + (Session.RecipeRead ? "●" : "○") + " 레시피 열람";
            Vector2 pos = World.Player.transform.position;
            mapCamera.cullingMask = CafeWorld.RoomMask(pos);
            location.text = pos.x < -7 ? pos.y > 2 ? "감시실" : "로비" : pos.x > 13 ? pos.y > 1 ? "창고" : "숙직실" : "카페 홀";
            for (int i = 0; i < 5; i++)
            {
                bool owned = Session.Inventory[i];
                inventoryLabels[i].text = (i + 1) + "  " + GameTuning.IngredientName((Ingredient)i) + (owned ? "" : "  —");
                inventoryLabels[i].color = owned ? CafeUi.Paper : CafeUi.Muted;
                inventorySlots[i].color = Session.SelectedSlot == i ? new Color32(78, 98, 77, 255) : new Color32(30, 44, 47, 255);
            }
            prompt.text = !InputBlocked && nearest != null ? "[Space]  " + nearest.title + (nearest.kind == InteractionKind.Cup && Session.SelectedSlot >= 0 ? "  ·  선택 재료 사용" : "") : "";
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
        private void ReturnToLobby() { CloseOverlay(); SceneManager.LoadScene("Lobby"); }
        private static void Quit()
        {
            Time.timeScale = 1;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void OnDestroy()
        {
            Time.timeScale = 1;
            if (feed != null) { feed.Release(); Destroy(feed); }
            if (clatter != null) Destroy(clatter);
        }
    }
}
