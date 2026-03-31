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
    // ── Inspector ─────────────────────────────────────────────────────────────

    [SerializeField] private Image           goalImage;
    [SerializeField] private GameObject      goalCountLabel;
    [SerializeField] private TextMeshProUGUI goalText;
    [SerializeField] private GameObject      victoryUI;
    [SerializeField] private Button          goBackButton;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        goBackButton.onClick.RemoveAllListeners();
        goBackButton.onClick.AddListener(OnGoBackClicked);
    }

    // ── ViewBase ──────────────────────────────────────────────────────────────

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Binder.Add<bool>  (nameof(GamePlayViewModel.IsGoalRandom),  OnIsGoalRandomChanged);
        Binder.Add<Sprite>(nameof(GamePlayViewModel.GoalSprite),    OnGoalSpriteChanged);
        Binder.Add<int>   (nameof(GamePlayViewModel.GoalRemaining), OnGoalRemainingChanged);
        Binder.Add<bool>  (nameof(GamePlayViewModel.IsVictory),     OnIsVictoryChanged);

        // Ensure a safe initial state regardless of ViewModel values
        victoryUI.SetActive(false);
    }

    // ── Binding handlers ──────────────────────────────────────────────────────

    private void OnIsGoalRandomChanged(bool _, bool isRandom)
    {
        goalImage.gameObject.SetActive(!isRandom);
        goalCountLabel.SetActive(isRandom);
    }

    private void OnGoalSpriteChanged(Sprite _, Sprite newSprite)
        => goalImage.sprite = newSprite;

    private void OnGoalRemainingChanged(int _, int newCount)
        => goalText.text = newCount.ToString();

    private void OnIsVictoryChanged(bool _, bool isVictory)
        => victoryUI.SetActive(isVictory);

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnGoBackClicked()
    {
        DOTween.KillAll();
        ScenesManager.Instance.LoadMainMenu();
    }
}
