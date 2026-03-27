using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level selection button.
/// Writes the chosen level to <see cref="GameSession"/> (ScriptableObject)
/// instead of PlayerPrefs — no disk I/O, no magic string keys.
/// <see cref="GameSession"/> is injected via the Inspector on the prefab.
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
