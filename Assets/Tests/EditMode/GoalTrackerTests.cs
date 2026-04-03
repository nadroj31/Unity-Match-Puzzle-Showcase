using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Edit-mode unit tests for <see cref="GoalTracker"/>.
/// Covers counting, completion guard, and event firing behaviour.
/// </summary>
public class GoalTrackerTests
{
    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a transient BrickTypeSO instance for testing.
    /// Uses SerializedObject to set private [SerializeField] fields because
    /// BrickTypeSO fields are intentionally encapsulated (no public setters).
    /// </summary>
    private static BrickTypeSO MakeBrick(bool isRandom = false, bool isNone = false)
    {
        var brick = ScriptableObject.CreateInstance<BrickTypeSO>();
        var so    = new SerializedObject(brick);
        so.FindProperty("isRandom").boolValue = isRandom;
        so.FindProperty("isNone").boolValue   = isNone;
        so.ApplyModifiedProperties();
        return brick;
    }

    // ── Initial state ─────────────────────────────────────────────────────────

    [Test]
    public void Remaining_StartsAtInitialCount()
    {
        var tracker = new GoalTracker(MakeBrick(), 10);
        Assert.AreEqual(10, tracker.Remaining);
    }

    [Test]
    public void IsComplete_FalseAtStart()
    {
        var tracker = new GoalTracker(MakeBrick(), 10);
        Assert.IsFalse(tracker.IsComplete);
    }

    // ── Counting ──────────────────────────────────────────────────────────────

    [Test]
    public void RegisterMatch_SameBrick_DecreasesRemaining()
    {
        var brick   = MakeBrick();
        var tracker = new GoalTracker(brick, 10);

        tracker.RegisterMatch(brick, 3);

        Assert.AreEqual(7, tracker.Remaining);
    }

    [Test]
    public void RegisterMatch_DifferentBrick_DoesNotDecrement()
    {
        var goal    = MakeBrick();
        var other   = MakeBrick(); // different reference = different type
        var tracker = new GoalTracker(goal, 10);

        tracker.RegisterMatch(other, 5);

        Assert.AreEqual(10, tracker.Remaining); // unchanged
    }

    [Test]
    public void RegisterMatch_ExcessCount_ClampedToZero()
    {
        var brick   = MakeBrick();
        var tracker = new GoalTracker(brick, 5);

        tracker.RegisterMatch(brick, 100); // way more than needed

        Assert.AreEqual(0, tracker.Remaining); // never goes negative
    }

    // ── Completion ────────────────────────────────────────────────────────────

    [Test]
    public void IsComplete_TrueWhenRemainingReachesZero()
    {
        var brick   = MakeBrick();
        var tracker = new GoalTracker(brick, 5);

        tracker.RegisterMatch(brick, 5);

        Assert.IsTrue(tracker.IsComplete);
    }

    [Test]
    public void RegisterMatch_AfterComplete_IsIgnored()
    {
        // This guards against the bug where Remaining would be re-evaluated
        // after completion and OnGoalCompleted would fire multiple times.
        var brick   = MakeBrick();
        var tracker = new GoalTracker(brick, 5);

        tracker.RegisterMatch(brick, 5);  // completes goal
        tracker.RegisterMatch(brick, 10); // must be silently ignored

        Assert.AreEqual(0, tracker.Remaining);
        Assert.IsTrue(tracker.IsComplete);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    [Test]
    public void OnGoalCompleted_FiresExactlyOnce()
    {
        var brick    = MakeBrick();
        var tracker  = new GoalTracker(brick, 5);
        int fireCount = 0;
        tracker.OnGoalCompleted += () => fireCount++;

        tracker.RegisterMatch(brick, 5);  // completes → fires once
        tracker.RegisterMatch(brick, 5);  // ignored   → must NOT fire again
        tracker.RegisterMatch(brick, 5);  // ignored   → must NOT fire again

        Assert.AreEqual(1, fireCount);
    }

    [Test]
    public void OnGoalCountChanged_FiresOnEveryDecrement()
    {
        var brick    = MakeBrick();
        var tracker  = new GoalTracker(brick, 10);
        int fireCount = 0;
        tracker.OnGoalCountChanged += _ => fireCount++;

        tracker.RegisterMatch(brick, 3);
        tracker.RegisterMatch(brick, 2);

        Assert.AreEqual(2, fireCount);
    }

    [Test]
    public void OnGoalCountChanged_ReportsCorrectRemainingValues()
    {
        var brick      = MakeBrick();
        var tracker    = new GoalTracker(brick, 10);
        int lastValue  = -1;
        tracker.OnGoalCountChanged += v => lastValue = v;

        tracker.RegisterMatch(brick, 4);
        Assert.AreEqual(6, lastValue);

        tracker.RegisterMatch(brick, 3);
        Assert.AreEqual(3, lastValue);
    }

    // ── Wildcard (IsRandom) ───────────────────────────────────────────────────

    [Test]
    public void RandomGoal_AcceptsAnyBrickType()
    {
        // A goal whose GoalType.IsRandom == true should accept any matched colour.
        var randomGoal = MakeBrick(isRandom: true);
        var blue       = MakeBrick();
        var red        = MakeBrick();
        var tracker    = new GoalTracker(randomGoal, 10);

        tracker.RegisterMatch(blue, 3);
        tracker.RegisterMatch(red,  4);

        Assert.AreEqual(3, tracker.Remaining); // 10 - 3 - 4 = 3
    }
}
