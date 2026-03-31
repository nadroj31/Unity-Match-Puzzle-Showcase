using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Stateless gravity layer. Pure algorithm — no MonoBehaviour, no scene coupling.</summary>
public static class BoardLogic
{
    // ── Gravity ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Collapses each affected column after bricks are removed; fills vacated top
    /// slots with new random bricks from <paramref name="registry"/>. Fires
    /// <paramref name="onBrickMoved"/> for every position that changes:
    /// <list type="bullet">
    ///   <item>(sourceBrick, targetBrick) — existing brick slides down</item>
    ///   <item>(null, targetBrick)        — new random brick drops in from above</item>
    /// </list>
    /// </summary>
    public static void ApplyGravity(
        List<Brick>          removedBricks,
        Brick[,]             board,
        BrickTypeRegistry    registry,
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
                board[col, y].SetBrickType(registry.GetRandom());
                onBrickMoved?.Invoke(null, board[col, y]);
            }
        }
    }
}
