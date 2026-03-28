# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity match-3 puzzle game similar to Toon Blast. Players tap connected groups of same-coloured bricks to remove them; surviving bricks fall down and new random ones drop in from above. Each level has a goal (clear N bricks of a specific colour).

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
| `LevelDetails` | Runtime level data parsed from JSON |
| `LevelInfo` | JSON-deserialisable DTO for a level file |
| `GoalTracker` | Tracks remaining goal count; fires `OnGoalCountChanged` / `OnGoalCompleted` events |
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
| `GamePlayBoard` | Scene orchestrator — owns `Brick[,]` + `BrickShow[,]` arrays; creates `GamePlayViewModel`; bridges `GoalTracker` events into ViewModel |
| `GamePlayViewModel` | Observable UI state: `GoalRemaining`, `GoalSprite`, `IsVictory` |
| `GamePlayView` | `ViewBase<GamePlayViewModel>` — binds goal HUD + victory panel; handles back-button navigation |
| `BrickShow` | Per-cell view: sprite, click forwarding via `Action<Brick>`, DOTween drop animation |
| `BrickFactory` | Instantiates `BrickShow` prefabs; extensible via `brickCreators` dictionary |
| `MainMenu` | Controller — loads level data, pools `LevelButton` instances, drives `MainMenuViewModel` |
| `MainMenuViewModel` | Observable UI state: `IsLevelSelectOpen` |
| `MainMenuView` | `ViewBase<MainMenuViewModel>` — binds level-select panel visibility |
| `LevelButton` | Writes chosen level to `GameSession`, then calls `ScenesManager` |
| `ScenesManager` | `DontDestroyOnLoad` service; implements `ISceneNavigator`; async scene loading with loading UI |

### Interface
- `ISceneNavigator` — decouples callers from `ScenesManager` concrete type.

## Key Data Flow

```
MainMenu → LevelButton.OpenGamePlayScene()
    → GameSession.SelectedLevel = N
    → ScenesManager.Instance.LoadGamePlayScene()

GamePlayBoard.Awake()
    → new GamePlayViewModel()
    → gamePlayView.BindingContext = viewModel   ← subscribe bindings first
    → LevelRepository.GetLevelDetails(...)
    → Initialize() → PopulateBricks() + SetupGoal()
        → viewModel.GoalSprite.Value = ...      ← fires View binding
        → viewModel.GoalRemaining.Value = ...   ← fires View binding

OnBrickClick(clicked)
    → BoardLogic.FindMatchBricks()              // BFS flood-fill
    → BoardLogic.ApplyGravity()                 // mutates Brick[,], fires OnBrickMoved
    → GoalTracker.RegisterMatch()
        → OnGoalCountChanged → viewModel.GoalRemaining.Value = count  ← View updates goalText
        → OnGoalCompleted   → viewModel.IsVictory.Value = true        ← View shows victoryUI
```

## Level JSON Format

Files live in `Assets/Resources/LevelInfos/` and are named `level_XX.json`.

```json
{
  "levelNumber": 1,
  "gridWidth": 5,
  "gridHeight": 5,
  "goal": "b",
  "goalNumber": 20,
  "grid": ["g","g","r","g","g", ...]
}
```

- `goal`: `"b"` blue · `"g"` green · `"r"` red · `"y"` yellow · anything else = random (any colour counts)
- `grid`: flat array, **top-to-bottom** order in JSON (inverted to bottom-to-top in `LevelRepository` to match world-space Y)
- Grid cell codes mirror `BrickType` values; unknown codes generate a random colour brick

## Adding a New Brick Type

1. Add a value to `BrickType` enum — keep `NONE` at index 0 and `RANDOM_BRICK` at index 1 (the random picker uses `Range(2, length)`)
2. Register a creator in `BrickFactory.brickCreators`
3. Add a `BrickVisual` entry in the `BrickVisualConfig` asset in the Inspector

## Coding Conventions

- **No singletons** (except `ScenesManager` which is intentionally persistent across scenes)
- All inter-system dependencies injected via Unity Inspector (`[SerializeField]`)
- Section banners: `// ── Title ───────────────────────────────────────────────────`
- XML `<summary>` on all public types and non-trivial public methods
- Single-line methods use expression body (`=>`)
