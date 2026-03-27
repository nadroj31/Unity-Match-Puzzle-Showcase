using UnityEngine;

/// <summary>
/// Holds transient session data shared across scenes.
/// Replaces PlayerPrefs for in-memory cross-scene communication.
/// Assign a single asset instance in the Inspector wherever needed.
/// </summary>
[CreateAssetMenu(fileName = "GameSession", menuName = "Game/GameSession")]
public class GameSession : ScriptableObject
{
    public int SelectedLevel;
}
