using System.Collections.Generic;
using UnityEngine;

namespace NightCafe
{
    public static class CafeNavigation
    {
        // Room-sized flood search keeps the event pursuer out of solid furniture.
        // The returned step is always rechecked against the live 2D colliders.
        public static Vector2 NextStep(Vector2 start, Vector2 target, int room, GameObject player)
        {
            if (Vector2.Distance(start, target) < 0.65f) return target;
            bool clear=true;
            foreach(var hit in Physics2D.CircleCastAll(start,0.32f,(target-start).normalized,Vector2.Distance(start,target)))
                if(!hit.collider.isTrigger && hit.collider.gameObject!=player) { clear=false;break; }
            if(clear) return target;
            const float cell = 0.6f;
            Rect bounds = CafeWorld.Bounds(room);
            Vector2Int Node(Vector2 p) => Vector2Int.RoundToInt((p - bounds.min) / cell);
            Vector2 Point(Vector2Int n) => bounds.min + (Vector2)n * cell;
            var source = Node(start); var goal = Node(target);
            var queue = new Queue<Vector2Int>(); var parents = new Dictionary<Vector2Int, Vector2Int>();
            queue.Enqueue(source); parents[source] = source;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            bool reached = false;
            while (queue.Count > 0 && parents.Count < 2400)
            {
                var node = queue.Dequeue();
                if (node == goal) { reached = true; break; }
                foreach (var delta in directions)
                {
                    var next = node + delta; Vector2 p = Point(next);
                    if (parents.ContainsKey(next) || !bounds.Contains(p)) continue;
                    bool blocked = false;
                    foreach (var hit in Physics2D.OverlapCircleAll(p, 0.32f))
                        if (!hit.isTrigger && hit.gameObject != player) { blocked = true; break; }
                    if (blocked) continue;
                    parents[next] = node; queue.Enqueue(next);
                }
            }
            if (!reached) return start;
            var step = goal;
            while (parents[step] != source && step != source) step = parents[step];
            return Point(step);
        }
    }
}
