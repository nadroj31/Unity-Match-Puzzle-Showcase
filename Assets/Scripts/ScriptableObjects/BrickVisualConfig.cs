using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps BrickType to its Sprite via the Inspector.
/// Replaces the fragile index-based sprites[] array.
/// Call Initialize() once before using GetSprite().
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
