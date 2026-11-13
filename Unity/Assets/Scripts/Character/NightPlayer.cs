using UnityEngine;
using UnityEngine.InputSystem;

namespace NightCafe
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class NightPlayer : MonoBehaviour
    {
        public NightGame Game { get; set; }
        public Vector2 Facing { get; private set; } = Vector2.down;
        public bool FlashlightOn { get; private set; } = true;
        private Rigidbody2D body;
        public Transform flashlight;
        private Vector2 movement;

        public Vector2 PlanPosition => transform.position;

        public void SnapFacingToCardinal()
        {
            Facing = Mathf.Abs(Facing.x) >= Mathf.Abs(Facing.y)
                ? new Vector2(Mathf.Sign(Facing.x), 0)
                : new Vector2(0, Mathf.Sign(Facing.y));
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            GetComponent<CircleCollider2D>().radius = 0.31f;

        }

        private void Update()
        {
            movement = Vector2.zero;
            var keys = Keyboard.current;
            if (Game == null || Game.InputBlocked || keys == null) return;
            if (keys.wKey.isPressed || keys.upArrowKey.isPressed) movement.y++;
            if (keys.sKey.isPressed || keys.downArrowKey.isPressed) movement.y--;
            if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) movement.x--;
            if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) movement.x++;
            movement = Game.FilterDirection(movement, Facing);
            if (movement.sqrMagnitude > 0) Facing = movement;
            if (keys.fKey.wasPressedThisFrame) FlashlightOn = !FlashlightOn;
            flashlight.gameObject.SetActive(FlashlightOn);
            flashlight.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg - 90);
            movement *= Game.Tuning.moveSpeed * (keys.leftShiftKey.isPressed ? Game.Tuning.runMultiplier : 1);
        }

        private void FixedUpdate()
        {
            if (Game != null && !Game.InputBlocked) body.MovePosition(body.position + movement * Time.fixedDeltaTime);
        }

        public void Teleport(Vector2 position)
        {
            movement = Vector2.zero;
            body.position = position;
            transform.position = position;
        }

        public void Reach(float progress)
        {
            var head = transform.Find("Head");
            if (head != null) head.localPosition = new Vector3(0, 0.16f + Mathf.Sin(progress * Mathf.PI) * 0.18f, 0);
        }
    }
}
