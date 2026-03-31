/// <summary>Runtime representation of a level, parsed from <see cref="LevelInfo"/>.</summary>
public class LevelDetails
{
    public int          levelNumber;
    public int          gridWidth;
    public int          gridHeight;
    public string       goal;
    public int          goalNumber;
    public BrickTypeSO[,] gridData;
}
