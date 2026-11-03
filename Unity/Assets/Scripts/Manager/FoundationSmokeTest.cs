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
            yield return new WaitForSeconds(0.6f);
            testKeyboard = InputSystem.AddDevice<Keyboard>();
            Check(Screen.width == 1920 && Screen.height == 1080, "Player must render at FHD.");
            Check(UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings == 2, "Exactly two scenes must be built.");
            Check((Camera.main.cullingMask & (1 << 10)) == 0, "Security movement view must hide cafe objects.");
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
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.Space));
            yield return new WaitForSeconds(0.1f);
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return new WaitForSeconds(0.5f);
            Vector3 initial = game.World.Player.transform.position;
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.D));
            yield return new WaitForSeconds(0.2f);
            Check(Vector3.Distance(initial, game.World.Player.transform.position) < 0.02f, "CCTV must block movement.");
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            ScreenCapture.CaptureScreenshot(OutputPath("02-cctv.png"));
            yield return new WaitForSeconds(0.3f);
            game.CloseOverlay();

            // Walk through the actual wall collider, then open and traverse the doorway.
            game.World.Player.Teleport(new Vector2(-13, 3.4f));
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.S));
            yield return new WaitForSeconds(0.6f);
            Check(game.World.Player.transform.position.y > 2.3f, "Closed security door must block the player.");
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
            var securityDoor = game.World.Interactables.Find(item => item.securityDoor);
            game.Interact(securityDoor);
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.S));
            yield return new WaitForSeconds(0.7f);
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
            Check(game.World.Player.transform.position.y < 2, "Open security door must permit passage.");

            game.World.Player.Teleport(CafeWorld.IncidentSpawn);
            yield return null;
            Check(game.Session.Phase == ShiftPhase.Incident, "Entering cafe must start incident.");
            Check((Camera.main.cullingMask & (1 << 10)) != 0 && (Camera.main.cullingMask & (1 << 8)) == 0, "Cafe movement view must reveal cafe and hide security objects.");
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
            InputSystem.RemoveDevice(testKeyboard);
            File.WriteAllText(OutputPath("player-smoke-test.txt"), failures.Count == 0 ? "PASS: FHD, two scenes, room visibility, reachable interactions, CCTV input, door collision, incident trigger, reading time, wrong ingredient, pause, failure, retry, correct recipe, cleanup, return and completion.\n" : string.Join("\n\n", failures));
            Application.Quit(failures.Count == 0 ? 0 : 1);
        }

        private void CheckReachability()
        {
            // After doors open, all authored interactions must have a collision-free route
            // and a clear interaction ray. This also catches items hidden inside furniture.
            foreach (var item in game.World.Interactables) if (item.kind == InteractionKind.Door) item.SetDoor(true);
            Physics2D.SyncTransforms();
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
                    foreach (var hit in Physics2D.OverlapCircleAll(origin + (Vector2)next * 0.5f, 0.33f))
                        if (hit.gameObject != game.World.Player.gameObject && !hit.isTrigger) { blocked = true; break; }
                    if (!blocked) queue.Enqueue(next);
                }
            }
            foreach (var item in game.World.Interactables)
            {
                bool accessible = false;
                foreach (var point in reachable)
                {
                    if (Vector2.Distance(point, item.transform.position) >= game.Tuning.interactionDistance) continue;
                    bool blocked = false;
                    foreach (var hit in Physics2D.LinecastAll(point, item.transform.position))
                        if (hit.collider.gameObject != game.World.Player.gameObject && hit.collider.gameObject != item.gameObject && !hit.collider.isTrigger) { blocked = true; break; }
                    if (!blocked) { accessible = true; break; }
                }
                Check(accessible, "Unreachable interaction: " + item.title);
            }
        }
    }
}
