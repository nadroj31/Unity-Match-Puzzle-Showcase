using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flood-fill match strategy: finds all bricks connected to the tapped brick
/// that share the same <see cref="BrickTypeSO"/>. Uses iterative BFS.
/// The neighbour directions are configurable via the Inspector — add diagonal
/// entries (e.g. <c>(1, 1)</c>) to enable 8-directional matching.
/// </summary>
[CreateAssetMenu(fileName = "ConnectedMatchStrategy", menuName = "Game/Match Strategy/Connected")]
public class ConnectedMatchStrategy : ScriptableObject, IMatchStrategy
{
    [Tooltip("Neighbour offsets to search. Default is 4-directional orthogonal.\nAdd e.g. (1,1), (-1,1), (1,-1), (-1,-1) for 8-directional matching.")]
    [SerializeField] private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int( 1,  0),
        new Vector2Int(-1,  0),
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
    };

    /// <inheritdoc/>
    public List<Brick> FindMatches(Brick startBrick, Brick[,] board)
    {
        int width  = board.GetLength(0);
        int height = board.GetLength(1);

        var result  = new List<Brick>();
        var visited = new HashSet<Brick> { startBrick };
        var queue   = new Queue<Brick>();
        queue.Enqueue(startBrick);

        while (queue.Count > 0)
        {
            Brick current = queue.Dequeue();
            result.Add(current);

            foreach (var dir in directions)
            {
                int nx = current.X + dir.x;
                int ny = current.Y + dir.y;

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                Brick neighbor = board[nx, ny];
                if (visited.Contains(neighbor) || neighbor.BrickType != startBrick.BrickType)
                    continue;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return result;
    }
}
