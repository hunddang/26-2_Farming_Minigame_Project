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
        private SpriteRenderer marker;
        private Color original;

        public void ConfigureMarker(SpriteRenderer value)
        {
            marker = value;
            original = value.color;
        }

        public void Highlight(bool enabled)
        {
            if (marker != null) marker.color = enabled ? new Color(1, 0.83f, 0.39f) : original;
        }

        public void SetDoor(bool open)
        {
            isOpen = open;
            var obstacle = GetComponent<BoxCollider2D>();
            if (obstacle != null) obstacle.enabled = !open;
            var visual = GetComponent<SpriteRenderer>();
            if (visual != null) visual.color = open ? new Color(0.3f, 0.7f, 0.6f, 0.2f) : new Color(0.43f, 0.5f, 0.47f);
        }
    }
}
