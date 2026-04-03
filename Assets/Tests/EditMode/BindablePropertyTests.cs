using NUnit.Framework;

/// <summary>
/// Edit-mode unit tests for <see cref="BindableProperty{T}"/>.
/// No Unity scene or Play Mode required — pure C# logic only.
/// </summary>
public class BindablePropertyTests
{
    // ── Initial value ─────────────────────────────────────────────────────────

    [Test]
    public void Value_StartsAtInitialValue()
    {
        var prop = new BindableProperty<int>(42);
        Assert.AreEqual(42, prop.Value);
    }

    [Test]
    public void Value_DefaultIsDefaultOfType()
    {
        var prop = new BindableProperty<int>(); // no initial value
        Assert.AreEqual(0, prop.Value);
    }

    // ── ValueChanged fires ────────────────────────────────────────────────────

    [Test]
    public void ValueChanged_FiredWhenValueChanges()
    {
        var prop = new BindableProperty<int>(0);
        bool fired = false;
        prop.ValueChanged += (_, __) => fired = true;

        prop.Value = 1;

        Assert.IsTrue(fired);
    }

    [Test]
    public void ValueChanged_ProvidesCorrectOldAndNewValues()
    {
        var prop = new BindableProperty<int>(10);
        int receivedOld = -1, receivedNew = -1;
        prop.ValueChanged += (old, next) => { receivedOld = old; receivedNew = next; };

        prop.Value = 20;

        Assert.AreEqual(10, receivedOld);
        Assert.AreEqual(20, receivedNew);
    }

    // ── ValueChanged does NOT fire ────────────────────────────────────────────

    [Test]
    public void ValueChanged_NotFiredWhenSameValueAssigned()
    {
        var prop = new BindableProperty<int>(5);
        bool fired = false;
        prop.ValueChanged += (_, __) => fired = true;

        prop.Value = 5; // same value — should be a no-op

        Assert.IsFalse(fired);
    }

    [Test]
    public void ValueChanged_NotFiredForNullToNull()
    {
        var prop = new BindableProperty<string>(null);
        bool fired = false;
        prop.ValueChanged += (_, __) => fired = true;

        prop.Value = null;

        Assert.IsFalse(fired);
    }

    // ── Unsubscribe ───────────────────────────────────────────────────────────

    [Test]
    public void ValueChanged_UnsubscribedHandlerNotCalled()
    {
        var prop = new BindableProperty<int>(0);
        int callCount = 0;
        BindableProperty<int>.ValueChangedHandler handler = (_, __) => callCount++;

        prop.ValueChanged += handler;
        prop.Value = 1;             // fires → callCount = 1

        prop.ValueChanged -= handler;
        prop.Value = 2;             // handler gone → callCount stays 1

        Assert.AreEqual(1, callCount);
    }

    // ── Multiple subscribers ──────────────────────────────────────────────────

    [Test]
    public void ValueChanged_AllSubscribersNotified()
    {
        var prop = new BindableProperty<int>(0);
        int a = 0, b = 0;
        prop.ValueChanged += (_, v) => a = v;
        prop.ValueChanged += (_, v) => b = v;

        prop.Value = 7;

        Assert.AreEqual(7, a);
        Assert.AreEqual(7, b);
    }
}
