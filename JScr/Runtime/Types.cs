using System;
using System.Xml.Linq;
using static JScr.Runtime.Values;

namespace JScr.Runtime
{
    internal static class Types
    {
        private static Dictionary<Type, string> types = new(){ { Type.Dynamic(), "dynamic" }, /*{ Type.Object(), "object" },*/ { Type.Void(), "void" }, { Type.Bool(), "bool" }, { Type.Int(), "int" }, { Type.String(), "string" }, { Type.Char(), "char" },};
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

            var type = types.FirstOrDefault(x => x.Value == val).Key;

            if (type == null)
            {
                return Type.Object(val);
            }

            return type;
        }

        /// <summary>
        /// Find a suitable return type for a [Values.ValueType].
        /// </summary>
        /// <returns>Null if it is suitable for any type.</returns>
        public static Type? SuitableType(RuntimeVal? runtimeVal)
        {
            Values.ValueType? valueType = runtimeVal?.Type;

            if (runtimeVal != null && valueType == Values.ValueType.function)
                return ((FunctionVal)runtimeVal).Type_;
            else if (runtimeVal != null && valueType == Values.ValueType.nativeFn)
                return ((NativeFnVal)runtimeVal).Type_;

            switch (valueType)
            {
                case Values.ValueType.null_:
                {
                    return null;
                }
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
                        if (oldType != null && !oldType.Equals(type))
                        {
                            dynamic = true;
                            break;
                        }
                    }

                    if (type == null) dynamic = true;

                    return Type.Array(dynamic ? Type.Dynamic() : type);
                }
                case Values.ValueType.objectInstance:
                {
                    if (runtimeVal == null)
                        break;

                    var objVal = (ObjectInstanceVal)runtimeVal;
                    return Type.Object(objVal.ObjType);
                }
                case null: {
                    return Type.Void();
                }
                case Values.ValueType.boolean:
                {
                    return Type.Bool();
                }
                case Values.ValueType.integer:
                {
                    return Type.Int();
                }
                case Values.ValueType.string_:
                {
                    return Type.String();
                }
                case Values.ValueType.char_:
                {
                    return Type.Char();
                }
            }

            return Type.Dynamic();
        }

        /// <summary>Check if a [RuntimeVal] matches the specified [Type].</summary>
        /// <returns>False if the input [RuntimeVal] has a different type than the `type` parameter.</returns>
        public static bool RuntimeValMatchesType(Type type, RuntimeVal runtimeVal)
        {
            var suitable = SuitableType(runtimeVal);

            return suitable != null ? suitable.Equals(type) : true;
        }

        public class Type : IComparable<Type>
        {
            public ushort Val { get; }
            public string? Data { get; }
            public Type? Child { get; }
            public Type[]? LambdaTypes { get; }

            public bool IsLambda => LambdaTypes != null;

            private Type(ushort v, Type[]? lambdaTypes, Type? child = null, string? data = null)
            {
                Val = v; LambdaTypes = lambdaTypes?.Length == 0 ? null : lambdaTypes; Child = child; Data = data;
            }

            public static Type Array(Type of, Type[]? lambdaTypes = null) => new(0, lambdaTypes, of);
            public static Type Dynamic(Type[]? lambdaTypes = null) => new(1, lambdaTypes);
            public static Type Object(string name, Type[]? lambdaTypes = null) => new(2, lambdaTypes, data: name);
            public static Type Void(Type[]? lambdaTypes = null) => new(3, lambdaTypes);
            public static Type Bool(Type[]? lambdaTypes = null) => new(4, lambdaTypes);
            public static Type Int(Type[]? lambdaTypes = null) => new(5, lambdaTypes);
            public static Type String(Type[]? lambdaTypes = null) => new(6, lambdaTypes);
            public static Type Char(Type[]? lambdaTypes = null) => new(7, lambdaTypes);

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
                    bool sameChild = true;
                    bool sameData = true;
                    bool sameLambdaTypes = true;
                    if (Child != null) sameChild = Child.Equals(other.Child);
                    if (Data != null) sameData = Data.Equals(other.Data);
                    if (LambdaTypes != null)
                    {
                        for (int i = 0; i < LambdaTypes.Length; i++)
                        {
                            if (!LambdaTypes[i].Equals(other.LambdaTypes?.ElementAtOrDefault(i)))
                            {
                                sameLambdaTypes = false;
                                break;
                            }
                        }
                    }
                    return sameChild && sameLambdaTypes;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return (Val, Child).GetHashCode();
            }

            public Type CopyWith(Type[]? lambdaTypes = null, Type? child = null, string? data = null)
            {
                return new Type(Val, lambdaTypes ?? LambdaTypes, child ?? Child, data ?? Data);
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
