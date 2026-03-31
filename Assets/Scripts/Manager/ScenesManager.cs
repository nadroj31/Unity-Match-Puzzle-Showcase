using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent scene-transition service (survives scene loads).
/// Implements <see cref="ISceneNavigator"/> so callers depend on the interface, not the concrete class.
/// </summary>
public class ScenesManager : MonoBehaviour, ISceneNavigator
{
    [Tooltip("Exact name of the main-menu scene as it appears in Build Settings.")]
    [SerializeField] private string mainSceneName     = "MainScene";

    [Tooltip("Exact name of the gameplay scene as it appears in Build Settings.")]
    [SerializeField] private string gamePlaySceneName = "GamePlayScene";

    [SerializeField] private GameObject loadingUI;

    private static ScenesManager instance;

    /// <summary>Returns the <see cref="ISceneNavigator"/> interface.</summary>
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

    // ── ISceneNavigator ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void LoadMainMenu()      => SceneManager.LoadScene(mainSceneName);

    /// <inheritdoc/>
    public void LoadGamePlayScene() => StartCoroutine(LoadAsyncScene(gamePlaySceneName));

    // ── Internals ─────────────────────────────────────────────────────────────

    private IEnumerator LoadAsyncScene(string sceneName)
    {
        loadingUI.SetActive(true);

        var asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        yield return new WaitUntil(() => asyncLoad.isDone);
        yield return new WaitForSeconds(0.5f); // brief hold so the loading UI isn't a flash

        loadingUI.SetActive(false);
    }
}
