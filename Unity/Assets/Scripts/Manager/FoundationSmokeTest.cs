using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace NightCafe
{
    // Opt-in Windows player acceptance test. Never runs during ordinary play.
    public sealed class FoundationSmokeTest : MonoBehaviour
    {
        public NightGame game;
        private readonly List<string> failures = new List<string>();
        private Keyboard testKeyboard;
        public static string OutputPath(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-testArtifacts");
            string path = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.persistentDataPath, "FoundationQA");
            Directory.CreateDirectory(path);
            return Path.Combine(path, name);
        }
        private void OnEnable() { Application.logMessageReceived += OnLog; }
        private void OnDisable() { Application.logMessageReceived -= OnLog; }
        private void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failures.Add(message + "\n" + trace);
        }
        private void Check(bool condition, string message) { if (!condition) failures.Add(message); }
        private CafeInteractable Find(InteractionKind kind, Ingredient ingredient = Ingredient.Milk)
        {
            return game.World.Interactables.Find(item => item.kind == kind && (kind != InteractionKind.Ingredient || item.ingredient == ingredient));
        }
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            yield return new WaitForSeconds(0.6f);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testKeyboard = InputSystem.AddDevice<Keyboard>();
            Check(Screen.width == 1920 && Screen.height == 1080, "Player must render at FHD.");
            Check(UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings == 2, "Exactly two scenes must be built.");
            Check((Camera.main.cullingMask & (1 << 10)) == 0, "Security movement view must hide cafe objects.");
            Check(!game.mapCamera.orthographic && !game.feedCamera.orthographic, "Gameplay and CCTV must use real 3D perspective cameras.");
            Check(game.World.GetComponentsInChildren<Collider2D>(true).Length == 0, "The 3D world must not retain 2D collisions.");
            Check(game.World.GetComponentsInChildren<MeshRenderer>(true).Length > 100, "The world must contain authored 3D assets.");
            Check(game.World.Player.GetComponent<Rigidbody>() != null, "Player must use 3D physics.");
            int authoredObjects = game.view.GetComponentsInChildren<Transform>(true).Length;
            for (int i = 0; i < 3; i++)
            {
                game.view.rulesButton.onClick.Invoke();
                Check(game.view.rules.activeSelf && game.InputBlocked, "Rules button must open the authored overlay.");
                game.view.closeButtons[0].onClick.Invoke();
                Check(!game.view.rules.activeSelf && !game.InputBlocked, "Close button must hide and retain the authored overlay.");
            }
            Check(game.view.GetComponentsInChildren<Transform>(true).Length == authoredObjects, "Repeated overlays must not create or destroy presentation objects.");
            game.World.Player.Teleport(new Vector2(-15, 6.5f));
            yield return new WaitForSeconds(0.1f);
            Check(game.CurrentTarget != null && game.CurrentTarget.kind == InteractionKind.Camera, "CCTV must be reachable from in front of the desk.");
            yield return new WaitForEndOfFrame();
            Color[] beforeCctv = CaptureBackgroundSamples();
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.Space));
            yield return new WaitForSeconds(0.1f);
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return new WaitForSeconds(0.5f);
            for (int channel = 0; channel < 4; channel++)
            {
                game.SelectCamera(channel);
                yield return new WaitForEndOfFrame();
                Check(game.feedCamera.enabled && game.feedCamera.targetTexture != null && game.view.feedImage.texture == game.feedCamera.targetTexture,
                    "Each CCTV channel must render the separate camera to the live UI texture.");
                Check(Vector3.Distance(game.feedCamera.transform.position, game.World.CctvMounts[channel].position) < 0.001f,
                    "CCTV must render from its physical lens, channel " + channel);
                Check(Quaternion.Angle(game.feedCamera.transform.rotation, game.World.CctvMounts[channel].rotation) < 0.01f,
                    "CCTV must retain its fixed mounting direction.");
                ScreenCapture.CaptureScreenshot(OutputPath("02-cctv-channel-" + (channel + 1) + ".png"));
                yield return new WaitForSeconds(0.12f);
            }
            game.SelectCamera(0);
            Vector3 initial = game.World.Player.transform.position;
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.D));
            yield return new WaitForSeconds(0.2f);
            Check(Vector3.Distance(initial, game.World.Player.transform.position) < 0.02f, "CCTV must block movement.");
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            ScreenCapture.CaptureScreenshot(OutputPath("02-cctv.png"));
            yield return new WaitForSeconds(0.3f);
            game.CloseOverlay();
            Check(!game.feedCamera.enabled && game.view.feedImage.texture == null, "Closing CCTV must stop remote observation.");
            yield return new WaitForEndOfFrame();
            Color[] afterCctv = CaptureBackgroundSamples();
            for (int i = 0; i < beforeCctv.Length; i++)
            {
                Color delta = beforeCctv[i] - afterCctv[i];
                Check(Mathf.Max(Mathf.Abs(delta.r), Mathf.Abs(delta.g), Mathf.Abs(delta.b)) < 0.01f,
                    "CCTV close must restore background pixel " + i + ": before=" + beforeCctv[i] + ", after=" + afterCctv[i]);
            }
            ScreenCapture.CaptureScreenshot(OutputPath("02-after-cctv.png"));

            // Walk through the actual wall collider, then open and traverse the doorway.
            game.World.Player.Teleport(new Vector2(-13, 3.4f));
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.S));
            yield return new WaitForSeconds(0.6f);
            Check(game.World.Player.transform.position.z > 2.3f, "Closed security door must block the player.");
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
            var securityDoor = game.World.Interactables.Find(item => item.securityDoor);
            game.Interact(securityDoor);
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.S));
            yield return new WaitForSeconds(0.7f);
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
            Check(game.World.Player.transform.position.z < 2, "Open security door must permit passage.");

            game.World.Player.Teleport(CafeWorld.IncidentSpawn);
            yield return null;
            Check(game.Session.Phase == ShiftPhase.Incident, "Entering cafe must start incident.");
            Check((Camera.main.cullingMask & (1 << 10)) != 0 && (Camera.main.cullingMask & (1 << 8)) == 0, "Cafe movement view must reveal cafe and hide security objects.");
            ScreenCapture.CaptureScreenshot(OutputPath("03-cafe-3d.png"));
            yield return new WaitForSeconds(0.2f);
            CheckReachability();
            game.Interact(Find(InteractionKind.Recipe));
            yield return new WaitForSeconds(0.2f);
            ScreenCapture.CaptureScreenshot(OutputPath("03-recipe.png"));
            float before = game.Session.Remaining;
            yield return new WaitForSeconds(0.3f);
            Check(game.Session.Remaining < before, "Reading must not pause incident.");
            game.CloseOverlay();
            game.OpenRules();
            yield return new WaitForSeconds(0.3f);
            ScreenCapture.CaptureScreenshot(OutputPath("04-rules.png"));
            yield return new WaitForSeconds(0.2f);
            game.CloseOverlay();
            game.World.Player.Teleport(new Vector2(3, -0.4f));
            game.Interact(Find(InteractionKind.Cup));
            game.Interact(Find(InteractionKind.Ingredient, Ingredient.Almond));
            game.Interact(Find(InteractionKind.Cup));
            yield return new WaitForSeconds(1);
            Check(game.Session.Mistakes == 1 && game.Session.RecipeStep == 0, "Wrong ingredient must reset recipe with a penalty.");
            ScreenCapture.CaptureScreenshot(OutputPath("05-incident.png"));
            yield return new WaitForSeconds(0.3f);
            // Pause via the same keyboard path as normal play.
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.Escape));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            before = game.Session.Remaining;
            yield return new WaitForSecondsRealtime(0.3f);
            Check(Mathf.Approximately(before, game.Session.Remaining) && Time.timeScale == 0, "Pause must freeze time.");
            game.CloseOverlay();
            game.Session.Tick(999);
            yield return null;
            Check(game.Session.Phase == ShiftPhase.Failed && game.InputBlocked, "Flood timeout must open failure UI.");
            ScreenCapture.CaptureScreenshot(OutputPath("06-failure.png"));
            yield return new WaitForSeconds(0.3f);
            game.Retry();
            Check(game.Session.RecipeRead && game.Session.CupInspected && game.Session.SelectedSlot == -1 && game.Session.RecipeStep == 0, "Retry must preserve knowledge and reset items/progress.");
            foreach (var ingredient in game.Tuning.recipe)
            {
                game.Interact(Find(InteractionKind.Ingredient, ingredient));
                game.Interact(Find(InteractionKind.Cup));
                yield return new WaitForSeconds(1);
            }
            Check(game.Session.Phase == ShiftPhase.Resolved, "Correct recipe must resolve incident.");
            yield return new WaitForSeconds(game.Tuning.cleanupSeconds + 0.2f);
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);
            yield return null; yield return null;
            Check(game.Session.Phase == ShiftPhase.Complete, "Returning to security must complete day.");
            Check(game.view.complete.activeSelf && !game.view.failure.activeSelf, "Completion must show only the authored completion overlay.");
            Check(game.view.GetComponentsInChildren<Transform>(true).Length == authoredObjects, "All gameplay overlays must reuse the saved hierarchy.");
            ScreenCapture.CaptureScreenshot(OutputPath("07-complete.png"));
            yield return new WaitForSeconds(0.5f);
            yield return CheckLaterShifts();
            InputSystem.RemoveDevice(testKeyboard);
            File.WriteAllText(OutputPath("player-smoke-test.txt"), failures.Count == 0 ? "PASS: Day 1 and CCTV regression checks, Day 2 menu/turning/failure/retry/cleanup, Day 3 CCTV/passages/detour/chairs/chalk, and campaign progression through the return to security.\n" : string.Join("\n\n", failures));
            Application.Quit(failures.Count == 0 ? 0 : 1);
        }

        private CafeInteractable Task(NightAction action) => game.World.Interactables.Find(item => item.nightAction == action);

        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
        }

        private IEnumerator WaitForShift(int day)
        {
            var previous = game;
            float deadline = Time.realtimeSinceStartup + 10;
            while (Time.realtimeSinceStartup < deadline)
            {
                var next = FindFirstObjectByType<NightGame>();
                if (next != null && next != previous && next.Day == day && next.Later != null)
                {
                    game = next;
                    yield break;
                }
                yield return null;
            }
            Check(false, "Scene did not load night " + day);
            game = null;
        }

        private IEnumerator CheckLaterShifts()
        {
            game.view.completeButtons[0].onClick.Invoke();
            yield return WaitForShift(2);
            if (game == null) yield break;
            game.Interact(Find(InteractionKind.Camera));
            game.SelectCamera(3);
            yield return new WaitForSeconds(0.3f);
            Check(game.Later.Viewed && game.feedCamera.enabled && (game.feedCamera.cullingMask & (1 << 9)) != 0,
                "Day 2 lobby must be visible on CCTV.");
            ScreenCapture.CaptureScreenshot(OutputPath("08-menu-cctv.png"));
            yield return null;
            game.CloseOverlay();
            Check(!game.feedCamera.enabled && game.view.feedImage.texture == null, "Day 2 CCTV must clear on close.");
            game.World.Player.Teleport(new Vector2(-10.1f, -3.2f));
            yield return new WaitForSeconds(0.2f);
            Check(game.Later.RestrictTurning && game.CurrentTarget == Task(NightAction.Menu), "Approaching the menu must start Day 2.");
            ScreenCapture.CaptureScreenshot(OutputPath("09-menu-incident.png"));
            yield return null;
            var position = game.World.Player.PlanPosition;
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.W, Key.S));
            yield return new WaitForSeconds(0.15f);
            Check(game.Later.RestrictTurning && Vector2.Distance(position, game.World.Player.PlanPosition) < 0.03f,
                "Opposing keys must cancel without causing a reversal.");
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
            Check(game.FilterDirection(Vector2.one, Vector2.right) == Vector2.right, "Diagonals must become cardinal.");
            game.FilterDirection(Vector2.up, Vector2.down);
            yield return new WaitForSeconds(1);
            Check(game.view.failure.activeSelf && game.Later.FailureKind == NightFailure.Menu, "An exact reversal must fail Day 2.");
            game.view.failureButtons[0].onClick.Invoke();
            yield return WaitForShift(2);
            if (game == null) yield break;
            Check(game.Later.Phase == ShiftPhase.Surveillance && game.Later.Water == WaterState.None &&
                CafeWorld.RoomLayer(game.World.Player.PlanPosition) == 8, "Day 2 retry must reset the night in security.");
            CheckReachability();
            game.World.Player.Teleport(new Vector2(-10.1f, -3.2f));
            yield return new WaitForSeconds(0.1f);
            var menu = Task(NightAction.Menu);
            game.Interact(menu);
            game.Interact(menu);
            Check(game.Later.MenuInspected && game.Later.WipesLeft == 3, "Dry wiping must increase the work.");
            foreach (var action in new[] { NightAction.EmptyCup, NightAction.Sink, NightAction.Warmer, NightAction.Cloth })
                game.Interact(Task(action));
            game.Interact(menu);
            Check(game.Later.WetCloth && game.Later.Water == WaterState.Hot, "The sink, warmer and cloth must prepare a wet cloth.");
            for (int i = 0; i < 3; i++) game.Interact(menu);
            Check(game.Phase == ShiftPhase.Resolved, "Wiping the menu must resolve Day 2.");
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);
            yield return new WaitForSeconds(0.15f);
            Check(game.view.complete.activeSelf, "Day 2 must complete on returning to security.");
            game.view.completeButtons[0].onClick.Invoke();
            yield return WaitForShift(3);
            if (game == null) yield break;

            var routes = FindFirstObjectByType<NightRoutes>();
            var content = FindFirstObjectByType<NightContent>();
            game.Interact(Find(InteractionKind.Camera));
            game.SelectCamera(3);
            yield return new WaitForSeconds(0.3f);
            Check(game.feedCamera.enabled && game.feedCamera.cullingMask == (1 << 14), "Day 3 CCTV must show the tentacle passage.");
            ScreenCapture.CaptureScreenshot(OutputPath("10-tentacle-cctv.png"));
            yield return null;
            game.CloseOverlay();
            game.World.Player.Teleport(new Vector2(-8.3f, -4));
            yield return new WaitForSeconds(0.15f);
            Check(game.CurrentTarget == routes.hallDoor, "Hall passage must be reachable from the lobby.");
            yield return Tap(Key.Space);
            Check(CafeWorld.RoomLayer(game.World.Player.PlanPosition) == 14, "Hall door must enter the tentacle passage.");
            game.World.Player.Teleport(new Vector2(40, 7.6f));
            yield return new WaitForSeconds(0.15f);
            yield return Tap(Key.Space);
            Check(game.Phase == ShiftPhase.Incident && CafeWorld.RoomLayer(game.World.Player.PlanPosition) == 10,
                "The far tentacle exit must lead to the hall and start Day 3.");
            ScreenCapture.CaptureScreenshot(OutputPath("11-hall.png"));
            yield return null;

            game.World.Player.Teleport(new Vector2(-11.5f, -7.1f));
            yield return new WaitForSeconds(0.15f);
            yield return Tap(Key.Space);
            Check(CafeWorld.RoomLayer(game.World.Player.PlanPosition) == 15, "Lobby shortcut must open the pursuit passage.");
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.W, Key.LeftShift));
            yield return new WaitForSeconds(2.2f);
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
            yield return Tap(Key.Space);
            Check(CafeWorld.RoomLayer(game.World.Player.PlanPosition) == 10 && game.Phase == ShiftPhase.Incident,
                "Sprinting must reach the counter exit ahead of the pursuer.");

            game.Interact(routes.detourDoor);
            float before = game.Later.Remaining;
            yield return new WaitForSeconds(0.2f);
            Check(game.view.reading.activeSelf && !routes.detourDoor.isOpen && game.Later.Remaining < before,
                "The detour puzzle must block the door while the night timer keeps running.");
            ScreenCapture.CaptureScreenshot(OutputPath("12-detour-puzzle.png"));
            yield return null;
            foreach (var key in new[] { Key.W, Key.W, Key.S, Key.D, Key.D, Key.W }) yield return Tap(key);
            Check(routes.Puzzle.Solved && routes.detourDoor.isOpen && !game.view.reading.activeSelf, "Solving the detour puzzle must unlock its door.");
            if (game.InputBlocked) game.CloseOverlay();

            foreach (var seat in content.seats) game.Interact(seat.chair);
            Check(content.seats.Length == 8 && game.Later.Warehouse && !content.seats[0].chair.enabled,
                "All eight chairs must be placed on tables before the warehouse incident.");
            game.World.Player.Teleport(new Vector2(15.8f, 6.15f));
            yield return new WaitForSeconds(0.15f);
            Check(game.CurrentTarget == Task(NightAction.Eraser), "The eraser must be reachable in the warehouse.");
            yield return Tap(Key.Space);
            var board = Task(NightAction.Board);
            game.World.Player.Teleport(new Vector2(15.45f, 8.55f));
            yield return new WaitForSeconds(0.15f);
            Check(game.CurrentTarget == board, "The chalkboard must have a clear interaction point.");
            float deadline = Time.time + 6;
            while (!content.ChalkIdle && Time.time < deadline) yield return null;
            ScreenCapture.CaptureScreenshot(OutputPath("13-warehouse.png"));
            yield return null;
            yield return Tap(Key.Space);
            Check(game.Later.BoardErased && game.Phase == ShiftPhase.Incident, "The eraser must clear the board during a pause.");
            yield return Tap(Key.Space);
            Check(game.Phase == ShiftPhase.Resolved, "Writing the closing notice must resolve Day 3.");
            game.World.Player.Teleport(CafeWorld.SecuritySpawn);
            yield return new WaitForSeconds(0.15f);
            Check(game.Phase == ShiftPhase.Complete && game.view.complete.activeSelf, "Returning to security must complete Day 3.");
        }

        private static Color[] CaptureBackgroundSamples()
        {
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            // Uncovered display margins and a vacant part of the gameplay viewport.
            var points = new[] { new Vector2Int(8, 360), new Vector2Int(8, 540), new Vector2Int(1904, 540), new Vector2Int(100, 540) };
            var colors = Array.ConvertAll(points, point => screenshot.GetPixel(point.x, point.y));
            Destroy(screenshot);
            return colors;
        }

        private void CheckReachability()
        {
            // After doors open, all authored interactions must have a collision-free route
            // and a clear interaction ray. This also catches items hidden inside furniture.
            foreach (var item in game.World.Interactables) if (item.kind == InteractionKind.Door) item.SetDoor(true);
            Physics.SyncTransforms();
            const int columns = 77, rows = 43;
            var visited = new bool[columns, rows];
            var queue = new Queue<Vector2Int>();
            var reachable = new List<Vector2>();
            Vector2 origin = new Vector2(-18.5f, -10.5f);
            var start = new Vector2Int(10, 33);
            queue.Enqueue(start); visited[start.x, start.y] = true;
            Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var point = origin + (Vector2)node * 0.5f;
                reachable.Add(point);
                foreach (var offset in offsets)
                {
                    var next = node + offset;
                    if (next.x < 0 || next.y < 0 || next.x >= columns || next.y >= rows || visited[next.x, next.y]) continue;
                    visited[next.x, next.y] = true;
                    bool blocked = false;
                    foreach (var hit in Physics.OverlapCapsule(CafeWorld.GroundPosition(origin + (Vector2)next * 0.5f, 0.33f), CafeWorld.GroundPosition(origin + (Vector2)next * 0.5f, 1.42f), 0.33f, ~0, QueryTriggerInteraction.Ignore))
                        if (hit.gameObject != game.World.Player.gameObject && !hit.isTrigger) { blocked = true; break; }
                    if (!blocked) queue.Enqueue(next);
                }
            }
            foreach (var item in game.World.Interactables)
            {
                if (!item.isActiveAndEnabled) continue;
                bool accessible = false;
                foreach (var point in reachable)
                {
                    if (Vector2.Distance(point, CafeWorld.PlanPosition(item.transform.position)) >= game.Tuning.interactionDistance) continue;
                    bool blocked = false;
                    foreach (var hit in CafeWorld.InteractionHits(point, CafeWorld.PlanPosition(item.transform.position)))
                        if (hit.collider.gameObject != game.World.Player.gameObject && hit.collider.gameObject != item.gameObject && !hit.collider.isTrigger) { blocked = true; break; }
                    if (!blocked) { accessible = true; break; }
                }
                Check(accessible, "Unreachable interaction: " + item.title);
            }
        }
    }
}
