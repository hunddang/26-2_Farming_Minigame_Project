using UnityEngine;
using UnityEngine.EventSystems;

namespace NightCafe
{
    public sealed class InventoryCell : MonoBehaviour, IPointerClickHandler, IBeginDragHandler,
        IDragHandler, IDropHandler, IEndDragHandler
    {
        public UnityEngine.UI.Image frame, icon;
        public UnityEngine.UI.Text label;
        internal InventoryView View { get; set; }
        internal int Index { get; set; }

        public void OnPointerClick(PointerEventData input)
        {
            if (input.button == PointerEventData.InputButton.Right) View?.RightClick(this);
        }

        public void OnBeginDrag(PointerEventData input)
        {
            if (input.button == PointerEventData.InputButton.Left) View?.BeginDrag(this, input);
        }

        public void OnDrag(PointerEventData input)
        {
            if (input.button == PointerEventData.InputButton.Left) View?.Drag(input);
        }

        public void OnDrop(PointerEventData input)
        {
            if (input.button == PointerEventData.InputButton.Left) View?.Drop(this);
        }

        public void OnEndDrag(PointerEventData input)
        {
            if (input.button == PointerEventData.InputButton.Left) View?.EndDrag();
        }
    }
}
