using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightCafe
{
    public enum DetourCell { Floor, Wall, Goal, Bag, Delivered, Worker }

    public sealed class DetourPuzzle
    {
        // # wall, . disposal point, $ bag, @ worker. Every layout has a solution.
        private static readonly string[][] Layouts =
        {
            new[] { "######", "# .  #", "# $  #", "# @  #", "#    #", "######" },
            new[] { "######", "#   .#", "#  $ #", "#  @ #", "#    #", "######" },
            new[] { "######", "# . .#", "# $ $#", "#    #", "# @  #", "######" },
            new[] { "######", "# .  #", "# $  #", "#  # #", "# @  #", "######" },
            new[] { "######", "#.  .#", "# $ $#", "#    #", "#  @ #", "######" }
        };

        public const int LayoutCount = 5;
        private readonly HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> goals = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> bags = new HashSet<Vector2Int>();
        private readonly int layout;
        private Vector2Int worker;
        public bool Solved => goals.SetEquals(bags);

        public DetourPuzzle(int index)
        {
            layout = index;
            Reset();
        }

        public void Reset()
        {
            bags.Clear();
            var rows = Layouts[layout];
            for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
            {
                var cell = new Vector2Int(x, y);
                switch (rows[y][x])
                {
                    case '#': walls.Add(cell); break;
                    case '.': goals.Add(cell); break;
                    case '$': bags.Add(cell); break;
                    case '@': worker = cell; break;
                }
            }
        }

        public void Move(Vector2Int direction)
        {
            if (Solved || Math.Abs(direction.x) + Math.Abs(direction.y) != 1) return;
            var next = worker + direction;
            if (walls.Contains(next)) return;
            if (bags.Contains(next))
            {
                var destination = next + direction;
                if (walls.Contains(destination) || bags.Contains(destination)) return;
                bags.Remove(next);
                bags.Add(destination);
            }
            worker = next;
        }

        public DetourCell CellAt(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            if (cell == worker) return DetourCell.Worker;
            if (walls.Contains(cell)) return DetourCell.Wall;
            if (bags.Contains(cell)) return goals.Contains(cell) ? DetourCell.Delivered : DetourCell.Bag;
            return goals.Contains(cell) ? DetourCell.Goal : DetourCell.Floor;
        }
    }
}
