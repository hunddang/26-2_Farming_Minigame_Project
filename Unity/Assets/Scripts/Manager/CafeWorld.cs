using System.Collections.Generic;
using UnityEngine;

namespace NightCafe
{
    public sealed class CafeWorld : MonoBehaviour
    {
        public List<CafeInteractable> Interactables = new List<CafeInteractable>();
        public Transform Root => transform;
        public Transform Cup;
        public SpriteRenderer Flood;
        public Transform[] CoffeeDrops;
        public NightPlayer Player;
        public Vector3 CupOrigin => cupOrigin;
        private Vector3 cupOrigin;
        private void Awake() => cupOrigin = Cup.position;
        public static readonly Vector2 SecuritySpawn = new Vector2(-14, 6);
        public static readonly Vector2 IncidentSpawn = new Vector2(-3.5f, -4);
        public static readonly Vector2[] CameraPositions = { new Vector2(3, 2), new Vector2(-13, 6), new Vector2(16.5f, 5), new Vector2(16.5f, -5) };
        public static readonly string[] CameraNames = { "카페 홀", "감시실", "창고", "숙직실" };
        // Keep the floor plan visible, but reveal objects only in the player's room.
        // CCTV uses its own mask, so leaving security really loses remote observation.
        public static int RoomLayer(Vector2 position)
        {
            if (position.x < -7) return position.y > 2 ? 8 : 9;
            if (position.x > 13) return position.y > 1 ? 11 : 12;
            return 10;
        }
        public static int RoomMask(Vector2 position) => 1 | (1 << RoomLayer(position));

    }
}
