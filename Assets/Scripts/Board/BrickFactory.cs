using UnityEngine;

/// <summary>
/// Instantiates <see cref="BrickShow"/> GameObjects from <see cref="Brick"/> data.
/// To support a new brick type with unique behaviour, subclass this factory and
/// override <see cref="CreateBrick"/>.
/// </summary>
public class BrickFactory : MonoBehaviour
{
    [Tooltip("Prefab to instantiate for each brick. Must have a BrickShow component.")]
    [SerializeField] private GameObject brickPrefab;

    /// <summary>
    /// Instantiates a <see cref="BrickShow"/> for <paramref name="brick"/> under <paramref name="parent"/>.
    /// Returns <c>null</c> for <see cref="BrickTypeSO.IsNone"/> bricks (empty cells).
    /// </summary>
    public virtual BrickShow CreateBrick(Brick brick, Transform parent)
    {
        if (brick.BrickType.IsNone)
            return null;

        var go = Instantiate(brickPrefab, parent);
        go.name = $"Brick({brick.X},{brick.Y})";

        var brickShow = go.GetComponent<BrickShow>();
        if (brickShow == null)
        {
            Debug.LogError("[BrickFactory] brickPrefab does not have a BrickShow component.", go);
            Destroy(go);
            return null;
        }

        brickShow.SetData(brick);
        return brickShow;
    }
}
