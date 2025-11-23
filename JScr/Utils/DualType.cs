namespace JScr.Utils;

/// <summary>A type that accepts a value of two different types.</summary>
/// <typeparam name="T1">The first type.</typeparam>
/// <typeparam name="T2">The second type.</typeparam>
public class DualType<T1, T2>
{
    private object? _value;

    public object? Value
    {
        private get => _value;
        set
        {
            if (value is not null && value is not T1 && value is not T2)
                throw new Exception($"Invalid type passed for a DualType. Got `{value.GetType()}` but need {ToString()}.");
            _value = value;
        }
    }

    public DualType(object? value)
    {
        Value = value;
    }

    public override string ToString() => $"({typeof(T1)} | {typeof(T2)})";

    public static implicit operator T1(DualType<T1, T2> d) => (T1)d.Value!;
    public static implicit operator T2(DualType<T1, T2> d) => (T2)d.Value!;
}