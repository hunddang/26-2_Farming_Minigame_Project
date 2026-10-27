using System;

namespace NightCafe
{
    // Plain C# state: presentation, input and scene objects do not own the rules.
    public sealed class ShiftSession
    {
        public GameTuning Tuning { get; }
        public ShiftPhase Phase { get; private set; } = ShiftPhase.Surveillance;
        public float Remaining { get; private set; }
        public float SurgeRemaining { get; private set; }
        public float CleanupRemaining { get; private set; }
        public int RecipeStep { get; private set; }
        public int Mistakes { get; private set; }
        public bool CupInspected { get; set; }
        public bool RecipeRead { get; set; }
        public bool CameraViewed { get; set; }
        public bool[] Inventory { get; } = new bool[5];
        public int SelectedSlot { get; private set; } = -1;
        public float Flood => Phase == ShiftPhase.Resolved || Phase == ShiftPhase.Complete
            ? CleanupRemaining / Math.Max(0.01f, Tuning.cleanupSeconds)
            : 1 - Remaining / Math.Max(1, Tuning.incidentSeconds);

        public ShiftSession(GameTuning tuning)
        {
            Tuning = tuning;
            Remaining = tuning.incidentSeconds;
        }

        public void StartIncident()
        {
            if (Phase == ShiftPhase.Surveillance) Phase = ShiftPhase.Incident;
        }

        public void Tick(float seconds)
        {
            if (seconds <= 0) return;
            if (Phase == ShiftPhase.Incident)
            {
                float boosted = Math.Min(seconds, SurgeRemaining);
                Remaining = Math.Max(0, Remaining - seconds - boosted * (Tuning.surgeMultiplier - 1));
                SurgeRemaining = Math.Max(0, SurgeRemaining - seconds);
                if (Remaining <= 0) Phase = ShiftPhase.Failed;
            }
            else if (Phase == ShiftPhase.Resolved)
                CleanupRemaining = Math.Max(0, CleanupRemaining - seconds);
        }

        public bool Collect(Ingredient item)
        {
            if (Phase != ShiftPhase.Incident || Inventory[(int)item]) return false;
            Inventory[(int)item] = true;
            SelectedSlot = (int)item;
            return true;
        }

        public void Select(int index)
        {
            if (index >= 0 && index < Inventory.Length)
                SelectedSlot = SelectedSlot == index ? -1 : index;
        }

        public bool TryPour()
        {
            if (Phase != ShiftPhase.Incident || SelectedSlot < 0 || !Inventory[SelectedSlot]) return false;
            Ingredient item = (Ingredient)SelectedSlot;
            Inventory[SelectedSlot] = false;
            SelectedSlot = -1;
            if (item != Tuning.recipe[RecipeStep])
            {
                RecipeStep = 0;
                Mistakes++;
                Remaining = Math.Max(0, Remaining - Tuning.mistakePenaltySeconds);
                SurgeRemaining = Tuning.surgeSeconds;
                if (Remaining <= 0) Phase = ShiftPhase.Failed;
                return false;
            }
            RecipeStep++;
            if (RecipeStep == Tuning.recipe.Length)
            {
                Phase = ShiftPhase.Resolved;
                CleanupRemaining = Tuning.cleanupSeconds;
            }
            return true;
        }

        public void ReturnToSecurity()
        {
            if (Phase == ShiftPhase.Resolved && CleanupRemaining <= 0) Phase = ShiftPhase.Complete;
        }

        public void RetryIncident()
        {
            Phase = ShiftPhase.Incident;
            Remaining = Tuning.incidentSeconds;
            SurgeRemaining = CleanupRemaining = 0;
            RecipeStep = Mistakes = 0;
            SelectedSlot = -1;
            Array.Clear(Inventory, 0, Inventory.Length);
        }
    }
}
