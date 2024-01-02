using static JScr.Runtime.Values;

namespace JScr.Runtime
{
    internal static class Types
    {
        /*
        public interface RuntimeType
        {
            // Reserved keywords <-> type

            /// <summary> Get all the reserved types and their keywords. </summary>
            public static readonly IReadOnlyDictionary<Type, string> reservedTypesCollection = new Dictionary<Type, string>(){ { typeof(VoidType), "void" }, { typeof(BoolType),"bool" }, { typeof(IntegerType),"int" } };

            /// <summary> Get a list of all reserved keywords for reserved types. </summary>
            public static readonly IReadOnlyList<string> reservedTypesStrs = reservedTypesCollection.Values.ToList();

            // Helper functions

            /// <summary> Converts a reserved type string (like `void`) to a Type like `VoidType`. </summary>
            public static Type ReservedTypeStringToType(string str) => reservedTypesCollection.FirstOrDefault(x => x.Value == str).Key;

            /// <summary> Check if the input type actually is a `RuntimeType`. </summary>
            public static bool ReservedTypeIsValid(Type type) => reservedTypesCollection.ContainsKey(type);

            // Instance
            public string ReservedKeyword => reservedTypesStrs[ReservedKeywordIndex];
            protected int ReservedKeywordIndex { get; }
        }

        public abstract class RuntimeType<T> : RuntimeType
        {
            public int ReservedKeywordIndex { get; }
            public T? Value { get; }

            protected RuntimeType(int reservedKW, T? value)
            {
                Value = value;
                ReservedKeywordIndex = reservedKW;
            }

            public override string ToString() => Value?.ToString() ?? "";
        }

        #region Reserved Types

        public class VoidType : RuntimeType<dynamic?>
        {
            public VoidType() : base(0, null) { }
        }

        public class BoolType : RuntimeType<bool?>
        {
            public BoolType(bool? value = true) : base(1, value) { }
        }

        public class IntegerType : RuntimeType<int?>
        {
            public IntegerType(int? value = 0) : base(2, value) { }
        }

        #endregion
        */

        private static Dictionary<Type, string> types = new(){ { Type.Dynamic, "dynamic" }, { Type.Object, "object" }, { Type.Void, "void" }, { Type.Bool, "bool" }, { Type.Int, "int" }, };
        public static IReadOnlyDictionary<Type, string> reservedTypesDict => types;

        /// <summary>
        /// Convert a string to a Type. A trimmed version of the string will be used.
        /// For example, 'void' will be Type.Void.
        /// </summary>
        /// <returns>Null if no string matches a type.</returns>
        public static Type? FromString(string input)
        {
            return types.FirstOrDefault(x => x.Value == input.Trim()).Key;
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
            }

            return Type.Dynamic;
        }

        /// <summary>Check if a [RuntimeVal] matches the specified [Type].</summary>
        /// <returns>False if the input [RuntimeVal] has a different type than the `type` parameter.</returns>
        public static bool RuntimeValMatchesType(Type type, RuntimeVal runtimeVal)
        {
            return SuitableType(runtimeVal) == type;
        }

        public enum Type : ushort
        {
            Dynamic,
            Object,
            Void,
            Bool,
            Int
        }
    }
}
