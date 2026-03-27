using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrator for the gameplay scene.
/// Owns the board data arrays and wires together the sub-systems:
///   - BoardLogic      (pure, stateless algorithms)
///   - GoalTracker     (goal state + events)
///   - BrickFactory    (view creation)
///   - BrickVisualConfig (sprite mapping)
///
/// All Singleton dependencies have been removed; references are injected via Inspector.
/// </summary>
public class GamePlayBoard : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector references
    // -------------------------------------------------------------------------

    [Header("Data / Config")]
    [SerializeField] private LevelRepository   levelRepository;
    [SerializeField] private GameSession       gameSession;
    [SerializeField] private BrickVisualConfig visualConfig;

    [Header("Scene Components")]
    [SerializeField] private BrickFactory    brickFactory;
    [SerializeField] private SpriteRenderer  boardBackground;
    [SerializeField] private Transform       bricksParent;

    [Header("UI")]
    [SerializeField] private Image           goalImage;
    [SerializeField] private TextMeshProUGUI goalText;
    [SerializeField] private GameObject      victoryUI;
    [SerializeField] private Button          goBackButton;

    [Header("Rules")]
    [SerializeField] private int minMatchCount = 2;

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const float BackgroundPadding  = 0.3f;
    private const float DropHeightOffset   = 2.3f;
    private const float ProcessLockSeconds = 0.4f;

    // -------------------------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------------------------

    private LevelDetails levelDetails;
    private Brick[,]     bricks;
    private BrickShow[,] brickShows;
    private GoalTracker  goalTracker;
    private bool         isProcessing;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        goBackButton.onClick.RemoveAllListeners();
        goBackButton.onClick.AddListener(GoBack);

        levelDetails = levelRepository.GetLevelDetails(gameSession.SelectedLevel);
        Initialize();
    }

    // -------------------------------------------------------------------------
    // Initialisation
    // -------------------------------------------------------------------------

    private void Initialize()
    {
        visualConfig.Initialize();

        boardBackground.size = new Vector2(
            levelDetails.gridWidth  + BackgroundPadding,
            levelDetails.gridHeight + BackgroundPadding);

        bricks     = new Brick[levelDetails.gridWidth, levelDetails.gridHeight];
        brickShows = new BrickShow[levelDetails.gridWidth, levelDetails.gridHeight];

        PopulateBricks();
        SetupGoal();
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
                if (brickShow == null) continue;

                brickShow.SetSprite(visualConfig.GetSprite(brick.BrickType));
                brickShow.SetOnClickAction(OnBrickClick);
                brickShows[x, y] = brickShow;
            }
        }
    }

    private void SetupGoal()
    {
        BrickType goalType = ParseGoalType(levelDetails.goal);

        goalTracker = new GoalTracker(goalType, levelDetails.goalNumber);
        goalTracker.OnGoalCountChanged += count => goalText.text = count.ToString();
        goalTracker.OnGoalCompleted    += ShowVictory;

        goalImage.sprite = visualConfig.GetSprite(goalType);
        goalText.text    = levelDetails.goalNumber.ToString();
    }

    private static BrickType ParseGoalType(string code) => code switch
    {
        "b" => BrickType.BLUE_BRICK,
        "g" => BrickType.GREEN_BRICK,
        "y" => BrickType.YELLOW_BRICK,
        "r" => BrickType.RED_BRICK,
        _   => BrickType.RANDOM_BRICK,
    };

    // -------------------------------------------------------------------------
    // Input handling
    // -------------------------------------------------------------------------

    private void OnBrickClick(Brick clicked)
    {
        if (isProcessing) return;

        List<Brick> matches = BoardLogic.FindMatchBricks(clicked, bricks);
        if (matches.Count < minMatchCount) return;

        isProcessing = true;
        ProcessMatches(matches);
    }

    // -------------------------------------------------------------------------
    // Match processing
    // -------------------------------------------------------------------------

    private void ProcessMatches(List<Brick> matches)
    {
        BrickType matchType = matches[0].BrickType;

        foreach (var brick in matches)
            brickShows[brick.X, brick.Y].Hide();

        BoardLogic.ApplyGravity(matches, bricks, OnBrickMoved);

        goalTracker.RegisterMatch(matchType, matches.Count);

        StartCoroutine(UnlockAfterDelay(ProcessLockSeconds));
    }

    /// <summary>
    /// Called by <see cref="BoardLogic.ApplyGravity"/> for every position that changes.
    /// <paramref name="from"/> is null when a brand-new random brick drops in from above.
    /// </summary>
    private void OnBrickMoved(Brick from, Brick to)
    {
        BrickShow show = brickShows[to.X, to.Y];
        show.Show();
        show.SetSprite(visualConfig.GetSprite(from?.BrickType ?? to.BrickType));
        show.TweenMove(
            originY: from?.Position.y ?? levelDetails.gridHeight / 2f + DropHeightOffset,
            targetY: to.Position.y);
    }

    private IEnumerator UnlockAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        isProcessing = false;
    }

    // -------------------------------------------------------------------------
    // Victory / navigation
    // -------------------------------------------------------------------------

    private void ShowVictory()
    {
        isProcessing = true; // block further input
        victoryUI.SetActive(true);
    }

    private void GoBack()
    {
        DOTween.KillAll();
        ScenesManager.Instance.LoadMainMenu();
    }
}
