using UnityEngine;

namespace NightCafe
{
    public sealed class NightRoutes : MonoBehaviour
    {
        public GameObject routeObjects;
        public CafeInteractable hallDoor, detourDoor;
        public CafeInteractable[] entrances;
        public Transform tentacleCamera, pursuer;
        public Transform[] tentacles;
        public DetourPuzzle Puzzle { get; private set; }
        public int TentacleHits { get; private set; }

        private const float ExitY = 6.6f;
        private static readonly Vector2 LobbyHall = new Vector2(-8.25f, -4);
        private static readonly Vector2 Hall = new Vector2(-5.4f, -4);
        private static readonly Vector2 LobbyCounter = new Vector2(-11.5f, -7.1f);
        private static readonly Vector2 Counter = new Vector2(-0.3f, 6.7f);
        private static readonly Vector2 Warehouse = new Vector2(15f, 4.6f);
        private static readonly Vector2 LobbyService = new Vector2(-14.4f, -8.3f);
        private float pursuitBegins;
        private float nextHit;

        public void Activate(CafeWorld world, int day)
        {
            Puzzle = new DetourPuzzle((day - 1) % DetourPuzzle.LayoutCount);
            routeObjects.SetActive(true);
            world.Interactables.AddRange(entrances);
            hallDoor.SetDoor(false);
            detourDoor.SetDoor(false);
            hallDoor.title = "촉수 복도 입구";
            detourDoor.title = "쓰레기 처리 우회문";
        }

        public bool IsDetourDoor(CafeInteractable item) => item == detourDoor;

        public void UnlockDetour() => detourDoor.SetDoor(true);

        public bool Enter(CafeInteractable item, NightPlayer player)
        {
            var position = player.PlanPosition;
            if (item == hallDoor)
            {
                player.Teleport(new Vector2(40, position.x < -7 ? -ExitY : ExitY));
                return true;
            }
            switch (item.nightAction)
            {
                case NightAction.TentacleExit:
                    player.Teleport(item.taskIndex == 0 ? LobbyHall : Hall);
                    return true;
                case NightAction.ChaseEntrance:
                    int sign = item.taskIndex == 0 ? -1 : 1;
                    player.Teleport(new Vector2(48, sign * ExitY));
                    pursuer.position = new Vector3(48, sign * 8.5f, 0);
                    pursuitBegins = Time.time + 0.6f;
                    return true;
                case NightAction.ChaseExit:
                    player.Teleport(item.taskIndex == 0 ? LobbyCounter : Counter);
                    return true;
                case NightAction.ServiceDoor:
                    player.Teleport(item.taskIndex == 0 ? Warehouse : LobbyService);
                    return true;
                default: return false;
            }
        }

        public void ViewTentacles(Camera camera)
        {
            camera.transform.SetPositionAndRotation(tentacleCamera.position, tentacleCamera.rotation);
            camera.orthographicSize = 9.5f;
            camera.cullingMask = 1 << 14;
        }

        public string Tick(NightPlayer player, LaterShift shift)
        {
            Vector2 position = player.PlanPosition;
            int room = CafeWorld.RoomLayer(position);
            for (int i = 0; i < tentacles.Length; i++)
            {
                var arm = tentacles[i];
                float phase = Mathf.Repeat(Time.time + i * 1.1f, 4);
                float sweep = Mathf.Clamp01((phase - 1.45f) / 0.35f) * Mathf.Clamp01((2.95f - phase) / 0.35f);
                arm.localRotation = Quaternion.Euler(0, 0, -75 * sweep);
                if (room != 14 || sweep < 0.8f || Time.time < nextHit || Mathf.Abs(position.y - arm.position.y) > 0.48f) continue;
                TentacleHits++;
                nextHit = Time.time + 1.2f;
                if (TentacleHits >= 3)
                {
                    shift.Fail("세 번째 촉수에 붙잡혀 복도를 벗어나지 못했다.", NightFailure.Passage);
                    return shift.Failure;
                }
                return $"촉수에 스쳤다. {TentacleHits}/3";
            }
            if (room != 15 || Time.time < pursuitBegins) return "";
            Vector3 target = position;
            var direction = target - pursuer.position;
            if (direction.sqrMagnitude > 0.001f) pursuer.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
            pursuer.position = Vector3.MoveTowards(pursuer.position, target, Time.deltaTime * 5.35f);
            if (Vector3.Distance(pursuer.position, target) >= 0.56f) return "";
            shift.Fail("복도 끝에 닿기 전에 추격자에게 붙잡혔다.", NightFailure.Passage);
            return shift.Failure;
        }
    }
}
