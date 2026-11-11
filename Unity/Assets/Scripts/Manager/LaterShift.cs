using System;

namespace NightCafe
{
    public enum NightAction
    {
        None, Menu, EmptyCup, Sink, Warmer, Cloth, Chair, Board, Chalk, Eraser,
        TentacleExit, ChaseEntrance, ChaseExit, ServiceDoor
    }

    public enum WaterState { None, Empty, Cold, Hot }
    public enum NightFailure { Menu, Chairs, Chalk, Passage }

    // Rules shared by the controller and the build validation.
    public sealed class LaterShift
    {
        public int Day { get; }
        public float Duration { get; }
        public float Remaining { get; private set; }
        public ShiftPhase Phase { get; private set; } = ShiftPhase.Surveillance;
        public string Failure { get; private set; }
        public NightFailure FailureKind { get; private set; }
        public bool Viewed { get; set; }
        public WaterState Water { get; private set; }
        public bool HasCloth { get; private set; }
        public bool WetCloth { get; private set; }
        public bool MenuInspected { get; private set; }
        public int WipesLeft { get; private set; } = 2;
        public bool[] Stacked { get; }
        public int ChairsLeft { get; private set; }
        public bool Warehouse => Day == 3 && ChairsLeft == 0;
        public bool HasEraser { get; private set; }
        public bool BoardErased { get; private set; }
        public bool RestrictTurning => Day == 2 && Phase == ShiftPhase.Incident;
        public int RemainingIncidents => Phase == ShiftPhase.Resolved || Phase == ShiftPhase.Complete
            ? 0 : Day == 3 && !Warehouse ? 2 : 1;

        public LaterShift(int day, float duration, int chairs)
        {
            if (day != 2 && day != 3) throw new ArgumentOutOfRangeException(nameof(day));
            if (day == 3 && chairs < 1) throw new ArgumentOutOfRangeException(nameof(chairs));
            Day = day;
            Duration = Remaining = duration;
            Stacked = new bool[chairs];
            ChairsLeft = chairs;
        }

        public bool EnterSite()
        {
            if (Phase != ShiftPhase.Surveillance) return false;
            Phase = ShiftPhase.Incident;
            return true;
        }

        public void Tick(float seconds)
        {
            if (Phase != ShiftPhase.Surveillance && Phase != ShiftPhase.Incident) return;
            Remaining = Math.Max(0, Remaining - Math.Max(0, seconds));
            if (Remaining == 0)
                Fail(Day == 2 ? "등 뒤에서 손톱 긁는 소리가 들린다." :
                    Warehouse ? "분필이 움직임을 묶고 칠판이 쓰러졌다." : "의자들이 한꺼번에 덮쳐 왔다.",
                    Day == 2 ? NightFailure.Menu : Warehouse ? NightFailure.Chalk : NightFailure.Chairs);
        }

        public void Fail(string reason, NightFailure kind)
        {
            if (Phase != ShiftPhase.Incident && Phase != ShiftPhase.Surveillance) return;
            Failure = reason;
            FailureKind = kind;
            Phase = ShiftPhase.Failed;
        }

        public void ReturnToSecurity()
        {
            if (Phase == ShiftPhase.Resolved) Phase = ShiftPhase.Complete;
        }

        public string Act(NightAction action, int index = 0, bool alternate = false, bool chalkIdle = false)
        {
            if (Phase != ShiftPhase.Incident) return "지금은 처리할 이상현상이 없다.";
            return Day == 2 ? CleanMenu(action) : CloseCafe(action, index, alternate, chalkIdle);
        }

        private string CleanMenu(NightAction action)
        {
            switch (action)
            {
                case NightAction.EmptyCup:
                    if (Water != WaterState.None) return "이미 컵을 챙겼다.";
                    Water = WaterState.Empty;
                    return "빈 컵을 챙겼다.";
                case NightAction.Sink:
                    if (Water != WaterState.Empty) return "빈 컵이 있어야 물을 받을 수 있다.";
                    Water = WaterState.Cold;
                    return "컵에 찬물을 받았다.";
                case NightAction.Warmer:
                    if (Water != WaterState.Cold) return "데울 물이 담긴 컵이 필요하다.";
                    Water = WaterState.Hot;
                    return "컵 워머에서 물을 데웠다.";
                case NightAction.Cloth:
                    if (HasCloth) return "이미 행주를 챙겼다.";
                    HasCloth = true;
                    return "마른 행주를 챙겼다.";
                case NightAction.Menu:
                    if (!MenuInspected)
                    {
                        MenuInspected = true;
                        return "「뒤돌아보지 마.」 손톱자국 사이로 붉은 얼룩이 흐른다.";
                    }
                    if (!HasCloth || Water != WaterState.Hot)
                    {
                        WipesLeft = Math.Min(6, WipesLeft + 1);
                        return "마른 상태로 문지르자 얼룩이 더 번졌다.";
                    }
                    if (!WetCloth)
                    {
                        WetCloth = true;
                        return "데운 물을 행주에 묻혔다.";
                    }
                    if (--WipesLeft > 0) return "붉은 얼룩이 조금 옅어졌다.";
                    Phase = ShiftPhase.Resolved;
                    return "자국이 사라졌다. 감시실로 돌아가자.";
                default: return "";
            }
        }

        private string CloseCafe(NightAction action, int index, bool alternate, bool chalkIdle)
        {
            if (action == NightAction.Chair)
            {
                if (Warehouse || Stacked[index]) return "이미 정리한 의자다.";
                if (alternate)
                {
                    Fail("의자를 빼 주자 홀의 의자들이 날아들었다.", NightFailure.Chairs);
                    return Failure;
                }
                Stacked[index] = true;
                ChairsLeft--;
                return Warehouse ? "안광이 번쩍였다. 창고에서 무언가 긁는 소리가 난다." : "의자를 뒤집어 테이블 위에 올렸다.";
            }
            if (!Warehouse) return "창고에는 별다른 움직임이 없다.";
            switch (action)
            {
                case NightAction.Chalk:
                    if (BoardErased) return "분필은 이미 멈춰 있다.";
                    Fail("분필들이 옷자락을 꿰뚫고 칠판이 쓰러졌다.", NightFailure.Chalk);
                    return Failure;
                case NightAction.Eraser:
                    HasEraser = true;
                    return "칠판지우개를 챙겼다.";
                case NightAction.Board:
                    if (BoardErased)
                    {
                        Phase = ShiftPhase.Resolved;
                        return "「마감 완료」 분필이 떨어졌다. 감시실로 돌아가자.";
                    }
                    if (!HasEraser) return "칠판지우개가 필요하다.";
                    if (!chalkIdle) return "분필이 글씨를 쓰고 있다. 멈출 때를 기다리자.";
                    BoardErased = true;
                    return "글씨를 지웠다. 칠판에 새 문구를 적을 수 있다.";
                default: return "";
            }
        }
    }
}
