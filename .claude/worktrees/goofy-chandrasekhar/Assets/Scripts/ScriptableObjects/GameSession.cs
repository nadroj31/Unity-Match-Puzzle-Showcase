using UnityEngine;

/// <summary>
/// ScriptableObject that carries transient session data between scenes.
/// Assign the same asset instance in every Inspector slot that needs it.
/// </summary>
[CreateAssetMenu(fileName = "GameSession", menuName = "Game/GameSession")]
public class GameSession : ScriptableObject
{
    public int SelectedLevel;
}
