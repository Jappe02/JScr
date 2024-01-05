using System;
using System.Xml.Linq;
using static JScr.Runtime.Values;

namespace JScr.Runtime
{
    internal static class Types
    {
        private static Dictionary<Type, string> types = new(){ { Type.Dynamic, "dynamic" }, { Type.Object, "object" }, { Type.Void, "void" }, { Type.Bool, "bool" }, { Type.Int, "int" }, { Type.String, "string" }, { Type.Char, "char" },};
        public static IReadOnlyDictionary<Type, string> reservedTypesDict => types;

        /// <summary>
        /// Convert a string to a Type. A trimmed version of the string will be used.
        /// For example, 'void' will be Type.Void.
        /// </summary>
        /// <returns>Null if no string matches a type.</returns>
        public static Type? FromString(string input)
        {
            string val = input.Trim().Replace(" ", "");
            bool array = input.EndsWith("[]");

            if (array)
            {
                val = val.Replace("[]", "");
                var arrayType = FromString(val);
                return arrayType != null ? Type.Array(arrayType) : null;
            }

            return types.FirstOrDefault(x => x.Value == val).Key;
        }

        /// <summary>
        /// Find a suitable return type for a [Values.ValueType].
        /// </summary>
        public static Type SuitableType(RuntimeVal? runtimeVal)
        {
            Values.ValueType? valueType = runtimeVal?.Type;

            if (runtimeVal != null && valueType == Values.ValueType.function)
                return ((FunctionVal)runtimeVal).Type_;
            else if (runtimeVal != null && valueType == Values.ValueType.nativeFn)
                return ((NativeFnVal)runtimeVal).Type_;

            switch (valueType)
            {
                case Values.ValueType.array:
                {
                    if (runtimeVal == null)
                        break;

                    var arrayVal = (ArrayVal)runtimeVal;
                    Type? type = null;
                    bool dynamic = false;
                    foreach (var t in arrayVal.Value)
                    {
                        var oldType = type;
                        type = SuitableType(t);
                        if (oldType != null && oldType != type)
                        {
                            dynamic = true;
                            break;
                        }
                    }

                    if (type == null) dynamic = true;

                    return Type.Array(dynamic ? Type.Dynamic : type);
                }
                case Values.ValueType.object_:
                {
                    return Type.Object;
                }
                case null: {
                    return Type.Void;
                }
                case Values.ValueType.boolean:
                {
                    return Type.Bool;
                }
                case Values.ValueType.integer:
                {
                    return Type.Int;
                }
                case Values.ValueType.string_:
                {
                    return Type.String;
                }
                case Values.ValueType.char_:
                {
                    return Type.Char;
                }
            }

            return Type.Dynamic;
        }

        /// <summary>Check if a [RuntimeVal] matches the specified [Type].</summary>
        /// <returns>False if the input [RuntimeVal] has a different type than the `type` parameter.</returns>
        public static bool RuntimeValMatchesType(Type type, RuntimeVal runtimeVal)
        {
            return SuitableType(runtimeVal).Equals(type);
        }

        public class Type : IComparable<Type>
        {
            public ushort Val { get; }
            public Type? Child { get; }
            private Type(ushort v, Type? child = null) { Val = v; Child = child; }

            public static Type Array(Type of) => new(0, of);
            public static readonly Type Dynamic = new(1);
            public static readonly Type Object = new(2);
            public static readonly Type Void = new(3);
            public static readonly Type Bool = new(4);
            public static readonly Type Int = new(5);
            public static readonly Type String = new(6);
            public static readonly Type Char = new(7);

            public int CompareTo(Type? other)
            {
                return -1;
            }

            public override bool Equals(object? obj)
            {
                if (obj == null || GetType() != obj.GetType())
                {
                    return false;
                }

                Type other = (Type)obj;
                if (Val == other.Val)
                {
                    if (Child != null) return Child.Equals(other.Child);
                    return true;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return (Val, Child).GetHashCode();
            }
        }

        /*
        public enum Type : ushort
        {
            Dynamic,
            Object,
            Void,
            Bool,
            Int,
            String,
            Char,
        }*/
    }
}
