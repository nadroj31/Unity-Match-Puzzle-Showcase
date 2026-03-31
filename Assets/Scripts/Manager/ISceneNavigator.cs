/// <summary>Defines scene-transition behaviour for the application.</summary>
public interface ISceneNavigator
{
    /// <summary>Unloads the current scene and loads the main menu synchronously.</summary>
    void LoadMainMenu();

    /// <summary>Loads the gameplay scene asynchronously behind a loading UI.</summary>
    void LoadGamePlayScene();
}
