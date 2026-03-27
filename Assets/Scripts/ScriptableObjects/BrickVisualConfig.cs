using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps each <see cref="BrickType"/> to its sprite via the Inspector.
/// Call <see cref="Initialize"/> once before using <see cref="GetSprite"/>.
/// </summary>
[CreateAssetMenu(fileName = "BrickVisualConfig", menuName = "Game/BrickVisualConfig")]
public class BrickVisualConfig : ScriptableObject
{
    [Serializable]
    public struct BrickVisual
    {
        public BrickType brickType;
        public Sprite    sprite;
    }

    [SerializeField] private BrickVisual[] visuals;

    private Dictionary<BrickType, Sprite> spriteMap;

    public void Initialize()
    {
        spriteMap = new Dictionary<BrickType, Sprite>(visuals.Length);
        foreach (var v in visuals)
            spriteMap[v.brickType] = v.sprite;
    }

    public Sprite GetSprite(BrickType type)
    {
        if (spriteMap != null && spriteMap.TryGetValue(type, out var sprite))
            return sprite;

        Debug.LogWarning($"[BrickVisualConfig] No sprite configured for BrickType: {type}");
        return null;
    }
}
