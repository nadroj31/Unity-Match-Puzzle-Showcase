/// <summary>JSON-serialisable data transfer object for a single level file.</summary>
[System.Serializable]
public class LevelInfo
{
    public int        levelNumber;
    public int        gridWidth;
    public int        gridHeight;

    /// <summary>Maximum number of moves the player is allowed. 0 means unlimited (no fail condition).</summary>
    public int        moveLimit;

    /// <summary>
    /// Up to three brick-clearing goals. An empty array means no win condition (free play).
    /// </summary>
    public GoalInfo[] goals;

    /// <summary>Flat grid array in top-to-bottom, left-to-right order. Length must equal gridWidth × gridHeight.</summary>
    public string[]   grid;
}
