using System;
using UnityEngine;

/// <summary>
/// Tracks progress toward the level goal and fires events when updated or completed.
/// </summary>
public class GoalTracker
{
    /// <summary>Fired each time the remaining count changes. Argument is the new count.</summary>
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
    /// Registers a completed match. Decrements the counter when the matched type qualifies.
    /// <see cref="BrickType.RANDOM_BRICK"/> as goal type accepts any colour.
    /// </summary>
    public void RegisterMatch(BrickType matchedType, int count)
    {
        if (GoalType != BrickType.RANDOM_BRICK && GoalType != matchedType)
            return;

        Remaining = Mathf.Max(Remaining - count, 0);
        OnGoalCountChanged?.Invoke(Remaining);

        if (Remaining <= 0)
            OnGoalCompleted?.Invoke();
    }
}
