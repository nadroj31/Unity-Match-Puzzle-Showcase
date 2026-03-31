using System.Collections.Generic;

/// <summary>
/// Defines how matching bricks are found when a cell is tapped.
/// Implement this interface (as a <c>ScriptableObject</c>) and assign it to
/// <see cref="GamePlayBoard"/> via the Inspector to replace the default
/// flood-fill with custom matching logic.
/// </summary>
public interface IMatchStrategy
{
    /// <summary>
    /// Returns all bricks that form a match with <paramref name="startBrick"/>
    /// on <paramref name="board"/>. The start brick must be included in the result.
    /// </summary>
    List<Brick> FindMatches(Brick startBrick, Brick[,] board);
}
