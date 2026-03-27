using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Stateless, static game-logic layer for the board.
/// No singleton, no MonoBehaviour — pure algorithms with no Unity scene coupling.
/// Replaces GamePlayBoardHandler.
/// </summary>
public static class BoardLogic
{
    private static readonly int[] DirX = { 1, -1, 0,  0 };
    private static readonly int[] DirY = { 0,  0, 1, -1 };

    // -------------------------------------------------------------------------
    // Flood-fill (iterative BFS — no recursion, no stack-overflow risk)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all bricks orthogonally connected to <paramref name="startBrick"/>
    /// that share the same BrickType.
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

    // -------------------------------------------------------------------------
    // Gravity / board fill
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies gravity to each affected column after bricks are removed.
    /// Surviving bricks fall to the bottom; random bricks fill from the top.
    ///
    /// <paramref name="onBrickMoved"/> fires for every position that changes:
    ///   - (sourceBrick, targetBrick)  → existing brick slides down
    ///   - (null,        targetBrick)  → new random brick drops in from above
    ///
    /// The callback is responsible for updating the view layer.
    /// </summary>
    public static void ApplyGravity(
        List<Brick>            removedBricks,
        Brick[,]               board,
        Action<Brick, Brick>   onBrickMoved)
    {
        int height = board.GetLength(1);

        // Group removed rows by column
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

            // Drop survivors to the bottom of the column.
            // We read survivors[y].BrickType BEFORE any write to that slot,
            // because survivors[y].Y >= y always (bricks only fall down).
            for (int y = 0; y < survivors.Count; y++)
            {
                Brick source = survivors[y];
                Brick target = board[col, y];

                if (source.Y == y) continue; // already in correct slot

                target.SetBrickType(source.BrickType);
                onBrickMoved?.Invoke(source, target);
            }

            // Fill vacated top slots with new random bricks
            for (int y = survivors.Count; y < height; y++)
            {
                board[col, y].SetBrickType(GetRandomBrickType());
                onBrickMoved?.Invoke(null, board[col, y]);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    public static BrickType GetRandomBrickType()
    {
        BrickType[] types = (BrickType[])Enum.GetValues(typeof(BrickType));
        // Index 0 = NONE, 1 = RANDOM_BRICK, 2+ = actual colour bricks
        return types[UnityEngine.Random.Range(2, types.Length)];
    }
}
