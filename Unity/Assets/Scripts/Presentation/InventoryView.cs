using UnityEngine;
using UnityEngine.EventSystems;

namespace NightCafe
{
    public sealed class InventoryView : MonoBehaviour
    {
        public InventoryCell[] cells;
        public InventoryCell hand, equipment;
        public UnityEngine.UI.Image dragIcon;
        public Sprite[] icons;

        private NightGame game;
        private int source = -1;
        private int hudSibling;
        private Transform Hud => hand.transform.parent;

        internal void Bind(NightGame owner)
        {
            game = owner;
            hand.View = equipment.View = this;
            for (int index = 0; index < cells.Length; index++)
            {
                cells[index].View = this;
                cells[index].Index = index;
            }
            hudSibling = Hud.GetSiblingIndex();
            SetOpen(false);
        }

        internal void SetOpen(bool open)
        {
            hand.frame.raycastTarget = equipment.frame.raycastTarget = open;
            if (open)
            {
                hudSibling = Hud.GetSiblingIndex();
                transform.SetAsLastSibling();
                Hud.SetAsLastSibling();
            }
            else
            {
                Hud.SetSiblingIndex(hudSibling);
                EndDrag();
            }
            gameObject.SetActive(open);
        }

        internal void Refresh()
        {
            Paint(hand, game.HeldItem);
            Paint(equipment, game.EquippedItem);
            if (!gameObject.activeSelf) return;
            for (int index = 0; index < cells.Length; index++)
                Paint(cells[index], game.InventoryItemAt(index));
        }

        private void Paint(InventoryCell cell, InventoryItem item)
        {
            bool inUse = item != InventoryItem.None && (cell == hand || cell == equipment);
            cell.frame.color = inUse ? CafeUi.Amber : CafeUi.Muted;
            cell.icon.sprite = icons[(int)item];
            cell.icon.enabled = cell.icon.sprite != null;
            cell.label.text = game.InventoryName(item);
        }

        internal void RightClick(InventoryCell cell)
        {
            if (cell == hand) game.UnequipInventoryItem(InventoryItemKind.Hand);
            else if (cell == equipment) game.UnequipInventoryItem(InventoryItemKind.Equipment);
            else game.UseInventoryItem(cell.Index);
        }

        internal void BeginDrag(InventoryCell cell, PointerEventData input)
        {
            if (!gameObject.activeSelf || cell == hand || cell == equipment ||
                game.InventoryItemAt(cell.Index) == InventoryItem.None) return;
            source = cell.Index;
            dragIcon.sprite = cell.icon.sprite;
            dragIcon.enabled = true;
            Drag(input);
        }

        internal void Drag(PointerEventData input)
        {
            if (source < 0) return;
            var parent = (RectTransform)dragIcon.transform.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, input.position, input.pressEventCamera, out var point))
                dragIcon.rectTransform.localPosition = point;
        }

        internal void Drop(InventoryCell cell)
        {
            if (source >= 0 && cell != hand && cell != equipment) game.MoveInventoryItem(source, cell.Index);
            EndDrag();
        }

        internal void EndDrag()
        {
            source = -1;
            dragIcon.enabled = false;
        }
    }
}
