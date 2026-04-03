using System;
using UnityEngine;

/// <summary>
/// Win condition: clear all brick goals defined in the level data.
/// Supports 0–3 simultaneous brick-type goals. Victory fires once every goal reaches zero.
/// A wildcard goal type (<see cref="BrickTypeSO.IsRandom"/>) accepts any colour.
/// </summary>
public class ClearGoalWinCondition : MonoBehaviour, IWinCondition
{
    /// <inheritdoc/>
    public event Action OnCompleted;

    private GoalTracker[] goalTrackers;
    private int           completedCount;

    /// <inheritdoc/>
    public void Initialize(
        LevelDetails      levelDetails,
        BrickTypeRegistry registry,
        BrickVisualConfig visualConfig,
        GamePlayViewModel viewModel)
    {
        var goals = levelDetails.goals;

        // No goals defined — no win condition (free-play mode).
        if (goals == null || goals.Length == 0)
        {
            InitSlot(viewModel, 0, null, visualConfig);
            InitSlot(viewModel, 1, null, visualConfig);
            InitSlot(viewModel, 2, null, visualConfig);
            return;
        }

        goalTrackers   = new GoalTracker[goals.Length];
        completedCount = 0;

        for (int i = 0; i < 3; i++)
            InitSlot(viewModel, i, i < goals.Length ? goals[i] : null, visualConfig);

        for (int i = 0; i < goals.Length; i++)
        {
            int captured = i;
            goalTrackers[i] = new GoalTracker(goals[i].BrickType, goals[i].Count);

            goalTrackers[i].OnGoalCountChanged += count => UpdateSlotRemaining(viewModel, captured, count);
            goalTrackers[i].OnGoalCompleted    += () =>
            {
                completedCount++;
                if (completedCount >= goals.Length)
                    OnCompleted?.Invoke();
            };
        }
    }

    /// <inheritdoc/>
    public void OnMatchMade(BrickTypeSO matchedType, int count)
    {
        if (goalTrackers == null) return;
        foreach (var tracker in goalTrackers)
            tracker.RegisterMatch(matchedType, count);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void InitSlot(GamePlayViewModel viewModel, int slot, GoalData goal, BrickVisualConfig visualConfig)
    {
        bool   active  = goal != null;
        Sprite sprite  = active ? visualConfig.GetSprite(goal.BrickType) : null;
        int    count   = active ? goal.Count : 0;

        switch (slot)
        {
            case 0:
                viewModel.Goal0Active.Value    = active;
                viewModel.Goal0Sprite.Value    = sprite;
                viewModel.Goal0Remaining.Value = count;
                break;
            case 1:
                viewModel.Goal1Active.Value    = active;
                viewModel.Goal1Sprite.Value    = sprite;
                viewModel.Goal1Remaining.Value = count;
                break;
            case 2:
                viewModel.Goal2Active.Value    = active;
                viewModel.Goal2Sprite.Value    = sprite;
                viewModel.Goal2Remaining.Value = count;
                break;
        }
    }

    private static void UpdateSlotRemaining(GamePlayViewModel viewModel, int slot, int count)
    {
        switch (slot)
        {
            case 0: viewModel.Goal0Remaining.Value = count; break;
            case 1: viewModel.Goal1Remaining.Value = count; break;
            case 2: viewModel.Goal2Remaining.Value = count; break;
        }
    }
}
