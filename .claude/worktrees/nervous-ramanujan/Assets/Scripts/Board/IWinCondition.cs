using System;

/// <summary>
/// Defines a pluggable win condition for a gameplay session.
/// Implement this interface (as a <c>MonoBehaviour</c>) and assign it to
/// <see cref="GamePlayBoard"/> via the Inspector to change how victory is determined.
/// </summary>
public interface IWinCondition
{
    /// <summary>Fired once when the win condition is satisfied.</summary>
    event Action OnCompleted;

    /// <summary>
    /// Called once at the start of the level. The implementation is responsible for
    /// setting up any initial HUD state on <paramref name="viewModel"/>.
    /// </summary>
    void Initialize(
        LevelDetails       levelDetails,
        BrickTypeRegistry  registry,
        BrickVisualConfig  visualConfig,
        GamePlayViewModel  viewModel);

    /// <summary>
    /// Called after every successful match. The implementation decides whether
    /// the match contributes toward the win condition.
    /// </summary>
    void OnMatchMade(BrickTypeSO matchedType, int count);
}
