/// <summary>JSON-serialisable data transfer object for a single level file.</summary>
[System.Serializable]
public class LevelInfo
{
    public int      levelNumber;
    public int      gridWidth;
    public int      gridHeight;
    public string   goal;
    public int      goalNumber;
    public string[] grid;
}
