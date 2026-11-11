using System;
using UnityEngine;
using UnityEngine.UI;

namespace NightCafe
{
    public sealed class NightContent : MonoBehaviour
    {
        [Serializable]
        public struct Seat
        {
            public CafeInteractable chair;
            public Transform table;
        }

        public GameObject menuNight, shadowNight, cupPickup, clothPickup, eraserPickup;
        public CafeInteractable menuBoard;
        public TextMesh menuWriting, chalkWriting;
        public Transform lobbyCctv, shadow, shadowHead, shadowEyes, warehouseMark, chalk, chalkboard;
        public Seat[] seats;
        public CafeInteractable[] menuTasks, shadowTasks;
        public NightRoutes routes;
        public Transform scratchHand;
        public GameObject menuSmear;
        public GameObject detourGrid;
        public Image[] detourTiles;
        public Text[] detourSymbols;
        public Text controlHelp, itemHelp;

        private const string ChalkMessage = "영업 중.\n손님이 남아 있습니다.";
        private Transform[] shadowParts;
        private Vector3 chalkHome;
        private Vector3 chalkHomeScale;
        private Vector3 chalkboardHome;
        private Vector3 shadowFlashPosition;
        private float flashUntil;
        private float writingBegan;
        private int shadowLayer;
        private int previousChairs;
        private bool previousResolved;
        private bool warehouseSeen;

        public bool ChalkIdle => Time.time >= flashUntil && Mathf.Repeat(Time.time - writingBegan, 5f) >= 2.6f;

        public void Activate(LaterShift shift, CafeWorld world)
        {
            world.SetFlood(0);
            world.Cup.gameObject.SetActive(false);
            foreach (var drop in world.CoffeeDrops) drop.gameObject.SetActive(false);
            foreach (var item in world.Interactables)
                if (item.kind == InteractionKind.Ingredient || item.kind == InteractionKind.Recipe)
                    item.gameObject.SetActive(false);

            menuNight.SetActive(shift.Day == 2);
            shadowNight.SetActive(shift.Day == 3);
            world.Interactables.AddRange(shift.Day == 2 ? menuTasks : shadowTasks);
            if (shift.Day == 2) return;

            foreach (var seat in seats) world.Interactables.Add(seat.chair);
            if (routes != null) routes.Activate(world, shift.Day);
            previousChairs = shift.ChairsLeft;
            shadowParts = shadow.GetComponentsInChildren<Transform>(true);
            chalkHome = chalk.localPosition;
            chalkHomeScale = chalk.localScale;
            chalkboardHome = chalkboard.localPosition;
            writingBegan = Time.time;
        }

        public bool AtFirstSite(LaterShift shift, Vector2 position)
        {
            if (shift.Day == 3) return CafeWorld.RoomLayer(position) == 10;
            return CafeWorld.RoomLayer(position) == 9 &&
                Vector2.Distance(position, CafeWorld.PlanPosition(menuBoard.transform.position)) < 3.8f;
        }

        public void PlaceChair(int index)
        {
            Seat seat = seats[index];
            var chair = seat.chair.transform;
            var obstacle = chair.GetComponent<Collider>();
            if (obstacle != null) obstacle.enabled = false;
            float side = chair.position.z > seat.table.position.z ? 1 : -1;
            chair.SetPositionAndRotation(seat.table.position + new Vector3(side * 0.48f, 2.16f, 0),
                Quaternion.Euler(180, side > 0 ? 180 : 0, 0));
            seat.chair.enabled = false;
        }

        public void Refresh(LaterShift shift, Vector2 player, bool remoteMenu)
        {
            if (shift.Phase == ShiftPhase.Failed) return;
            if (shift.Day == 2)
            {
                cupPickup.SetActive(shift.Water == WaterState.None);
                clothPickup.SetActive(!shift.HasCloth);
                bool cleared = shift.Phase == ShiftPhase.Resolved || shift.Phase == ShiftPhase.Complete;
                bool blurred = !cleared && (remoteMenu || shift.Phase == ShiftPhase.Surveillance);
                menuSmear.SetActive(blurred);
                menuWriting.gameObject.SetActive(!blurred);
                menuWriting.text = cleared ? "NIGHT CAFÉ" : "뒤돌아보지 마.";
                Color stain = new Color(0.91f, 0.24f, 0.17f);
                stain.a = Mathf.Lerp(0.5f, 1f, shift.WipesLeft / 4f);
                menuWriting.color = cleared ? Color.white : stain;
                return;
            }

            bool resolved = shift.Phase == ShiftPhase.Resolved || shift.Phase == ShiftPhase.Complete;
            if ((previousChairs > 0 && shift.ChairsLeft == 0) || (resolved && !previousResolved))
            {
                flashUntil = Time.time + 0.55f;
                shadowFlashPosition = shadow.position;
                if (!resolved) writingBegan = flashUntil;
            }
            previousChairs = shift.ChairsLeft;
            previousResolved = resolved;

            bool flash = Time.time < flashUntil;
            if (!flash && !resolved)
            {
                if (shift.Warehouse) SetShadowRoom(11, warehouseMark.position);
                else
                {
                    int index = FindSeat(shift, Mathf.FloorToInt(Time.time / 3.5f) % shift.ChairsLeft);
                    SetShadowRoom(10, seats[index].chair.transform.position + Vector3.up * 0.26f);
                }
            }
            if (flash) shadow.position = shadowFlashPosition;
            shadow.gameObject.SetActive(!resolved || flash);
            shadowEyes.gameObject.SetActive(flash);
            shadowHead.localRotation = Quaternion.Euler(0, 0, 12 + Mathf.Sin(Time.time * 2) * 13);
            shadow.localScale = new Vector3(1, shift.Warehouse || flash ? 1 : 0.66f + Mathf.PingPong(Time.time * 0.48f, 0.34f), 1);

            bool writing = shift.Warehouse && !shift.BoardErased && !resolved && !flash;
            float elapsed = Mathf.Repeat(Time.time - writingBegan, 5f);
            float amount = Mathf.Clamp01(elapsed / 2.6f);
            chalkWriting.text = resolved ? "마감 완료" : writing ? ChalkMessage.Substring(0, Mathf.CeilToInt(ChalkMessage.Length * amount)) : "";
            if (writing)
            {
                float line = Mathf.Min(amount * 2, 1.99f);
                float x = Mathf.Lerp(-0.91f, 0.91f, line % 1);
                chalk.position = chalkboard.TransformPoint(new Vector3(x, line < 1 ? 1.66f : 1.35f, -0.23f));
            }
            else chalk.localPosition = chalkHome;
            chalk.localScale = chalkHomeScale;
            eraserPickup.SetActive(!shift.HasEraser);
            if (!warehouseSeen && shift.Warehouse && CafeWorld.RoomLayer(player) == 11)
            {
                warehouseSeen = true;
                writingBegan = Time.time;
                flashUntil = Time.time + 0.45f;
                shadowFlashPosition = warehouseMark.position;
                SetShadowRoom(11, warehouseMark.position);
            }
        }

        public void AnimateFailure(NightFailure kind, Transform camera, Vector3 player, float amount)
        {
            switch (kind)
            {
                case NightFailure.Menu:
                    if (scratchHand == null) return;
                    scratchHand.gameObject.SetActive(amount < 1);
                    scratchHand.SetPositionAndRotation(
                        camera.TransformPoint(new Vector3(Mathf.Lerp(1.15f, -0.06f, amount), Mathf.Lerp(-0.7f, 0.04f, amount), 1.75f)),
                        camera.rotation * Quaternion.Euler(0, 0, 38));
                    break;
                case NightFailure.Chairs:
                    foreach (var seat in seats)
                    {
                        var chair = seat.chair.transform;
                        chair.position = Vector3.MoveTowards(chair.position, player + Vector3.up * 1.4f, Time.deltaTime * 17);
                        chair.Rotate(190 * Time.deltaTime, 0, 270 * Time.deltaTime);
                    }
                    break;
                case NightFailure.Chalk:
                    chalkboard.localPosition = chalkboardHome + Vector3.down * (0.26f * amount);
                    chalkboard.localRotation = Quaternion.Euler(-85 * amount, 0, 0);
                    chalk.position = Vector3.MoveTowards(chalk.position, player + Vector3.up * 0.65f, Time.deltaTime * 17);
                    break;
            }
        }

        private void SetShadowRoom(int layer, Vector3 position)
        {
            if (shadowLayer != layer)
            {
                foreach (var part in shadowParts) part.gameObject.layer = layer;
                shadowLayer = layer;
            }
            shadow.position = position;
        }

        private static int FindSeat(LaterShift shift, int rank)
        {
            for (int i = 0; i < shift.Stacked.Length; i++)
                if (!shift.Stacked[i] && rank-- == 0) return i;
            return 0;
        }
    }
}
