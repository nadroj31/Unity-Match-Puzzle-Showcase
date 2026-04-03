using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Edit-mode unit tests for <see cref="BoardLogic.ApplyGravity"/>.
/// BoardLogic is a static class with no MonoBehaviour dependency, making it
/// straightforward to test without entering Play Mode.
/// </summary>
public class BoardLogicTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BrickTypeSO MakeBrick()
        => ScriptableObject.CreateInstance<BrickTypeSO>();

    /// <summary>
    /// Builds a registry whose GetRandom() returns <paramref name="fillType"/>.
    /// Used to control what ApplyGravity spawns into vacated top slots.
    /// </summary>
    private static BrickTypeRegistry MakeRegistry(BrickTypeSO fillType)
    {
        var reg = ScriptableObject.CreateInstance<BrickTypeRegistry>();
        var so  = new SerializedObject(reg);

        var arr = so.FindProperty("playableTypes");
        arr.arraySize = 1;
        arr.GetArrayElementAtIndex(0).objectReferenceValue = fillType;
        so.ApplyModifiedProperties();

        return reg;
    }

    /// <summary>
    /// Builds a W×H board filled entirely with <paramref name="type"/>.
    /// </summary>
    private static Brick[,] MakeBoard(int w, int h, BrickTypeSO type)
    {
        var board = new Brick[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                board[x, y] = new Brick(x, y, type);
        return board;
    }

    // ── Gravity: bricks fall down ─────────────────────────────────────────────

    [Test]
    public void ApplyGravity_BrickAboveGap_FallsDown()
    {
        // Column layout (y=0 is bottom):
        //   y=1: typeA  ← should fall to y=0
        //   y=0: REMOVED
        var typeA    = MakeBrick();
        var registry = MakeRegistry(typeA);
        var board    = MakeBoard(1, 2, typeA);
        var removed  = new List<Brick> { board[0, 0] };

        BoardLogic.ApplyGravity(removed, board, registry, null);

        // The cell at y=0 should now hold typeA (fallen from y=1).
        Assert.AreEqual(typeA, board[0, 0].BrickType);
    }

    [Test]
    public void ApplyGravity_MovedBrick_FiresOnBrickMoved()
    {
        var typeA    = MakeBrick();
        var registry = MakeRegistry(typeA);
        var board    = MakeBoard(1, 2, typeA);
        var removed  = new List<Brick> { board[0, 0] };

        var movedTargets = new List<Brick>();
        BoardLogic.ApplyGravity(removed, board, registry,
            (from, to) => { if (from != null) movedTargets.Add(to); });

        // Exactly one existing brick moved (from y=1 to y=0).
        Assert.AreEqual(1, movedTargets.Count);
        Assert.AreEqual(0, movedTargets[0].Y);
    }

    // ── Gravity: new bricks fill top ──────────────────────────────────────────

    [Test]
    public void ApplyGravity_VacatedTopSlot_FilledWithNewBrick()
    {
        // Remove the bottom brick; after the survivor falls, y=1 needs a new brick.
        var typeA    = MakeBrick();
        var registry = MakeRegistry(typeA);
        var board    = MakeBoard(1, 2, typeA);
        var removed  = new List<Brick> { board[0, 0] };

        bool newBrickDropped = false;
        BoardLogic.ApplyGravity(removed, board, registry,
            (from, to) => { if (from == null) newBrickDropped = true; });

        Assert.IsTrue(newBrickDropped);
    }

    [Test]
    public void ApplyGravity_NewBrick_TypeMatchesRegistry()
    {
        // The new brick spawned at the top should be whatever the registry provides.
        var typeA    = MakeBrick();
        var typeB    = MakeBrick(); // registry will fill with typeB
        var registry = MakeRegistry(typeB);
        var board    = MakeBoard(1, 2, typeA);
        var removed  = new List<Brick> { board[0, 0] };

        BoardLogic.ApplyGravity(removed, board, registry, null);

        // Top slot (y=1) must now hold typeB (newly spawned).
        Assert.AreEqual(typeB, board[0, 1].BrickType);
    }

    // ── Gravity: only affected columns change ─────────────────────────────────

    [Test]
    public void ApplyGravity_UnaffectedColumn_NeverFiresCallback()
    {
        // 2-column board; only remove from col 0.
        var typeA    = MakeBrick();
        var registry = MakeRegistry(typeA);
        var board    = MakeBoard(2, 2, typeA);
        var removed  = new List<Brick> { board[0, 0] };

        var touchedCols = new HashSet<int>();
        BoardLogic.ApplyGravity(removed, board, registry,
            (from, to) => touchedCols.Add(to.X));

        Assert.IsFalse(touchedCols.Contains(1),
            "Column 1 was not affected and must not receive any callbacks.");
    }

    [Test]
    public void ApplyGravity_RemoveEntireColumn_AllSlotsRefilled()
    {
        // Remove all bricks in a single column; every slot must be refilled.
        var typeA    = MakeBrick();
        var registry = MakeRegistry(typeA);
        int height   = 3;
        var board    = MakeBoard(1, height, typeA);

        var removed = new List<Brick>();
        for (int y = 0; y < height; y++) removed.Add(board[0, y]);

        int newBrickCount = 0;
        BoardLogic.ApplyGravity(removed, board, registry,
            (from, to) => { if (from == null) newBrickCount++; });

        Assert.AreEqual(height, newBrickCount,
            "All slots should be filled with new bricks when the entire column is removed.");
    }
}
