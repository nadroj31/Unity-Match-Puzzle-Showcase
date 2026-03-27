using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Stateless game-logic layer. Pure algorithms — no MonoBehaviour, no scene coupling.</summary>
public static class BoardLogic
{
    private static readonly int[] DirX = {  1, -1,  0,  0 };
    private static readonly int[] DirY = {  0,  0,  1, -1 };

    // ── Flood-fill ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all bricks orthogonally connected to <paramref name="startBrick"/>
    /// that share the same <see cref="BrickType"/>. Uses iterative BFS.
    /// </summary>
    public static List<Brick> FindMatchBricks(Brick startBrick, Brick[,] board)
    {
        int width  = board.GetLength(0);
        int height = board.GetLength(1);

        var result  = new List<Brick>();
        var visited = new HashSet<Brick> { startBrick };
        var queue   = new Queue<Brick>();
        queue.Enqueue(startBrick);

        while (queue.Count > 0)
        {
            Brick current = queue.Dequeue();
            result.Add(current);

            for (int d = 0; d < 4; d++)
            {
                int nx = current.X + DirX[d];
                int ny = current.Y + DirY[d];

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                Brick neighbor = board[nx, ny];
                if (visited.Contains(neighbor) || neighbor.BrickType != startBrick.BrickType)
                    continue;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return result;
    }

    // ── Gravity ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Collapses each affected column after bricks are removed; fills vacated top
    /// slots with new random bricks. Fires <paramref name="onBrickMoved"/> for every
    /// position that changes:
    /// <list type="bullet">
    ///   <item>(sourceBrick, targetBrick) — existing brick slides down</item>
    ///   <item>(null, targetBrick)        — new random brick drops in from above</item>
    /// </list>
    /// </summary>
    public static void ApplyGravity(
        List<Brick>          removedBricks,
        Brick[,]             board,
        Action<Brick, Brick> onBrickMoved)
    {
        int height = board.GetLength(1);

        var removedByColumn = removedBricks
            .GroupBy(b => b.X)
            .ToDictionary(g => g.Key, g => new HashSet<int>(g.Select(b => b.Y)));

        foreach (var (col, removedRows) in removedByColumn)
        {
            // Collect surviving bricks bottom-to-top
            var survivors = new List<Brick>(height);
            for (int y = 0; y < height; y++)
            {
                if (!removedRows.Contains(y))
                    survivors.Add(board[col, y]);
            }

            // Drop survivors; source Y is always >= target Y so reads are safe before writes
            for (int y = 0; y < survivors.Count; y++)
            {
                Brick source = survivors[y];
                Brick target = board[col, y];

                if (source.Y == y) continue;

                target.SetBrickType(source.BrickType);
                onBrickMoved?.Invoke(source, target);
            }

            // Fill vacated top slots with random bricks
            for (int y = survivors.Count; y < height; y++)
            {
                board[col, y].SetBrickType(GetRandomBrickType());
                onBrickMoved?.Invoke(null, board[col, y]);
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns a random colour brick type, excluding NONE and RANDOM_BRICK.</summary>
    public static BrickType GetRandomBrickType()
    {
        BrickType[] types = (BrickType[])Enum.GetValues(typeof(BrickType));
        return types[UnityEngine.Random.Range(2, types.Length)];
    }
}
