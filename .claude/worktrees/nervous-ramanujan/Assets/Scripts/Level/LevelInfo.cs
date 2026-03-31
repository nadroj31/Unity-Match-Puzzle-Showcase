/// <summary>JSON-serialisable data transfer object for a single level file.</summary>
[System.Serializable]
public class LevelInfo
{
    public int      levelNumber;
    public int      gridWidth;
    public int      gridHeight;
    /// <summary>Single-character brick-type code for the goal (e.g. <c>"b"</c> = blue). Unknown codes become random.</summary>
    public string   goal;
    public int      goalNumber;
    /// <summary>Flat grid array in top-to-bottom, left-to-right order. Length must equal gridWidth × gridHeight.</summary>
    public string[] grid;
}
