using UnityEngine;

/// <summary>
/// Central registry for all <see cref="BrickTypeSO"/> assets used in a game.
/// Assign one registry asset to <see cref="GamePlayBoard"/> and <see cref="LevelRepository"/>
/// via the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "BrickTypeRegistry", menuName = "Game/Brick Type Registry")]
public class BrickTypeRegistry : ScriptableObject
{
    [Tooltip("The empty / destroyed-cell placeholder type.")]
    [SerializeField] private BrickTypeSO noneType;

    [Tooltip("The wildcard goal type — matches any colour.")]
    [SerializeField] private BrickTypeSO randomType;

    [Tooltip("All playable colour types. These are the types that appear on the board and are picked at random when refilling.")]
    [SerializeField] private BrickTypeSO[] playableTypes;

    /// <summary>The empty / destroyed-cell placeholder.</summary>
    public BrickTypeSO NoneType   => noneType;

    /// <summary>The wildcard goal type — matches any colour.</summary>
    public BrickTypeSO RandomType => randomType;

    /// <summary>Returns a uniformly random playable brick type.</summary>
    public BrickTypeSO GetRandom()
        => playableTypes[Random.Range(0, playableTypes.Length)];

    /// <summary>
    /// Looks up a playable type by its JSON character code.
    /// Returns <see cref="RandomType"/> if no match is found, so unknown codes
    /// generate a random colour brick (same behaviour as before).
    /// </summary>
    public BrickTypeSO GetByCode(string code)
    {
        foreach (var t in playableTypes)
            if (t.code == code) return t;
        return randomType;
    }
}
