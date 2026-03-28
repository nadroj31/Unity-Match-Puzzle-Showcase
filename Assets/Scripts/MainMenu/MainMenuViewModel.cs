/// <summary>
/// Exposes observable main-menu state for <see cref="MainMenuView"/> to bind against.
/// Created and updated by <see cref="MainMenu"/>; consumed read-only by the View.
/// </summary>
public class MainMenuViewModel : ViewModelBase
{
    /// <summary><c>true</c> while the level-select panel is visible.</summary>
    public BindableProperty<bool> IsLevelSelectOpen = new BindableProperty<bool>(false);
}
