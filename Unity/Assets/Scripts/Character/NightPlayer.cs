using UnityEngine;
using UnityEngine.InputSystem;

namespace NightCafe
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class NightPlayer : MonoBehaviour
    {
        public NightGame Game { get; set; }
        public Vector2 Facing { get; private set; } = Vector2.down;
        public bool FlashlightOn { get; private set; } = true;
        public Transform flashlight;
        public Transform model, leftLeg, rightLeg, reachArm;
        private Rigidbody body;
        private Vector3 movement;
        private float walkClock;
        private bool reaching;
        public Vector2 PlanPosition => CafeWorld.PlanPosition(transform.position);

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void Update()
        {
            movement = Vector3.zero;
            var keys = Keyboard.current;
            if (Game != null && !Game.InputBlocked && keys != null)
            {
                if (keys.wKey.isPressed || keys.upArrowKey.isPressed) movement.z++;
                if (keys.sKey.isPressed || keys.downArrowKey.isPressed) movement.z--;
                if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) movement.x--;
                if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) movement.x++;
                movement.Normalize();
                if (movement.sqrMagnitude > 0) Facing = new Vector2(movement.x, movement.z);
                if (keys.fKey.wasPressedThisFrame) FlashlightOn = !FlashlightOn;
                movement *= Game.Tuning.moveSpeed * (keys.leftShiftKey.isPressed ? Game.Tuning.runMultiplier : 1);
            }
            if (flashlight != null)
            {
                flashlight.gameObject.SetActive(FlashlightOn);
                flashlight.localPosition = new Vector3(Facing.x * 0.42f, 1.1f, Facing.y * 0.42f);
                flashlight.rotation = Quaternion.LookRotation(new Vector3(Facing.x, -0.22f, Facing.y));
            }
            if (model != null) model.rotation = Quaternion.LookRotation(new Vector3(Facing.x, 0, Facing.y));
            walkClock += movement.magnitude * Time.deltaTime * 2.5f;
            float stride = movement.sqrMagnitude > 0 ? Mathf.Sin(walkClock) * 24 : 0;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(stride, 0, 0);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-stride, 0, 0);
            if (!reaching && reachArm != null) reachArm.localRotation = Quaternion.Euler(-stride * 0.5f, 0, 0);
        }

        private void FixedUpdate()
        {
            body.linearVelocity = Game != null && !Game.InputBlocked ? movement : Vector3.zero;
        }

        public void Teleport(Vector2 position)
        {
            movement = Vector3.zero;
            body.linearVelocity = Vector3.zero;
            body.position = CafeWorld.GroundPosition(position);
            transform.position = body.position;
        }

        public void Reach(float progress)
        {
            reaching = progress > 0;
            if (reachArm != null) reachArm.localRotation = Quaternion.Euler(-Mathf.Sin(progress * Mathf.PI) * 85, 0, 0);
        }
    }
}
