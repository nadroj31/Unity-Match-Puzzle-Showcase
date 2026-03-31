using UnityEngine;

/// <summary>
/// Instantiates <see cref="BrickShow"/> GameObjects from <see cref="Brick"/> data.
/// To support a new brick type with unique behaviour, subclass this factory and
/// override <see cref="CreateBrick"/>.
/// </summary>
public class BrickFactory : MonoBehaviour
{
    [SerializeField] private GameObject brickPrefab;

    /// <summary>
    /// Instantiates a <see cref="BrickShow"/> for <paramref name="brick"/> under <paramref name="parent"/>.
    /// Returns <c>null</c> for <see cref="BrickTypeSO.isNone"/> bricks (empty cells).
    /// </summary>
    public virtual BrickShow CreateBrick(Brick brick, Transform parent)
    {
        if (brick.BrickType.isNone)
            return null;

        var go = Instantiate(brickPrefab, parent);
        go.name = $"Brick({brick.X},{brick.Y})";

        var brickShow = go.GetComponent<BrickShow>();
        brickShow.SetData(brick);
        return brickShow;
    }
}
