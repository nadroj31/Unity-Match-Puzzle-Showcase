using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UI controller for the main menu and level-select screen.</summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private LevelRepository levelRepository;
    [SerializeField] private Button          exitButton;
    [SerializeField] private Button          startButton;
    [SerializeField] private GameObject      levelSelectUI;
    [SerializeField] private LevelButton     levelButtonPrefab;
    [SerializeField] private Transform       levelButtonParent;

    private readonly List<LevelButton> levelButtons = new List<LevelButton>();

    private void Awake()
    {
        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(CloseLevelSelectMenu);
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OpenLevelSelectMenu);
    }

    private void CloseLevelSelectMenu() => levelSelectUI.SetActive(false);

    private void OpenLevelSelectMenu()
    {
        levelRepository.LoadLevelData();

        List<int> levelKeys = levelRepository.GetAllLevelKeys();
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

        levelSelectUI.SetActive(true);
    }
}
