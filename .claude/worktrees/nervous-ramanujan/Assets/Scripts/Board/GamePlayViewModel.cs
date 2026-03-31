using UnityEngine;

/// <summary>
/// Exposes observable board-UI state for <see cref="GamePlayView"/> to bind against.
/// Created and updated by <see cref="GamePlayBoard"/>; consumed read-only by the View.
/// </summary>
public class GamePlayViewModel : ViewModelBase
{
    /// <summary>Remaining bricks until the goal is reached.</summary>
    public BindableProperty<int>    GoalRemaining = new BindableProperty<int>(0);

    /// <summary>Sprite that represents the current goal type.</summary>
    public BindableProperty<Sprite> GoalSprite    = new BindableProperty<Sprite>(null);

    /// <summary>Set to <c>true</c> once the goal is completed.</summary>
    public BindableProperty<bool>   IsVictory     = new BindableProperty<bool>(false);

    /// <summary><c>true</c> when the goal accepts any colour (no specific type required).</summary>
    public BindableProperty<bool>   IsGoalRandom  = new BindableProperty<bool>(false);
}
