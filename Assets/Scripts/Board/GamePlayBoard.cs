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
    [SerializeField] private Camera         gameCamera;
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
    private int               movesRemaining;
    private int               pendingAnimations;

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

        AdjustCamera();
        SetupMoveLimit();
        PopulateBricks();
        SetupWinCondition();
    }

    private void SetupMoveLimit()
    {
        bool hasLimit = levelDetails.moveLimit > 0;
        movesRemaining = levelDetails.moveLimit;

        viewModel.HasMoveLimit.Value   = hasLimit;
        viewModel.MovesRemaining.Value = movesRemaining;
    }

    /// <summary>
    /// Adjusts the camera's orthographic size so the entire board fits on screen,
    /// regardless of grid dimensions or device aspect ratio.
    /// </summary>
    private void AdjustCamera()
    {
        if (gameCamera == null) return;

        float boardW    = levelDetails.gridWidth  + animationConfig.backgroundPadding;
        float boardH    = levelDetails.gridHeight + animationConfig.backgroundPadding;
        float camAspect = (float)Screen.width / Screen.height;

        float fitByHeight = boardH / 2f;
        float fitByWidth  = boardW / (2f * camAspect);

        gameCamera.orthographicSize = Mathf.Max(fitByHeight, fitByWidth);
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

        if (levelDetails.moveLimit > 0)
        {
            movesRemaining--;
            viewModel.MovesRemaining.Value = movesRemaining;
        }

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

        // If no animations were queued (e.g. empty board edge case), settle immediately.
        if (pendingAnimations <= 0)
            StartCoroutine(CascadeAfterSettle());
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

        pendingAnimations++;
        show.TweenMove(
            originY: from?.Position.y ?? levelDetails.gridHeight / 2f + animationConfig.dropHeightOffset,
            targetY: to.Position.y,
            config:  animationConfig,
            onComplete: OnSingleAnimationComplete);
    }

    private void OnSingleAnimationComplete()
    {
        pendingAnimations--;
        if (pendingAnimations <= 0)
            StartCoroutine(CascadeAfterSettle());
    }

    // ── Cascade ───────────────────────────────────────────────────────────────

    private IEnumerator CascadeAfterSettle()
    {
        yield return new WaitForSeconds(animationConfig.cascadeSettleDelay);
        CheckCascades();
    }

    /// <summary>
    /// Scans the board for any groups that qualify as matches. If found,
    /// removes them all and re-applies gravity (triggering further cascades via
    /// animation callbacks). If none found, the board is stable — unlock input
    /// and evaluate fail condition.
    /// </summary>
    private void CheckCascades()
    {
        // Stop if an outcome has already been decided.
        if (viewModel.IsVictory.Value || viewModel.IsFailed.Value) return;

        List<List<Brick>> allGroups = FindAllCascadeGroups();

        if (allGroups.Count > 0)
        {
            foreach (var group in allGroups)
                ProcessMatches(group);
        }
        else
        {
            // Board is stable — check fail condition then release input lock.
            if (levelDetails.moveLimit > 0 && movesRemaining <= 0 && !viewModel.IsVictory.Value)
                OnFailConditionMet();
            else
                isProcessing = false;
        }
    }

    /// <summary>
    /// Finds every connected group on the current board that meets
    /// <see cref="minMatchCount"/>. Each cell is visited at most once.
    /// </summary>
    private List<List<Brick>> FindAllCascadeGroups()
    {
        var result  = new List<List<Brick>>();
        var visited = new HashSet<Brick>();

        for (int x = 0; x < levelDetails.gridWidth; x++)
        {
            for (int y = 0; y < levelDetails.gridHeight; y++)
            {
                Brick brick = bricks[x, y];
                if (brick == null || visited.Contains(brick)) continue;

                List<Brick> group = matchStrategy.FindMatches(brick, bricks);
                foreach (var b in group) visited.Add(b);

                if (group.Count >= minMatchCount)
                    result.Add(group);
            }
        }

        return result;
    }

    // ── Outcome ───────────────────────────────────────────────────────────────

    private void OnWinConditionCompleted()
    {
        isProcessing = true;
        viewModel.IsVictory.Value = true;
    }

    private void OnFailConditionMet()
    {
        isProcessing = true;
        viewModel.IsFailed.Value = true;
    }
}
