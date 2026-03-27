using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent scene-transition manager (survives scene loads).
/// Implements <see cref="ISceneNavigator"/> so callers depend on the interface,
/// not the concrete class.
///
/// Scene names are constants — no magic strings in callers.
/// Access via <see cref="Instance"/> which returns <see cref="ISceneNavigator"/>.
/// </summary>
public class ScenesManager : MonoBehaviour, ISceneNavigator
{
    private const string MainSceneName     = "MainScene";
    private const string GamePlaySceneName = "GamePlayScene";

    [SerializeField] private GameObject loadingUI;

    private static ScenesManager instance;

    /// <summary>Returns the navigator interface. Performs a scene search on first access.</summary>
    public static ISceneNavigator Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<ScenesManager>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // -------------------------------------------------------------------------
    // ISceneNavigator
    // -------------------------------------------------------------------------

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(MainSceneName);
    }

    public void LoadGamePlayScene()
    {
        StartCoroutine(LoadAsyncScene(GamePlaySceneName));
    }

    // -------------------------------------------------------------------------
    // Internals
    // -------------------------------------------------------------------------

    private IEnumerator LoadAsyncScene(string sceneName)
    {
        loadingUI.SetActive(true);

        var asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        yield return new WaitUntil(() => asyncLoad.isDone);
        yield return new WaitForSeconds(0.5f);

        loadingUI.SetActive(false);
    }
}
