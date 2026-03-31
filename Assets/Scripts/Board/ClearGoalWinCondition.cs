using System;
using UnityEngine;

/// <summary>
/// Win condition: clear <c>goalNumber</c> bricks of the type specified by <c>goal</c>
/// in the level data. A wildcard goal type (<see cref="BrickTypeSO.isRandom"/>) accepts
/// any colour. This is the default Toon-Blast-style objective.
/// </summary>
public class ClearGoalWinCondition : MonoBehaviour, IWinCondition
{
    /// <inheritdoc/>
    public event Action OnCompleted;

    private GoalTracker goalTracker;

    /// <inheritdoc/>
    public void Initialize(
        LevelDetails      levelDetails,
        BrickTypeRegistry registry,
        BrickVisualConfig visualConfig,
        GamePlayViewModel viewModel)
    {
        BrickTypeSO goalType = registry.GetGoalByCode(levelDetails.goal);
        goalTracker = new GoalTracker(goalType, levelDetails.goalNumber);

        goalTracker.OnGoalCountChanged += count => viewModel.GoalRemaining.Value = count;
        goalTracker.OnGoalCompleted    += () => OnCompleted?.Invoke();

        // Push initial HUD state; random goals show no sprite
        viewModel.IsGoalRandom.Value  = goalType.isRandom;
        viewModel.GoalSprite.Value    = goalType.isRandom ? null : visualConfig.GetSprite(goalType);
        viewModel.GoalRemaining.Value = levelDetails.goalNumber;
    }

    /// <inheritdoc/>
    public void OnMatchMade(BrickTypeSO matchedType, int count)
        => goalTracker.RegisterMatch(matchedType, count);
}
