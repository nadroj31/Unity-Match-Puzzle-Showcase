using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for the main menu. Manages level data loading and the recycled level-select
/// list; delegates all UI visibility changes to <see cref="MainMenuView"/> via <see cref="MainMenuViewModel"/>.
/// </summary>
public class MainMenu : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("ScriptableObject that implements ILevelLoader (e.g. LevelRepository).")]
    [SerializeField] private ScriptableObject  levelLoaderAsset;
    [SerializeField] private GameSession       gameSession;
    [SerializeField] private Button            exitButton;
    [SerializeField] private Button            startButton;
    [SerializeField] private RecycledScrollView recycledScrollView;
    [SerializeField] private MainMenuView      mainMenuView;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private MainMenuViewModel viewModel;
    private ILevelLoader      levelLoader;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        levelLoader = levelLoaderAsset as ILevelLoader;
        if (levelLoader == null)
        {
            Debug.LogError("[MainMenu] levelLoaderAsset does not implement ILevelLoader.", this);
            return;
        }

        viewModel = new MainMenuViewModel();
        mainMenuView.BindingContext = viewModel;

        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(CloseLevelSelectMenu);
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OpenLevelSelectMenu);

        // When returning from the gameplay scene after pressing OK, skip the title screen
        // and go straight to the level-select panel so the player doesn't have to tap Start.
        if (gameSession != null && gameSession.ReturnToLevelSelect)
        {
            gameSession.ReturnToLevelSelect = false; // consume the flag
            OpenLevelSelectMenu();
        }
    }

    // ── Menu actions ──────────────────────────────────────────────────────────

    private void CloseLevelSelectMenu() => viewModel.IsLevelSelectOpen.Value = false;

    private void OpenLevelSelectMenu()
    {
        levelLoader.LoadLevelData();
        recycledScrollView.SetData(levelLoader.GetAllLevelKeys());
        viewModel.IsLevelSelectOpen.Value = true;
    }
}
