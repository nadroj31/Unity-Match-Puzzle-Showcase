using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for the main menu. Manages level data loading and button pooling;
/// delegates all UI visibility changes to <see cref="MainMenuView"/> via <see cref="MainMenuViewModel"/>.
/// </summary>
public class MainMenu : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("ScriptableObject that implements ILevelLoader (e.g. LevelRepository).")]
    [SerializeField] private ScriptableObject levelLoaderAsset;
    [SerializeField] private Button          exitButton;
    [SerializeField] private Button          startButton;
    [SerializeField] private LevelButton     levelButtonPrefab;
    [SerializeField] private Transform       levelButtonParent;
    [SerializeField] private MainMenuView    mainMenuView;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private readonly List<LevelButton> levelButtons = new List<LevelButton>();
    private MainMenuViewModel          viewModel;
    private ILevelLoader               levelLoader;

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
    }

    // ── Menu actions ──────────────────────────────────────────────────────────

    private void CloseLevelSelectMenu() => viewModel.IsLevelSelectOpen.Value = false;

    private void OpenLevelSelectMenu()
    {
        levelLoader.LoadLevelData();

        List<int> levelKeys = levelLoader.GetAllLevelKeys();
        int       keysCount = levelKeys.Count;
        int       btnCount  = levelButtons.Count;

        for (int i = 0; i < Mathf.Max(keysCount, btnCount); i++)
        {
            if (i >= btnCount)
            {
                var btn = Instantiate(levelButtonPrefab, levelButtonParent);
                levelButtons.Add(btn.GetComponent<LevelButton>());
            }

            bool isActive = i < keysCount;
            levelButtons[i].gameObject.SetActive(isActive);
            if (isActive)
                levelButtons[i].SetLevelText(levelKeys[i]);
        }

        viewModel.IsLevelSelectOpen.Value = true;
    }
}
