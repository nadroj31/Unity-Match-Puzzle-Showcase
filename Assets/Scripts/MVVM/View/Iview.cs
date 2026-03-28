/// <summary>
/// Contract for an MVVM View. Provides a <see cref="BindingContext"/> through which
/// the View receives and reacts to a ViewModel of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The ViewModel type this View accepts.</typeparam>
public interface IView<T> where T : ViewModelBase
{
    /// <summary>Gets or sets the ViewModel bound to this View.</summary>
    T BindingContext { get; set; }
}
