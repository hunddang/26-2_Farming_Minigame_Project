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
                if(next!=null && next!=previous && next.Day==day){game=next;yield return null;yield return null;yield break;}
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
            keyboard=InputSystem.AddDevice<Keyboard>();yield return new WaitForSeconds(0.5f);
            Check(Screen.width==1920 && Screen.height==1080,"FHD window");
            Check(UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings==2,"The two scene entry points remain playable");
            Check(game.mapCamera.orthographic && game.feedCamera.orthographic,"2D cameras");
            Check(game.World.Player.GetComponent<Rigidbody2D>()!=null,"2D movement and collisions");
            CheckReachability();
            game.World.Player.Teleport(new Vector2(-15,6.5f));yield return new WaitForSeconds(0.1f);
            Check(game.CurrentTarget!=null && game.CurrentTarget.kind==InteractionKind.Camera,"Security CCTV can be reached");
            yield return Tap(Key.Space);yield return new WaitForSeconds(0.1f);
            for(int i=0;i<4;i++)
            {
                game.SelectCamera(i);yield return new WaitForEndOfFrame();
                Check(game.feedCamera.enabled && game.view.feedImage.texture==game.feedCamera.targetTexture,"Real-time CCTV channel "+i);
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
            Check(!game.feedCamera.enabled && game.view.feedImage.texture==null,"CCTV closes without remote observation");
            game.World.Player.Teleport(CafeWorld.IncidentSpawn);yield return null;yield return null;
            Check(game.Session.Phase==ShiftPhase.Incident,"Hall entry begins the espresso incident");
            var spillDeadline = Time.time + 2.3f;
            while (Array.IndexOf(game.World.CupFrames, cupRenderer.sprite) < 17 && Time.time < spillDeadline)
                yield return null;
            Check(Array.IndexOf(game.World.CupFrames, cupRenderer.sprite) >= 17,"The GIF reaches its spilling frames in the hall");
            yield return Shot("02b-cup-spill");
            game.Interact(Item(InteractionKind.Cup));
            game.Interact(game.World.Interactables.First(i=>i.kind==InteractionKind.Ingredient && i.ingredient==Ingredient.Milk));
            game.Interact(Item(InteractionKind.Cup));yield return new WaitForSeconds(0.9f);
            Check(game.Session.Mistakes==1 && game.Session.Inventory[0],"Wrong first ingredient stays in inventory");
            game.Interact(Item(InteractionKind.Recipe));float before=game.Session.Remaining;
            yield return new WaitForSeconds(0.15f);Check(game.Session.Remaining<before,"Document reading consumes the incident timer");game.CloseOverlay();
            game.World.Player.Teleport(new Vector2(8.5f,10.2f));yield return new WaitForSeconds(0.1f);
            game.Interact(Task(NightAction.Microwave));yield return new WaitForSeconds(game.Tuning.heatMilkSeconds+0.15f);
            Check(game.Session.MilkHeated,"Milk is heated at the microwave");
            game.World.Player.Teleport(new Vector2(3,-7.2f));
            game.Interact(game.World.Interactables.First(i=>i.kind==InteractionKind.Ingredient && i.ingredient==Ingredient.Chocolate));
            game.Interact(Item(InteractionKind.Cup));yield return new WaitForSeconds(0.9f);
            Check(game.Session.RecipeStep==1,"Chocolate is the first recipe ingredient");
            game.SelectSlot(0);game.Interact(Item(InteractionKind.Cup));yield return new WaitForSeconds(0.9f);
            Check(game.Session.Phase==ShiftPhase.Resolved,"Chocolate and hot milk resolve the espresso");
            yield return Shot("03-mocha-cleanup");yield return new WaitForSeconds(game.Tuning.cleanupSeconds+0.2f);
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);yield return new WaitForSeconds(0.15f);
            Check(game.Phase==ShiftPhase.Complete,"Night one ends on return");yield return Next(2);

            game.Interact(Item(InteractionKind.Camera));game.SelectCamera(3);yield return Shot("04-menu-cctv");game.CloseOverlay();
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
            game.World.Player.Teleport(new Vector2(-11,-5.6f));yield return new WaitForSeconds(0.1f);
            game.Interact(Task(NightAction.Menu));game.Interact(Task(NightAction.Menu));
            Check(game.Later.WipesLeft==3,"Dry wiping adds work");
            foreach(var action in new[]{NightAction.EmptyCup,NightAction.Sink,NightAction.Warmer,NightAction.Cloth})game.Interact(Task(action));
            game.Interact(Task(NightAction.Menu));for(int i=0;i<3;i++)game.Interact(Task(NightAction.Menu));
            Check(game.Phase==ShiftPhase.Resolved && !game.Later.RestrictTurning,"Warm wet cloth clears the menu and restriction");
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);yield return new WaitForSeconds(0.15f);yield return Next(3);

            var routes=FindFirstObjectByType<NightRoutes>();var content=FindFirstObjectByType<NightContent>();
            game.Interact(Item(InteractionKind.Camera));game.SelectCamera(3);yield return Shot("05-tentacle-cctv");game.CloseOverlay();
            game.World.Player.Teleport(new Vector2(-8.8f,-9.5f));yield return new WaitForSeconds(0.1f);
            game.Interact(routes.hallDoor);yield return null;yield return null;
            Check(CafeWorld.RoomLayer(game.World.Player.PlanPosition)==14,"Lobby portal enters the tentacle corridor");
            game.World.Player.Teleport(new Vector2(49,0));yield return new WaitForSeconds(0.1f);
            game.Interact(routes.portals[12].item);yield return null;yield return null;
            Check(CafeWorld.RoomLayer(game.World.Player.PlanPosition)==10 && game.Phase==ShiftPhase.Incident,"Far corridor exit starts the hall encounter");
            game.World.Player.Teleport(new Vector2(-9,-5));game.Interact(routes.portals[6].item);yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.LeftShift));yield return new WaitForSeconds(2.7f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            game.Interact(routes.portals[8].item);yield return null;
            Check(CafeWorld.RoomLayer(game.World.Player.PlanPosition)==13 && game.Phase==ShiftPhase.Incident,"Sprinting crosses the pursuit corridor into the separate counter");
            game.World.Player.Teleport(new Vector2(24.2f,4.1f));game.Interact(routes.detourDoor);
            before=game.Later.Remaining;yield return new WaitForSeconds(0.15f);
            Check(game.Later.Remaining<before && content.detourGrid.activeSelf,"Detour puzzle uses the same running night timer");
            yield return Shot("06-upper-detour");
            foreach(var key in new[]{Key.W,Key.W,Key.S,Key.D,Key.D,Key.W})yield return Tap(key);
            Check(routes.Puzzle.Solved && routes.detourDoor.isOpen,"Night three upper detour puzzle unlocks the door");
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
            game.Interact(Item(InteractionKind.Camera));game.SelectCamera(3);yield return Shot("09-fourth-leak-cctv");game.CloseOverlay();
            Check(game.Fourth.LeakObserved,"Lobby CCTV marks the leak as observed");
            game.World.Player.Teleport(new Vector2(-12,-7.1f));yield return new WaitForSeconds(0.1f);yield return Ready();
            Check(game.Fourth.Stage==HuntStage.FirstChase,"Observed leak triggers the first drop only once");
            Use(StoryAction.Extinguisher);
            game.story.hunter.position=game.World.Player.PlanPosition+game.World.Player.Facing*2;
            yield return Tap(Key.E);
            Check(game.Fourth.Stage==HuntStage.ReadNotes && game.Fourth.Charges==1,"Real E input suppresses the first chase and preserves one charge");
            game.OpenRules();yield return Shot("10-fourth-note");game.CloseOverlay();
            Check(game.Fourth.NotesRead,"The same notebook opens the equipment stage");
            game.World.Player.Teleport(new Vector2(18,7));Use(StoryAction.Net);Use(StoryAction.Anchor);yield return null;
            Check(game.Fourth.HasNet && game.Fourth.HasAnchor && game.Fourth.Stage==HuntStage.Equipment,"Both required pieces are picked up in storage");
            game.World.Player.Teleport(new Vector2(-17,-7));yield return new WaitForSeconds(0.1f);yield return Ready();
            Check(game.Fourth.Stage==HuntStage.SecondChase,"The first general room after storage starts the second chase");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.LeftShift));yield return new WaitForSeconds(0.87f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            Check(game.Fourth.Stage==HuntStage.InstallTrap && game.Fourth.Charges==1,"Running across the escape boundary clears the second chase without a spray");
            game.World.Player.Teleport(new Vector2(3,-9));Use(StoryAction.Trap,10);yield return null;
            Check(game.Fourth.TrapRoom==10 && game.Fourth.Stage==HuntStage.Search,"A selected hall marker installs the trap");
            yield return Shot("11-fourth-trap");
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);game.Interact(Item(InteractionKind.Camera));game.SelectCamera(3);
            Check(game.Fourth.SearchRoom==9,"CCTV locates and pins the hunter in the lobby");
            yield return Shot("12-fourth-search");game.CloseOverlay();
            game.World.Player.Teleport(new Vector2(-14,-10));yield return new WaitForSeconds(0.15f);
            Check(game.Fourth.Stage==HuntStage.ThirdChase,"Arriving at the observed room starts the lure");
            var routes=FindFirstObjectByType<NightRoutes>();game.Interact(routes.hallDoor);yield return new WaitForSeconds(0.1f);yield return Ready();
            Check(game.Fourth.CorridorScenePlayed && CafeWorld.RoomLayer(game.World.Player.PlanPosition)==10,"Fixed monster confrontation restores control at the safe far exit");
            game.World.Player.Teleport(new Vector2(5,-8));game.story.hunter.position=game.story.trap.position;yield return new WaitForSeconds(0.1f);
            Check(game.Fourth.Stage==HuntStage.Captured && game.story.closedNet.activeSelf,"Entering the installed trap automatically closes the net");
            yield return Shot("13-fourth-captured");game.World.Player.Teleport(CafeWorld.SecuritySpawn);yield return new WaitForSeconds(0.15f);
            Check(game.Phase==ShiftPhase.Complete,"Captured monster and return complete night four");yield return Next(5);
        }
        private IEnumerator TestFifth()
        {
            Check(game.Fifth!=null && game.Day==5,"Campaign advances into the fifth night");
            game.Interact(Item(InteractionKind.Camera));yield return Shot("14-fifth-blackout");
            Check(game.story.feedEffect.activeSelf,"Blackout is presented on the actual CCTV screen");game.CloseOverlay();
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
            Use(StoryAction.Badge,1);Use(StoryAction.Identity);Use(StoryAction.Identity);Use(StoryAction.Identity);
            yield return new WaitForSeconds(0.1f);yield return Ready();yield return new WaitForSeconds(game.Tuning.cinematicSeconds*4+0.2f);
            Check(game.Phase==ShiftPhase.Failed && game.view.failure.activeSelf,"Three wrong identity confirmations play the captured ending");
            yield return Shot("17-captured-ending");game.view.failureButtons[0].onClick.Invoke();yield return new WaitForSeconds(0.1f);
            Check(game.Fifth.Cracks==0 && game.Fifth.CanLeaveSecurity && game.Fifth.IdentityInspected,"Retry keeps the acquired security evidence and identity inspection");
            Use(StoryAction.Badge,0);Use(StoryAction.Identity);yield return Ready();
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
            game.World.Player.Teleport(new Vector2(18,-5.6f));Use(StoryAction.Computer);game.story.choices[0].onClick.Invoke();game.CloseOverlay();
            Use(StoryAction.Computer);game.story.choices[1].onClick.Invoke();
            Check(!game.Fifth.SmokeCleared && game.Fifth.Cracks==0,"Uncleared exhaust returns smoke without accumulating a mistake");
            Use(StoryAction.ExhaustBox);Use(StoryAction.Computer);game.story.choices[1].onClick.Invoke();yield return Ready();
            Check(game.Fifth.SmokeCleared,"Physical box removal and outward fan control clear the pursuit cause");
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
                Check(Physics2D.OverlapCircleAll(portal.target,0.32f).All(hit=>hit.isTrigger || hit.gameObject==game.World.Player.gameObject),"Door arrival avoids walls and furniture: "+portal.item.title);
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
                    bool accessible=reached.Any(node=>
                    {
                        Vector2 point=Point(node);
                        return Vector2.Distance(point,item.transform.position)<game.Tuning.interactionDistance && Physics2D.LinecastAll(point,item.transform.position)
                            .All(hit=>hit.collider.isTrigger || hit.collider.gameObject==game.World.Player.gameObject || hit.collider.gameObject==item.gameObject);
                    });
                    Check(accessible,"Reachable interaction in "+CafeWorld.RoomNames[room-8]+": "+item.title);
                }
            }
        }
    }
}
