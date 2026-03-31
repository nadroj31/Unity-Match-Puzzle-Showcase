using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level-select button. Stores the chosen level number in <see cref="GameSession"/>
/// before loading the gameplay scene.
/// </summary>
public class LevelButton : MonoBehaviour
{
    [SerializeField] private Button          levelButton;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameSession     gameSession;

    private int level;

    private void Awake()
    {
        levelButton.onClick.RemoveAllListeners();
        levelButton.onClick.AddListener(OpenGamePlayScene);
    }

    public void SetLevelText(int levelNumber)
    {
        levelText.text = $"Level {levelNumber}";
        level          = levelNumber;
    }

    private void OpenGamePlayScene()
    {
        gameSession.SelectedLevel = level;
        ScenesManager.Instance.LoadGamePlayScene();
    }
}
