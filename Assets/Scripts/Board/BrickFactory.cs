using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates BrickShow GameObjects from Brick data.
/// Extensible: add a new BrickType entry to <see cref="brickCreators"/> to support new brick kinds.
///
/// Singleton removed — attach to a GameObject in the scene and inject into
/// <see cref="GamePlayBoard"/> via the Inspector.
/// </summary>
public class BrickFactory : MonoBehaviour
{
    [SerializeField] private GameObject brickPrefab;

    private static readonly Dictionary<BrickType, Func<Brick, GameObject, BrickShow>> brickCreators
        = new Dictionary<BrickType, Func<Brick, GameObject, BrickShow>>
        {
            { BrickType.BLUE_BRICK,   CreateColorBrick },
            { BrickType.GREEN_BRICK,  CreateColorBrick },
            { BrickType.YELLOW_BRICK, CreateColorBrick },
            { BrickType.RED_BRICK,    CreateColorBrick },
        };

    public BrickShow CreateBrick(Brick brick, Transform parent)
    {
        if (brick.BrickType == BrickType.NONE)
            return null;

        var go = Instantiate(brickPrefab, parent);
        go.name = $"Brick({brick.X},{brick.Y})";

        if (brickCreators.TryGetValue(brick.BrickType, out var create))
            return create(brick, go);

        Debug.LogError($"[BrickFactory] No creator registered for BrickType: {brick.BrickType}");
        Destroy(go);
        return null;
    }

    private static BrickShow CreateColorBrick(Brick brick, GameObject go)
    {
        var brickShow = go.GetComponent<BrickShow>();
        brickShow.SetData(brick);
        return brickShow;
    }
}
