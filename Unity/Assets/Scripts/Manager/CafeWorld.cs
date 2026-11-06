using System.Collections.Generic;
using UnityEngine;

namespace NightCafe
{
    public sealed class CafeWorld : MonoBehaviour
    {
        public List<CafeInteractable> Interactables = new List<CafeInteractable>();
        public Transform Root => transform;
        public Transform Cup;
        public MeshRenderer Flood;
        public Transform[] CoffeeDrops;
        public NightPlayer Player;
        [Header("Fixed cameras physically mounted in each room")]
        public Transform[] CctvMounts;
        public float[] CctvFieldsOfView = { 88, 92, 90, 92 };
        public Vector3 CupOrigin => cupOrigin;
        private Vector3 cupOrigin;
        private MaterialPropertyBlock floodProperties;
        private void Awake()
        {
            cupOrigin = Cup.position;
            floodProperties = new MaterialPropertyBlock();
        }
        // Original floor-plan coordinates are retained; plan Y becomes world Z.
        public static Vector3 GroundPosition(Vector2 plan, float height = 0) => new Vector3(plan.x, height, plan.y);
        public static Vector2 PlanPosition(Vector3 world) => new Vector2(world.x, world.z);
        public static RaycastHit[] InteractionHits(Vector2 from, Vector2 to)
        {
            Vector3 direction = GroundPosition(to - from);
            return Physics.RaycastAll(GroundPosition(from, 0.7f), direction.normalized, direction.magnitude, ~0, QueryTriggerInteraction.Ignore);
        }
        public static readonly Vector2 SecuritySpawn = new Vector2(-14, 6);
        public static readonly Vector2 IncidentSpawn = new Vector2(-3.5f, -4);
        public static readonly Vector2[] CameraPositions = { new Vector2(3, 2), new Vector2(-13, 6), new Vector2(16.5f, 5), new Vector2(16.5f, -5) };
        public static readonly string[] CameraNames = { "카페 홀", "감시실", "창고", "숙직실" };
        public const int CctvWallsLayer = 13;
        public static int RoomLayer(Vector2 position)
        {
            if (position.x < -7) return position.y > 2 ? 8 : 9;
            if (position.x > 13) return position.y > 1 ? 11 : 12;
            return 10;
        }
        public static int RoomMask(Vector2 position) => 1 | (1 << RoomLayer(position));

        public void SetFlood(float amount)
        {
            Flood.gameObject.SetActive(amount > 0.001f);
            Flood.transform.localPosition = new Vector3(3.5f, 0.025f + amount * 0.62f, 0);
            floodProperties.SetColor("_BaseColor", new Color(0.17f, 0.065f, 0.024f, Mathf.Lerp(0.25f, 0.83f, amount)));
            Flood.SetPropertyBlock(floodProperties);
        }

        public void ViewCctv(Camera camera, int index)
        {
            camera.transform.SetPositionAndRotation(CctvMounts[index].position, CctvMounts[index].rotation);
            camera.orthographic = false;
            camera.fieldOfView = CctvFieldsOfView[index];
            camera.cullingMask = RoomMask(CameraPositions[index]) | (1 << CctvWallsLayer);
        }
    }
}
