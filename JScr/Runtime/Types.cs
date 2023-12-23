using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JScr.Runtime.Values;

namespace JScr.Runtime
{
    internal static class Types
    {
        public enum Type
        {
            Void,
            Bool,
            Int,
        }

        public interface RuntimeType
        {
            public static readonly IReadOnlyDictionary<Type, string> reservedTypesCollection = new Dictionary<Type, string>(){ { typeof(VoidType), "void" }, { typeof(BoolType),"bool" }, { typeof(IntegerType),"int" } };
            public static readonly IReadOnlyList<string> reservedTypesStrs = reservedTypesCollection.Values.ToList();

            public static Type ReservedTypeStringToType(string str) => reservedTypesCollection.FirstOrDefault(x => x.Value == str).Key;
            public static bool ReservedTypeIsValid(Type type) => reservedTypesCollection.ContainsKey(type);


            //public static Type StringToType(string s) { return typeof(string); }

            public string ReservedKeyword => reservedTypesStrs[ReservedKeywordIndex];
            protected int ReservedKeywordIndex { get; }
        }

        public abstract class RuntimeType<T> : RuntimeVal, RuntimeType
        {
            public Type Type_ { get; }
            public int ReservedKeywordIndex { get; }
            public T Value { get; }

            protected RuntimeType(Type type, int reservedKW, T value) : base(ValueType.runtimeType)
            {
                Type_ = type;
                Value = value;
                ReservedKeywordIndex = reservedKW;
            }

            public override string ToString() => Value?.ToString() ?? "";
        }

        public class VoidType : RuntimeType<dynamic?>
        {
            public VoidType() : base(Type.Void, 0, null) { }
        }

        public class BoolType : RuntimeType<bool>
        {
            public BoolType(bool value = true) : base(1, value) { }
        }

        public class IntegerType : RuntimeType<int>
        {
            public IntegerType(int value = 0) : base(2, value) { }
        }
    }
}
