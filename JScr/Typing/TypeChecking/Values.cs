using JScr.Typing;

namespace JScr.Typing.TypeChecking
{
    public abstract class RuntimeVal
    {
        internal abstract Type Type { get; }
    }

    public abstract class RuntimeVal<T> : RuntimeVal
    {
        internal RuntimeVal(T value)
        {
            Value = value;
        }

        internal T Value { get; private set; }
    }

    public class NullValue : RuntimeVal
    {
        internal override Type Type { get => new SimpleType("void"); }
    }

    public class CharLiteralValue : RuntimeVal<char>
    {
        public CharLiteralValue(char value) : base(value) {}

        internal override Type Type { get => new SimpleType("char"); }
    }

    public class IntLiteralValue : RuntimeVal<int>
    {
        public IntLiteralValue(int value) : base(value) { }

        internal override Type Type { get => new SimpleType("int"); }
    }
}
