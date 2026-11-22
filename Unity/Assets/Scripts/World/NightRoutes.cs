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
        public Transform pursuer;
        public Transform[] tentacles;
        public SpriteRenderer[] tentacleWarnings;
        public DetourPuzzle Puzzle { get; private set; }
        public int TentacleHits { get; private set; }
        public int LastSafeRoom { get; private set; } = 9;
        private DetourPuzzle upper, lower;
        private CafeInteractable activeGate;
        private float nextHit, nextPursuitStep;
        private Vector2 pursuitWaypoint;
        private bool chaseStarted, enteredFromLeft;
        private bool hazards;

        public void Activate(CafeWorld world,int day)
        {
            upper=new DetourPuzzle((day-1)%DetourPuzzle.LayoutCount);
            lower=new DetourPuzzle((day+1)%DetourPuzzle.LayoutCount);
            Puzzle=upper; hazards=day>=3;
            routeObjects.SetActive(true);
            foreach (var arm in tentacles) arm.gameObject.SetActive(hazards);
            foreach (var warning in tentacleWarnings) warning.enabled = false;
            pursuer.gameObject.SetActive(false);
            world.Interactables.AddRange(entrances);
        }
        public bool IsDetourDoor(CafeInteractable item) => portals.Any(p=>p.item==item && p.puzzle && !p.item.isOpen);
        public void PreparePuzzle(CafeInteractable item)
        {
            activeGate=item;
            Puzzle = DetourRoom(portals[item.taskIndex]) == 18 ? lower : upper;
        }
        public void UnlockDetour()
        {
            int room = DetourRoom(portals[activeGate.taskIndex]);
            foreach (var link in portals)
                if (link.puzzle && DetourRoom(link) == room) link.item.SetDoor(true);
        }
        private static int DetourRoom(Portal portal)
            => portal.item.gameObject.layer is 17 or 18 ? portal.item.gameObject.layer : portal.paired.gameObject.layer;
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
                enteredFromLeft = destination.x < CafeWorld.RoomCenter(PursuitCorridor).x;
                pursuer.position = destination + (enteredFromLeft ? Vector2.left : Vector2.right) * .85f;
                chaseStarted = false;
                nextPursuitStep = 0;
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
        public string Tick(NightPlayer player, Action<string> fail, bool suppressAttacks = false,
            bool tentaclesReleased = false, bool smokeCleared = false, Action tentacleHit = null)
        {
            if (!hazards) return "";

            Vector2 position=player.PlanPosition;
            int room=CafeWorld.RoomLayer(position);
            pursuer.gameObject.SetActive(!smokeCleared && room == PursuitCorridor);
            string warningText = "";
            for (int index = 0; index < tentacles.Length; index++)
            {
                var arm = tentacles[index];
                float phase=Mathf.Repeat(Time.time+arm.position.x*0.21f,4);
                float sweep=Mathf.Clamp01((phase-1.45f)/0.35f)*Mathf.Clamp01((2.95f-phase)/0.35f);
                arm.localRotation=Quaternion.Euler(0,0,tentaclesReleased ? -70 : Mathf.Sin(phase)*4);
                arm.localScale=new Vector3(1,tentaclesReleased ? 0.28f : Mathf.Lerp(0.2f,2.47f,sweep),1);
                var warning = tentacleWarnings[index];
                warning.enabled = !tentaclesReleased && sweep > .08f;
                warning.color = new Color(.53f, .16f, .25f, sweep * .32f);
                if (suppressAttacks || tentaclesReleased || room != TentacleCorridor || sweep < 0.8f ||
                    Time.time < nextHit || Mathf.Abs(position.x - arm.position.x) >= 1f) continue;
                TentacleHits++; nextHit=Time.time+1.2f;
                if(tentacleHit!=null) tentacleHit();
                else if(TentacleHits>=3) fail("세 번째 촉수에 붙잡혔다.");
                warningText = $"촉수에 스쳤다. {TentacleHits}/3";
            }
            if (!pursuer.gameObject.activeSelf || suppressAttacks) return warningText;
            Rect bounds = CafeWorld.Bounds(PursuitCorridor);
            chaseStarted |= enteredFromLeft ? position.x >= bounds.xMin + 3 : position.x <= bounds.xMax - 3;
            if (!chaseStarted) return warningText;
            if (Time.time >= nextPursuitStep || Vector2.Distance(pursuer.position, pursuitWaypoint) < .06f)
            {
                pursuitWaypoint = CafeNavigation.NextStep(pursuer.position, position, PursuitCorridor, player.gameObject);
                nextPursuitStep = Time.time + .14f;
            }
            pursuer.position = Vector2.MoveTowards(pursuer.position, pursuitWaypoint, Time.deltaTime * 4.85f);
            if (Vector2.Distance(pursuer.position, position) < .56f) fail("추격자에게 붙잡혔다.");
            return warningText;
        }
    }
}
