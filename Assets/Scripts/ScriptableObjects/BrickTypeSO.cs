using UnityEngine;

/// <summary>
/// Identifies a single brick type as a ScriptableObject asset.
/// Create one asset per type via <c>Assets → Create → Game → Brick Type</c>.
/// </summary>
[CreateAssetMenu(fileName = "BrickType_New", menuName = "Game/Brick Type")]
public class BrickTypeSO : ScriptableObject
{
    [Tooltip("Single-character code used in level JSON (e.g. \"b\", \"g\"). Leave empty for None and Random types.")]
    [SerializeField] public string code;

    [Tooltip("Marks this as the empty / destroyed-cell placeholder. Exactly one type should set this.")]
    [SerializeField] public bool isNone;

    [Tooltip("Marks this as the wildcard goal type — accepts any colour. Exactly one type should set this.")]
    [SerializeField] public bool isRandom;
}
