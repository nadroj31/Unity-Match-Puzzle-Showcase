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

    public BrickTypeSO GoalType  { get; }
    public int         Remaining { get; private set; }

    public GoalTracker(BrickTypeSO goalType, int count)
    {
        GoalType  = goalType;
        Remaining = count;
    }

    /// <summary>
    /// Registers a completed match. Decrements the counter when the matched type qualifies.
    /// A goal whose <see cref="BrickTypeSO.isRandom"/> flag is set accepts any colour.
    /// </summary>
    public void RegisterMatch(BrickTypeSO matchedType, int count)
    {
        if (!GoalType.isRandom && GoalType != matchedType)
            return;

        Remaining = Mathf.Max(Remaining - count, 0);
        OnGoalCountChanged?.Invoke(Remaining);

        if (Remaining <= 0)
            OnGoalCompleted?.Invoke();
    }
}
