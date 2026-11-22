using System.Collections.Generic;
using UnityEngine;

namespace NightCafe
{
    public sealed class CafeWorld : MonoBehaviour
    {
        public List<CafeInteractable> Interactables = new List<CafeInteractable>();
        public Transform Root => transform;
        public Transform Cup;
        public Sprite[] CupFrames;
        public SpriteRenderer Flood;
        public Transform[] CoffeeDrops;
        public NightPlayer Player;
        public Vector3 CupOrigin => cupOrigin;
        private Vector3 cupOrigin;
        private SpriteRenderer cupRenderer;

        private void Awake()
        {
            cupOrigin = Cup.position;
            cupRenderer = Cup.GetComponent<SpriteRenderer>();
        }

        public bool PlayCupFrame(float elapsed, ShiftPhase phase)
        {
            if (CupFrames == null || CupFrames.Length == 0 || cupRenderer == null) return false;

            // The first six GIF frames loop without spilling. Each source frame lasts 100 ms.
            int frame = phase switch
            {
                ShiftPhase.Surveillance => Mathf.FloorToInt(elapsed * 10) % Mathf.Min(6, CupFrames.Length),
                ShiftPhase.Incident => Mathf.FloorToInt(elapsed * 10) % CupFrames.Length,
                _ => 0
            };
            Sprite sprite = CupFrames[frame];
            if (sprite == null) return false;
            if (cupRenderer.sprite != sprite) cupRenderer.sprite = sprite;
            return true;
        }
        public static readonly Vector2 SecuritySpawn = new Vector2(-14, 6);
        public static readonly Vector2 IncidentSpawn = new Vector2(1, -8);
        public static readonly Vector2[] CameraPositions = { new Vector2(3, -8), new Vector2(3, 10), new Vector2(20, 7), new Vector2(20, -7) };
        public static readonly string[] CameraNames = { "카페 홀", "카운터", "창고", "숙직실" };
        public static readonly Vector2[] Centers = {
            new Vector2(-14,6), new Vector2(-14,-7), new Vector2(3,-8), new Vector2(20,7),
            new Vector2(20,-7), new Vector2(3,10), new Vector2(42,0), new Vector2(90,0),
            new Vector2(65,0), new Vector2(115,0), new Vector2(140,0)
        };
        public static readonly Vector2[] Sizes = {
            new Vector2(9,7), new Vector2(13,9), new Vector2(19,11), new Vector2(11,8),
            new Vector2(11,8), new Vector2(14,9), new Vector2(18,8), new Vector2(20,6),
            new Vector2(17,11), new Vector2(18,6), new Vector2(18,7)
        };
        public static readonly string[] RoomNames = { "감시실", "로비", "홀", "창고", "숙직실", "카운터", "촉수 복도", "추격 복도", "외부", "상부 우회로", "하부 우회로" };
        public static Vector2 RoomCenter(int layer) => Centers[Mathf.Clamp(layer-8,0,Centers.Length-1)];
        public static Rect Bounds(int layer) => new Rect(RoomCenter(layer)-Sizes[layer-8]/2, Sizes[layer-8]);
        public static int RoomLayer(Vector2 position)
        {
            for (int i=0;i<Centers.Length;i++)
                if (new Rect(Centers[i]-Sizes[i]/2,Sizes[i]).Contains(position)) return i+8;
            return 0;
        }
        public static int RoomMask(Vector2 position) => 1 | (1 << RoomLayer(position));
        public void ViewCctv(Camera camera, int index) => ViewRoom(camera, RoomLayer(CameraPositions[index]));
        public static void ViewRoom(Camera camera, int layer)
        {
            Vector2 position = RoomCenter(layer);
            camera.transform.SetPositionAndRotation(new Vector3(position.x, position.y, -25), Quaternion.identity);
            camera.orthographic = true;
            camera.cullingMask = 1 | (1 << layer);
            camera.orthographicSize = Mathf.Max(Sizes[layer-8].y * 0.55f, Sizes[layer-8].x / (16f/9f) * 0.55f);
        }
    }
}
