using System.Collections.Generic;
using DG.Tweening;
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
        // Pre-allocate DOTween capacity to avoid runtime auto-expansion warnings.
        // 500 tweeners comfortably covers a full board drop when all columns refill at once.
        DOTween.SetTweensCapacity(500, 50);

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

        // Count how many ghost-destruction animations will play.
        // IsNone cells have no BrickShow, so they don't contribute to the count.
        int pendingDestroys = 0;
        foreach (var brick in matches)
            if (brickShows[brick.X, brick.Y] != null) pendingDestroys++;

        // Called once every ghost has finished shrinking.
        // Gravity and win-condition evaluation are deferred until this point so
        // bricks don't start falling while the destruction animation is still playing.
        void OnAllDestroyed()
        {
            BoardLogic.ApplyGravity(matches, bricks, brickTypeRegistry, OnBrickMoved);
            winCondition.OnMatchMade(matchType, matches.Count);

            // All gravity animations may already be done (e.g. top-row match with nothing above).
            if (pendingAnimations == 0)
                OnBoardSettled();
        }

        if (pendingDestroys == 0)
        {
            // No visible bricks in the match (edge case) — proceed immediately.
            OnAllDestroyed();
            return;
        }

        int remaining = pendingDestroys;
        foreach (var brick in matches)
        {
            var show = brickShows[brick.X, brick.Y];
            if (show != null)
                show.Hide(animationConfig, () =>
                {
                    remaining--;
                    if (remaining == 0) OnAllDestroyed();
                });
        }
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
        if (pendingAnimations == 0)
            OnBoardSettled();
    }

    /// <summary>
    /// Called once all drop animations for a move have completed.
    /// Returns immediately if victory was already triggered (the board stays locked).
    /// Otherwise evaluates the fail condition and releases the input lock.
    /// </summary>
    private void OnBoardSettled()
    {
        // Victory was already handled by OnWinConditionCompleted — keep the board locked.
        if (viewModel.IsVictory.Value) return;

        if (levelDetails.moveLimit > 0 && movesRemaining <= 0)
        {
            OnFailConditionMet();
            return;
        }

        // Deadlock: if no valid move exists anywhere on the board the player is stuck.
        // Trigger fail regardless of remaining moves so the game never freezes.
        if (IsDeadlocked())
        {
            OnFailConditionMet();
            return;
        }

        isProcessing = false;
    }

    /// <summary>
    /// Returns <c>true</c> when no brick on the board belongs to a connected group
    /// large enough to be removed (i.e. every possible match is smaller than
    /// <see cref="minMatchCount"/>). Uses the same <see cref="IMatchStrategy"/> as
    /// normal gameplay so the result is consistent with what the player can actually do.
    /// </summary>
    private bool IsDeadlocked()
    {
        int width  = levelDetails.gridWidth;
        int height = levelDetails.gridHeight;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Brick brick = bricks[x, y];
                if (brick.BrickType == null || brick.BrickType.IsNone) continue;

                if (matchStrategy.FindMatches(brick, bricks).Count >= minMatchCount)
                    return false;
            }

        return true;
    }

    // ── Outcome ───────────────────────────────────────────────────────────────

    private void OnWinConditionCompleted()
    {
        isProcessing = true;

        // Check whether a next level exists in the repository so the View can
        // show or hide the "Next Level" button accordingly.
        int nextLevel = levelDetails.levelNumber + 1;
        viewModel.HasNextLevel.Value = levelLoader.GetAllLevelKeys().Contains(nextLevel);

        viewModel.StarCount.Value  = CalculateStars();
        viewModel.IsVictory.Value  = true;
    }

    /// <summary>
    /// Determines the star rating (1–3) for the current victory.
    /// Unlimited-move levels always award 3 stars.
    /// Move-limited levels check moves remaining against the thresholds defined in the level JSON;
    /// a threshold of 0 means that tier is not available for this level.
    /// </summary>
    private int CalculateStars()
    {
        // Free-play / unlimited-move levels always deserve full marks.
        if (levelDetails.moveLimit <= 0) return 3;

        if (levelDetails.star3Threshold > 0 && movesRemaining >= levelDetails.star3Threshold) return 3;
        if (levelDetails.star2Threshold > 0 && movesRemaining >= levelDetails.star2Threshold) return 2;
        return 1;
    }

    private void OnFailConditionMet()
    {
        isProcessing = true;
        viewModel.IsFailed.Value = true;
    }
}
