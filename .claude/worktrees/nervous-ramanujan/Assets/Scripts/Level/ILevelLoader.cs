using System.Collections.Generic;

/// <summary>
/// Defines how level data is loaded and queried. Implement this interface (as a
/// <c>ScriptableObject</c>) and assign it to <see cref="GamePlayBoard"/> and
/// <see cref="MainMenu"/> via the Inspector to swap in a different data source
/// (e.g. Addressables, a server API, or a ScriptableObject-based level bank).
/// </summary>
public interface ILevelLoader
{
    /// <summary>Loads all available levels into memory. Call once before querying level data.</summary>
    void LoadLevelData();

    /// <summary>Returns the <see cref="LevelDetails"/> for <paramref name="level"/>, or <c>null</c> if not found.</summary>
    LevelDetails GetLevelDetails(int level);

    /// <summary>Returns all available level numbers.</summary>
    List<int> GetAllLevelKeys();
}
