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
}
