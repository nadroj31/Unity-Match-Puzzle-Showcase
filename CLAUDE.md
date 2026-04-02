# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity match-3 puzzle game similar to Toon Blast. Players tap connected groups of same-coloured bricks to remove them; surviving bricks fall down and new random ones drop in from above. Each level defines up to three brick-clearing goals and an optional move limit.

**Unity version:** Check `ProjectSettings/ProjectVersion.txt`
**Key packages:** DOTween (tween animations), TextMeshPro (UI text)
**Scenes:** `MainScene` (main menu) → `GamePlayScene` (gameplay)

## Architecture

All scripts live in `Assets/Scripts/` and are split into clear layers:

### MVVM Framework (`Assets/Scripts/MVVM/`)

A lightweight hand-rolled MVVM system used for all UI state. Key contracts:

| Class | Role |
|---|---|
| `BindableProperty<T>` | Observable value wrapper. Fires `ValueChanged(oldVal, newVal)` only when value actually changes (uses `EqualityComparer<T>`) |
| `ViewModelBase` | Abstract base; all ViewModels inherit this |
| `ViewBase<T>` | Abstract `MonoBehaviour` base for Views. Calls `OnInitialize()` lazily on first `BindingContext` assignment, then `Binder.Bind()` on each swap |
| `PropertyBinder<T>` | Resolves `BindableProperty` fields by name via reflection (`BindingFlags.Instance | BindingFlags.Public`) and manages subscribe/unsubscribe |
| `IView<T>` | Interface exposing `BindingContext` |

**Critical rules:**
- ViewModel properties **must be public instance fields** (not C# properties) — `PropertyBinder` uses `GetField`, not `GetProperty`
- Use `nameof(ViewModel.Field)` when calling `Binder.Add<T>(name, handler)` — avoids magic strings
- Assign `BindingContext` **before** setting ViewModel values — `ValueChanged` only fires after handlers are subscribed via `Bind()`
- Set initial UI state imperatively in `OnInitialize()` — `ValueChanged` will not fire for a value that hasn't changed from its default

### Data Layer (plain C#, no MonoBehaviour)
| Class | Role |
|---|---|
| `Brick` | Grid cell model — holds `(X, Y)`, `BrickType`, and computed `Position` |
| `LevelDetails` | Runtime level data parsed from JSON; contains `moveLimit` and `goals` array |
| `LevelInfo` | JSON-deserialisable DTO for a level file |
| `GoalInfo` | JSON-deserialisable DTO for a single goal (`brickCode` + `count`) |
| `GoalData` | Runtime representation of a resolved goal (`BrickTypeSO` + `count`) |
| `GoalTracker` | Tracks remaining count for one goal; fires `OnGoalCountChanged` / `OnGoalCompleted` |
| `BoardLogic` (static) | Stateless algorithms: BFS flood-fill (`FindMatchBricks`) and column gravity (`ApplyGravity`) |

### ScriptableObjects (shared assets injected via Inspector)
| Asset | Role |
|---|---|
| `GameSession` | Carries `SelectedLevel` between scenes (replaces PlayerPrefs) |
| `LevelRepository` | Loads all JSON level files from `Resources/LevelInfos/`; caches as `LevelDetails` |
| `BrickVisualConfig` | Maps `BrickType → Sprite`; must call `Initialize()` before use |

### View / MonoBehaviour Layer
| Class | Role |
|---|---|
| `GamePlayBoard` | Scene orchestrator — owns `Brick[,]` + `BrickShow[,]` arrays; creates `GamePlayViewModel`; manages move-limit countdown; triggers victory and fail |
| `GamePlayViewModel` | Observable UI state: 3 goal slots (`GoalNActive/Sprite/Remaining`), `HasMoveLimit`, `MovesRemaining`, `IsVictory`, `IsFailed` |
| `GamePlayView` | `ViewBase<GamePlayViewModel>` — binds HUD (moves + goal slots) and outcome panels (victory / fail); handles navigation and retry |
| `BrickShow` | Per-cell view: sprite, click forwarding via `Action<Brick>`, DOTween drop animation |
| `BrickFactory` | Instantiates `BrickShow` prefabs; extensible via `brickCreators` dictionary |
| `MainMenu` | Controller — loads level data, drives `RecycledScrollView` and `MainMenuViewModel` |
| `RecycledScrollView` | Pool-based scroll list — renders only visible `LevelButton` items; sorts by level number ascending |
| `MainMenuViewModel` | Observable UI state: `IsLevelSelectOpen` |
| `MainMenuView` | `ViewBase<MainMenuViewModel>` — binds level-select panel visibility |
| `LevelButton` | Writes chosen level to `GameSession`, then calls `ScenesManager` |
| `ScenesManager` | `DontDestroyOnLoad` service; implements `ISceneNavigator`; async scene loading with loading UI |

### Interfaces
- `ISceneNavigator` — decouples callers from `ScenesManager` concrete type
- `IMatchStrategy` — pluggable match-detection algorithm
- `IWinCondition` — pluggable win condition (default: `ClearGoalWinCondition`)
- `ILevelLoader` — pluggable level data source (default: `LevelRepository`)

## Key Data Flow

```
MainMenu → LevelButton.OpenGamePlayScene()
    → GameSession.SelectedLevel = N
    → ScenesManager.Instance.LoadGamePlayScene()

GamePlayBoard.Awake()
    → new GamePlayViewModel()
    → gamePlayView.BindingContext = viewModel      ← subscribe bindings first
    → LevelRepository.GetLevelDetails(...)
    → Initialize()
        → SetupMoveLimit()  → viewModel.HasMoveLimit / MovesRemaining
        → PopulateBricks()
        → SetupWinCondition() → ClearGoalWinCondition.Initialize()
            → viewModel.Goal0-2 Active/Sprite/Remaining  ← fires View bindings

OnBrickClick(clicked)
    → BoardLogic.FindMatchBricks()                 // BFS flood-fill
    → movesRemaining--  → viewModel.MovesRemaining ← View updates counter
    → BoardLogic.ApplyGravity()                    // mutates Brick[,]
    → ClearGoalWinCondition.OnMatchMade()
        → GoalTracker(s).RegisterMatch()
            → OnGoalCountChanged → viewModel.GoalN Remaining  ← View updates count
            → OnGoalCompleted (all) → OnCompleted → viewModel.IsVictory = true
    → if moves == 0 && !IsVictory → viewModel.IsFailed = true
```

## Level JSON Format

Files live in `Assets/Resources/LevelInfos/` and are named `level_XX.json`.

```json
{
  "levelNumber": 1,
  "gridWidth": 5,
  "gridHeight": 5,
  "moveLimit": 30,
  "goals": [
    { "brickCode": "b", "count": 10 },
    { "brickCode": "r", "count": 5 }
  ],
  "grid": ["g","g","r","g","g", ...]
}
```

- `moveLimit`: maximum moves allowed; `0` = unlimited (no fail condition)
- `goals`: 0–3 entries; all must be satisfied for victory; empty array = free-play mode
- `brickCode`: `"b"` blue · `"g"` green · `"r"` red · `"y"` yellow · anything else = random
- `grid`: flat array, **top-to-bottom** order in JSON (inverted to bottom-to-top in `LevelRepository` to match world-space Y)

## Adding a New Brick Type

1. Add a new `BrickTypeSO` asset via `Create → Game → BrickTypeSO`
2. Register it in the `BrickTypeRegistry` asset in the Inspector
3. Add a `BrickVisual` entry in the `BrickVisualConfig` asset in the Inspector

## Coding Conventions

- **No singletons** (except `ScenesManager` which is intentionally persistent across scenes)
- All inter-system dependencies injected via Unity Inspector (`[SerializeField]`)
- Section banners: `// ── Title ───────────────────────────────────────────────────`
- XML `<summary>` on all public types and non-trivial public methods
- Single-line methods use expression body (`=>`)
