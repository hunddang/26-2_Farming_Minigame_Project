using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace NightCafe
{
    // Opt-in acceptance run uses the same interactions, input and scene transitions as play.
    public sealed class FoundationSmokeTest : MonoBehaviour
    {
        public NightGame game;
        private Keyboard keyboard;
        private Mouse mouse;
        private readonly List<string> failures=new List<string>();
        private int checks;
        private float began;
        public static string OutputPath(string name)
        {
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-testArtifacts");
            string path=index>=0 && index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"FoundationQA");
            Directory.CreateDirectory(path);return Path.Combine(path,name);
        }
        private void OnEnable()=>Application.logMessageReceived+=OnLog;
        private void OnDisable()=>Application.logMessageReceived-=OnLog;
        private void OnLog(string message,string trace,LogType type)
        {
            if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) failures.Add(message+"\n"+trace);
        }
        private void Check(bool valid,string message)
        {
            checks++;if(!valid) failures.Add(message);
            File.WriteAllText(OutputPath("progress.txt"),$"Check {checks}: {message} — {(valid?"PASS":"FAIL")}\n"+string.Join("\n",failures));
        }
        private void Update()
        {
            if(began>0 && Time.realtimeSinceStartup-began>240) Finish("Timed out while running the five-night acceptance flow.");
        }
        private void Finish(string extra="")
        {
            if(extra.Length>0) failures.Add(extra);
            File.WriteAllText(OutputPath("player-smoke-test.txt"),failures.Count==0?$"PASS: {checks} five-night UI, physics, route, retry, puzzle, pursuit and ending checks.\n":string.Join("\n\n",failures));
            Application.Quit(failures.Count==0?0:1);
        }
        private CafeInteractable Item(InteractionKind kind)=>game.World.Interactables.First(i=>i.kind==kind && i.isActiveAndEnabled);
        private CafeInteractable Task(NightAction action)=>game.World.Interactables.First(i=>i.nightAction==action && i.isActiveAndEnabled);
        private CafeInteractable Story(StoryAction action,int index=0)=>game.World.Interactables.First(i=>i.storyAction==action && i.taskIndex==index && i.isActiveAndEnabled);
        private void Use(StoryAction action,int index=0)=>game.Interact(Story(action,index));
        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
        }
        private IEnumerator Pointer(Vector2 position, MouseButton? button = null)
        {
            var state = new MouseState { position = position };
            if (button.HasValue) state = state.WithButton(button.Value);
            InputSystem.QueueStateEvent(mouse, state);
            yield return null;
            yield return null;
        }
        private Vector2 SlotPoint(int index) => SlotPoint(game.view.inventory.cells[index]);
        private static Vector2 SlotPoint(InventoryCell slot)
        {
            var rect = slot.frame.rectTransform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }
        private IEnumerator RightClickSlot(int index) => RightClickSlot(game.view.inventory.cells[index]);
        private IEnumerator RightClickSlot(InventoryCell slot)
        {
            var point = SlotPoint(slot);
            yield return Pointer(point, MouseButton.Right);
            yield return Pointer(point);
        }
        private IEnumerator DragSlot(int source, Vector2 destination)
        {
            var point = SlotPoint(source);
            yield return Pointer(point, MouseButton.Left);
            yield return Pointer(Vector2.Lerp(point, destination, .5f), MouseButton.Left);
            yield return Pointer(destination, MouseButton.Left);
            yield return Pointer(destination);
        }
        private IEnumerator Equip(InventoryItem item)
        {
            if (game.HeldItem == item || game.EquippedItem == item) yield break;
            yield return Tap(Key.Tab);
            int slot = Array.FindIndex(game.view.inventory.cells, cell => game.InventoryItemAt(cell.Index) == item);
            Check(slot >= 0, $"Collected {item} is in the inventory");
            if (slot >= 0) yield return RightClickSlot(slot);
            Check(game.HeldItem == item || game.EquippedItem == item, $"Right click equips {item}");
            yield return Tap(Key.Tab);
        }
        private IEnumerator ReadDocument(InventoryItem item)
        {
            var held = game.HeldItem;
            var equipped = game.EquippedItem;
            yield return Tap(Key.Tab);
            int slot = Array.FindIndex(game.view.inventory.cells, cell => game.InventoryItemAt(cell.Index) == item);
            Check(slot >= 0, $"Collected {item} has an inventory cell");
            if (slot < 0) { yield return Tap(Key.Tab); yield break; }
            yield return RightClickSlot(slot);
            Check(!game.view.inventory.gameObject.activeSelf &&
                (game.view.reading.activeSelf || game.view.rules.activeSelf) &&
                game.InventoryItemAt(slot) == item && game.HeldItem == held && game.EquippedItem == equipped,
                $"Right-clicking {item} opens the document and leaves the inventory and equipped items in place");
        }
        private IEnumerator CheckNotebook()
        {
            var notebook = Item(InteractionKind.RuleBook);
            game.World.Player.Teleport((Vector2)notebook.transform.position + Vector2.down * .9f);
            yield return new WaitForSeconds(.1f);
            Check(game.CurrentTarget == notebook, "The former employee's notebook can be picked up at the desk");
            yield return Tap(Key.Space);
            Check(!notebook.gameObject.activeSelf && game.view.rules.activeSelf, "Picking up the notebook removes it from the desk and opens it");
            yield return Tap(Key.Space);
            Check(game.view.rules.activeSelf, "Space leaves the rulebook open");
            yield return Tap(Key.Escape);
            Check(!game.view.rules.activeSelf, "Escape closes the rulebook");
            yield return ReadDocument(InventoryItem.RuleBook);
            yield return Tap(Key.Escape);
        }
        private IEnumerator CheckFlashlightEquipment()
        {
            var pickup = game.World.FlashlightPickup;
            Check(pickup != null && pickup.isActiveAndEnabled && !game.story.darkness.enabled &&
                !game.World.Player.FlashlightOn, "Security is fully lit and the player starts without a flashlight");
            if (pickup == null) yield break;
            yield return Tap(Key.F);
            Check(!game.World.Player.FlashlightOn, "F does not turn on a flashlight the player has not equipped");
            game.World.Player.Teleport((Vector2)pickup.transform.position + Vector2.down * .9f);
            yield return new WaitForSeconds(.1f);
            Check(game.CurrentTarget == pickup, "The flashlight is within reach from the front of the security desk");
            yield return Tap(Key.Space);
            Check(!pickup.gameObject.activeSelf && !game.World.Player.FlashlightOn, "Pickup puts the flashlight in the bag");
            yield return Tap(Key.Tab);
            int slot = Array.FindIndex(game.view.inventory.cells, cell => game.InventoryItemAt(cell.Index) == InventoryItem.Flashlight);
            Check(slot >= 0, "The flashlight has a grid slot");
            if (slot >= 0)
            {
                yield return DragSlot(slot, SlotPoint(7));
                Check(game.InventoryItemAt(7) == InventoryItem.Flashlight, "The flashlight moves between grid slots");
                yield return RightClickSlot(7);
                Check(game.EquippedItem == InventoryItem.Flashlight && game.HeldItem == InventoryItem.None &&
                    game.InventoryItemAt(7) == InventoryItem.None && game.World.Player.FlashlightOn &&
                    game.view.inventory.equipment.icon.enabled, "Equipping transfers the flashlight to the equipment HUD and powers it on");
                yield return RightClickSlot(game.view.inventory.equipment);
                Check(game.EquippedItem == InventoryItem.None && game.InventoryItemAt(7) == InventoryItem.Flashlight &&
                    !game.World.Player.FlashlightOn, "Right-clicking the equipment HUD returns the flashlight and turns it off");
                yield return RightClickSlot(7);
            }
            yield return Tap(Key.Tab);
            game.World.Player.Teleport(CafeWorld.RoomCenter(9));
            yield return null;
            yield return null;
            Check(game.story.darkness.enabled && game.story.darkness.material.GetFloat("_On") == 1 &&
                game.story.darkness.material.GetVector("_DoorLamp").z == 1, "Lobby shows equipped light and the independent red door lamp");
            yield return Tap(Key.F);
            Check(game.story.darkness.material.GetFloat("_On") == 0 &&
                game.story.darkness.material.GetVector("_DoorLamp").z == 1, "Powering off leaves the red door lamp visible");
            yield return Tap(Key.F);
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);
            yield return null;
            yield return null;
            Check(!game.story.darkness.enabled && game.World.Player.FlashlightOn, "Security lighting stays bright with the flashlight equipped");
        }
        private IEnumerator CheckInventoryPause(Func<float> clock)
        {
            yield return Tap(Key.Tab);
            float time = Time.time, remaining = clock();
            var position = game.World.Player.PlanPosition;
            var cup = game.World.Cup.position;
            yield return Tap(Key.W);
            yield return new WaitForSecondsRealtime(.15f);
            Check(game.view.inventory.gameObject.activeInHierarchy && Time.timeScale == 0 && AudioListener.pause &&
                clock() == remaining && Time.time == time && game.World.Player.PlanPosition == position &&
                game.World.Cup.position == cup, $"Night {game.Day} inventory freezes time, world movement and audio");
            yield return Tap(Key.Escape);
            yield return new WaitForSeconds(.1f);
            Check(!game.view.inventory.gameObject.activeSelf && Time.timeScale == 1 && !AudioListener.pause &&
                clock() < remaining, $"Night {game.Day} resumes after closing inventory");
        }
        private IEnumerator SprintTo(float coordinate, bool horizontal)
        {
            float Position() => horizontal ? game.World.Player.PlanPosition.x : game.World.Player.PlanPosition.y;
            float direction = Mathf.Sign(coordinate - Position());
            Key key = horizontal ? direction > 0 ? Key.D : Key.A : direction > 0 ? Key.W : Key.S;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key, Key.LeftShift));
            float deadline = Time.time + 3;
            while (direction * (coordinate - Position()) > .12f && !game.InputBlocked && Time.time < deadline)
                yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Check(Mathf.Abs(coordinate - Position()) < .48f, "Running reaches the next clear lane around the cabinet");
        }
        private IEnumerator CheckCorridorCollision(NightRoutes routes)
        {
            string room = CafeWorld.RoomNames[NightRoutes.PursuitCorridor - 8];
            var corridor = routes.routeObjects.transform.Find(room);
            var shelf = corridor.Find("Service cabinet A");
            var spriteBounds = shelf.GetComponent<SpriteRenderer>().bounds;
            var footprint = shelf.GetComponent<Collider2D>().bounds;
            var returnPosition = game.World.Player.PlanPosition;
            float left = spriteBounds.min.x - .65f;
            game.World.Player.Teleport(new Vector2(left, spriteBounds.center.y));
            yield return SprintTo(spriteBounds.max.x + .65f, true);

            game.World.Player.Teleport(new Vector2(left, footprint.center.y));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.LeftShift));
            yield return new WaitForSeconds(.45f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            float x = game.World.Player.PlanPosition.x;
            Check(x > left + .1f && x <= footprint.min.x - .25f,
                "The same shelf still blocks movement through its floor footprint");

            float floorEdge = corridor.Find("Rear wall facing").GetComponent<SpriteRenderer>().bounds.min.y;
            game.World.Player.Teleport(new Vector2(left, floorEdge - .7f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            yield return new WaitForSeconds(.45f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            float y = game.World.Player.PlanPosition.y;
            Check(y > floorEdge - .6f && y <= floorEdge - .25f,
                "Sprinting north stops at the visible rear wall, with the player's feet on the floor");
            game.World.Player.Teleport(returnPosition);
        }
        private IEnumerator CheckFurnitureSorting(NightRoutes routes)
        {
            var player = game.World.Player;
            var appearance = player.GetComponentInChildren<SpriteRenderer>();
            var returnPosition = player.PlanPosition;
            int standingOrder = appearance.sortingOrder;
            string room = CafeWorld.RoomNames[NightRoutes.PursuitCorridor - 8];
            var corridor = routes.routeObjects.transform.Find(room);
            var polygon = corridor.Find("Service cabinet B").GetComponent<PolygonCollider2D>();
            Check(polygon != null && polygon.pathCount == 1 && polygon.GetPath(0).Length == 4 && polygon.useDelaunayMesh,
                "Cabinet B keeps the four-point collider from Play Mode");

            var furniture = new[]
            {
                corridor.Find("Service cabinet A"), corridor.Find("Service cabinet B"), corridor.Find("Service cabinet C"),
                game.World.transform.Find("Storage/Furniture/Storage shelf"),
                game.World.transform.Find("Night Room/Furniture/Staff locker")
            };
            foreach (var item in furniture)
            {
                var footprint = item.GetComponent<Collider2D>().bounds;
                player.Teleport(new Vector2(footprint.center.x, footprint.max.y + .35f));
                yield return null;
                yield return null;
                var front = item.GetComponentsInChildren<SpriteRenderer>().Where(sprite => sprite.enabled).ToArray();
                Check(front.Any(sprite => sprite.bounds.Intersects(appearance.bounds)) &&
                    front.All(sprite => appearance.sortingOrder < sprite.sortingOrder),
                    $"Furniture draws over the player from behind: {item.name}");
            }

            var cabinet = furniture[0].GetComponent<Collider2D>().bounds;
            player.Teleport(new Vector2(cabinet.center.x, cabinet.min.y - .4f));
            yield return null;
            yield return null;
            Check(appearance.sortingOrder == standingOrder &&
                appearance.sortingOrder > furniture[0].GetComponent<SpriteRenderer>().sortingOrder,
                "The player is drawn in front when standing in front of cabinet A");
            player.Teleport(returnPosition);
            yield return null;
        }
        private void CheckRoomWalls(NightRoutes routes)
        {
            var walls = game.World.GetComponentsInChildren<SpriteRenderer>()
                .Concat(routes.routeObjects.GetComponentsInChildren<SpriteRenderer>())
                .Where(sprite => sprite.name == "Rear wall facing").ToArray();
            Check(walls.Length == CafeWorld.RoomNames.Length, "Every room has a rear wall facing");
            foreach (var facing in walls)
            {
                int room = facing.gameObject.layer;
                var bounds = facing.bounds;
                var blocker = facing.transform.parent.GetComponentsInChildren<BoxCollider2D>()
                    .Single(wall => wall.name == "Wall" && Mathf.Abs(wall.transform.position.y - bounds.max.y) < .005f);
                Check(Mathf.Abs(blocker.bounds.min.y - bounds.min.y) < .005f &&
                    blocker.bounds.min.x <= bounds.min.x && blocker.bounds.max.x >= bounds.max.x,
                    $"The rear wall covers the visible wall in room {room}");
            }
        }
        private IEnumerator Number(string value)
        {
            foreach(char digit in value) yield return Tap(digit=='0'?Key.Digit0:(Key)((int)Key.Digit1+digit-'1'));
            yield return Tap(Key.Enter);
        }
        private IEnumerator Ready()
        {
            float deadline=Time.realtimeSinceStartup+25;
            while(game!=null && game.StoryBusy && Time.realtimeSinceStartup<deadline) yield return null;
            yield return null;yield return null;
        }
        private IEnumerator Next(int day)
        {
            var previous=game;game.view.completeButtons[0].onClick.Invoke();
            float deadline=Time.realtimeSinceStartup+10;
            while(Time.realtimeSinceStartup<deadline)
            {
                var next=FindFirstObjectByType<NightGame>();
                if(next!=null && next!=previous && next.Day==day)
                {
                    game=next;yield return null;yield return null;
                    Check(!game.World.Interactables.Any(item => item.kind == InteractionKind.RuleBook && item.gameObject.activeSelf),
                        $"The collected notebook stays off the security desk on night {day}");
                    yield return CheckInventoryPause(() => day switch
                    {
                        4 => game.Fourth.Remaining, 5 => game.Fifth.Remaining, _ => game.Later.Remaining
                    });
                    yield break;
                }
                yield return null;
            }
            Check(false,"Expected next night "+day);
        }
        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(OutputPath(name+".png"));yield return null;
        }
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);began=Time.realtimeSinceStartup;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard=InputSystem.AddDevice<Keyboard>();
            mouse=InputSystem.AddDevice<Mouse>();
            yield return new WaitForSeconds(0.5f);
            Check(Application.isEditor || (Screen.width==1920 && Screen.height==1080),"FHD window");
            Check(UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings==2,"The two scene entry points remain playable");
            Check(game.mapCamera.orthographic && game.feedCamera.orthographic,"2D cameras");
            Check(game.World.Player.GetComponent<Rigidbody2D>()!=null,"2D movement and collisions");
            Check(game.view.inventory.cells.Length == 8 && game.mapCamera.rect == new Rect(0,0,1,1) &&
                game.view.transform.Find("Header") == null && game.view.transform.Find("Shift Record") == null &&
                game.view.transform.Find("Inventory") == null, "Full-screen play area and eight slots replace the marked HUD");
            yield return CheckFlashlightEquipment();
            yield return CheckNotebook();
            var roomRoutes = FindFirstObjectByType<NightRoutes>();
            yield return CheckCorridorCollision(roomRoutes);
            yield return CheckFurnitureSorting(roomRoutes);
            CheckRoomWalls(roomRoutes);
            Check(!game.World.GetComponentsInChildren<UnityEngine.UI.Text>(true)
                .Concat(roomRoutes.routeObjects.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                .Any(label => CafeWorld.RoomNames.Contains(label.text)), "Rooms and shortcuts show no world-space room captions");
            CheckReachability();
            game.World.Player.Teleport(new Vector2(-15,6.5f));yield return new WaitForSeconds(0.1f);
            Check(game.CurrentTarget!=null && game.CurrentTarget.kind==InteractionKind.Camera,"Security CCTV can be reached");
            yield return Tap(Key.Space);yield return new WaitForSeconds(0.1f);
            Check(game.ActiveRoomCamera == game.roomCameras[CafeWorld.CameraChannel(8)], "CCTV opens on the security room");
            Check(game.view.cameraButtons.Length == 11 && game.view.cctv.GetComponentsInChildren<Transform>()
                .Any(item => item.name == "Desk Post-it"), "All map nodes and the desk Post-it are present on the monitor");
            for(int i=0;i<CafeWorld.CameraRooms.Length;i++)
            {
                game.view.cameraButtons[i].onClick.Invoke();yield return new WaitForEndOfFrame();
                var selected = game.ActiveRoomCamera;
                Check(selected != null && selected.enabled && selected == game.roomCameras[i] &&
                    game.view.feedImage.texture == selected.targetTexture &&
                    CafeWorld.RoomLayer(selected.transform.position) == CafeWorld.CameraRooms[i] &&
                    game.roomCameras.Count(camera => camera.enabled) == 1, "Map button switches to the real room camera "+i);
            }
            var location=game.World.Player.PlanPosition;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));yield return new WaitForSeconds(0.15f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            Check(Vector2.Distance(location,game.World.Player.PlanPosition)<0.02f,"CCTV consumes movement input");
            game.SelectCamera(0);
            var cupRenderer = game.World.Cup.GetComponent<SpriteRenderer>();
            Sprite firstCupFrame = cupRenderer.sprite;
            yield return new WaitForSeconds(0.15f);
            Check(cupRenderer.sprite != firstCupFrame && Array.IndexOf(game.World.CupFrames, cupRenderer.sprite) >= 0,
                "The real CCTV renders the floating cup flipbook before the incident");
            yield return Shot("02-cafe-cctv");game.CloseOverlay();
            Check(game.roomCameras.All(camera => !camera.enabled) && !game.feedCamera.enabled && game.view.feedImage.texture==null,
                "CCTV closes without remote observation");
            yield return null;
            yield return Tap(Key.Space);
            Check(game.ActiveRoomCamera == game.roomCameras[CafeWorld.CameraChannel(8)], "Reopening CCTV returns to the security room");
            game.CloseOverlay();
            game.World.Player.Teleport(CafeWorld.IncidentSpawn);yield return null;yield return null;
            Check(game.Session.Phase==ShiftPhase.Incident,"Hall entry begins the espresso incident");
            yield return CheckInventoryPause(() => game.Session.Remaining);
            var spillDeadline = Time.time + 2.3f;
            while (Array.IndexOf(game.World.CupFrames, cupRenderer.sprite) < 17 && Time.time < spillDeadline)
                yield return null;
            Check(Array.IndexOf(game.World.CupFrames, cupRenderer.sprite) >= 17,"The GIF reaches its spilling frames in the hall");
            yield return Shot("02b-cup-spill");
            bool IngredientConsumed(Ingredient ingredient) =>
                !game.Session.Inventory[(int)ingredient] && game.Session.SelectedSlot == -1 &&
                game.HeldItem == InventoryItem.None && !game.view.inventory.hand.icon.enabled &&
                game.view.inventory.cells.All(cell => game.InventoryItemAt(cell.Index) != InventoryItem.Milk + (int)ingredient);
            var milk = game.World.Interactables.First(i => i.kind == InteractionKind.Ingredient && i.ingredient == Ingredient.Milk);
            game.Interact(Item(InteractionKind.Cup));
            game.Interact(milk);
            Check(game.HeldItem == InventoryItem.None && game.Session.SelectedSlot == -1, "Pickup does not equip the ingredient");
            yield return Equip(InventoryItem.Milk);
            game.Interact(Item(InteractionKind.Cup));yield return new WaitForSeconds(0.9f);
            Check(game.Session.Mistakes == 1 && IngredientConsumed(Ingredient.Milk),
                "An ingredient poured in the wrong order disappears from the hand and the bag");
            game.Interact(Item(InteractionKind.Recipe));float before=game.Session.Remaining;
            yield return new WaitForSeconds(0.15f);Check(game.Session.Remaining<before,"Document reading consumes the incident timer");game.CloseOverlay();
            game.World.Player.Teleport(new Vector2(8.5f,10.2f));yield return new WaitForSeconds(0.1f);
            game.Interact(milk);
            Check(game.Session.Inventory[(int)Ingredient.Milk] && !game.Session.MilkHeated, "The used milk can be replaced at the counter");
            game.Interact(Task(NightAction.Microwave));yield return new WaitForSeconds(game.Tuning.heatMilkSeconds+0.15f);
            Check(game.Session.MilkHeated,"Milk is heated at the microwave");
            game.World.Player.Teleport(new Vector2(3,-6.3f));
            game.Interact(game.World.Interactables.First(i=>i.kind==InteractionKind.Ingredient && i.ingredient==Ingredient.Chocolate));
            yield return Tap(Key.Tab);
            int milkSlot = Array.FindIndex(game.view.inventory.cells, cell => game.InventoryItemAt(cell.Index) == InventoryItem.Milk);
            int chocolateSlot = Array.FindIndex(game.view.inventory.cells, cell => game.InventoryItemAt(cell.Index) == InventoryItem.Chocolate);
            Check(milkSlot >= 0 && chocolateSlot >= 0, "Both ingredients are represented by separate inventory cells");
            if (milkSlot >= 0 && chocolateSlot >= 0)
            {
                yield return DragSlot(chocolateSlot, SlotPoint(milkSlot));
                Check(game.InventoryItemAt(milkSlot) == InventoryItem.Chocolate &&
                    game.InventoryItemAt(chocolateSlot) == InventoryItem.Milk, "A left drag to an occupied slot swaps items");
                yield return DragSlot(milkSlot, SlotPoint(7));
                Check(game.InventoryItemAt(7) == InventoryItem.Chocolate && game.InventoryItemAt(milkSlot) == InventoryItem.None,
                    "A left drag to an empty slot moves only the dragged item");
                yield return DragSlot(7, new Vector2(24, Screen.height - 24));
                Check(game.InventoryItemAt(7) == InventoryItem.Chocolate && !game.view.inventory.dragIcon.enabled,
                    "Dropping outside the inventory cancels the drag");
                yield return RightClickSlot(7);
                Check(game.HeldItem == InventoryItem.Chocolate && game.InventoryItemAt(7) == InventoryItem.None &&
                    game.view.inventory.hand.icon.enabled && game.EquippedItem == InventoryItem.Flashlight,
                    "The hand slot and equipment slot hold separate items");
                yield return RightClickSlot(game.view.inventory.hand);
                Check(game.HeldItem == InventoryItem.None && !game.view.inventory.hand.icon.enabled &&
                    game.InventoryItemAt(7) == InventoryItem.Chocolate, "Right-clicking the hand HUD returns the item to the bag");
                yield return RightClickSlot(7);
            }
            yield return Shot("02c-inventory");
            yield return Tap(Key.Tab);
            game.Interact(Item(InteractionKind.Cup));yield return new WaitForSeconds(0.9f);
            Check(game.Session.RecipeStep == 1 && IngredientConsumed(Ingredient.Chocolate),
                "Chocolate advances the recipe and disappears from the hand and the bag");
            yield return Equip(InventoryItem.Milk);
            yield return Tap(Key.Space);
            yield return new WaitForSeconds(0.9f);
            Check(game.Session.Phase == ShiftPhase.Resolved && IngredientConsumed(Ingredient.Milk),
                "Pouring hot milk with Space completes the recipe and clears the consumed item");
            yield return Shot("03-mocha-cleanup");yield return new WaitForSeconds(game.Tuning.cleanupSeconds+0.2f);
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);yield return new WaitForSeconds(0.15f);
            Check(game.Phase==ShiftPhase.Complete,"Night one ends on return");yield return Next(2);

            game.Interact(Item(InteractionKind.Camera));game.SelectCamera(CafeWorld.CameraChannel(9));yield return Shot("04-menu-cctv");game.CloseOverlay();
            game.World.Player.Teleport(new Vector2(-11,-5.6f));yield return new WaitForSeconds(0.15f);
            Check(game.Later.RestrictTurning,"Menu proximity starts the cardinal turning rule");
            location=game.World.Player.PlanPosition;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.S));yield return new WaitForSeconds(0.15f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            Check(game.Later.RestrictTurning && Vector2.Distance(location,game.World.Player.PlanPosition)<0.03f,"Opposing direction keys cancel without reversing");
            game.FilterDirection(Vector2.up,Vector2.down);yield return new WaitForSeconds(1);
            Check(game.view.failure.activeSelf,"A 180-degree reversal shows failure");
            var previous=game;game.Retry();float retryDeadline=Time.realtimeSinceStartup+10;
            while(FindFirstObjectByType<NightGame>()==previous && Time.realtimeSinceStartup<retryDeadline)yield return null;
            game=FindFirstObjectByType<NightGame>();yield return null;yield return null;
            Check(game.Day==2 && game.Later.Water==WaterState.None,"Second night retry resets equipment");
            var noticeBoard = Task(NightAction.Menu);
            var wallBoard = game.World.transform.Find("Lobby/Floor & Walls/Lobby notice board");
            Check(wallBoard != null && noticeBoard.transform.position == wallBoard.position &&
                noticeBoard.transform.Find("Frame") == null && noticeBoard.transform.Find("Slate") == null,
                "The night-two interaction uses the wall board and has no separate slate");
            game.World.Player.Teleport(noticeBoard.InteractionPoint + Vector2.down * .85f);
            yield return new WaitForSeconds(.1f);
            Check(game.CurrentTarget == noticeBoard, "The wall board can be reached with Space from the lobby floor");
            yield return Tap(Key.Space);

            game.Interact(Task(NightAction.Cloth));
            yield return Equip(InventoryItem.Cloth);
            yield return Tap(Key.Space);
            Check(game.Later.WipesLeft == 3, "Holding a dry cloth adds work when wiping");
            game.Interact(Task(NightAction.EmptyCup));
            game.Interact(Task(NightAction.Sink));
            yield return Tap(Key.Tab);
            yield return RightClickSlot(game.view.inventory.hand);
            int FindSlot(InventoryItem item) =>
                Array.FindIndex(game.view.inventory.cells, cell => game.InventoryItemAt(cell.Index) == item);
            int cup = FindSlot(InventoryItem.WaterCup);
            int cloth = FindSlot(InventoryItem.Cloth);
            Check(cup >= 0 && cloth >= 0, "The cold cup and dry cloth occupy separate slots");
            if (cup >= 0 && cloth >= 0)
            {
                yield return DragSlot(cup, SlotPoint(cloth));
                Check(!game.Later.WetCloth && game.InventoryItemAt(cloth) == InventoryItem.WaterCup &&
                    game.InventoryItemAt(cup) == InventoryItem.Cloth, "Dropping a cold cup on the cloth swaps the slots");
            }
            yield return Tap(Key.Tab);
            game.Interact(Task(NightAction.Warmer));
            yield return Tap(Key.Space);
            Check(!game.Later.WetCloth && game.Later.WipesLeft == 3, "Space does not combine supplies in the bag");

            yield return Tap(Key.Tab);
            cup = FindSlot(InventoryItem.WaterCup);
            cloth = FindSlot(InventoryItem.Cloth);
            if (cup >= 0 && cloth >= 0) yield return DragSlot(cloth, SlotPoint(cup));
            Check(cup >= 0 && game.InventoryItemAt(cup) == InventoryItem.WetCloth &&
                FindSlot(InventoryItem.WaterCup) < 0 && FindSlot(InventoryItem.Cloth) < 0 &&
                game.view.inventory.cells[cup].icon.enabled,
                "Dragging dry cloth onto a hot cup creates a new wet cloth in the drop slot");
            yield return Tap(Key.Tab);
            yield return Tap(Key.Space);
            Check(game.Later.WipesLeft == 3, "The wet cloth must be held to wipe");
            yield return Equip(InventoryItem.WetCloth);
            for (int i = 0; i < 3; i++) yield return Tap(Key.Space);
            Check(game.Phase == ShiftPhase.Resolved && !game.Later.RestrictTurning && game.HeldItem == InventoryItem.None &&
                FindSlot(InventoryItem.WetCloth) < 0, "The final wipe resolves the anomaly and clears the wet cloth");
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);yield return new WaitForSeconds(0.15f);yield return Next(3);

            var routes=FindFirstObjectByType<NightRoutes>();var content=FindFirstObjectByType<NightContent>();
            game.Interact(Item(InteractionKind.Camera));game.SelectCamera(CafeWorld.CameraChannel(14));yield return Shot("05-tentacle-cctv");game.CloseOverlay();
            game.World.Player.Teleport(routes.portals[11].ArrivalPosition);yield return new WaitForSeconds(0.1f);
            Check(game.CurrentTarget == routes.hallDoor, "The marked tentacle shortcut can be selected from the lobby");
            yield return Tap(Key.Space);
            Check(CafeWorld.RoomLayer(game.World.Player.PlanPosition)==14,"Lobby portal enters the tentacle corridor");
            game.World.Player.Teleport(routes.tentacles[0].position + Vector3.up);
            float tentacleDeadline = Time.time + 4.1f;
            while (routes.TentacleHits == 0 && Time.time < tentacleDeadline) yield return null;
            Check(routes.TentacleHits == 1, "An extending tentacle hits the player at its visible position");
            game.World.Player.Teleport(new Vector2(49,0));yield return new WaitForSeconds(0.1f);
            game.Interact(routes.portals[12].item);yield return null;yield return null;
            Check(CafeWorld.RoomLayer(game.World.Player.PlanPosition)==10 && game.Phase==ShiftPhase.Incident,"Far corridor exit starts the hall encounter");
            game.World.Player.Teleport(routes.portals[7].ArrivalPosition);yield return new WaitForSeconds(0.1f);
            Check(game.CurrentTarget == routes.portals[6].item, "The marked pursuit shortcut can be selected from the lobby");
            yield return Tap(Key.Space);
            Check(CafeWorld.RoomLayer(game.World.Player.PlanPosition) == 15 && routes.pursuer.gameObject.activeInHierarchy,
                "Entering the pursuit shortcut reveals the actual pursuer");
            Vector3 pursuerStart = routes.pursuer.position;
            yield return SprintTo(84.6f, true);
            yield return SprintTo(-2f, false);
            Check(Vector3.Distance(pursuerStart, routes.pursuer.position) > .4f, "The pursuer closes in during corridor traversal");
            yield return SprintTo(89.15f, true);
            yield return SprintTo(1.4f, false);
            yield return SprintTo(93.2f, true);
            yield return SprintTo(-2f, false);
            yield return SprintTo(98.65f, true);
            yield return SprintTo(0, false);
            yield return Tap(Key.Space);
            Check(CafeWorld.RoomLayer(game.World.Player.PlanPosition)==13 && game.Phase==ShiftPhase.Incident,"Sprinting crosses the pursuit corridor into the separate counter");
            game.World.Player.Teleport(new Vector2(24.2f,4.1f));game.Interact(routes.detourDoor);
            before=game.Later.Remaining;yield return new WaitForSeconds(0.15f);
            Check(game.Later.Remaining<before && content.detourGrid.activeSelf,"Detour puzzle uses the same running night timer");
            yield return Shot("06-upper-detour");
            foreach(var key in new[]{Key.W,Key.W,Key.S,Key.D,Key.D,Key.W})yield return Tap(key);
            Check(routes.Puzzle.Solved && routes.portals.Skip(14).Take(4).All(link => link.item.isOpen),
                "One upper detour puzzle unlocks both sides of both doors");
            if(game.InputBlocked)game.CloseOverlay();
            foreach(var seat in content.seats)game.Interact(seat.chair);
            Check(game.Later.Warehouse && game.Phase==ShiftPhase.Incident,"All chairs start the second shadow encounter");
            yield return Shot("07-hall-shadow");
            game.Interact(Task(NightAction.Eraser));game.World.Player.Teleport(new Vector2(18.5f,8.5f));
            float chalkDeadline=Time.time+6;while(!content.ChalkIdle && Time.time<chalkDeadline)yield return null;
            game.Interact(Task(NightAction.Board));game.Interact(Task(NightAction.Board));
            Check(game.Phase==ShiftPhase.Resolved,"Erasing during chalk pause and writing the closing notice resolves the warehouse");
            yield return Shot("08-warehouse");game.World.Player.Teleport(CafeWorld.SecuritySpawn);yield return new WaitForSeconds(0.15f);yield return Next(4);
            yield return TestFourth();yield return TestFifth();
            InputSystem.RemoveDevice(keyboard);Finish();
        }
        private IEnumerator TestFourth()
        {
            Check(game.Fourth!=null && game.Day==4,"Campaign advances into the fourth night");
            CheckReachability();
            game.Interact(Item(InteractionKind.Camera));game.SelectCamera(CafeWorld.CameraChannel(9));yield return Shot("09-fourth-leak-cctv");game.CloseOverlay();
            Check(game.Fourth.LeakObserved,"Lobby CCTV marks the leak as observed");
            game.World.Player.Teleport(new Vector2(-12,-7.1f));yield return new WaitForSeconds(0.1f);yield return Ready();
            Check(game.Fourth.Stage==HuntStage.FirstChase,"Observed leak triggers the first drop only once");
            Use(StoryAction.Extinguisher);
            game.story.hunter.position=game.World.Player.PlanPosition+game.World.Player.Facing*2;
            yield return Tap(Key.E);
            Check(game.Fourth.Stage==HuntStage.ReadNotes && game.Fourth.Charges==1,"Real E input suppresses the first chase and preserves one charge");
            yield return ReadDocument(InventoryItem.RuleBook);
            yield return Tap(Key.Space);
            Check(game.view.reading.activeSelf, "Space leaves the fourth-night document open");
            yield return Shot("10-fourth-note");
            yield return Tap(Key.Escape);
            Check(!game.view.reading.activeSelf, "Escape closes the fourth-night document");
            Check(game.Fourth.NotesRead,"The same notebook opens the equipment stage");
            game.World.Player.Teleport(new Vector2(18,7));Use(StoryAction.Net);Use(StoryAction.Anchor);yield return null;
            Check(game.Fourth.HasNet && game.Fourth.HasAnchor && game.Fourth.Stage==HuntStage.Equipment,"Both required pieces are picked up in storage");
            game.World.Player.Teleport(new Vector2(-17,-5.5f));yield return new WaitForSeconds(0.1f);yield return Ready();
            Check(game.Fourth.Stage==HuntStage.SecondChase,"The first general room after storage starts the second chase");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.LeftShift));yield return new WaitForSeconds(0.87f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            Check(game.Fourth.Stage==HuntStage.InstallTrap && game.Fourth.Charges==1,"Running across the escape boundary clears the second chase without a spray");
            game.World.Player.Teleport(new Vector2(3,-9));Use(StoryAction.Trap,10);yield return null;
            Check(game.Fourth.TrapRoom==10 && game.Fourth.Stage==HuntStage.Search,"A selected hall marker installs the trap");
            yield return Shot("11-fourth-trap");
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);game.Interact(Item(InteractionKind.Camera));game.SelectCamera(CafeWorld.CameraChannel(9));
            Check(game.Fourth.SearchRoom==9,"CCTV locates and pins the hunter in the lobby");
            yield return Shot("12-fourth-search");game.CloseOverlay();
            game.World.Player.Teleport(new Vector2(-14,-10));yield return new WaitForSeconds(0.15f);
            Check(game.Fourth.Stage==HuntStage.ThirdChase,"Arriving at the observed room starts the lure");
            var routes=FindFirstObjectByType<NightRoutes>();game.Interact(routes.hallDoor);yield return new WaitForSeconds(0.1f);
            Check(game.StoryBusy && game.story.cinemaFeed.gameObject.activeInHierarchy &&
                CafeWorld.RoomLayer(game.story.hunter.position) == NightRoutes.TentacleCorridor,
                "The real corridor and hunter appear in the fourth-night confrontation");
            yield return Ready();
            Check(game.Fourth.CorridorScenePlayed && CafeWorld.RoomLayer(game.World.Player.PlanPosition)==10,"Fixed monster confrontation restores control at the safe far exit");
            game.World.Player.Teleport(new Vector2(5,-8));game.story.hunter.position=game.story.trap.position;yield return new WaitForSeconds(0.1f);
            Check(game.Fourth.Stage==HuntStage.Captured && game.story.closedNet.activeSelf,"Entering the installed trap automatically closes the net");
            yield return Shot("13-fourth-captured");game.World.Player.Teleport(CafeWorld.SecuritySpawn);yield return new WaitForSeconds(0.15f);
            Check(game.Phase==ShiftPhase.Complete,"Captured monster and return complete night four");yield return Next(5);
        }
        private IEnumerator TestFifth()
        {
            Check(game.Fifth!=null && game.Day==5,"Campaign advances into the fifth night");
            game.Interact(Item(InteractionKind.Camera));
            Check(game.ActiveRoomCamera == game.roomCameras[CafeWorld.CameraChannel(8)] && !game.story.feedEffect.activeSelf,
                "Even during the fifth-night outage, CCTV opens on the live security room");
            game.SelectCamera(CafeWorld.CameraChannel(10));yield return Shot("14-fifth-blackout");
            Check(game.story.feedEffect.activeSelf,"Blackout is presented on the actual CCTV screen");game.CloseOverlay();
            game.Interact(Item(InteractionKind.Camera));
            game.view.cameraButtons[CafeWorld.CameraChannel(9)].onClick.Invoke();
            Check(!game.story.feedEffect.activeSelf && game.ActiveRoomCamera == game.roomCameras[CafeWorld.CameraChannel(9)],
                "The four-channel outage leaves the other room cameras live");
            game.CloseOverlay();
            for(int i=0;i<4;i++)
            {
                var digit=Story(StoryAction.DarkCameraDigit,i);game.World.Player.Teleport((Vector2)digit.transform.position+Vector2.down*0.7f);Use(StoryAction.DarkCameraDigit,i);
            }
            game.World.Player.Teleport(new Vector2(18,-5.6f));Use(StoryAction.Computer);yield return Number("0000");
            Check(game.Fifth.Cracks==0 && game.Fifth.Stage==FinaleStage.Blackout,"Wrong introductory password does not cause cracks");
            Use(StoryAction.Computer);yield return Number(game.Tuning.cameraPassword);
            Check(game.Fifth.Stage==FinaleStage.Return,"Four digits in notebook rank order restore the cameras");
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);yield return new WaitForSeconds(0.1f);yield return Ready();
            Check(game.Fifth.Frozen && game.Phase==ShiftPhase.Incident,"Returning freezes the clock instead of ending night five");
            float before=game.Fifth.Remaining;yield return new WaitForSeconds(0.15f);
            Check(game.Fifth.Remaining==before,"Frozen night has no hidden timer");
            var routes=FindFirstObjectByType<NightRoutes>();game.Interact(routes.portals[0].item);yield return null;
            Check(CafeWorld.RoomLayer(game.World.Player.PlanPosition)==8,"Security door is locked until its required clues are collected");
            Use(StoryAction.Roster);game.CloseOverlay();Use(StoryAction.Accident);yield return Shot("15-original-accident");game.CloseOverlay();
            game.Interact(Item(InteractionKind.Camera));game.SelectCamera(3);yield return Shot("16-identity-cctv");game.CloseOverlay();
            Check(game.Fifth.CanLeaveSecurity,"Roster, accident record and identity CCTV unlock security");
            game.World.Player.Teleport(new Vector2(18,-7));Use(StoryAction.Identity);game.CloseOverlay();
            Use(StoryAction.Badge,1);yield return Equip(InventoryItem.Badge);
            Use(StoryAction.Identity);Use(StoryAction.Identity);Use(StoryAction.Identity);
            yield return new WaitForSeconds(0.1f);yield return Ready();yield return new WaitForSeconds(game.Tuning.cinematicSeconds*4+0.2f);
            Check(game.Phase==ShiftPhase.Failed && game.view.failure.activeSelf,"Three wrong identity confirmations play the captured ending");
            yield return Shot("17-captured-ending");game.view.failureButtons[0].onClick.Invoke();yield return new WaitForSeconds(0.1f);
            Check(game.Fifth.Cracks==0 && game.Fifth.CanLeaveSecurity && game.Fifth.IdentityInspected,"Retry keeps the acquired security evidence and identity inspection");
            Use(StoryAction.Badge,0);yield return Equip(InventoryItem.Badge);Use(StoryAction.Identity);yield return Ready();
            Check(game.Fifth.Identified,"The correct employee badge removes the borrowed identity");
            game.World.Player.Teleport(new Vector2(35,1.8f));yield return new WaitForSeconds(0.1f);yield return Ready();
            Check(game.Fifth.RepairRequestRead && game.Fifth.MachineVideoSeen,"Tentacle memory gives repair and original machine evidence");
            Use(StoryAction.WireCode);game.CloseOverlay();Use(StoryAction.Control);yield return Number(game.Tuning.controlPassword);
            Check(game.Fifth.ControlUnlocked,"The observed wire number opens the control box");
            Use(StoryAction.Control);game.story.choices[2].onClick.Invoke();
            Check(!game.Fifth.TentaclesReleased && game.Fifth.Cracks==0,"Opening the release too early is feedback, not a confirmed wrong answer");
            Use(StoryAction.Control);game.story.choices[0].onClick.Invoke();
            Use(StoryAction.Control);game.story.choices[1].onClick.Invoke();
            Use(StoryAction.Control);game.story.choices[2].onClick.Invoke();
            Check(game.Fifth.TentaclesReleased,"Power off, stopped indicator and emergency release resolve the tentacles");
            game.World.Player.Teleport(new Vector2(84,0));yield return new WaitForSeconds(0.1f);yield return Ready();
            Check(game.Fifth.PursuitVideoSeen,"The pursuit corridor replays the original escape route");
            Check(routes.pursuer.gameObject.activeInHierarchy && game.Fifth.Identified,
                "The harmless pursuer remains visible until its cause is resolved");
            game.World.Player.Teleport(new Vector2(18,-5.6f));Use(StoryAction.Computer);game.story.choices[0].onClick.Invoke();game.CloseOverlay();
            Use(StoryAction.Computer);game.story.choices[1].onClick.Invoke();
            Check(!game.Fifth.SmokeCleared && game.Fifth.Cracks==0,"Uncleared exhaust returns smoke without accumulating a mistake");
            Use(StoryAction.ExhaustBox);Use(StoryAction.Computer);game.story.choices[1].onClick.Invoke();yield return Ready();
            Check(game.Fifth.SmokeCleared,"Physical box removal and outward fan control clear the pursuit cause");
            game.World.Player.Teleport(new Vector2(84, 0));yield return null;yield return null;
            Check(!routes.pursuer.gameObject.activeInHierarchy, "The pursuer disappears after ventilation actually clears the smoke");
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);Use(StoryAction.Accident);
            yield return Shot("18-record-cause");game.story.choices[1].onClick.Invoke();
            yield return Number("1");yield return Shot("19-record-departure");game.story.choices[0].onClick.Invoke();game.CloseOverlay();
            Check(game.Fifth.RecordCorrected && game.Fifth.HasNote,"Cause selection, numeric historical head count and departure status correct the record");
            yield return null;
            game.World.Player.Teleport(new Vector2(17,-8));
            Use(StoryAction.VictimCup);yield return null;Use(StoryAction.CupTable);yield return Ready();
            Check(game.Fifth.Stage==FinaleStage.Escort,"The victim cup and name summon the spirit after both causes and record correction");
            yield return Shot("20-escort");
            game.World.Player.Teleport(new Vector2(23,-5));game.story.spirit.position=new Vector2(23.5f,-5);
            game.Interact(routes.portals[18].item);yield return null;
            Check(CafeWorld.RoomLayer(game.story.spirit.position)==12,"Opening an unsolved detour leaves the escort in the current room");game.CloseOverlay();
            game.World.Player.Teleport(new Vector2(16,-5));game.story.spirit.position=new Vector2(16.5f,-5);
            game.Interact(routes.portals[5].item);yield return null;
            Check(CafeWorld.RoomLayer(game.story.spirit.position)==9 && CafeWorld.RoomLayer(game.World.Player.PlanPosition)==9,"A completed room transition takes the illuminated escort along");
            game.World.Player.Teleport(new Vector2(71,0));game.story.spirit.position=new Vector2(71.2f,0);
            foreach(var t in game.story.spirit.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=16;
            // Face toward the actual exit with the flashlight on, then remain inside.
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));yield return new WaitForSeconds(0.05f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            if(!game.World.Player.FlashlightOn) yield return Tap(Key.F);
            Use(StoryAction.Exit);yield return new WaitForSeconds(1.2f);
            Check(game.Fifth.SpiritPassed && game.Phase==ShiftPhase.Incident,"The illuminated spirit passes first while the player stays inside");
            yield return Shot("21-spirit-passed");Use(StoryAction.Departure);yield return new WaitForSeconds(0.1f);yield return Ready();
            yield return new WaitForSeconds(game.Tuning.cinematicSeconds*5+0.3f);
            Check(game.Fifth.Stage==FinaleStage.Happy && game.view.complete.activeSelf,"Inside departure confirmation completes the happy ending and epilogue");
            yield return Shot("22-last-shift-complete");
        }
        private void CheckReachability()
        {
            Physics2D.SyncTransforms();
            var content=FindFirstObjectByType<NightContent>();
            var allItems=game.World.Interactables.Concat(game.story.huntTasks).Concat(game.story.finalTasks)
                .Concat(content.menuTasks).Concat(content.shadowTasks).Distinct().ToArray();
            foreach(var item in allItems)
                Check(item.gameObject.layer==CafeWorld.RoomLayer(item.transform.position),"Interaction is rendered in its physical room: "+item.title);
            foreach(var portal in content.routes.portals)
                Check(Physics2D.OverlapCircleAll(portal.ArrivalPosition,0.32f).All(hit=>hit.isTrigger || hit.gameObject==game.World.Player.gameObject),"Door arrival avoids walls and furniture: "+portal.item.title);
            foreach(int room in Enumerable.Range(8,11))
            {
                var bounds=CafeWorld.Bounds(room);const float spacing=0.5f;
                var reached=new HashSet<Vector2Int>();var queue=new Queue<Vector2Int>();
                Vector2 Point(Vector2Int n)=>bounds.min+(Vector2)n*spacing;
                bool Free(Vector2 p)
                {
                    if(!bounds.Contains(p))return false;
                    return Physics2D.OverlapCircleAll(p,0.33f).All(hit=>hit.isTrigger || hit.gameObject==game.World.Player.gameObject);
                }
                Vector2Int start=default;bool found=false;
                for(int x=1;x<bounds.width/spacing && !found;x++)for(int y=1;y<bounds.height/spacing;y++)
                    if(Free(Point(new Vector2Int(x,y)))){start=new Vector2Int(x,y);found=true;break;}
                if(!found){Check(false,"No walkable seed for room "+room);continue;}
                reached.Add(start);queue.Enqueue(start);
                Vector2Int[] steps={Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
                while(queue.Count>0)
                {
                    var node=queue.Dequeue();
                    foreach(var d in steps){var next=node+d;if(!reached.Contains(next) && Free(Point(next))){reached.Add(next);queue.Enqueue(next);}}
                }
                foreach(var item in allItems.Where(i=>CafeWorld.RoomLayer(i.transform.position)==room))
                {
                    Vector2 target = item.InteractionPoint;
                    bool accessible=reached.Any(node=>
                    {
                        Vector2 point=Point(node);
                        return Vector2.Distance(point,target)<game.Tuning.interactionDistance && Physics2D.LinecastAll(point,target)
                            .All(hit=>hit.collider.isTrigger || hit.collider.gameObject==game.World.Player.gameObject || hit.collider.gameObject==item.gameObject);
                    });
                    Check(accessible,"Reachable interaction in "+CafeWorld.RoomNames[room-8]+": "+item.title);
                }
            }
        }
    }
}
