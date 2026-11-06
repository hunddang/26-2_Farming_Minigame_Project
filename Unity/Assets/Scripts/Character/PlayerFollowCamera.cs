using UnityEngine;

namespace NightCafe
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class PlayerFollowCamera : MonoBehaviour
    {
        public CafeWorld world;
        [Tooltip("North stays at the top of the screen, matching WASD and the existing floor plan.")]
        public Vector3 offset = new Vector3(0, 11, -9);
        public float lookHeight = 0.55f;
        private void OnEnable() => FollowPlayer();
        private void LateUpdate() => FollowPlayer();
        private void FollowPlayer()
        {
            if (world == null || world.Player == null) return;
            Vector3 player = world.Player.transform.position;
            transform.SetPositionAndRotation(player + offset, Quaternion.LookRotation(Vector3.up * lookHeight - offset));
        }
    }
}
