using UnityEngine;

/// <summary>
/// MVVM View for the main menu. Binds the level-select panel visibility to
/// <see cref="MainMenuViewModel"/>; owns no navigation or data-loading logic.
/// </summary>
public class MainMenuView : ViewBase<MainMenuViewModel>
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [SerializeField] private GameObject levelSelectUI;

    // ── ViewBase ──────────────────────────────────────────────────────────────

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Binder.Add<bool>(nameof(MainMenuViewModel.IsLevelSelectOpen), OnIsLevelSelectOpenChanged);

        // Ensure panel starts hidden regardless of ViewModel initial state
        levelSelectUI.SetActive(false);
    }

    // ── Binding handlers ──────────────────────────────────────────────────────

    private void OnIsLevelSelectOpenChanged(bool _, bool isOpen)
        => levelSelectUI.SetActive(isOpen);
}
