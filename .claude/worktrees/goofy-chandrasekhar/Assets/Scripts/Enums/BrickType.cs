/// <summary>Identifies the colour (or special state) of a single board cell.</summary>
public enum BrickType
{
    NONE,           // Empty / destroyed cell
    RANDOM_BRICK,   // Wildcard — used in goal definitions
    BLUE_BRICK,
    GREEN_BRICK,
    YELLOW_BRICK,
    RED_BRICK,
}
