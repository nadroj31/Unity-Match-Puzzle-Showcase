using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads and caches level data from JSON files stored in <c>Resources/LevelInfos</c>.
/// </summary>
[CreateAssetMenu(fileName = "LevelRepository", menuName = "Game/LevelRepository")]
public class LevelRepository : ScriptableObject
{
    [SerializeField] private BrickTypeRegistry brickTypeRegistry;

    private readonly Dictionary<int, LevelDetails> levels = new Dictionary<int, LevelDetails>();

    /// <summary>Clears the cache and reloads all JSON files from <c>Resources/LevelInfos</c>. Call once before accessing level data.</summary>
    public void LoadLevelData()
    {
        levels.Clear();

        foreach (var jsonFile in Resources.LoadAll<TextAsset>("LevelInfos"))
        {
            LevelInfo info = JsonUtility.FromJson<LevelInfo>(jsonFile.text);

            var details = new LevelDetails
            {
                levelNumber = info.levelNumber,
                gridWidth   = info.gridWidth,
                gridHeight  = info.gridHeight,
                goal        = info.goal,
                goalNumber  = info.goalNumber,
                gridData    = new BrickTypeSO[info.gridWidth, info.gridHeight]
            };

            int gridIndex = 0;
            // JSON rows are stored top-to-bottom; invert to match world-space Y
            for (int row = info.gridHeight - 1; row >= 0; row--)
            {
                for (int col = 0; col < info.gridWidth; col++)
                {
                    details.gridData[col, row] = brickTypeRegistry.GetByCode(info.grid[gridIndex++]);
                }
            }

            levels[details.levelNumber] = details;
        }
    }

    /// <summary>Returns the cached <see cref="LevelDetails"/> for <paramref name="level"/>, or <c>null</c> if not found.</summary>
    public LevelDetails GetLevelDetails(int level)
    {
        if (levels.TryGetValue(level, out var value))
            return value;

        Debug.LogError($"[LevelRepository] No data for level {level}");
        return null;
    }

    /// <summary>Returns all cached level numbers.</summary>
    public List<int> GetAllLevelKeys() => new List<int>(levels.Keys);
}
