using System.Collections;
using UnityEngine;

namespace NightCafe
{
    public sealed partial class NightGame
    {
        private void UseFinalTask(CafeInteractable item)
        {
            switch (item.storyAction)
            {
                case StoryAction.DarkCameraDigit:
                    Fifth.ReadDigit(item.taskIndex);
                    Toast($"카메라 아래에 숫자 {tuning.cameraPassword[DigitRank(item.taskIndex)]}가 적혀 있다. 근무 순위와 대조하자."); break;
                case StoryAction.Computer: OpenComputer(); break;
                case StoryAction.FinalMenu:
                    Read("메뉴판", Fifth.RecordCorrected ? "두 사람의 퇴실이 확인되지 않았습니다." : "근무 종료가 확인되지 않았습니다."); break;
                case StoryAction.Roster:
                    Fifth.ReadRoster(); Read("사고 당일 직원 근무표", RosterText()); break;
                case StoryAction.Accident:
                    Fifth.ReadAccident(); OpenAccident(); break;
                case StoryAction.Identity:
                    if (!Fifth.IdentityInspected)
                    {
                        Fifth.InspectIdentity();
                        Read("자신과 같은 얼굴", $"왼손은 붕대로 감겨 있다. 근무복에는 오래된 번호가 붙어 있다.\n\n직원번호  {tuning.victimNumber}\n\n쓰레기통에서 찾은 근무표와 대조해야 한다.");
                    }
                    else if (Fifth.SelectedBadge < 0) Toast("창고에서 번호와 일치하는 직원 이름 명찰을 찾아오자.");
                    else if (HeldItem != InventoryItem.Badge) Toast("Tab으로 인벤토리를 열고 명찰을 우클릭해 손에 들자.");
                    else
                    {
                        bool correct = Fifth.GiveBadge(); RefreshCracks();
                        if (correct) StartCoroutine(Movie($"명찰에 적힌 이름은 {tuning.victimName}.\n형체가 사라지자 홀 연결 복도에 제어함이 드러났다. 내 얼굴은 아직 원래대로 돌아오지 않았다.", tuning.cinematicSeconds));
                        else Toast("직원번호가 일치하지 않는다. 명찰과 근무표를 다시 확인하자.");
                    }
                    break;
                case StoryAction.Badge:
                    if (Fifth.SelectedBadge != item.taskIndex && HeldItem == InventoryItem.Badge)
                        SetHeldItem(InventoryItem.None);
                    Fifth.TakeBadge(item.taskIndex); Toast("명찰을 챙겼다.  " + BadgeName(item.taskIndex)); break;
                case StoryAction.WireCode:
                    Read("녹은 전선과 잠금장치", $"전선 피복 사이로 번호 {tuning.controlPassword}가 보인다.\n\n잠긴 출구의 자물쇠와 같은 번호다."); break;
                case StoryAction.Control: OpenControl(); break;
                case StoryAction.ExhaustBox:
                    Fifth.ClearBox(); Toast(Fifth.ExhaustClear ? "맨 위의 박스를 내려 외부 배기구를 확보했다." : "숙직실 컴퓨터의 복도 정비도를 먼저 확인하자."); break;
                case StoryAction.VictimCup:
                    Fifth.TakeCup(); Toast(Fifth.HasCup ? "왼손 자국과 오래된 이름표가 남은 컵을 챙겼다." : "컵의 주인이 누구인지 먼저 알아야 한다."); break;
                case StoryAction.CupTable:
                    if (Fifth.Summon())
                    {
                        story.spirit.position = item.transform.position; SetLayer(story.spirit, 12); story.spirit.gameObject.SetActive(true);
                        StartCoroutine(Movie($"테이블에 컵을 놓고 {tuning.victimName}의 이름을 불렀다.\n왼손을 감싼 형체가 일어났다. 손전등으로 비추며 홀을 지나 외부 출구로 함께 가야 한다.", tuning.cinematicSeconds));
                    }
                    else Toast("사고 기록과 두 복도의 원인을 해결하고, 이 사람의 컵을 가져와야 한다.");
                    break;
                case StoryAction.Exit: OpenExitDecision(); break;
                case StoryAction.Departure:
                    CafeWorld.ViewRoom(feedCamera,16);
                    Vector3 recorded=feedCamera.WorldToViewportPoint(story.spirit.position);
                    bool covered=CafeWorld.RoomLayer(story.spirit.position)==16 && recorded.z>0 && recorded.x>0 && recorded.x<1 && recorded.y>0 && recorded.y<1;
                    if (Fifth.ConfirmDeparture(covered)) Toast("두 사람의 퇴실이 기록되었다.");
                    else Toast("출구를 열고, 손전등으로 영혼을 비춰 문밖을 먼저 통과하게 해야 한다.");
                    break;
            }
        }
        private void UpdateFinale()
        {
            Vector2 player = World.Player.PlanPosition; int room = CafeWorld.RoomLayer(player);
            if (room == 8 && Fifth.Freeze())
            {
                StartCoroutine(FreezeMovie());
                sound.PlayOneShot(clatter, 0.25f);
            }
            if (Fifth.CanLeaveSecurity && !securityReleaseShown) { securityReleaseShown = true; Toast("감시실을 나갈 수 있습니다."); }
            if (room == 14 && Fifth.Identified && !tentacleMemoryPlayed)
            {
                tentacleMemoryPlayed = true;
                if (Fifth.TentacleMemory()) StartCoroutine(MemoryMovie(false));
            }
            if (room == 15 && Fifth.TentaclesReleased && !pursuitMemoryPlayed)
            {
                pursuitMemoryPlayed = true;
                if (Fifth.PursuitMemory()) StartCoroutine(MemoryMovie(true));
            }
            if (Fifth.Stage == FinaleStage.Escort)
            {
                bool sameRoom = CafeWorld.RoomLayer(story.spirit.position) == room;
                Vector2 delta = (Vector2)story.spirit.position - player;
                bool lit = World.Player.FlashlightOn && (delta.magnitude < 0.8f || Vector2.Dot(World.Player.Facing, delta.normalized) > 0.3f);
                if (sameRoom && lit)
                {
                    Vector2 target = Fifth.DoorOpen && room == 16 ? new Vector2(73.2f, 0) : player - World.Player.Facing * 0.65f;
                    story.spirit.position = Vector2.MoveTowards(story.spirit.position, target, Time.deltaTime * 2.8f);
                }
                if (room == 16 && story.spirit.position.x > 72.5f) Fifth.PassSpirit();
            }
            if (Fifth.Frozen && room == 16 && player.x > 72.6f) Fifth.CrossThreshold();
            previousRoom = room;
        }
        private IEnumerator MemoryMovie(bool pursuit)
        {
            story.memoryActor.gameObject.SetActive(true);
            Vector2 start = World.Player.PlanPosition + Vector2.right * 1.3f;
            story.memoryActor.position = start; SetLayer(story.memoryActor, CafeWorld.RoomLayer(start));
            string text = pursuit
                ? "원본 영상  —  커피 머신의 전원은 꺼져 있다.\n배연 방향을 따라가던 A는 연기가 몰린 숙직실로 돌아선다. 잠긴 문을 두드리며 카메라에 손을 흔든다.\n숙직실 컴퓨터에 복도 정비도가 나타났다."
                : "원본 영상  —  마감을 끝낸 A가 문을 밀지만 열리지 않는다.\n손상된 냉장 설비 전원부에서 불꽃이 튀고, 녹은 전선이 다리를 감는다.\n인벤토리에 미처 처리되지 않은 수리 요청서가 들어왔다.";
            yield return Movie(text, tuning.cinematicSeconds * 2, amount => story.memoryActor.position = start + Vector2.right * Mathf.Sin(amount * Mathf.PI) * 2);
            story.memoryActor.gameObject.SetActive(false);
        }
        private void SyncFinalObjects()
        {
            if (Fifth == null) return;
            foreach (var item in story.finalTasks)
            {
                bool active = true;
                switch (item.storyAction)
                {
                    case StoryAction.DarkCameraDigit: active = Fifth.Stage == FinaleStage.Blackout; break;
                    case StoryAction.Roster:
                    case StoryAction.Accident: active = Fifth.Frozen; break;
                    case StoryAction.Identity: active = Fifth.Frozen && !Fifth.Identified; break;
                    case StoryAction.Badge: active = Fifth.Frozen && !Fifth.Identified; break;
                    case StoryAction.Control:
                    case StoryAction.WireCode: active = Fifth.Identified && !Fifth.TentaclesReleased; break;
                    case StoryAction.ExhaustBox: active = Fifth.PursuitVideoSeen && !Fifth.ExhaustClear; break;
                    case StoryAction.VictimCup: active = Fifth.Identified && !Fifth.HasCup; break;
                    case StoryAction.CupTable: active = Fifth.RecordCorrected && Fifth.Stage == FinaleStage.Frozen; break;
                    case StoryAction.Departure: active = Fifth.Stage == FinaleStage.Escort; break;
                }
                item.gameObject.SetActive(active);
            }
            story.smoke.SetActive(Fifth.Frozen && !Fifth.SmokeCleared);
            story.spirit.gameObject.SetActive(Fifth.Stage == FinaleStage.Escort || Fifth.Stage == FinaleStage.Happy);
            story.wallClock.text = Fifth.Frozen ? FinaleClock() : shiftClockStart.AddSeconds(pulseClock).ToString("HH:mm:ss");
            if (Fifth.Frozen) feedTitle.text = CameraHeading() + "  /  " + FinaleClock();
            RefreshCracks();
        }
        private string FinaleClock()
        {
            if(Fifth.Stage==FinaleStage.Happy && System.TimeSpan.TryParse(tuning.accidentTime,out var time))
                return (time+System.TimeSpan.FromSeconds(1)).ToString(@"hh\:mm\:ss");
            return tuning.accidentTime;
        }
        private IEnumerator FreezeMovie()
        {
            yield return Movie("종료음이 끊겼다.\n의자가 모니터를 향해 돌아간다. 벽시계와 CCTV가 "+tuning.accidentTime+"에 멈췄다.",tuning.cinematicSeconds,
                amount=>story.securityChair.rotation=Quaternion.Euler(0,0,180*(1-amount)));
            story.memoryActor.position=new Vector2(-14,-7);SetLayer(story.memoryActor,9);story.memoryActor.gameObject.SetActive(true);
            CafeWorld.ViewRoom(feedCamera,9);
            yield return Movie("로비 CCTV  —  밖으로 나갔던 내가 같은 자리에 다시 나타났다.\n영상의 인물은 움직이지만 시각은 바뀌지 않는다.",tuning.cinematicSeconds*2,
                amount=>story.memoryActor.position=new Vector2(-14+Mathf.Repeat(amount*2,1)*4,-7),true);
            story.memoryActor.gameObject.SetActive(false);
        }
        private void OpenComputer()
        {
            if (Fifth.Stage == FinaleStage.Blackout)
            {
                Numeric("카메라 복구 암호", "꺼진 네 곳에서 찾은 숫자를 근무 서열 순으로 입력한다.\n손님 → 사장님 → 카운터 알바생 → 창고 알바생", value =>
                {
                    bool success = Fifth.RestoreCameras(value);
                    Toast(success ? "카메라가 돌아왔다. 감시실로 복귀하자." : "암호가 맞지 않거나 네 곳의 숫자를 아직 찾지 못했다. 이 입력은 균열에 누적되지 않는다.");
                }); return;
            }
            if (!Fifth.PursuitVideoSeen) { Read("숙직실 컴퓨터", "마지막 영상은 냉장 설비와 복도 카메라로 이어진다. 아직 복원되지 않은 구간이 있다."); return; }
            Choices("복도 정비도", "외부 배기 방향 → / 현재 조절판 ←\n외부 배기구 위에는 사장의 임시 보관 박스가 쌓여 있다.",
                new[] { "정비도 읽기", "조절판을 외부 방향으로 돌려 작동", "원본 영상 다시 보기" }, selection =>
            {
                if (selection == 0)
                {
                    Fifth.ReadDiagram(); Read("복도 정비도", "외부 배기구의 맨 위 박스를 내린 뒤 숙직실 컴퓨터에서 외부 배기로 돌린다.\n기류 리본이 움직이지 않으면 막힌 물품이 남아 있다.");
                }
                if (selection == 1)
                {
                    bool success = Fifth.Ventilate();
                    if (success) StartCoroutine(Movie("기류 리본이 외부를 향해 움직인다.\n연기가 빠지고 추격자의 윤곽이 흩어졌다. 출구 쪽으로 뻗은 손만 잠깐 남았다.", tuning.cinematicSeconds));
                    else Toast("기류 리본이 움직이지 않고 연기가 되돌아온다. 정비도를 읽고 막힌 물품부터 치워야 한다.");
                }
                if (selection == 2) StartCoroutine(MemoryMovie(true));
            });
        }
        private void OpenControl()
        {
            if (!Fifth.ControlUnlocked)
            {
                Numeric("냉장 설비 제어함", "촉수 표면의 전선 피복과 잠금장치 번호를 대조한다.", value =>
                {
                    bool correct = Fifth.UnlockControl(value); RefreshCracks(); Toast(correct ? "제어함이 열렸다. 안쪽 도면을 확인할 수 있다." : "잠금장치가 열리지 않는다.");
                }); return;
            }
            Choices("냉장 설비 제어함", "도면  —  구동 정지 확인 후 비상 해제\n전원 " + (Fifth.MotorOff ? "꺼짐" : "켜짐") + " / 정지 표시 " + (Fifth.StopConfirmed ? "확인됨" : "미확인"),
                new[] { "냉장 설비 구동 전원 끄기", "구동 정지 표시 확인", "수동 비상 해제 열기", "수리 요청서 다시 읽기" }, choice =>
            {
                if (choice == 0) { Fifth.SwitchOff(); Toast("냉장 설비의 전원을 껐다. 정지 표시를 확인하자."); }
                if (choice == 1) Toast(Fifth.ConfirmStop() ? "구동 정지 표시가 켜졌다." : "아직 설비가 작동 중이다.");
                if (choice == 2)
                {
                    bool success = Fifth.ReleaseLock();
                    Toast(success ? "녹은 전선이 풀리고 잠금 표시가 바뀌었다. 출구 쪽에는 연기가 고여 있다." : "구동을 끄고 정지 표시를 확인한 뒤 해제해야 한다. 스위치 확인은 오답으로 누적되지 않는다.", 6);
                }
                if (choice == 3) Read("수리 요청서", RepairText());
            });
        }
        private void OpenAccident()
        {
            if (!Fifth.TentaclesReleased || !Fifth.SmokeCleared)
            {
                Read("사고 기록서", "원인  직원의 커피 머신 미정지 추정\n사고 당시 잔류 인원  0명\n퇴실 확인  완료\n\n근거 칸  직원 근무표 / 냉장 설비 수리 요청서 / 머신 정지 원본 영상 / 마지막 도주 영상\n\n각 근거와 두 복도의 원인을 확인하면 세 항목을 정정할 수 있다."); return;
            }
            Choices("사고 기록 정정 · 원인", "근거  수리 요청서, 커피 머신 정지 영상, 창고의 발화 장면",
                new[] { "직원의 커피 머신 미정지", "냉장 설비 전원부 발화", "손님의 담뱃불", "전기난로 과열", "근거 문서 다시 확인" }, choice =>
            {
                if (choice == 4) { Read("원본 영상과 수리 요청", RepairText() + "\n\n머신 전원은 사고 전에 꺼졌다. 손상된 냉장 설비의 자동 운전 직후 불꽃이 튀었다."); return; }
                recordCause = choice;
                Numeric("사고 기록 정정 · 잔류 인원", "사고 당시 건물 안에 남은 사람 수를 숫자로 입력한다.\n현재 주인공이 아닌 사고 당일 직원 근무표를 확인한다.", value =>
                {
                    recordPeople = value;
                    Choices("사고 기록 정정 · 퇴실 확인", "영상에서 A가 건물을 빠져나간 장면이 있는지 확인한다.",
                        new[] { "미완료", "완료", "근거 영상 다시 확인" }, selected =>
                    {
                        if (selected == 2) { StartCoroutine(MemoryMovie(true)); return; }
                        bool success = Fifth.CorrectRecord(recordCause, recordPeople, selected == 1); RefreshCracks();
                        if (success) Read("A가 남긴 쪽지", "수리해 달라는 말을 몇 번이나 했어요.\n사장님은 돈이 든다며 다음으로 미뤘고, 제가 안에 있는 것도 보지 않고 문을 잠갔어요.\n마지막까지 보러 온 사람이 없었어요. 이번에는 혼자 두고 가지 말아 주세요.");
                        else Toast("근거와 정정 내용이 다르다. 현재 기록 정정만 처음부터 다시 확인해야 한다.");
                    });
                });
            });
        }
        private void OpenExitDecision()
        {
            if(!Fifth.Frozen) { Toast("카메라 복구와 근무 종료 확인이 먼저다."); return; }
            if (Fifth.Stage == FinaleStage.Escort)
            {
                Fifth.OpenExit(); Toast("출입문을 열었다. 안쪽에 머물러 손전등으로 영혼을 먼저 비춰 보내자.", 6); return;
            }
            Choices("카페 출입문", "규칙서에는 당장 나가라고 쓰여 있다.\n문턱 너머로 나갈지, 남은 사람을 확인할지 결정한다.",
                new[] { "밖으로 나간다", "물러나서 계속 조사한다" }, choice => { if (choice == 0) Fifth.CrossThreshold(); });
        }
        private void OpenAdvancedNotes()
        {
            if (Day == 4)
            {
                if (Fourth.Stage == HuntStage.FirstChase) { Toast("지금은 읽을 틈이 없다. 벽면 소화기를 찾아야 한다."); return; }
                Fourth.ReadNotes();
                Read("전임자의 노트 · 네 번째 밤", "천장에서 물이 떨어진다. 소리가 멈추면 그곳에서 떨어져.\n소화기 분말을 맞히니 잠시 멈췄다. 완전히 쓰러진 건 아니다.\n그놈은 다시 천장으로 올라간다. 길을 막을 방법만 있다면…\n창고에 그물과 고정할 만한 쇠붙이가 있다. 해봐야 한다.\n<color=#B0A394><i>또 소리가 난다. 이번에는 가까…</i></color>\n\nTab 인벤토리");
            }
            else if (!Fifth.Frozen) Read("전임자의 노트 · 다섯 번째 밤", "카메라 복구 서열\n\n손님 1순위 · 사장님 2순위\n카운터 알바생 3순위 · 창고 알바생 4순위\n\n각 카메라 아래의 숫자를 그 순서로 숙직실 컴퓨터에 입력한다.\n\nTab 인벤토리");
            else Read("같은 노트의 마지막 페이지", "<b>근무 종료가 확인되지 않으면 당장 카페에서 나가십시오. 뒤를 돌아보거나 남은 사람을 확인하지 마십시오.</b>\n\n<color=#C0A597><i>5일째에는 노트에 적힌 방법을 그대로 따라 하지 말 것.</i></color>\n\n눌러 쓴 글씨 옆으로 잉크가 번져 있다.\n\nTab 인벤토리");
        }
        private void ReadEvidence(InventoryItem item)
        {
            switch (item)
            {
                case InventoryItem.Roster: Read("직원 근무표", RosterText()); break;
                case InventoryItem.Accident: OpenAccident(); break;
                case InventoryItem.RepairRequest: Read("수리 요청서", RepairText()); break;
                case InventoryItem.Diagram: Read("복도 정비도", "외부 배기구를 막은 박스를 내리고 조절판을 외부 쪽으로 돌린다."); break;
                case InventoryItem.Note: Read("A의 쪽지", "사장님이 수리를 미루고 제가 안에 있는 채 문을 잠갔어요. 이번에는 혼자 두고 가지 말아 주세요."); break;
            }
        }
        private string RosterText() => $"사고 당일 마감 근무\n\n{tuning.victimNumber}  {tuning.victimName}  —  마지막까지 남은 근무자\nB-021  이도현  —  주간 근무 후 퇴실\nC-032  박지우  —  휴무\n\n사고 당시 인원을 확인해야 한다. 지금의 근무자는 이 표에 없다.";
        private string RepairText() => "냉장 설비 자동 운전 때 타는 냄새가 납니다.\n전원부 피복이 녹고 차단기가 반복해서 내려갑니다. 수리 후 사용해 주세요.\n\n사장 메모  —  비용 때문에 다음 주로 미룸. 영업은 계속할 것.";
        private string BadgeName(int index) => index == 0 ? tuning.victimName : index == 1 ? "이도현" : "박지우";
        private static int DigitRank(int camera) => new[] { 0, 2, 3, 1 }[camera];
    }
}
