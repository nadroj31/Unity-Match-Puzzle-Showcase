using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Unity Editor tool for creating and editing level JSON files.
/// Open via <b>Tools → Level Editor</b>.
/// </summary>
public class LevelEditorWindow : EditorWindow
{
    // ── Serialized references (survive domain reload / recompile) ─────────────

    [SerializeField] private BrickTypeRegistry registry;
    [SerializeField] private BrickVisualConfig  visualConfig;
    [SerializeField] private GameSession        gameSession;

    // ── Level data ────────────────────────────────────────────────────────────

    [SerializeField] private int             levelNumber = 1;
    [SerializeField] private int             gridWidth   = 5;
    [SerializeField] private int             gridHeight  = 7;
    [SerializeField] private int             moveLimit   = 30;
    [SerializeField] private List<GoalEntry> goals       = new List<GoalEntry>();

    /// <summary>
    /// Flat grid in row-major, top-to-bottom order (matches JSON format).
    /// index = row * gridWidth + col, where row 0 is the top of the board.
    /// </summary>
    [SerializeField] private List<string> gridFlat = new List<string>();

    // ── Editor state ──────────────────────────────────────────────────────────

    [SerializeField] private string selectedCode = "b";
    private int     pendingW;
    private int     pendingH;
    private Vector2 scrollPos;

    // ── Constants ─────────────────────────────────────────────────────────────

    private const float  CELL      = 42f;
    private const float  PAD       = 2f;
    private const string RAND_CODE = "?";

    // Grid size constraints — keeps bricks large enough to tap on a 1080×1920 screen.
    // Width  ≤ 10 → each brick ≥ ~100 px wide.
    // Height ≤ 12 → board stays within screen height without scrolling.
    private const int MIN_W = 3;
    private const int MAX_W = 10;
    private const int MIN_H = 3;
    private const int MAX_H = 12;

    private static readonly Dictionary<string, Color> CellColors = new Dictionary<string, Color>
    {
        { "b", new Color(0.24f, 0.49f, 0.95f) },
        { "g", new Color(0.22f, 0.70f, 0.30f) },
        { "r", new Color(0.90f, 0.23f, 0.23f) },
        { "y", new Color(0.97f, 0.79f, 0.12f) },
        { RAND_CODE, new Color(0.52f, 0.27f, 0.80f) },
    };
    private static readonly Color UnknownColor = new Color(0.28f, 0.28f, 0.28f);

    // Cached GUIStyle — created once to avoid per-frame allocations.
    private GUIStyle _centredWhite;
    private GUIStyle CentredWhite
    {
        get
        {
            if (_centredWhite == null)
            {
                _centredWhite = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                _centredWhite.normal.textColor = Color.white;
            }
            return _centredWhite;
        }
    }

    // ── Nested serializable type ──────────────────────────────────────────────

    [Serializable]
    private class GoalEntry
    {
        public string brickCode = "b";
        public int    count     = 10;
    }

    // ── Window entry point ────────────────────────────────────────────────────

    [MenuItem("Tools/Level Editor")]
    public static void Open()
    {
        var w = GetWindow<LevelEditorWindow>("Level Editor");
        w.minSize = new Vector2(440, 640);
    }

    private void OnEnable()
    {
        // Auto-resolve references so the user never has to drag them manually.
        if (registry    == null) registry    = FindAsset<BrickTypeRegistry>();
        if (visualConfig == null) visualConfig = FindAsset<BrickVisualConfig>();
        if (gameSession  == null) gameSession  = FindAsset<GameSession>();

        pendingW = gridWidth;
        pendingH = gridHeight;
        EnsureGridSize();
    }

    /// <summary>
    /// Finds the first asset of type <typeparamref name="T"/> in the project.
    /// Works for ScriptableObjects that are expected to have a single instance.
    /// </summary>
    private static T FindAsset<T>() where T : ScriptableObject
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    // ── OnGUI ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawSection("References",                      DrawReferences);
        DrawSection("Level Settings",                  DrawLevelSettings);
        DrawSection("Goals  (0 – 3)",                  DrawGoals);
        DrawSection("Palette",                         DrawPalette);
        DrawSection("Grid  ← click or drag to paint",  DrawGrid);
        DrawSection("Actions",                         DrawActions);

        EditorGUILayout.EndScrollView();
    }

    // ── Section: References ───────────────────────────────────────────────────

    private void DrawReferences()
    {
        registry     = ObjField<BrickTypeRegistry>("Brick Registry", registry);
        visualConfig = ObjField<BrickVisualConfig> ("Visual Config",  visualConfig);
        gameSession  = ObjField<GameSession>       ("Game Session",   gameSession);
    }

    // ── Section: Level Settings ───────────────────────────────────────────────

    private void DrawLevelSettings()
    {
        EditorGUILayout.BeginHorizontal();
        levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);
        if (SmallBtn("Load")) LoadFromFile();
        if (SmallBtn("New"))  NewLevel();
        EditorGUILayout.EndHorizontal();

        moveLimit = EditorGUILayout.IntField(
            new GUIContent("Move Limit", "Maximum moves the player gets. 0 = unlimited (no fail condition)."),
            moveLimit);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);

        pendingW = Mathf.Clamp(EditorGUILayout.IntField($"Width   ({MIN_W}–{MAX_W})",  pendingW), MIN_W, MAX_W);
        pendingH = Mathf.Clamp(EditorGUILayout.IntField($"Height  ({MIN_H}–{MAX_H})", pendingH), MIN_H, MAX_H);

        using (new EditorGUI.DisabledScope(pendingW == gridWidth && pendingH == gridHeight))
        {
            if (GUILayout.Button("Apply Grid Size"))
                ResizeGrid(pendingW, pendingH);
        }

    }

    // ── Section: Goals ────────────────────────────────────────────────────────

    private void DrawGoals()
    {
        string[] allCodes = GetPlayableCodes();

        // Collect the removal request after the loop so every BeginHorizontal has a
        // matching EndHorizontal. Breaking out of the loop mid-row leaves the layout
        // stack unbalanced and causes Unity's "Invalid GUILayout state" error.
        int removeIdx = -1;

        for (int i = 0; i < goals.Count; i++)
        {
            // Build a list of codes available to this slot:
            // own current code is always included; codes used by other slots are excluded.
            var available = new List<string>();
            foreach (var code in allCodes)
            {
                bool usedByOther = false;
                for (int j = 0; j < goals.Count; j++)
                    if (j != i && goals[j].brickCode == code) { usedByOther = true; break; }
                if (!usedByOther) available.Add(code);
            }

            string[] avCodes  = available.ToArray();
            string[] avLabels = GetPlayableLabels(avCodes);

            EditorGUILayout.BeginHorizontal();

            int idx = Mathf.Max(0, Array.IndexOf(avCodes, goals[i].brickCode));
            idx = EditorGUILayout.Popup($"Goal {i + 1}", idx, avLabels);
            if (idx >= 0 && idx < avCodes.Length)
                goals[i].brickCode = avCodes[idx];

            goals[i].count = Mathf.Max(1, EditorGUILayout.IntField(goals[i].count, GUILayout.Width(64)));
            EditorGUILayout.LabelField("bricks", GUILayout.Width(42));

            if (SmallBtn("✕")) removeIdx = i;

            EditorGUILayout.EndHorizontal();
        }

        if (removeIdx >= 0)
        {
            goals.RemoveAt(removeIdx);
            Repaint();
        }

        // Disable Add if all playable types are already used, or if 3 goals are set
        bool allUsed = goals.Count >= allCodes.Length;
        using (new EditorGUI.DisabledScope(goals.Count >= 3 || allUsed))
        {
            if (GUILayout.Button("＋  Add Goal"))
            {
                // Pick the first code not yet assigned to any goal
                string nextCode = "b";
                foreach (var code in allCodes)
                {
                    bool taken = false;
                    foreach (var g in goals) if (g.brickCode == code) { taken = true; break; }
                    if (!taken) { nextCode = code; break; }
                }
                goals.Add(new GoalEntry { brickCode = nextCode });
            }
        }
    }

    // ── Section: Palette ──────────────────────────────────────────────────────

    private void DrawPalette()
    {
        string[] codes = GetPlayableCodes();

        EditorGUILayout.BeginHorizontal();
        foreach (var code in codes)
            DrawPaletteButton(code, code.ToUpperInvariant());
        DrawPaletteButton(RAND_CODE, "RND");
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPaletteButton(string code, string label)
    {
        Color brickColor = CellColors.TryGetValue(code, out var c) ? c : UnknownColor;
        bool  selected   = selectedCode == code;

        Rect r = GUILayoutUtility.GetRect(50f, 50f, GUILayout.Width(50f), GUILayout.Height(50f));

        if (selected)
            EditorGUI.DrawRect(Inflate(r, 3f), Color.white);
        EditorGUI.DrawRect(r, brickColor);
        GUI.Label(r, label, CentredWhite);

        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
        {
            selectedCode = code;
            Event.current.Use();
            Repaint();
        }
    }

    // ── Section: Grid ─────────────────────────────────────────────────────────

    private void DrawGrid()
    {
        Rect area = GUILayoutUtility.GetRect(gridWidth * CELL, gridHeight * CELL);
        var  evt  = Event.current;

        for (int row = 0; row < gridHeight; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                Rect cell = new Rect(
                    area.x + col * CELL + PAD,
                    area.y + row * CELL + PAD,
                    CELL - PAD * 2,
                    CELL - PAD * 2);

                string code  = GetCell(col, row);
                Color  color = CellColors.TryGetValue(code, out var cc) ? cc : UnknownColor;

                EditorGUI.DrawRect(cell, color);

                string cellLabel = code == RAND_CODE ? "RND" : code.ToUpperInvariant();
                GUI.Label(cell, cellLabel, CentredWhite);

                // Paint on click or drag
                if (evt.button == 0 &&
                    (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag) &&
                    cell.Contains(evt.mousePosition))
                {
                    SetCell(col, row, selectedCode);
                    evt.Use();
                    Repaint();
                }
            }
        }
    }

    // ── Section: Actions ──────────────────────────────────────────────────────

    private void DrawActions()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("💾  Save to JSON", GUILayout.Height(36)))
            SaveToFile();

        using (new EditorGUI.DisabledScope(gameSession == null))
        {
            if (GUILayout.Button("▶  Test Level", GUILayout.Height(36)))
                TestLevel();
        }

        EditorGUILayout.EndHorizontal();

        if (gameSession == null)
            EditorGUILayout.HelpBox(
                "Assign a Game Session asset above to enable Test Level.",
                MessageType.Warning);
    }

    // ── Grid state ────────────────────────────────────────────────────────────

    private string GetCell(int col, int row)
    {
        int idx = row * gridWidth + col;
        return idx < gridFlat.Count ? gridFlat[idx] : "b";
    }

    private void SetCell(int col, int row, string code)
    {
        EnsureGridSize();
        int idx = row * gridWidth + col;
        if (idx < gridFlat.Count)
            gridFlat[idx] = code;
    }

    private void EnsureGridSize()
    {
        int needed = gridWidth * gridHeight;
        while (gridFlat.Count < needed) gridFlat.Add("b");
    }

    private void ResizeGrid(int newW, int newH)
    {
        newW = Mathf.Clamp(newW, MIN_W, MAX_W);
        newH = Mathf.Clamp(newH, MIN_H, MAX_H);

        var newFlat = new List<string>(newW * newH);
        for (int row = 0; row < newH; row++)
            for (int col = 0; col < newW; col++)
                newFlat.Add(col < gridWidth && row < gridHeight ? GetCell(col, row) : "b");

        gridWidth  = newW;
        gridHeight = newH;
        gridFlat   = newFlat;
        pendingW   = newW;
        pendingH   = newH;
        Repaint();
    }

    private void NewLevel()
    {
        if (!EditorUtility.DisplayDialog("New Level", "Discard current changes and start fresh?", "Yes", "Cancel"))
            return;
        goals.Clear();
        moveLimit = 30;
        gridFlat.Clear();
        EnsureGridSize();
        Repaint();
    }

    // ── Registry helpers ──────────────────────────────────────────────────────

    private string[] GetPlayableCodes()
    {
        if (registry == null)
            return new[] { "b", "g", "r", "y" };

        var types = registry.GetAllPlayableTypes();
        if (types == null || types.Length == 0)
            return new[] { "b", "g", "r", "y" };

        var result = new string[types.Length];
        for (int i = 0; i < types.Length; i++)
            result[i] = types[i] != null ? types[i].Code : "b";
        return result;
    }

    private static string[] GetPlayableLabels(string[] codes)
    {
        var labels = new string[codes.Length];
        for (int i = 0; i < codes.Length; i++)
            labels[i] = codes[i].ToUpperInvariant();
        return labels;
    }

    // ── File IO ───────────────────────────────────────────────────────────────

    private string GetFilePath()
    {
        string dir = Path.Combine(Application.dataPath, "Resources", "LevelInfos");
        return Path.Combine(dir, $"level_{levelNumber:D2}.json");
    }

    private void SaveToFile()
    {
        var goalInfos = new GoalInfo[goals.Count];
        for (int i = 0; i < goals.Count; i++)
            goalInfos[i] = new GoalInfo { brickCode = goals[i].brickCode, count = goals[i].count };

        var gridArray = new string[gridWidth * gridHeight];
        for (int row = 0; row < gridHeight; row++)
            for (int col = 0; col < gridWidth; col++)
                gridArray[row * gridWidth + col] = GetCell(col, row);

        var info = new LevelInfo
        {
            levelNumber = levelNumber,
            gridWidth   = gridWidth,
            gridHeight  = gridHeight,
            moveLimit   = moveLimit,
            goals       = goalInfos,
            grid        = gridArray
        };

        string path = GetFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonUtility.ToJson(info, true));
        AssetDatabase.Refresh();

        Debug.Log($"[LevelEditor] Saved level_{levelNumber:D2}.json");
        EditorUtility.DisplayDialog("Saved", $"level_{levelNumber:D2}.json saved successfully.", "OK");
    }

    private void LoadFromFile()
    {
        string path = GetFilePath();
        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Not Found", $"level_{levelNumber:D2}.json does not exist.", "OK");
            return;
        }

        var info = JsonUtility.FromJson<LevelInfo>(File.ReadAllText(path));

        gridWidth  = Mathf.Clamp(info.gridWidth,  MIN_W, MAX_W);
        gridHeight = Mathf.Clamp(info.gridHeight, MIN_H, MAX_H);
        moveLimit  = info.moveLimit;
        pendingW   = gridWidth;
        pendingH   = gridHeight;

        goals.Clear();
        if (info.goals != null)
            foreach (var g in info.goals)
            {
                if (goals.Count >= 3) break; // clamp to editor / ViewModel limit
                goals.Add(new GoalEntry { brickCode = g.brickCode, count = g.count });
            }

        gridFlat.Clear();
        if (info.grid != null)
            gridFlat.AddRange(info.grid);
        EnsureGridSize();

        Repaint();
        Debug.Log($"[LevelEditor] Loaded level_{levelNumber:D2}.json");
    }

    private void TestLevel()
    {
        SaveToFile();

        gameSession.SelectedLevel = levelNumber;
        EditorUtility.SetDirty(gameSession);
        AssetDatabase.SaveAssets();

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/Scenes/GamePlayScene.unity");
            EditorApplication.isPlaying = true;
        }
    }

    // ── GUI helpers ───────────────────────────────────────────────────────────

    private static void DrawSection(string title, Action draw)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        draw();
        EditorGUILayout.Space(4);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f),
            new Color(0.5f, 0.5f, 0.5f, 0.4f));
        EditorGUILayout.Space(8);
    }

    private static T ObjField<T>(string label, T value) where T : UnityEngine.Object
        => (T)EditorGUILayout.ObjectField(label, value, typeof(T), false);

    private static bool SmallBtn(string label)
        => GUILayout.Button(label, GUILayout.Width(48));

    private static Rect Inflate(Rect r, float amount)
        => new Rect(r.x - amount, r.y - amount, r.width + amount * 2, r.height + amount * 2);
}
