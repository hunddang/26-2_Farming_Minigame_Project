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
        public Transform doorHinge;
        private Renderer[] markers;
        private MaterialPropertyBlock highlightProperties;
        private float doorAngle;
        private void Awake()
        {
            markers = GetComponentsInChildren<MeshRenderer>(true);
            highlightProperties = new MaterialPropertyBlock();
            if (kind == InteractionKind.Door) SetDoor(isOpen);
        }
        public void Highlight(bool enabled)
        {
            if (markers == null) return;
            foreach (var marker in markers)
            {
                if (enabled)
                {
                    highlightProperties.Clear();
                    highlightProperties.SetColor("_EmissionColor", new Color(0.25f, 0.13f, 0.025f));
                    marker.SetPropertyBlock(highlightProperties);
                }
                else marker.SetPropertyBlock(null);
            }
        }
        public void SetDoor(bool open)
        {
            isOpen = open;
            var obstacle = GetComponent<BoxCollider>();
            if (obstacle != null) obstacle.enabled = !open;
            if (!Application.isPlaying && doorHinge != null) doorHinge.localRotation = Quaternion.Euler(0, open ? -95 : 0, 0);
        }
        private void Update()
        {
            if (doorHinge == null) return;
            doorAngle = Mathf.MoveTowards(doorAngle, isOpen ? -95 : 0, Time.deltaTime * 160);
            doorHinge.localRotation = Quaternion.Euler(0, doorAngle, 0);
        }
    }
}
