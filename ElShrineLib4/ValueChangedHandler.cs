namespace ElShrine;

public class ValueChangedEventArgs<TValue>(TValue oldValue, TValue newValue) : EventArgs
{
    public TValue OldValue { get; init; } = oldValue;
    public TValue NewValue { get; init; } = newValue;
}
public delegate void ValueChangedHandler<TValue>(object? sender, ValueChangedEventArgs<TValue> e);
