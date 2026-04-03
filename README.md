# Unity Match-3 Puzzle — Architecture Showcase

A Toon Blast–style match-3 puzzle game built in Unity, designed as a **technical architecture showcase**.
The focus is on clean separation of concerns, testable code, and extensible systems — not on content volume.

> **Note:** This repository is a portfolio piece. A feature-complete version (level progression, audio, star ratings, 20+ levels) is available separately on the Unity Asset Store.

---

## Gameplay

Players tap connected groups of same-coloured bricks to remove them. Surviving bricks fall down under gravity and new random bricks drop in from above. Each level defines up to three brick-clearing goals and an optional move limit.

*Screenshot / GIF coming soon*

---

## Architecture Overview

The project is split into four clear layers with strict dependency rules:

```
┌─────────────────────────────────────────────────────┐
│                   View Layer                         │
│  (MonoBehaviours — GamePlayView, MainMenuView, etc.) │
└────────────────────┬────────────────────────────────┘
                     │ binds to
┌────────────────────▼────────────────────────────────┐
│                ViewModel Layer                       │
│  (plain C# — GamePlayViewModel, MainMenuViewModel)   │
└────────────────────┬────────────────────────────────┘
                     │ driven by
┌────────────────────▼────────────────────────────────┐
│              Board / Domain Layer                    │
│  (GamePlayBoard, BoardLogic, GoalTracker, etc.)      │
└────────────────────┬────────────────────────────────┘
                     │ reads from
┌────────────────────▼────────────────────────────────┐
│                 Data Layer                           │
│  (ScriptableObjects, JSON level files, DTOs)         │
└─────────────────────────────────────────────────────┘
```

### Hand-rolled MVVM

Rather than using a third-party binding framework, a minimal MVVM system was built from scratch:

| Class | Responsibility |
|---|---|
| `BindableProperty<T>` | Observable value wrapper — fires `ValueChanged` only on actual change, using `EqualityComparer<T>` |
| `ViewBase<T>` | Abstract `MonoBehaviour` base for all Views; manages binding lifecycle (initialize → bind → unbind → destroy) |
| `PropertyBinder<T>` | Resolves `BindableProperty` fields by name via reflection; caches `FieldInfo` for subsequent bind/unbind calls |
| `ViewModelBase` | Marker base class; all ViewModels inherit from it |

**Why hand-rolled?**
Keeping the framework small (~150 lines total) means buyers and reviewers can read and understand the entire binding system in minutes. No hidden magic.

### ScriptableObject Dependency Injection

All cross-system dependencies are injected via the Unity Inspector using ScriptableObject assets as shared references. This avoids singletons (except `ScenesManager`, which is intentionally persistent) and makes every system independently testable.

```
GamePlayBoard
  ├── [SerializeField] LevelRepository    (ILevelLoader)
  ├── [SerializeField] BrickTypeRegistry
  ├── [SerializeField] BrickVisualConfig
  ├── [SerializeField] ConnectedMatchStrategy  (IMatchStrategy)
  └── [SerializeField] ClearGoalWinCondition   (IWinCondition)
```

### Pluggable Interfaces

Three key behaviours are abstracted behind interfaces, making them swappable without touching the board orchestrator:

| Interface | Default Implementation | To Replace |
|---|---|---|
| `IMatchStrategy` | `ConnectedMatchStrategy` (BFS flood-fill) | Create a `ScriptableObject` implementing the interface |
| `IWinCondition` | `ClearGoalWinCondition` | Create a `MonoBehaviour` implementing the interface |
| `ILevelLoader` | `LevelRepository` (JSON from Resources) | Implement to load from Addressables, a server API, etc. |

---

## Key Systems

### Board & Gravity (`BoardLogic`, `GamePlayBoard`)

`BoardLogic.ApplyGravity` is a **static, pure function** — it takes the board state and a callback, mutates nothing on its own, and is safe to unit-test without a running Unity instance.

```csharp
// Stateless gravity — board is mutated via the callback, no MonoBehaviour required
BoardLogic.ApplyGravity(removedBricks, board, registry, onBrickMoved);
```

### BFS Match Detection (`ConnectedMatchStrategy`)

Iterative BFS flood-fill with configurable neighbour directions. Switching from 4-directional to 8-directional matching is a one-line Inspector change — no code required.

### Brick Destruction Animation (`BrickShow`)

A **Ghost GameObject pattern** decouples the visual animation from the logical object:

1. A lightweight clone (`BrickGhost`) is spawned at the matched brick's position
2. The original `BrickShow` is immediately reset to scale-zero so the gravity system can reuse it for the incoming brick
3. The ghost plays the shrink-to-zero animation independently, then self-destructs

This avoids the common pitfall of animation coroutines being cancelled when the object is recycled.

### Win / Fail Conditions (`ClearGoalWinCondition`, `GoalTracker`)

Each goal is tracked by a `GoalTracker` instance with an `IsComplete` guard, ensuring `OnGoalCompleted` fires **exactly once** regardless of how many matches are registered after the goal is satisfied.

### Recycled Scroll View (`RecycledScrollView`)

Pool-based level-select list. Only the visible rows plus a small buffer are instantiated — performance is constant regardless of total level count.

### Level Editor (`Assets/Editor/LevelEditorWindow.cs`)

A custom `EditorWindow` (excluded from builds) for authoring level JSON files:

- Auto-discovers ScriptableObject references via `AssetDatabase.FindAssets`
- Click or drag to paint brick types on the grid
- Enforces grid size constraints (width 3–10, height 3–12)
- Prevents duplicate goal types across slots
- **Test Level**: saves JSON → sets `GameSession.SelectedLevel` → opens `GamePlayScene` → enters Play Mode in one click

---

## Project Structure

```
Assets/
├── Editor/
│   └── LevelEditorWindow.cs        # Editor-only level authoring tool
├── Scripts/
│   ├── MVVM/                       # Hand-rolled MVVM framework
│   │   ├── Core/                   # BindableProperty, ViewModelBase
│   │   ├── View/                   # ViewBase<T>, IView<T>
│   │   └── Binding/                # PropertyBinder<T>
│   ├── Board/                      # Game logic + View layer for gameplay
│   │   ├── GamePlayBoard.cs        # Scene orchestrator
│   │   ├── BoardLogic.cs           # Static gravity algorithm
│   │   ├── ConnectedMatchStrategy  # BFS flood-fill (IMatchStrategy)
│   │   ├── ClearGoalWinCondition   # Goal-based win condition (IWinCondition)
│   │   ├── GoalTracker.cs          # Per-goal progress tracker
│   │   ├── BrickShow.cs            # Per-cell view + ghost animation
│   │   ├── BrickFactory.cs         # Prefab instantiation (extensible)
│   │   ├── GamePlayViewModel.cs    # Observable HUD state
│   │   └── GamePlayView.cs         # MVVM View — binds HUD and outcome panels
│   ├── MainMenu/                   # Main menu controller + MVVM
│   ├── Level/                      # DTOs and data contracts (no Unity deps)
│   ├── Manager/                    # ScenesManager, ISceneNavigator
│   └── ScriptableObjects/          # Shared config assets
└── Resources/
    └── LevelInfos/                 # level_01.json … level_05.json
```

---

## Level JSON Format

```json
{
  "levelNumber": 1,
  "gridWidth": 6,
  "gridHeight": 8,
  "moveLimit": 30,
  "goals": [
    { "brickCode": "b", "count": 15 },
    { "brickCode": "r", "count": 10 }
  ],
  "grid": ["b","g","r","b","g","r", ...]
}
```

- `moveLimit`: `0` = unlimited (free-play, no fail condition)
- `goals`: 0–3 entries; empty array = free-play mode
- `brickCode`: `"b"` blue · `"g"` green · `"r"` red · `"y"` yellow · unknown = random

---

## Extending the Project

### Add a New Brick Type

1. Create a `BrickTypeSO` asset via **Create → Game → Brick Type**
2. Register it in the `BrickTypeRegistry` asset
3. Add a `BrickVisual` entry in `BrickVisualConfig`

### Swap the Match Strategy

1. Create a `ScriptableObject` that implements `IMatchStrategy`
2. Override `FindMatches(Brick startBrick, Brick[,] board)`
3. Assign it to `GamePlayBoard.Match Strategy` in the Inspector

### Add a New Win Condition

1. Create a `MonoBehaviour` that implements `IWinCondition`
2. Override `Initialize(...)` and `OnMatchMade(...)`
3. Assign it to `GamePlayBoard.Win Condition` in the Inspector

---

## Tech Stack

- **Unity** (2022 LTS+)
- **DOTween** — tween animations
- **TextMeshPro** — UI text
- **JsonUtility** — level file parsing (no external JSON library required)

---

## Design Decisions

**No reflection-heavy frameworks.**
`PropertyBinder` uses reflection only once per `Add<T>()` call (in `OnInitialize`) to cache the `FieldInfo`. Subsequent bind/unbind calls reuse the cached info.

**No cascades.**
Toon Blast removes one connected group per tap — cascading chain reactions are explicitly not part of the design. Gravity runs once after each tap.

**ScriptableObjects as config, not managers.**
`GameSession` carries the selected level between scenes using a ScriptableObject asset reference instead of `PlayerPrefs` or a static variable. Every system that needs it receives it via the Inspector.

**DontDestroyOnLoad used sparingly.**
Only `ScenesManager` persists across scenes. All other systems are scene-local and injected via the Inspector, avoiding the global-state problems that `DontDestroyOnLoad` overuse creates.

---

## License

This repository is provided for **portfolio and reference purposes**.
Please do not redistribute or use as the basis of a commercial product without permission.
