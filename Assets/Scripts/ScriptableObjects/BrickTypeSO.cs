using UnityEngine;

/// <summary>
/// Identifies a single brick type as a ScriptableObject asset.
/// Create one asset per type via <c>Assets → Create → Game → Brick Type</c>.
/// </summary>
[CreateAssetMenu(fileName = "BrickType_New", menuName = "Game/Brick Type")]
public class BrickTypeSO : ScriptableObject
{
    [Tooltip("Single-character JSON code (e.g. \"b\", \"g\"). Empty for None/Random types.")]
    [SerializeField] public string code;

    [Tooltip("Empty/destroyed-cell placeholder. Only one type should set this.")]
    [SerializeField] public bool isNone;

    [Tooltip("Wildcard goal type — matches any colour. Only one type should set this.")]
    [SerializeField] public bool isRandom;
}
