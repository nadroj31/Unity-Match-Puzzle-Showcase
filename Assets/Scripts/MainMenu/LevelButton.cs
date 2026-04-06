using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level-select button. Stores the chosen level number in <see cref="GameSession"/>
/// before loading the gameplay scene via the injected <see cref="ISceneNavigator"/>.
/// </summary>
public class LevelButton : MonoBehaviour
{
    [SerializeField] private Button          levelButton;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameSession     gameSession;

    private int              level;
    private ISceneNavigator  sceneNavigator;

    private void Awake()
    {
        levelButton.onClick.RemoveAllListeners();
        levelButton.onClick.AddListener(OpenGamePlayScene);
    }

    /// <summary>
    /// Injects the scene navigator used when this button is clicked.
    /// Must be called by the owner (e.g. <see cref="RecycledScrollView"/>) after instantiation.
    /// </summary>
    public void SetNavigator(ISceneNavigator navigator) => sceneNavigator = navigator;

    /// <summary>Configures the displayed level number for this button.</summary>
    public void SetLevelText(int levelNumber)
    {
        levelText.text = $"Level {levelNumber}";
        level          = levelNumber;
    }

    private void OpenGamePlayScene()
    {
        // sceneNavigator is stored as ISceneNavigator (interface); the underlying
        // MonoBehaviour may have been destroyed without the C# reference becoming null.
        // Cast to UnityEngine.Object to use Unity's overloaded == operator, which
        // correctly detects destroyed instances.
        ISceneNavigator nav = sceneNavigator;
        if (nav is Object navObj && navObj == null)
            nav = ScenesManager.Instance;

        if (nav == null)
        {
            Debug.LogError("[LevelButton] No valid ISceneNavigator found.", this);
            return;
        }

        gameSession.SelectedLevel = level;
        nav.LoadGamePlayScene();
    }
}
