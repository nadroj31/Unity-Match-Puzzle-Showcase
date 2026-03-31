using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates the gameplay scene: owns board data, wires game-logic sub-systems
/// (<see cref="BoardLogic"/>, <see cref="IMatchStrategy"/>, <see cref="IWinCondition"/>, <see cref="BrickFactory"/>),
/// and pushes observable state into <see cref="GamePlayViewModel"/> for the View layer.
/// </summary>
public class GamePlayBoard : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Data / Config")]
    [Tooltip("ScriptableObject that implements ILevelLoader (e.g. LevelRepository).")]
    [SerializeField] private ScriptableObject    levelLoaderAsset;
    [SerializeField] private GameSession         gameSession;
    [SerializeField] private BrickVisualConfig   visualConfig;
    [SerializeField] private BrickTypeRegistry   brickTypeRegistry;
    [SerializeField] private BoardAnimationConfig animationConfig;

    [Header("Scene Components")]
    [SerializeField] private BrickFactory   brickFactory;
    [SerializeField] private SpriteRenderer boardBackground;
    [SerializeField] private Transform      bricksParent;
    [SerializeField] private GamePlayView   gamePlayView;

    [Header("Rules")]
    [SerializeField] private int           minMatchCount = 2;
    [Tooltip("ScriptableObject that implements IMatchStrategy (e.g. ConnectedMatchStrategy).")]
    [SerializeField] private ScriptableObject matchStrategyAsset;
    [Tooltip("MonoBehaviour that implements IWinCondition (e.g. ClearGoalWinCondition). Must be on a GameObject in this scene.")]
    [SerializeField] private MonoBehaviour winConditionBehaviour;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private ILevelLoader      levelLoader;
    private IMatchStrategy    matchStrategy;
    private LevelDetails      levelDetails;
    private Brick[,]          bricks;
    private BrickShow[,]      brickShows;
    private IWinCondition     winCondition;
    private GamePlayViewModel viewModel;
    private bool              isProcessing;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        levelLoader = levelLoaderAsset as ILevelLoader;
        if (levelLoader == null)
        {
            Debug.LogError("[GamePlayBoard] levelLoaderAsset does not implement ILevelLoader.", this);
            return;
        }

        matchStrategy = matchStrategyAsset as IMatchStrategy;
        if (matchStrategy == null)
        {
            Debug.LogError("[GamePlayBoard] matchStrategyAsset does not implement IMatchStrategy.", this);
            return;
        }

        winCondition = winConditionBehaviour as IWinCondition;
        if (winCondition == null)
        {
            Debug.LogError("[GamePlayBoard] winConditionBehaviour does not implement IWinCondition.", this);
            return;
        }

        // Create ViewModel and bind the View BEFORE Initialize() so that
        // property assignments inside SetupWinCondition fire ValueChanged on the View.
        viewModel = new GamePlayViewModel();
        gamePlayView.BindingContext = viewModel;

        levelDetails = levelLoader.GetLevelDetails(gameSession.SelectedLevel);
        if (levelDetails == null)
        {
            Debug.LogError($"[GamePlayBoard] No level data for level {gameSession.SelectedLevel}.", this);
            return;
        }

        Initialize();
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    private void Initialize()
    {
        visualConfig.Initialize();

        boardBackground.size = new Vector2(
            levelDetails.gridWidth  + animationConfig.backgroundPadding,
            levelDetails.gridHeight + animationConfig.backgroundPadding);

        bricks     = new Brick[levelDetails.gridWidth, levelDetails.gridHeight];
        brickShows = new BrickShow[levelDetails.gridWidth, levelDetails.gridHeight];

        PopulateBricks();
        SetupWinCondition();
    }

    private void PopulateBricks()
    {
        for (int x = 0; x < levelDetails.gridWidth; x++)
        {
            for (int y = 0; y < levelDetails.gridHeight; y++)
            {
                var brick = new Brick(x, y, levelDetails.gridData[x, y]);
                brick.SetPosition(levelDetails.gridWidth, levelDetails.gridHeight);
                bricks[x, y] = brick;

                BrickShow brickShow = brickFactory.CreateBrick(brick, bricksParent);
                if (brickShow == null) continue; // isNone bricks have no view

                brickShow.SetSprite(visualConfig.GetSprite(brick.BrickType));
                brickShow.SetOnClickAction(OnBrickClick);
                brickShows[x, y] = brickShow;
            }
        }
    }

    private void SetupWinCondition()
    {
        winCondition.Initialize(levelDetails, brickTypeRegistry, visualConfig, viewModel);
        winCondition.OnCompleted += OnWinConditionCompleted;
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnBrickClick(Brick clicked)
    {
        if (isProcessing) return;

        List<Brick> matches = matchStrategy.FindMatches(clicked, bricks);
        if (matches.Count < minMatchCount) return;

        isProcessing = true;
        ProcessMatches(matches);
    }

    // ── Match processing ──────────────────────────────────────────────────────

    private void ProcessMatches(List<Brick> matches)
    {
        BrickTypeSO matchType = matches[0].BrickType;

        foreach (var brick in matches)
            brickShows[brick.X, brick.Y].Hide();

        BoardLogic.ApplyGravity(matches, bricks, brickTypeRegistry, OnBrickMoved);
        winCondition.OnMatchMade(matchType, matches.Count);

        StartCoroutine(UnlockAfterDelay(animationConfig.processLockSeconds));
    }

    /// <summary>
    /// Invoked by <see cref="BoardLogic.ApplyGravity"/> for each position that changes.
    /// <paramref name="from"/> is <c>null</c> when a new random brick drops in from above.
    /// </summary>
    private void OnBrickMoved(Brick from, Brick to)
    {
        BrickShow show = brickShows[to.X, to.Y];
        if (show == null) return; // isNone cell — no view to update

        show.Show();
        show.SetSprite(visualConfig.GetSprite(from?.BrickType ?? to.BrickType));
        show.TweenMove(
            originY: from?.Position.y ?? levelDetails.gridHeight / 2f + animationConfig.dropHeightOffset,
            targetY: to.Position.y,
            config:  animationConfig);
    }

    private IEnumerator UnlockAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        isProcessing = false;
    }

    // ── Victory ───────────────────────────────────────────────────────────────

    private void OnWinConditionCompleted()
    {
        isProcessing = true;              // block further input
        viewModel.IsVictory.Value = true; // View handles victoryUI via binding
    }
}
