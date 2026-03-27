/// <summary>
/// Abstracts scene-transition logic so callers depend on behaviour, not implementation.
/// </summary>
public interface ISceneNavigator
{
    void LoadMainMenu();
    void LoadGamePlayScene();
}
