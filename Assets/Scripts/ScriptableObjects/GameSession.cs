using UnityEngine;

/// <summary>
/// ScriptableObject that carries transient session data between scenes.
/// Assign the same asset instance in every Inspector slot that needs it.
/// </summary>
[CreateAssetMenu(fileName = "GameSession", menuName = "Game/GameSession")]
public class GameSession : ScriptableObject
{
    /// <summary>Level number chosen on the main menu; read by the gameplay scene on load.</summary>
    public int SelectedLevel;

    /// <summary>
    /// When <c>true</c>, <see cref="MainMenu"/> will automatically open the level-select
    /// panel when the main scene loads. Set this before navigating to the main scene
    /// so the player returns directly to the level list instead of the title screen.
    /// Reset to <c>false</c> by <see cref="MainMenu"/> after consuming.
    /// </summary>
    public bool ReturnToLevelSelect;
}
