using UnityEngine;

/// <summary>
/// Identifies a single brick type as a ScriptableObject asset.
/// Create one asset per type via <c>Assets → Create → Game → Brick Type</c>.
/// </summary>
[CreateAssetMenu(fileName = "BrickType_New", menuName = "Game/Brick Type")]
public class BrickTypeSO : ScriptableObject
{
    [Tooltip("Single-character JSON code (e.g. \"b\", \"g\"). Empty for None/Random types.")]
    [SerializeField] private string code;

    [Tooltip("Empty/destroyed-cell placeholder. Only one type should set this.")]
    [SerializeField] private bool isNone;

    [Tooltip("Wildcard goal type — matches any colour. Only one type should set this.")]
    [SerializeField] private bool isRandom;

    /// <summary>Single-character JSON code used to identify this type in level data.</summary>
    public string Code     => code;

    /// <summary>True if this type represents an empty/destroyed cell rather than a real brick.</summary>
    public bool   IsNone   => isNone;

    /// <summary>True if this type is a wildcard goal that accepts any matched colour.</summary>
    public bool   IsRandom => isRandom;
}
