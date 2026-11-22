using System;
using System.Linq;
using UnityEngine;

namespace NightCafe
{
    public sealed class NightRoutes : MonoBehaviour
    {
        [Serializable] public struct Portal
        {
            public CafeInteractable item, paired;
            public Vector2 target;
            public bool puzzle;
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
        public Vector2 Destination(CafeInteractable item) => portals[item.taskIndex].target;
        public bool Enter(CafeInteractable item,NightPlayer player)
        {
            if(item.nightAction!=NightAction.Travel || item.taskIndex<0 || item.taskIndex>=portals.Length) return false;
            var link=portals[item.taskIndex];
            int from=CafeWorld.RoomLayer(player.PlanPosition), to=CafeWorld.RoomLayer(link.target);
            if(to==14 || to==15) LastSafeRoom=from;
            item.SetDoor(true); link.paired.SetDoor(true);
            player.Teleport(link.target);
            if(to==15)
            {
                pursuer.position=link.target + (link.target.x<90 ? Vector2.left : Vector2.right)*0.85f;
                pursuitBegins=Time.time+0.85f;
            }
            return true;
        }
        public void SkipCorridor(NightPlayer player)
        {
            int room=CafeWorld.RoomLayer(player.PlanPosition);
            foreach(var link in portals)
            {
                if(CafeWorld.RoomLayer(link.item.transform.position)!=room || CafeWorld.RoomLayer(link.target)==LastSafeRoom) continue;
                player.Teleport(link.target); return;
            }
        }
        public void ViewTentacles(Camera camera) => CafeWorld.ViewRoom(camera,14);
        public string Tick(NightPlayer player,Action<string> fail,bool tentaclesSafe=false,bool pursuitSafe=false,Action tentacleHit=null)
        {
            Vector2 position=player.PlanPosition;
            int room=CafeWorld.RoomLayer(position);
            foreach(var arm in tentacles)
            {
                float phase=Mathf.Repeat(Time.time+arm.position.x*0.21f,4);
                float sweep=Mathf.Clamp01((phase-1.45f)/0.35f)*Mathf.Clamp01((2.95f-phase)/0.35f);
                arm.localRotation=Quaternion.Euler(0,0,tentaclesSafe ? -70 : -8+Mathf.Sin(phase)*8);
                arm.localScale=new Vector3(1,tentaclesSafe ? 0.28f : Mathf.Lerp(0.2f,1.4f,sweep),1);
                if(!hazards || tentaclesSafe || room!=14 || sweep<0.8f || Time.time<nextHit || Mathf.Abs(position.x-arm.position.x)>0.48f || position.y>arm.position.y+2.85f*arm.localScale.y+0.3f) continue;
                TentacleHits++; nextHit=Time.time+1.2f;
                if(tentacleHit!=null) tentacleHit();
                else if(TentacleHits>=3) fail("세 번째 촉수에 붙잡혔다.");
                return $"촉수에 스쳤다. {TentacleHits}/3";
            }
            pursuer.gameObject.SetActive(hazards && !pursuitSafe && room==15);
            if(!hazards || pursuitSafe || room!=15 || Time.time<pursuitBegins) return "";
            Vector3 target=position;
            pursuer.position=Vector3.MoveTowards(pursuer.position,target,Time.deltaTime*5.35f);
            if(Vector3.Distance(pursuer.position,target)<0.56f) fail("추격자에게 붙잡혔다.");
            return "";
        }
    }
}
