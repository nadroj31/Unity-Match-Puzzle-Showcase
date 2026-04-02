/// <summary>JSON-serialisable DTO for a single level goal.</summary>
[System.Serializable]
public class GoalInfo
{
    /// <summary>Single-character brick-type code (e.g. <c>"b"</c> = blue). Unknown codes become random.</summary>
    public string brickCode;

    /// <summary>Number of bricks of this type that must be cleared to satisfy this goal.</summary>
    public int count;
}
