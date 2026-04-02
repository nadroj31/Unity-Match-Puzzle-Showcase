/// <summary>Runtime representation of a single level goal, resolved from <see cref="GoalInfo"/>.</summary>
public class GoalData
{
    /// <summary>The resolved brick type that must be cleared.</summary>
    public BrickTypeSO BrickType { get; }

    /// <summary>Number of bricks of this type that must be cleared.</summary>
    public int Count { get; }

    public GoalData(BrickTypeSO brickType, int count)
    {
        BrickType = brickType;
        Count     = count;
    }
}
