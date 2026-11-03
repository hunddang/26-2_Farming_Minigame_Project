using UnityEngine;

namespace NightCafe
{
    public enum InteractionKind { Camera, Note, RuleBook, Recipe, Cup, Ingredient, Door, Inspect }

    public sealed class CafeInteractable : MonoBehaviour
    {
        public InteractionKind kind;
        public string title;
        [TextArea] public string description;
        public Ingredient ingredient;
        public bool isOpen;
        public bool securityDoor;
        [SerializeField] private SpriteRenderer marker;
        [SerializeField] private Color original = Color.white;
        private void Awake()
        {
            if (marker == null) marker = GetComponent<SpriteRenderer>();
            original = marker.color;
            if (kind == InteractionKind.Door) SetDoor(isOpen);
        }

        public void ConfigureMarker(SpriteRenderer value)
        {
            marker = value;
            original = value.color;
        }

        public void Highlight(bool enabled)
        {
            if (marker != null) marker.color = enabled ? new Color(1, 0.83f, 0.39f) : BaseColor;
        }

        private Color BaseColor => kind == InteractionKind.Door && isOpen
            ? new Color(original.r, original.g, original.b, 0.2f) : original;

        public void SetDoor(bool open)
        {
            isOpen = open;
            var obstacle = GetComponent<BoxCollider2D>();
            if (obstacle != null) obstacle.enabled = !open;
            if (marker == null) marker = GetComponent<SpriteRenderer>();
            if (marker != null) marker.color = BaseColor;
        }
    }
}
