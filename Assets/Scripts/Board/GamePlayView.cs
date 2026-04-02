using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MVVM View for the gameplay scene. Binds UI widgets to <see cref="GamePlayViewModel"/>;
/// owns no game logic. Wired to its ViewModel by <see cref="GamePlayBoard"/>.
/// </summary>
public class GamePlayView : ViewBase<GamePlayViewModel>
{
    // ── Inspector — HUD ───────────────────────────────────────────────────────

    [Header("Move Limit")]
    [Tooltip("Root GameObject for the moves counter. Hidden when the level has no move limit.")]
    [SerializeField] private GameObject      movesPanel;
    [SerializeField] private TextMeshProUGUI movesText;

    [Header("Goal Slot 0")]
    [SerializeField] private GameObject      goal0Panel;
    [SerializeField] private Image           goal0Image;
    [SerializeField] private TextMeshProUGUI goal0Text;

    [Header("Goal Slot 1")]
    [SerializeField] private GameObject      goal1Panel;
    [SerializeField] private Image           goal1Image;
    [SerializeField] private TextMeshProUGUI goal1Text;

    [Header("Goal Slot 2")]
    [SerializeField] private GameObject      goal2Panel;
    [SerializeField] private Image           goal2Image;
    [SerializeField] private TextMeshProUGUI goal2Text;

    // ── Inspector — Outcome ───────────────────────────────────────────────────

    [Header("Outcome Screens")]
    [SerializeField] private GameObject victoryUI;
    [SerializeField] private GameObject failedUI;

    // ── Inspector — Buttons ───────────────────────────────────────────────────

    [Header("Buttons")]
    [SerializeField] private Button goBackButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button goBackFromFailButton;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        goBackButton.onClick.RemoveAllListeners();
        goBackButton.onClick.AddListener(OnGoBackClicked);

        retryButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(OnRetryClicked);

        goBackFromFailButton.onClick.RemoveAllListeners();
        goBackFromFailButton.onClick.AddListener(OnGoBackClicked);
    }

    // ── ViewBase ──────────────────────────────────────────────────────────────

    protected override void OnInitialize()
    {
        base.OnInitialize();

        Binder.Add<bool>  (nameof(GamePlayViewModel.HasMoveLimit),   OnHasMoveLimitChanged);
        Binder.Add<int>   (nameof(GamePlayViewModel.MovesRemaining), OnMovesRemainingChanged);

        Binder.Add<bool>  (nameof(GamePlayViewModel.Goal0Active),    OnGoal0ActiveChanged);
        Binder.Add<Sprite>(nameof(GamePlayViewModel.Goal0Sprite),    OnGoal0SpriteChanged);
        Binder.Add<int>   (nameof(GamePlayViewModel.Goal0Remaining), OnGoal0RemainingChanged);

        Binder.Add<bool>  (nameof(GamePlayViewModel.Goal1Active),    OnGoal1ActiveChanged);
        Binder.Add<Sprite>(nameof(GamePlayViewModel.Goal1Sprite),    OnGoal1SpriteChanged);
        Binder.Add<int>   (nameof(GamePlayViewModel.Goal1Remaining), OnGoal1RemainingChanged);

        Binder.Add<bool>  (nameof(GamePlayViewModel.Goal2Active),    OnGoal2ActiveChanged);
        Binder.Add<Sprite>(nameof(GamePlayViewModel.Goal2Sprite),    OnGoal2SpriteChanged);
        Binder.Add<int>   (nameof(GamePlayViewModel.Goal2Remaining), OnGoal2RemainingChanged);

        Binder.Add<bool>  (nameof(GamePlayViewModel.IsVictory),      OnIsVictoryChanged);
        Binder.Add<bool>  (nameof(GamePlayViewModel.IsFailed),       OnIsFailedChanged);

        // Safe initial state
        movesPanel.SetActive(false);
        goal0Panel.SetActive(false);
        goal1Panel.SetActive(false);
        goal2Panel.SetActive(false);
        victoryUI.SetActive(false);
        failedUI.SetActive(false);
    }

    // ── Move limit handlers ───────────────────────────────────────────────────

    private void OnHasMoveLimitChanged(bool _, bool hasLimit)
        => movesPanel.SetActive(hasLimit);

    private void OnMovesRemainingChanged(int _, int remaining)
        => movesText.text = remaining.ToString();

    // ── Goal slot handlers ────────────────────────────────────────────────────

    private void OnGoal0ActiveChanged(bool _, bool active)  => goal0Panel.SetActive(active);
    private void OnGoal0SpriteChanged(Sprite _, Sprite s)   => goal0Image.sprite = s;
    private void OnGoal0RemainingChanged(int _, int count)  => goal0Text.text = count.ToString();

    private void OnGoal1ActiveChanged(bool _, bool active)  => goal1Panel.SetActive(active);
    private void OnGoal1SpriteChanged(Sprite _, Sprite s)   => goal1Image.sprite = s;
    private void OnGoal1RemainingChanged(int _, int count)  => goal1Text.text = count.ToString();

    private void OnGoal2ActiveChanged(bool _, bool active)  => goal2Panel.SetActive(active);
    private void OnGoal2SpriteChanged(Sprite _, Sprite s)   => goal2Image.sprite = s;
    private void OnGoal2RemainingChanged(int _, int count)  => goal2Text.text = count.ToString();

    // ── Outcome handlers ──────────────────────────────────────────────────────

    private void OnIsVictoryChanged(bool _, bool isVictory) => victoryUI.SetActive(isVictory);
    private void OnIsFailedChanged(bool _, bool isFailed)   => failedUI.SetActive(isFailed);

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnGoBackClicked()
    {
        DOTween.KillAll();
        ScenesManager.Instance.LoadMainMenu();
    }

    private void OnRetryClicked()
    {
        DOTween.KillAll();
        ScenesManager.Instance.LoadGamePlayScene();
    }
}
