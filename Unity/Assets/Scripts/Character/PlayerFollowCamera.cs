using UnityEngine;

namespace NightCafe
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerFollowCamera : MonoBehaviour
    {
        [Tooltip("The scene world containing the player to keep centered.")]
        public CafeWorld world;

        private void OnEnable() => FollowPlayer();

        // Follow the interpolated player transform after movement has updated.
        private void LateUpdate() => FollowPlayer();

        private void FollowPlayer()
        {
            if (world == null || world.Player == null) return;
            Vector3 player = world.Player.transform.position;
            transform.position = new Vector3(player.x, player.y, transform.position.z);
        }
    }
}
