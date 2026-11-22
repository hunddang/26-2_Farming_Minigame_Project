using System;
using System.Linq;
using UnityEngine;

namespace NightCafe
{
    public sealed class NightRoutes : MonoBehaviour
    {
        public const int TentacleCorridor = 14;
        public const int PursuitCorridor = 15;
        public static bool IsHazardCorridor(int room) => room is TentacleCorridor or PursuitCorridor;

        [Serializable] public struct Portal
        {
            public CafeInteractable item, paired;
            public Vector2 target;
            [Tooltip("Optional scene marker. When assigned, its world position is the arrival point.")]
            public Transform arrival;
            public bool puzzle;
            public Vector2 ArrivalPosition => arrival != null ? (Vector2)arrival.position : target;
        }
        public GameObject routeObjects;
        public CafeInteractable hallDoor, detourDoor;
        public CafeInteractable[] entrances;
        public Portal[] portals;
        public Transform tentacleCamera, pursuer;
        public Transform[] tentacles;
        public DetourPuzzle Puzzle { get; private set; }
        public int TentacleHits { get; private set; }
        public int LastSafeRoom { get; private set; } = 9;
        private DetourPuzzle upper, lower;
        private CafeInteractable activeGate;
        private float pursuitBegins, nextHit;
        private bool hazards;

        public void Activate(CafeWorld world,int day)
        {
            upper=new DetourPuzzle((day-1)%DetourPuzzle.LayoutCount);
            lower=new DetourPuzzle((day+1)%DetourPuzzle.LayoutCount);
            Puzzle=upper; hazards=day>=3;
            routeObjects.SetActive(true);
            foreach (var arm in tentacles) arm.gameObject.SetActive(hazards);
            pursuer.gameObject.SetActive(false);
            world.Interactables.AddRange(entrances);
        }
        public bool IsDetourDoor(CafeInteractable item) => portals.Any(p=>p.item==item && p.puzzle && !p.item.isOpen);
        public void PreparePuzzle(CafeInteractable item)
        {
            activeGate=item;
            Puzzle = CafeWorld.RoomLayer(item.transform.position)==12 || CafeWorld.RoomLayer(item.transform.position)==18 ? lower : upper;
        }
        public void UnlockDetour()
        {
            foreach(var link in portals)
                if(link.item==activeGate) { link.item.SetDoor(true); link.paired.SetDoor(true); }
        }
        public Vector2 Destination(CafeInteractable item) => portals[item.taskIndex].ArrivalPosition;
        public bool Enter(CafeInteractable item,NightPlayer player)
        {
            if(item.nightAction!=NightAction.Travel || item.taskIndex<0 || item.taskIndex>=portals.Length) return false;
            var link=portals[item.taskIndex];
            Vector2 destination=link.ArrivalPosition;
            int from=CafeWorld.RoomLayer(player.PlanPosition), to=CafeWorld.RoomLayer(destination);
            if (IsHazardCorridor(to)) LastSafeRoom = from;
            item.SetDoor(true); link.paired.SetDoor(true);
            player.Teleport(destination);
            if (to == PursuitCorridor)
            {
                pursuer.position=destination + (destination.x<90 ? Vector2.left : Vector2.right)*0.85f;
                pursuitBegins=Time.time+0.85f;
            }
            return true;
        }
        public void SkipCorridor(NightPlayer player)
        {
            int room=CafeWorld.RoomLayer(player.PlanPosition);
            foreach(var link in portals)
            {
                if(CafeWorld.RoomLayer(link.item.transform.position)!=room || CafeWorld.RoomLayer(link.ArrivalPosition)==LastSafeRoom) continue;
                player.Teleport(link.ArrivalPosition); return;
            }
        }
        public void ViewTentacles(Camera camera) => CafeWorld.ViewRoom(camera, TentacleCorridor);

        public string Tick(NightPlayer player, Action<string> fail, bool suppressAttacks = false,
            bool tentaclesReleased = false, bool smokeCleared = false, Action tentacleHit = null)
        {
            if (!hazards) return "";

            Vector2 position=player.PlanPosition;
            int room=CafeWorld.RoomLayer(position);
            pursuer.gameObject.SetActive(!smokeCleared && room == PursuitCorridor);
            foreach(var arm in tentacles)
            {
                float phase=Mathf.Repeat(Time.time+arm.position.x*0.21f,4);
                float sweep=Mathf.Clamp01((phase-1.45f)/0.35f)*Mathf.Clamp01((2.95f-phase)/0.35f);
                arm.localRotation=Quaternion.Euler(0,0,tentaclesReleased ? -70 : -8+Mathf.Sin(phase)*8);
                arm.localScale=new Vector3(1,tentaclesReleased ? 0.28f : Mathf.Lerp(0.2f,1.4f,sweep),1);
                if (suppressAttacks || tentaclesReleased || room != TentacleCorridor || sweep < 0.8f ||
                    Time.time < nextHit || !TouchesTentacle(arm, position)) continue;
                TentacleHits++; nextHit=Time.time+1.2f;
                if(tentacleHit!=null) tentacleHit();
                else if(TentacleHits>=3) fail("세 번째 촉수에 붙잡혔다.");
                return $"촉수에 스쳤다. {TentacleHits}/3";
            }
            if (!pursuer.gameObject.activeSelf || suppressAttacks || Time.time < pursuitBegins) return "";
            Vector3 target=position;
            pursuer.position=Vector3.MoveTowards(pursuer.position,target,Time.deltaTime*5.35f);
            if(Vector3.Distance(pursuer.position,target)<0.56f) fail("추격자에게 붙잡혔다.");
            return "";
        }

        private static bool TouchesTentacle(Transform arm, Vector2 position)
        {
            Vector2 origin = arm.position;
            Vector2 length = (Vector2)arm.TransformPoint(0, 2.85f, 0) - origin;
            float fraction = Mathf.Clamp01(Vector2.Dot(position - origin, length) / length.sqrMagnitude);
            return (position - (origin + length * fraction)).sqrMagnitude <= 0.48f * 0.48f;
        }
    }
}
