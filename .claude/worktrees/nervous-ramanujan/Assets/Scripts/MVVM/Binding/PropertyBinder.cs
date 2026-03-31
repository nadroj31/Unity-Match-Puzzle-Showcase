using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Manages subscribe/unsubscribe operations for <see cref="BindableProperty{T}"/> fields
/// on a ViewModel. Register bindings once with <see cref="Add{TProperty}"/>, then call
/// <see cref="Bind"/> and <see cref="Unbind"/> whenever the ViewModel instance changes.
/// </summary>
/// <typeparam name="T">The ViewModel type.</typeparam>
public class PropertyBinder<T> where T : ViewModelBase
{
    private delegate void BindHandler(T viewmodel);
    private delegate void UnbindHandler(T viewmodel);

    private readonly List<BindHandler> _binders = new();
    private readonly List<UnbindHandler> _unBinders = new();

    /// <summary>
    /// Registers a handler for a public <see cref="BindableProperty{TProperty}"/> field on the ViewModel.
    /// The field is resolved by reflection once here; subsequent <see cref="Bind"/>/<see cref="Unbind"/>
    /// calls reuse the cached <see cref="FieldInfo"/>.
    /// </summary>
    /// <param name="name">The exact name of the public field on <typeparamref name="T"/>.</param>
    /// <param name="valueChangedHandler">The handler to subscribe/unsubscribe.</param>
    /// <exception cref="Exception">Thrown if the field does not exist or has an incompatible type.</exception>
    public void Add<TProperty>(string name, BindableProperty<TProperty>.ValueChangedHandler valueChangedHandler)
    {
        var fieldInfo = typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new Exception(string.Format("Cannot find bindable property field '{0}.{1}'", typeof(T).Name, name));

        _binders.Add(viewmodel =>
        {
            var property = GetPropertyValue<TProperty>(name, viewmodel, fieldInfo);
            property.ValueChanged += valueChangedHandler;
        });

        _unBinders.Add(viewmodel =>
        {
            var property = GetPropertyValue<TProperty>(name, viewmodel, fieldInfo);
            property.ValueChanged -= valueChangedHandler;
        });
    }

    /// <summary>Subscribes all registered handlers to the given ViewModel.</summary>
    public void Bind(T viewmodel)
    {
        if (viewmodel == null) return;
        foreach (var binder in _binders) binder(viewmodel);
    }

    /// <summary>Unsubscribes all registered handlers from the given ViewModel.</summary>
    public void Unbind(T viewmodel)
    {
        if (viewmodel == null) return;
        foreach (var unBinder in _unBinders) unBinder(viewmodel);
    }

    private BindableProperty<TProperty> GetPropertyValue<TProperty>(string name, T viewmodel, FieldInfo fieldInfo)
    {
        var value = fieldInfo.GetValue(viewmodel);
        if (value is BindableProperty<TProperty> property) return property;
        throw new Exception(string.Format("Invalid bindable property field type '{0}.{1}'", typeof(T).Name, name));
    }
}
