/// <summary>Runtime representation of a level, parsed from <see cref="LevelInfo"/>.</summary>
public class LevelDetails
{
    public int            levelNumber;
    public int            gridWidth;
    public int            gridHeight;

    /// <summary>Maximum moves allowed. 0 means unlimited.</summary>
    public int            moveLimit;

    /// <summary>Resolved goals (0–3 entries). All must be satisfied for victory.</summary>
    public GoalData[]     goals;

    /// <summary>Resolved brick types indexed [x, y] in world-space order (y = 0 is bottom).</summary>
    public BrickTypeSO[,] gridData;
}
