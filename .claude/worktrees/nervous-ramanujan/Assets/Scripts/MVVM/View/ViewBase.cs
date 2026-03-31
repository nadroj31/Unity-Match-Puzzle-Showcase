using UnityEngine;

/// <summary>
/// Abstract base class for MVVM Views. Manages the full ViewModel binding lifecycle:
/// initialization on first assignment, rebinding when the ViewModel is swapped, and
/// cleanup on destroy.
/// <para>
/// Override <see cref="OnInitialize"/> to register property bindings via <see cref="Binder"/>.
/// Override <see cref="OnBindingContextChange"/> for custom swap logic.
/// </para>
/// </summary>
/// <typeparam name="T">The ViewModel type this View accepts.</typeparam>
public abstract class ViewBase<T> : MonoBehaviour, IView<T> where T : ViewModelBase
{
    /// <summary>Manages subscribe/unsubscribe operations for ViewModel properties.</summary>
    protected readonly PropertyBinder<T> Binder = new();

    /// <summary>Wraps the current ViewModel; fires <see cref="OnBindingContextChange"/> on change.</summary>
    protected readonly BindableProperty<T> ViewmodelProperty = new();

    private bool _isInitialized = false;

    /// <summary>
    /// Gets or sets the bound ViewModel. Setting this for the first time triggers
    /// <see cref="OnInitialize"/>, then fires <see cref="OnBindingContextChange"/>
    /// to apply the new bindings.
    /// </summary>
    public T BindingContext
    {
        get => ViewmodelProperty.Value;
        set
        {
            if (!_isInitialized)
            {
                OnInitialize();
                _isInitialized = true;
            }
            ViewmodelProperty.Value = value;
        }
    }

    /// <summary>
    /// Called once before the first ViewModel is assigned. Override to register bindings
    /// with <see cref="Binder"/> via <c>Binder.Add(...)</c>.
    /// </summary>
    protected virtual void OnInitialize()
    {
        ViewmodelProperty.ValueChanged += OnBindingContextChange;
    }

    /// <summary>
    /// Called when <see cref="BindingContext"/> changes. Unbinds the old ViewModel and
    /// binds the new one. Override to add custom swap behaviour.
    /// </summary>
    protected virtual void OnBindingContextChange(T oldValue, T newValue)
    {
        if (oldValue != null) Binder.Unbind(oldValue);
        if (newValue != null) Binder.Bind(newValue);
    }

    /// <summary>
    /// Cleans up all bindings. Sets <see cref="BindingContext"/> to <c>null</c> first so
    /// <see cref="OnBindingContextChange"/> can properly unbind the current ViewModel before
    /// the event subscription is removed.
    /// </summary>
    protected virtual void OnDestroy()
    {
        BindingContext = null;
        ViewmodelProperty.ValueChanged -= OnBindingContextChange;
    }
}
