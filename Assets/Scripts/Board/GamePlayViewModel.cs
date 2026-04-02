using UnityEngine;

/// <summary>
/// Exposes observable board-UI state for <see cref="GamePlayView"/> to bind against.
/// Created and updated by <see cref="GamePlayBoard"/>; consumed read-only by the View.
/// </summary>
public class GamePlayViewModel : ViewModelBase
{
    // ── Move limit ────────────────────────────────────────────────────────────

    /// <summary><c>true</c> when the level has a move limit and the moves counter should be shown.</summary>
    public BindableProperty<bool> HasMoveLimit   = new BindableProperty<bool>(false);

    /// <summary>Remaining moves. Only meaningful when <see cref="HasMoveLimit"/> is <c>true</c>.</summary>
    public BindableProperty<int>  MovesRemaining = new BindableProperty<int>(0);

    // ── Goal slot 0 ───────────────────────────────────────────────────────────

    /// <summary><c>true</c> when goal slot 0 is in use and its UI should be visible.</summary>
    public BindableProperty<bool>   Goal0Active    = new BindableProperty<bool>(false);

    /// <summary>Sprite for the brick type of goal slot 0.</summary>
    public BindableProperty<Sprite> Goal0Sprite    = new BindableProperty<Sprite>(null);

    /// <summary>Remaining brick count for goal slot 0.</summary>
    public BindableProperty<int>    Goal0Remaining = new BindableProperty<int>(0);

    // ── Goal slot 1 ───────────────────────────────────────────────────────────

    /// <summary><c>true</c> when goal slot 1 is in use and its UI should be visible.</summary>
    public BindableProperty<bool>   Goal1Active    = new BindableProperty<bool>(false);

    /// <summary>Sprite for the brick type of goal slot 1.</summary>
    public BindableProperty<Sprite> Goal1Sprite    = new BindableProperty<Sprite>(null);

    /// <summary>Remaining brick count for goal slot 1.</summary>
    public BindableProperty<int>    Goal1Remaining = new BindableProperty<int>(0);

    // ── Goal slot 2 ───────────────────────────────────────────────────────────

    /// <summary><c>true</c> when goal slot 2 is in use and its UI should be visible.</summary>
    public BindableProperty<bool>   Goal2Active    = new BindableProperty<bool>(false);

    /// <summary>Sprite for the brick type of goal slot 2.</summary>
    public BindableProperty<Sprite> Goal2Sprite    = new BindableProperty<Sprite>(null);

    /// <summary>Remaining brick count for goal slot 2.</summary>
    public BindableProperty<int>    Goal2Remaining = new BindableProperty<int>(0);

    // ── Outcome ───────────────────────────────────────────────────────────────

    /// <summary>Set to <c>true</c> once all goals are completed.</summary>
    public BindableProperty<bool> IsVictory = new BindableProperty<bool>(false);

    /// <summary>Set to <c>true</c> when the move limit is exhausted without completing all goals.</summary>
    public BindableProperty<bool> IsFailed  = new BindableProperty<bool>(false);
}
