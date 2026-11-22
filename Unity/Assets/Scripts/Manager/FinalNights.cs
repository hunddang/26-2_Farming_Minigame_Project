using System;

namespace NightCafe
{
    public enum StoryAction
    {
        None, Leak, Extinguisher, Spray, Net, Anchor, Trap, Monster,
        DarkCameraDigit, Computer, Roster, Accident, Identity, Badge, Control,
        WireCode, RepairRequest, Diagram, ExhaustBox, Exit, Departure, CupTable, VictimCup, FinalMenu
    }

    public enum HuntStage { Leak, FirstChase, ReadNotes, Equipment, SecondChase, InstallTrap, Search, ThirdChase, Captured, Failed, Complete }

    // Event stages own the rules. World positions and animations are supplied by NightGame.
    public sealed class FourthShift
    {
        public HuntStage Stage { get; private set; } = HuntStage.Leak;
        public float Remaining { get; private set; }
        public bool LeakObserved { get; private set; }
        public bool HasExtinguisher { get; private set; }
        public int Charges { get; private set; } = 2;
        public bool NotesRead { get; private set; }
        public bool HasNet { get; private set; }
        public bool HasAnchor { get; private set; }
        public int TrapRoom { get; private set; } = -1;
        public int SearchRoom { get; private set; } = -1;
        public int TriggerRoom { get; private set; } = -1;
        public bool CorridorScenePlayed { get; private set; }
        public string Failure { get; private set; }
        public bool Chasing => Stage == HuntStage.FirstChase || Stage == HuntStage.SecondChase || Stage == HuntStage.ThirdChase;
        public ShiftPhase Phase => Stage == HuntStage.Complete ? ShiftPhase.Complete : Stage == HuntStage.Failed ? ShiftPhase.Failed : Stage == HuntStage.Captured ? ShiftPhase.Resolved : ShiftPhase.Incident;

        public FourthShift(float seconds) { Remaining = seconds; }
        public void Tick(float seconds)
        {
            if (Phase != ShiftPhase.Incident) return;
            Remaining = Math.Max(0, Remaining - Math.Max(0, seconds));
            if (Remaining == 0) Fail("천장으로 끌려 올라갔다. 근무 시간이 끝났다.");
        }
        public void Observe(int room)
        {
            if (Stage == HuntStage.Leak && room == 9) LeakObserved = true;
            if (Stage == HuntStage.Search && SearchRoom < 0) SearchRoom = room;
        }
        public bool StartFirst()
        {
            if (Stage != HuntStage.Leak || !LeakObserved) return false;
            Stage = HuntStage.FirstChase; CorridorScenePlayed = false; return true;
        }
        public void CollectExtinguisher() { if (Stage == HuntStage.FirstChase) HasExtinguisher = true; }
        public bool Spray(bool inRangeAndFacing)
        {
            if (!Chasing || !HasExtinguisher || Charges == 0 || !inRangeAndFacing) return false;
            if (Stage == HuntStage.ThirdChase) return false;
            Charges--;
            Stage = Stage == HuntStage.FirstChase ? HuntStage.ReadNotes : HuntStage.InstallTrap;
            return true;
        }
        public void ReadNotes()
        {
            if (Stage != HuntStage.ReadNotes && Stage != HuntStage.Equipment) return;
            NotesRead = true; Stage = HuntStage.Equipment;
        }
        public void Collect(bool net)
        {
            if (Stage != HuntStage.Equipment || !NotesRead) return;
            if (net) HasNet = true; else HasAnchor = true;
        }
        public bool StartSecond(int room)
        {
            if (Stage != HuntStage.Equipment || !HasNet || !HasAnchor || room == 11 || !IsTrapRoom(room)) return false;
            Stage = HuntStage.SecondChase; TriggerRoom = room; CorridorScenePlayed = false; return true;
        }
        public void EscapeSecond() { if (Stage == HuntStage.SecondChase) Stage = HuntStage.InstallTrap; }
        public bool Install(int room)
        {
            if (Stage != HuntStage.InstallTrap || !HasNet || !HasAnchor || !IsTrapRoom(room)) return false;
            TrapRoom = room; SearchRoom = -1; Stage = HuntStage.Search; return true;
        }
        public bool Recover()
        {
            if (Stage != HuntStage.Search) return false;
            TrapRoom = SearchRoom = -1; Stage = HuntStage.InstallTrap; return true;
        }
        public bool StartThird(int room)
        {
            if (Stage != HuntStage.Search || SearchRoom != room || TrapRoom < 0) return false;
            Stage = HuntStage.ThirdChase; CorridorScenePlayed = false; return true;
        }
        public void LoseMonster() { if (Stage == HuntStage.ThirdChase) { Stage = HuntStage.Search; SearchRoom = -1; } }
        public void CorridorScene() { CorridorScenePlayed = true; }
        public bool Capture(int monsterRoom, float distance)
        {
            if (Stage != HuntStage.ThirdChase || monsterRoom != TrapRoom || distance > 1.1f) return false;
            Stage = HuntStage.Captured; return true;
        }
        public void Return() { if (Stage == HuntStage.Captured) Stage = HuntStage.Complete; }
        public void Fail(string reason) { if (Phase != ShiftPhase.Incident) return; Failure = reason; Stage = HuntStage.Failed; }
        public static bool IsTrapRoom(int room) => room == 9 || room == 10 || room == 11 || room == 12 || room == 13;
    }

    public enum FinaleStage { Blackout, Return, Frozen, Escort, Happy, Captured }

    public sealed class FifthShift
    {
        private readonly GameTuning tuning;
        public FinaleStage Stage { get; private set; }
        public bool[] CameraDigits { get; } = new bool[4];
        public bool RosterRead { get; private set; }
        public bool AccidentRead { get; private set; }
        public bool IdentitySeenOnCamera { get; private set; }
        public bool IdentityInspected { get; private set; }
        public int SelectedBadge { get; private set; } = -1;
        public bool Identified { get; private set; }
        public bool RepairRequestRead { get; private set; }
        public bool MachineVideoSeen { get; private set; }
        public bool ControlUnlocked { get; private set; }
        public bool MotorOff { get; private set; }
        public bool StopConfirmed { get; private set; }
        public bool TentaclesReleased { get; private set; }
        public bool PursuitVideoSeen { get; private set; }
        public bool DiagramRead { get; private set; }
        public bool ExhaustClear { get; private set; }
        public bool SmokeCleared { get; private set; }
        public bool RecordCorrected { get; private set; }
        public bool HasNote => RecordCorrected;
        public bool HasCup { get; private set; }
        public bool DoorOpen { get; private set; }
        public bool SpiritPassed { get; private set; }
        public int Cracks { get; private set; }
        public float Remaining { get; private set; }
        public string Failure { get; private set; }
        public bool Frozen { get; private set; }
        public bool CanLeaveSecurity => RosterRead && AccidentRead && IdentitySeenOnCamera;
        public bool CanSummon => RecordCorrected && TentaclesReleased && SmokeCleared && HasCup;
        public ShiftPhase Phase => Stage == FinaleStage.Captured ? ShiftPhase.Failed : Stage == FinaleStage.Happy ? ShiftPhase.Complete : ShiftPhase.Incident;

        public FifthShift(GameTuning config) { tuning = config; Remaining = config.fifthNightSeconds; }
        public void Tick(float seconds)
        {
            if (Stage != FinaleStage.Blackout && Stage != FinaleStage.Return) return;
            Remaining = Math.Max(0, Remaining - Math.Max(0, seconds));
            if (Remaining == 0) Capture("복구하지 못한 화면 속에 갇혔다.");
        }
        public void ReadDigit(int index) { if (index >= 0 && index < 4 && Stage == FinaleStage.Blackout) CameraDigits[index] = true; }
        public bool RestoreCameras(string value)
        {
            if (Stage != FinaleStage.Blackout || Array.Exists(CameraDigits, found => !found) || value != tuning.cameraPassword) return false;
            Stage = FinaleStage.Return; return true;
        }
        public bool Freeze()
        {
            if (Stage != FinaleStage.Return) return false;
            Stage = FinaleStage.Frozen; Frozen = true; return true;
        }
        public void ReadRoster() { if (Frozen && Stage != FinaleStage.Captured) RosterRead = true; }
        public void ReadAccident() { if (Frozen && Stage != FinaleStage.Captured) AccidentRead = true; }
        public void ObserveIdentity() { if (Frozen && Stage != FinaleStage.Captured) IdentitySeenOnCamera = true; }
        public void InspectIdentity() { if (Frozen && !Identified) IdentityInspected = true; }
        public void TakeBadge(int index) { if (Frozen) SelectedBadge = index; }
        public bool GiveBadge()
        {
            if (!CanLeaveSecurity || !IdentityInspected || SelectedBadge < 0 || Identified) return false;
            if (SelectedBadge != 0) { Wrong("직원번호와 이름이 일치하지 않는다."); return false; }
            Identified = true; return true;
        }
        public bool TentacleMemory()
        {
            if (!Identified || RepairRequestRead) return false;
            RepairRequestRead = MachineVideoSeen = true; return true;
        }
        public bool UnlockControl(string value)
        {
            if (!Identified || !RepairRequestRead) return false;
            if (value != tuning.controlPassword) { Wrong("제어함의 번호가 맞지 않는다."); return false; }
            ControlUnlocked = true; return true;
        }
        public void SwitchOff() { if (ControlUnlocked) { MotorOff = true; StopConfirmed = false; } }
        public bool ConfirmStop() { if (!ControlUnlocked || !MotorOff) return false; StopConfirmed = true; return true; }
        public bool ReleaseLock()
        {
            if (!MotorOff || !StopConfirmed) return false;
            TentaclesReleased = true; return true;
        }
        public bool PursuitMemory()
        {
            if (!TentaclesReleased || PursuitVideoSeen) return false;
            PursuitVideoSeen = true; return true;
        }
        public void ReadDiagram() { if (PursuitVideoSeen) DiagramRead = true; }
        public void ClearBox() { if (Identified && DiagramRead) ExhaustClear = true; }
        public bool Ventilate()
        {
            if (!DiagramRead || !ExhaustClear) return false;
            SmokeCleared = true; return true;
        }
        public bool CorrectRecord(int cause, string people, bool departureComplete)
        {
            if (!RosterRead || !AccidentRead || !RepairRequestRead || !MachineVideoSeen || !PursuitVideoSeen || !TentaclesReleased || !SmokeCleared) return false;
            if (cause != 1 || people != "1" || departureComplete) { Wrong("사고 당시의 영상·근무표·수리 요청서와 기록이 다르다."); return false; }
            RecordCorrected = true; return true;
        }
        public void TakeCup() { if (Identified) HasCup = true; }
        public bool Summon()
        {
            if (!CanSummon || Stage != FinaleStage.Frozen) return false;
            Stage = FinaleStage.Escort; return true;
        }
        public void OpenExit() { if (Stage == FinaleStage.Escort) DoorOpen = true; }
        public void PassSpirit() { if (Stage == FinaleStage.Escort && DoorOpen) SpiritPassed = true; }
        public bool ConfirmDeparture(bool cameraCoverage)
        {
            if (Stage != FinaleStage.Escort || !DoorOpen || !SpiritPassed || !cameraCoverage) return false;
            Stage = FinaleStage.Happy; return true;
        }
        public void CrossThreshold()
        {
            if (Stage != FinaleStage.Happy) Capture("남은 사람의 퇴실 확인을 마치지 않은 채 문턱을 넘었다.");
        }
        public void Wrong(string message)
        {
            if (!Frozen || Stage == FinaleStage.Captured || Stage == FinaleStage.Happy) return;
            Cracks++;
            if (Cracks >= 3) Capture(message + " 세 번째 균열 너머에서 자신과 눈이 마주쳤다.");
        }
        public void Capture(string message) { Failure = message; Stage = FinaleStage.Captured; }
        public void Retry()
        {
            Cracks = 0; Failure = null;
            if (Frozen) Stage = FinaleStage.Frozen;
            else { Stage = FinaleStage.Blackout; Remaining = tuning.fifthNightSeconds; Array.Clear(CameraDigits, 0, 4); }
            DoorOpen = SpiritPassed = false;
            if (!TentaclesReleased) { MotorOff = StopConfirmed = ControlUnlocked = false; }
        }
    }
}
