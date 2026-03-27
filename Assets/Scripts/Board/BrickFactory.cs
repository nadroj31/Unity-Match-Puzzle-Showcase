using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instantiates <see cref="BrickShow"/> GameObjects from <see cref="Brick"/> data.
/// To support a new brick type, add a matching entry to <see cref="brickCreators"/>.
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
