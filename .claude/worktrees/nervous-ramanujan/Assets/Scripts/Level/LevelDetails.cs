/// <summary>Runtime representation of a level, parsed from <see cref="LevelInfo"/>.</summary>
public class LevelDetails
{
    public int          levelNumber;
    public int          gridWidth;
    public int          gridHeight;
    /// <summary>Raw goal code from JSON (e.g. <c>"b"</c>); resolved to a <see cref="BrickTypeSO"/> at runtime.</summary>
    public string       goal;
    public int          goalNumber;
    /// <summary>Resolved brick types indexed [x, y] in world-space order (y=0 is bottom).</summary>
    public BrickTypeSO[,] gridData;
}
