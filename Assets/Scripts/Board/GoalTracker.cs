using System;
using UnityEngine;

/// <summary>
/// Plain C# class that tracks the level goal.
/// Fires events instead of touching UI directly — keeps logic and view decoupled.
/// </summary>
public class GoalTracker
{
    /// <summary>Fired whenever the remaining count changes. Passes the new count.</summary>
    public event Action<int> OnGoalCountChanged;

    /// <summary>Fired once when the remaining count reaches zero.</summary>
    public event Action OnGoalCompleted;

    public BrickType GoalType  { get; }
    public int       Remaining { get; private set; }

    public GoalTracker(BrickType goalType, int count)
    {
        GoalType  = goalType;
        Remaining = count;
    }

    /// <summary>
    /// Call this after a match is made.
    /// If the matched type qualifies toward the goal the counter is decremented.
    /// </summary>
    public void RegisterMatch(BrickType matchedType, int count)
    {
        // RANDOM_BRICK goal = every colour counts
        if (GoalType != BrickType.RANDOM_BRICK && GoalType != matchedType)
            return;

        Remaining = Mathf.Max(Remaining - count, 0);
        OnGoalCountChanged?.Invoke(Remaining);

        if (Remaining <= 0)
            OnGoalCompleted?.Invoke();
    }
}
