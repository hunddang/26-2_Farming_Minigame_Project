using System.Collections;
using UnityEngine;

namespace NightCafe
{
    public sealed partial class NightGame
    {
        private void UseHuntTask(CafeInteractable item)
        {
            switch (item.storyAction)
            {
                case StoryAction.Leak:
                    if (!Fourth.LeakObserved) { Toast("먼저 감시실에서 로비 카메라를 확인하자."); return; }
                    if (Fourth.StartFirst()) StartCoroutine(DropHunter("긁는 소리가 멎었다. 물방울도 멈췄다.\n천장 덮개가 뒤틀리고 무언가 떨어졌다."));
                    break;
                case StoryAction.Extinguisher:
                    Fourth.CollectExtinguisher(); Toast("소화기를 챙겼다. 가까이 다가오는 괴물을 향해 E로 분사한다."); break;
                case StoryAction.Net:
                case StoryAction.Anchor:
                    Fourth.Collect(item.storyAction == StoryAction.Net);
                    Toast(Fourth.NotesRead ? "창고의 포획 장비를 챙겼다." : "먼저 전임자의 노트를 확인하자."); break;
                case StoryAction.Trap:
                    if (Fourth.Stage == HuntStage.Search && Fourth.TrapRoom == item.taskIndex)
                    {
                        if (Fourth.Recover()) Toast("그물과 고정구를 회수했다. 다른 표식에 설치할 수 있다.");
                    }
                    else if (Fourth.Install(item.taskIndex))
                    {
                        story.trap.position = item.transform.position; SetLayer(story.trap, item.taskIndex);
                        Toast("그물을 고정했다. 감시실 CCTV에서 낙하자의 위치를 찾아야 한다.", 6);
                        int room = item.taskIndex == 9 ? 10 : 9;
                        story.hunter.position = CafeWorld.RoomCenter(room); SetLayer(story.hunter, room);
                        story.hunter.gameObject.SetActive(true);
                    }
                    break;
            }
        }
        private void UpdateHunt()
        {
            Vector2 player = World.Player.PlanPosition; int room = CafeWorld.RoomLayer(player);
            if (Fourth.Stage == HuntStage.Leak && Fourth.LeakObserved && room == 9 &&
                Vector2.Distance(player, story.Find(StoryAction.Leak).transform.position) < 1.35f)
            {
                if (Fourth.StartFirst()) StartCoroutine(DropHunter("누수가 멎는 순간 천장에서 낙하자가 떨어졌다."));
                return;
            }
            if (Fourth.StartSecond(room))
            {
                secondChaseStart = player;
                StartCoroutine(DropHunter("천장 안에서 빠른 발소리가 들렸다.\n장비를 놓치지 말고 방의 반대편이나 출구로 달려야 한다."));
                return;
            }
            if (Fourth.Stage == HuntStage.Search && Fourth.SearchRoom == room && Vector2.Distance(player, story.hunter.position) < 5)
            {
                if (Fourth.StartThird(room)) { monsterGrace = Time.time + 1.2f; Toast("낙하자가 고개를 돌렸다. 설치한 함정으로 유인하자."); }
            }
            if (Fourth.Stage == HuntStage.SecondChase && (room != Fourth.TriggerRoom || Vector2.Distance(player, secondChaseStart) >= tuning.secondChaseDistance))
            {
                Fourth.EscapeSecond(); story.hunter.gameObject.SetActive(false);
                Toast("천장 안으로 물러났다. 노란 표식에 포획 장비를 설치할 수 있다.", 6);
            }
            if (Fourth.Chasing)
            {
                if ((room == 14 || room == 15) && !Fourth.CorridorScenePlayed)
                {
                    Fourth.CorridorScene(); StartCoroutine(MonsterConfrontation()); return;
                }
                if (Time.time >= monsterGrace && room >= 8)
                {
                    if (CafeWorld.RoomLayer(story.hunter.position) != room)
                    {
                        if (room == 8 && Fourth.Stage == HuntStage.ThirdChase)
                        { Fourth.LoseMonster(); story.hunter.position=CafeWorld.RoomCenter(Fourth.TrapRoom==9?10:9); SetLayer(story.hunter,Fourth.TrapRoom==9?10:9); Toast("낙하자를 놓쳤다. CCTV에서 위치를 다시 확인하자."); return; }
                        story.hunter.position = InsideRoom(player - World.Player.Facing * 3.3f, room);
                        SetLayer(story.hunter, room); monsterGrace = Time.time + 0.6f;
                    }
                    else
                    {
                        Vector2 next = CafeNavigation.NextStep(story.hunter.position, player, room, World.Player.gameObject);
                        story.hunter.position = Vector2.MoveTowards(story.hunter.position, next, Time.deltaTime * tuning.ceilingMonsterSpeed);
                    }
                    if (Vector2.Distance(story.hunter.position, player) < 0.55f && !(Fourth.Stage==HuntStage.ThirdChase && room==Fourth.TrapRoom && Vector2.Distance(story.hunter.position,story.trap.position)<=1.1f)) Fourth.Fail("낙하자가 팔을 붙잡고 천장으로 끌어올렸다.");
                }
                if (Fourth.Stage == HuntStage.ThirdChase && Fourth.Capture(CafeWorld.RoomLayer(story.hunter.position), Vector2.Distance(story.hunter.position, story.trap.position)))
                {
                    story.closedNet.transform.position = story.trap.position; SetLayer(story.closedNet.transform, Fourth.TrapRoom);
                    story.closedNet.SetActive(true); Toast("그물이 자동으로 닫혔다. 감시실로 돌아가자.", 6);
                }
            }
            if (room == 8) Fourth.Return();
            previousRoom = room;
        }
        private void UseSpray()
        {
            if (Fourth == null || !Fourth.Chasing) return;
            Vector2 delta = (Vector2)story.hunter.position - World.Player.PlanPosition;
            bool valid = delta.magnitude <= tuning.sprayRange && Vector2.Dot(World.Player.Facing, delta.normalized) > 0.12f;
            if (!Fourth.Spray(valid)) { Toast("가까운 괴물의 정면을 향해야 한다. 분말은 소모되지 않았다."); return; }
            story.powder.transform.position = story.hunter.position; SetLayer(story.powder.transform, CafeWorld.RoomLayer(World.Player.PlanPosition));
            powderUntil = Time.time + 1.2f; story.powder.SetActive(true); story.hunter.gameObject.SetActive(false);
            sound.PlayOneShot(clatter, 0.35f); Toast("분말을 뒤집어쓴 낙하자가 천장으로 물러났다. 지금은 안전하다.", 6);
        }
        private IEnumerator DropHunter(string message)
        {
            story.hunter.gameObject.SetActive(true);
            Vector2 basePosition = World.Player.PlanPosition + World.Player.Facing * 2.7f;
            var bounds = CafeWorld.Bounds(CafeWorld.RoomLayer(World.Player.PlanPosition));
            basePosition = new Vector2(Mathf.Clamp(basePosition.x, bounds.xMin + 0.8f, bounds.xMax - 0.8f), Mathf.Clamp(basePosition.y, bounds.yMin + 0.8f, bounds.yMax - 0.8f));
            story.hunter.position = basePosition + Vector2.up * 2; SetLayer(story.hunter, CafeWorld.RoomLayer(World.Player.PlanPosition));
            yield return Movie(message, tuning.cinematicSeconds, amount => story.hunter.position = basePosition + Vector2.up * (2 * (1 - amount)));
            monsterGrace = Time.time + 2;
        }
        private IEnumerator MonsterConfrontation()
        {
            Vector2 start = World.Player.PlanPosition;
            story.memoryActor.gameObject.SetActive(true); story.memoryActor.position = start + Vector2.right * 2;
            SetLayer(story.memoryActor, CafeWorld.RoomLayer(start));
            yield return Movie("복도를 막던 괴물이 낙하자에게 달려들었다.\n서로 맞붙은 틈에 반대편 출구로 빠져나간다.", tuning.cinematicSeconds,
                amount => story.memoryActor.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(amount * 30) * 20));
            story.memoryActor.gameObject.SetActive(false); Routes.SkipCorridor(World.Player);
            previousRoom = CafeWorld.RoomLayer(World.Player.PlanPosition); story.hunter.position = InsideRoom(World.Player.PlanPosition - Vector2.right * 3, previousRoom);
            SetLayer(story.hunter, previousRoom); monsterGrace = Time.time + 2;
            if (Fourth.Stage == HuntStage.SecondChase) Fourth.EscapeSecond();
        }
        private void RefreshHuntObjects()
        {
            story.leak.SetActive(Fourth.Stage == HuntStage.Leak);
            if(Fourth.Stage==HuntStage.Leak)
            {
                int index=0;
                foreach(Transform drop in story.leak.transform)
                    drop.localPosition=new Vector2(Mathf.Sin(index)*0.4f,1.5f-Mathf.Repeat(pulseClock*1.6f+index++*0.3f,1.5f));
                if(Time.time>=clatterAt && (overlay==Overlay.Cctv && cameraIndex==3 || Vector2.Distance(World.Player.PlanPosition,story.leak.transform.position)<9))
                { sound.PlayOneShot(scratch,0.35f);sound.PlayOneShot(clatter,0.15f);clatterAt=Time.time+2.5f; }
            }
            story.Find(StoryAction.Extinguisher).gameObject.SetActive(Fourth.Stage == HuntStage.FirstChase && !Fourth.HasExtinguisher);
            story.Find(StoryAction.Net).gameObject.SetActive(!Fourth.HasNet && Fourth.Stage == HuntStage.Equipment);
            story.Find(StoryAction.Anchor).gameObject.SetActive(!Fourth.HasAnchor && Fourth.Stage == HuntStage.Equipment);
            foreach (var marker in story.trapMarkers)
            {
                int room = marker.GetComponent<CafeInteractable>().taskIndex;
                marker.gameObject.SetActive(Fourth.Stage == HuntStage.InstallTrap || Fourth.Stage == HuntStage.Search && Fourth.TrapRoom == room);
                marker.localScale = Vector3.one * (1 + 0.12f * Mathf.Sin(Time.time * 3));
            }
            story.trap.gameObject.SetActive(Fourth.TrapRoom >= 0);
            if (Time.time > powderUntil) story.powder.SetActive(false);
            if (Fourth.Stage == HuntStage.ReadNotes || Fourth.Stage == HuntStage.Equipment || Fourth.Stage == HuntStage.InstallTrap)
                story.hunter.gameObject.SetActive(false);
        }
        private static Vector2 InsideRoom(Vector2 position, int room)
        {
            var bounds = CafeWorld.Bounds(room);
            return new Vector2(Mathf.Clamp(position.x,bounds.xMin+0.8f,bounds.xMax-0.8f),Mathf.Clamp(position.y,bounds.yMin+0.8f,bounds.yMax-0.8f));
        }
    }
}
