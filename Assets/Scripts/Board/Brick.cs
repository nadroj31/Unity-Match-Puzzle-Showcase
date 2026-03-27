using UnityEngine;

/// <summary>Pure data model for a single board cell. No Unity dependencies.</summary>
public class Brick
{
    public int       X         { get; private set; }
    public int       Y         { get; private set; }
    public BrickType BrickType { get; private set; }
    public Vector2   Position  { get; private set; }

    public Brick(int x, int y, BrickType brickType)
    {
        X         = x;
        Y         = y;
        BrickType = brickType;
    }

    /// <summary>Computes the local-space position for this cell within a grid of the given dimensions.</summary>
    public void SetPosition(int gridWidth, int gridHeight)
    {
        float xOffset = (gridWidth  % 2 == 0) ? 0.5f : 0f;
        float yOffset = (gridHeight % 2 == 0) ? 0.5f : 0f;
        Position = new Vector2(-gridWidth / 2 + xOffset + X, -gridHeight / 2 + yOffset + Y);
    }

    public void SetBrickType(BrickType brickType)
    {
        BrickType = brickType;
    }
}
