using UnityEngine;
using UnityEngine.InputSystem;

namespace NightCafe
{
    public enum InventoryItem
    {
        None, Milk, Chocolate, Almond, Tapioca, Cream, Recipe, RuleBook,
        WaterCup, Cloth, Eraser, Extinguisher, Net, Anchor,
        Roster, Accident, RepairRequest, Diagram, Note, Badge, VictimCup, Flashlight
    }

    internal enum InventoryItemKind { Hand, Equipment, CarryOnly }

    public sealed partial class NightGame
    {
        public const int InventoryCapacity = 8;
        private readonly InventoryItem[] inventory = new InventoryItem[InventoryCapacity];
        internal InventoryItem HeldItem { get; private set; }
        internal InventoryItem EquippedItem { get; private set; }
        internal bool FlashlightEquipped => EquippedItem == InventoryItem.Flashlight;
        private int handReturnSlot = -1, equipmentReturnSlot = -1;
        private bool ownsRuleBook, ownsFlashlight;
        private float beforeInventoryTimeScale, inventoryOpenedAt;
        private bool beforeInventoryAudioPause;
        private bool InventoryOpen { get; set; }

        internal InventoryItem InventoryItemAt(int index) => inventory[index];

        private bool HandleInventoryInput()
        {
            var keys = Keyboard.current;
            if (InventoryOpen)
            {
                if (keys != null)
                {
                    if (keys.tabKey.wasPressedThisFrame || keys.escapeKey.wasPressedThisFrame) CloseInventory();
                    else if (keys.spaceKey.wasPressedThisFrame && overlay == Overlay.None) ReadHeldInventoryItem();
                }
                return true;
            }

            if (keys == null || !keys.tabKey.wasPressedThisFrame || pouring || storyBusy ||
                Phase == ShiftPhase.Failed || Phase == ShiftPhase.Complete || overlay == Overlay.Pause)
                return false;

            beforeInventoryTimeScale = Time.timeScale;
            beforeInventoryAudioPause = AudioListener.pause;
            inventoryOpenedAt = Time.unscaledTime;
            Time.timeScale = 0;
            AudioListener.pause = true;
            InventoryOpen = true;
            view.inventory.SetOpen(true);
            RefreshInventory();
            return true;
        }

        private void CloseInventory()
        {
            if (!InventoryOpen) return;
            InventoryOpen = false;
            Time.timeScale = beforeInventoryTimeScale;
            AudioListener.pause = beforeInventoryAudioPause;
            toastUntil += Time.unscaledTime - inventoryOpenedAt;
            if (view == null || view.inventory == null) return;
            view.inventory.SetOpen(false);
        }

        internal void MoveInventoryItem(int source, int destination)
        {
            if (!InventoryOpen || source == destination || inventory[source] == InventoryItem.None) return;
            (inventory[destination], inventory[source]) = (inventory[source], inventory[destination]);
            view.inventory.Refresh();
        }

        internal void EquipInventoryItem(int index)
        {
            if (!InventoryOpen) return;
            var item = inventory[index];
            switch (ItemKind(item))
            {
                case InventoryItemKind.Hand:
                    inventory[index] = HeldItem;
                    SetHeldItem(item, index);
                    break;
                case InventoryItemKind.Equipment:
                    inventory[index] = EquippedItem;
                    SetEquippedItem(item, index);
                    break;
                default:
                    return;
            }
            view.inventory.Refresh();
        }

        internal void UnequipInventoryItem(InventoryItemKind kind)
        {
            if (!InventoryOpen) return;
            bool returned;
            if (kind == InventoryItemKind.Equipment)
            {
                returned = TryReturnItem(EquippedItem, equipmentReturnSlot);
                if (returned) SetEquippedItem(InventoryItem.None);
            }
            else returned = ReturnHeldItem();

            if (!returned) Toast("인벤토리에 빈칸이 없다.");
            view.inventory.Refresh();
        }

        private bool TryReturnItem(InventoryItem item, int previousSlot)
        {
            if (item == InventoryItem.None) return true;
            int empty = previousSlot >= 0 && inventory[previousSlot] == InventoryItem.None
                ? previousSlot : System.Array.IndexOf(inventory, InventoryItem.None);
            if (empty < 0) return false;
            inventory[empty] = item;
            return true;
        }

        private bool ReturnHeldItem()
        {
            if (!TryReturnItem(HeldItem, handReturnSlot)) return false;
            SetHeldItem(InventoryItem.None);
            return true;
        }

        private void SetHeldItem(InventoryItem item, int previousSlot = -1)
        {
            HeldItem = item;
            handReturnSlot = previousSlot;
            int ingredient = (int)item - (int)InventoryItem.Milk;
            Session.HoldIngredient(ingredient);
        }

        private void SetEquippedItem(InventoryItem item, int previousSlot = -1, bool powerOn = true)
        {
            EquippedItem = item;
            equipmentReturnSlot = previousSlot;
            World.Player.SetFlashlightPower(FlashlightEquipped && powerOn);
            UpdateSight();
        }

        private static InventoryItemKind ItemKind(InventoryItem item) => item switch
        {
            InventoryItem.Flashlight => InventoryItemKind.Equipment,
            InventoryItem.Milk or InventoryItem.Chocolate or InventoryItem.Almond or InventoryItem.Tapioca or InventoryItem.Cream or
                InventoryItem.Recipe or InventoryItem.RuleBook or InventoryItem.WaterCup or InventoryItem.Cloth or InventoryItem.Eraser or
                InventoryItem.Extinguisher or InventoryItem.Net or InventoryItem.Anchor or InventoryItem.Roster or InventoryItem.Accident or
                InventoryItem.RepairRequest or InventoryItem.Diagram or InventoryItem.Note or InventoryItem.Badge or InventoryItem.VictimCup
                => InventoryItemKind.Hand,
            _ => InventoryItemKind.CarryOnly
        };

        private void SetOwned(InventoryItem item, bool owned)
        {
            int existing = System.Array.IndexOf(inventory, item);
            if (owned)
            {
                if (existing >= 0 || HeldItem == item || EquippedItem == item) return;
                int empty = System.Array.IndexOf(inventory, InventoryItem.None);
                if (empty >= 0) inventory[empty] = item;
            }
            else
            {
                if (existing >= 0) inventory[existing] = InventoryItem.None;
                if (HeldItem == item) SetHeldItem(InventoryItem.None);
                if (EquippedItem == item) SetEquippedItem(InventoryItem.None);
            }
        }

        private void RefreshInventory()
        {
            SetOwned(InventoryItem.RuleBook, ownsRuleBook);
            SetOwned(InventoryItem.Flashlight, ownsFlashlight);
            if (Day == 1)
            {
                for (int index = 0; index < Session.Inventory.Length; index++)
                    SetOwned(InventoryItem.Milk + index, Session.Inventory[index]);
                SetOwned(InventoryItem.Recipe, Session.RecipeRead);
            }
            else if (Day == 2)
            {
                SetOwned(InventoryItem.WaterCup, laterShift.Water != WaterState.None);
                SetOwned(InventoryItem.Cloth, laterShift.HasCloth);
            }
            else if (Day == 3) SetOwned(InventoryItem.Eraser, laterShift.HasEraser);
            else if (Day == 4)
            {
                SetOwned(InventoryItem.Extinguisher, Fourth.HasExtinguisher);
                SetOwned(InventoryItem.Net, Fourth.HasNet && Fourth.TrapRoom < 0);
                SetOwned(InventoryItem.Anchor, Fourth.HasAnchor && Fourth.TrapRoom < 0);
            }
            else
            {
                SetOwned(InventoryItem.Roster, Fifth.RosterRead);
                SetOwned(InventoryItem.Accident, Fifth.AccidentRead);
                SetOwned(InventoryItem.RepairRequest, Fifth.RepairRequestRead);
                SetOwned(InventoryItem.Diagram, Fifth.DiagramRead);
                SetOwned(InventoryItem.Note, Fifth.HasNote);
                SetOwned(InventoryItem.Badge, Fifth.SelectedBadge >= 0 && !Fifth.Identified);
                SetOwned(InventoryItem.VictimCup, Fifth.HasCup && Fifth.Stage is not (FinaleStage.Escort or FinaleStage.Happy));
            }
            view.inventory.Refresh();
        }

        internal string InventoryName(InventoryItem item) => item switch
        {
            InventoryItem.Milk or InventoryItem.Chocolate or InventoryItem.Almond or InventoryItem.Tapioca or InventoryItem.Cream
                => GameTuning.IngredientName((Ingredient)(item - InventoryItem.Milk)),
            InventoryItem.Recipe => "카페모카 레시피",
            InventoryItem.RuleBook => "전임자의 노트",
            InventoryItem.WaterCup => laterShift.Water switch
            {
                WaterState.Cold => "찬물이 든 컵", WaterState.Hot => "데운 물이 든 컵", _ => "빈 컵"
            },
            InventoryItem.Cloth => laterShift.WetCloth ? "젖은 행주" : "마른 행주",
            InventoryItem.Eraser => "칠판지우개",
            InventoryItem.Extinguisher => "소화기",
            InventoryItem.Net => "포획용 그물",
            InventoryItem.Anchor => "그물 고정구",
            InventoryItem.Roster => "직원 근무표",
            InventoryItem.Accident => "사고 기록서",
            InventoryItem.RepairRequest => "수리 요청서",
            InventoryItem.Diagram => "복도 정비도",
            InventoryItem.Note => "A의 쪽지",
            InventoryItem.Badge => BadgeName(Fifth.SelectedBadge),
            InventoryItem.VictimCup => "직원의 컵",
            InventoryItem.Flashlight => "손전등",
            _ => ""
        };

        private void ReadHeldInventoryItem()
        {
            var item = HeldItem;
            if (item is not (InventoryItem.RuleBook or InventoryItem.Recipe or InventoryItem.Roster or
                InventoryItem.Accident or InventoryItem.RepairRequest or InventoryItem.Diagram or InventoryItem.Note)) return;
            CloseInventory();
            if (item == InventoryItem.RuleBook) OpenRules();
            else if (item == InventoryItem.Recipe) OpenRecipe();
            else ReadEvidence(item);
        }
    }
}
