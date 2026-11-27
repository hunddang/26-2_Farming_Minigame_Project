using System;
using System.Collections.Generic;
using System.IO;
using NightCafe;
using UnityEngine;

public static class UpdatedDesignValidation
{
    private static int checks;
    private static void Check(bool valid, string label)
    {
        checks++;
        if (!valid) throw new InvalidOperationException("Updated design validation failed: " + label);
    }
    public static void Run()
    {
        checks = 0;
        var tuning = ScriptableObject.CreateInstance<GameTuning>();
        var day = new ShiftSession(tuning);
        day.Tick(1); Check(day.Remaining == 180 && day.DiscoveryRemaining == tuning.discoverySeconds - 1, "Discovery timeout is separate from the incident timer");
        day.StartIncident(); day.Collect(Ingredient.Milk);
        Check(day.SelectedSlot == -1, "Picking up an ingredient does not put it in hand");
        day.HoldIngredient(0);
        Check(!day.TryPour() && day.Inventory[0], "Wrong first ingredient is retained");
        Check(day.RecipeStep == 0 && day.Mistakes == 1, "Wrong ingredient resets recipe progress");
        day.Collect(Ingredient.Chocolate); day.HoldIngredient(1);
        Check(day.TryPour() && day.RecipeStep == 1, "Chocolate must be first");
        day.HoldIngredient(0); Check(!day.TryPour() && day.Inventory[0] && day.Inventory[1], "Cold milk is wrong and previously used ingredients survive");
        day.HoldIngredient(1); Check(day.TryPour(), "Chocolate can be reused after a mistake");
        Check(day.HeatMilk(), "Microwave heats owned milk"); day.HoldIngredient(0);
        Check(day.TryPour() && day.Phase == ShiftPhase.Resolved, "Hot milk resolves the revised mocha recipe");
        day.ReturnToSecurity(); Check(day.Phase == ShiftPhase.Resolved, "Cleanup must finish before returning");
        day.Tick(4); day.ReturnToSecurity(); Check(day.Phase == ShiftPhase.Complete, "Automatic cleanup and return complete night one");
        day = new ShiftSession(tuning); day.Tick(tuning.discoverySeconds);
        Check(day.Phase == ShiftPhase.Failed, "Ignoring the anomaly eventually floods the cafe");
        day.RetryIncident(); Check(day.Remaining == 180 && day.SelectedSlot == -1 && !day.MilkHeated, "Night-one retry resets incident items and heating");

        var second = new LaterShift(2, 300, 0); second.EnterSite();
        second.Act(NightAction.Menu); second.Act(NightAction.Menu);
        Check(second.WipesLeft == 3, "Unprepared wiping adds work");
        foreach (var action in new[] { NightAction.EmptyCup, NightAction.Sink, NightAction.Warmer, NightAction.Cloth }) second.Act(action);
        second.Act(NightAction.Menu); Check(second.WetCloth && second.Water == WaterState.Hot, "Hot water wets the owned cloth");
        for (int i = 0; i < 3; i++) second.Act(NightAction.Menu);
        Check(second.Phase == ShiftPhase.Resolved && !second.RestrictTurning, "Wiping clears the turning restriction");
        var third = new LaterShift(3, 360, 8); third.EnterSite();
        for (int i = 0; i < 8; i++) third.Act(NightAction.Chair, i);
        Check(third.Warehouse && third.Phase == ShiftPhase.Incident, "All chairs start the warehouse encounter without completing the night");
        third.Act(NightAction.Eraser); third.Act(NightAction.Board, chalkIdle: false);
        Check(!third.BoardErased, "Cannot erase while chalk is moving");
        third.Act(NightAction.Board, chalkIdle: true); third.Act(NightAction.Board);
        Check(third.Phase == ShiftPhase.Resolved, "Erasing during a pause then writing closes the warehouse");
        third = new LaterShift(3, 360, 8); third.EnterSite(); third.Act(NightAction.Chair, 0, true);
        Check(third.Phase == ShiftPhase.Failed && third.FailureKind == NightFailure.Chairs, "Pulling a chair out triggers the poltergeist ending");

        foreach (int trapRoom in new[] { 9, 10, 11, 12, 13 })
        {
            var hunt = new FourthShift(540);
            Check(!hunt.StartFirst(), "First drop requires the lobby CCTV observation"); hunt.Observe(9); Check(hunt.StartFirst(), "Observed leak starts exactly one first chase");
            hunt.CollectExtinguisher(); Check(!hunt.Spray(false) && hunt.Charges == 2, "An out-of-range spray is never consumed");
            Check(hunt.Spray(true) && hunt.Stage == HuntStage.ReadNotes && hunt.Charges == 1, "First spray removes the hunter");
            hunt.Collect(true); Check(!hunt.HasNet, "The equipment stage follows reading the predecessor's note");
            hunt.ReadNotes(); hunt.Collect(true); hunt.Collect(false);
            Check(!hunt.StartSecond(11) && hunt.StartSecond(9), "Second chase begins in the first valid room after storage");
            hunt.EscapeSecond(); Check(hunt.HasNet && hunt.HasAnchor, "Escaping keeps the trap equipment");
            Check(!hunt.Install(8) && hunt.Install(trapRoom), "Only one validated room marker can be selected");
            Check(hunt.Recover() && hunt.TrapRoom == -1, "Trap can be recovered before the third chase"); hunt.Install(trapRoom);
            Check(!hunt.StartThird(10), "Third chase cannot start without CCTV search");
            hunt.Observe(10); Check(hunt.StartThird(10), "The observed room starts the lure");
            Check(!hunt.Recover(), "Armed trap cannot move during the third chase");
            hunt.LoseMonster(); Check(hunt.TrapRoom == trapRoom && hunt.SearchRoom == -1, "Losing sight keeps the trap and allows another camera search");
            hunt.Observe(9); hunt.StartThird(9);
            Check(!hunt.Capture(trapRoom, 2) && hunt.Capture(trapRoom, 1), "Trap closes automatically only within its radius");
            hunt.Return(); Check(hunt.Phase == ShiftPhase.Complete, "Captured hunter and return finish night four");
        }

        var final = new FifthShift(tuning);
        Check(!final.RestoreCameras(tuning.cameraPassword), "Four physical digits must be found before entering the password");
        for (int i = 0; i < 4; i++) final.ReadDigit(i);
        Check(!final.RestoreCameras("0000") && final.Cracks == 0, "Intro password mistakes never count toward capture");
        Check(final.RestoreCameras(tuning.cameraPassword) && final.Freeze(), "Returning after camera restoration stops time");
        float remaining = final.Remaining; final.Tick(10000);
        Check(final.Remaining == remaining, "There is no hidden timer in the frozen night");
        Check(!final.CanLeaveSecurity, "Security is locked until both documents and the CCTV are examined");
        final.ReadRoster(); final.ReadAccident(); final.ObserveIdentity();
        Check(final.CanLeaveSecurity, "All three security clues unlock the exit");
        final.InspectIdentity(); final.TakeBadge(1); Check(!final.GiveBadge() && final.Cracks == 1, "Wrong identity increments cracks");
        final.TakeBadge(0); Check(final.GiveBadge(), "Roster and matching number identify the victim");
        Check(final.TentacleMemory(), "Tentacle footage grants the repair request and machine-off evidence");
        Check(!final.UnlockControl("0000") && final.Cracks == 2, "Wrong control password increments cracks");
        final.Wrong("test"); Check(final.Phase == ShiftPhase.Failed, "Third confirmed mistake captures the worker");
        final.Retry(); Check(final.Identified && final.RepairRequestRead && final.Cracks == 0 && final.Stage == FinaleStage.Frozen, "Retry preserves acquired evidence and identity");
        Check(final.UnlockControl(tuning.controlPassword) && !final.ReleaseLock(), "Unlocking alone does not release a running mechanism");
        Check(!final.ConfirmStop(), "Stop indicator cannot be confirmed while the motor is on");
        final.SwitchOff(); final.ConfirmStop(); Check(final.ReleaseLock(), "Motor off, confirmation, then emergency release");
        Check(final.PursuitMemory(), "Original escape footage is available after releasing the lock");
        final.ReadDiagram(); Check(!final.Ventilate() && final.Cracks == 0, "Blocked airflow is feedback, not a confirmed puzzle mistake");
        final.ClearBox(); Check(final.Ventilate(), "Cleared exhaust and outward fan remove smoke");
        Check(!final.CorrectRecord(0, "2", true), "Wrong cause, present-day count and departure status are rejected");
        Check(final.CorrectRecord(1, "1", false) && final.HasNote, "Evidence supports the three corrected accident fields");
        final.TakeCup();final.Wrong("test");final.Wrong("test");final.Retry();
        Check(final.TentaclesReleased && final.SmokeCleared && final.RecordCorrected && final.HasCup && final.Cracks==0,"Retry preserves solved causes, corrected records and the cup");
        final.TakeCup(); Check(final.Summon(), "Corrected record, both mechanisms and the victim's cup summon the spirit");
        final.OpenExit(); Check(!final.ConfirmDeparture(true), "Door and camera alone cannot finish without the spirit's passage");
        final.PassSpirit(); Check(final.ConfirmDeparture(true) && final.Phase == ShiftPhase.Complete, "Spirit first, then the inside departure confirmation gives the happy ending");
        final = new FifthShift(tuning); for (int i = 0; i < 4; i++) final.ReadDigit(i); final.RestoreCameras(tuning.cameraPassword); final.Freeze();
        final.CrossThreshold(); Check(final.Phase == ShiftPhase.Failed, "Confirming an unresolved exit gives the capture ending");

        for (int layout = 0; layout < DetourPuzzle.LayoutCount; layout++) Check(PuzzleHasSolution(layout), "Detour layout " + layout + " has a solution");
        UnityEngine.Object.DestroyImmediate(tuning);
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Artifacts/rule-tests.txt"));
        Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, $"PASS: {checks} revised five-night rule and branch checks.\n");
        Debug.Log("NIGHT_CAFE_RULE_TESTS_PASS: " + checks);
    }
    private static bool PuzzleHasSolution(int index)
    {
        var queue = new Queue<string>(); var visited = new HashSet<string>();
        queue.Enqueue("");
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0 && visited.Count < 30000)
        {
            string moves = queue.Dequeue(); var puzzle = new DetourPuzzle(index);
            foreach (char move in moves) puzzle.Move(directions[move - '0']);
            if (puzzle.Solved) return true;
            string key = "";
            for (int y = 0; y < 6; y++) for (int x = 0; x < 6; x++) key += (int)puzzle.CellAt(x, y);
            if (!visited.Add(key)) continue;
            for (int i = 0; i < 4; i++) queue.Enqueue(moves + i);
        }
        return false;
    }
}
