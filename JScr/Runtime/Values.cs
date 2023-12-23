using System.Text.Json;
using static JScr.Frontend.Ast;

namespace JScr.Runtime
{
    internal static class Values
    {
        public enum ValueType
        {
            null_,
            runtimeType,
            object_,
            function,
            nativeFn,
        }

        public abstract class RuntimeVal
        {
            public ValueType Type { get; }

            public RuntimeVal(ValueType type) { Type = type; }
        }

        public interface RuntimeType {
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
            public int ReservedKeywordIndex { get; }
            public T Value { get; }

            public RuntimeType(int reservedKW, T value) : base(ValueType.runtimeType) {
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

        public class BoolType : RuntimeType<bool>
        {
            public BoolType(bool value = true) : base(1, value) {}
        }

        public class IntegerType : RuntimeType<int>
        {
            public IntegerType(int value = 0) : base(2, value) {}
        }

        #endregion

        public class NullVal : RuntimeVal
        {
            public dynamic? Value { get; }

            public NullVal() : base(ValueType.null_) { Value = null; }

            public override string ToString() => Value.ToString() ?? "";
        }

        public class ObjectVal : RuntimeVal
        {
            public Dictionary<string, RuntimeVal> Properties { get; }

            public ObjectVal(Dictionary<string, RuntimeVal> properties) : base(ValueType.object_) { Properties = properties; }

            public override string ToString() => Properties.ToJson();
        }

        public delegate RuntimeVal FunctionCall(RuntimeVal[] args, Environment env);

        public class NativeFnVal : RuntimeVal
        {
            public FunctionCall Call { get; }

            public NativeFnVal(FunctionCall call) : base(ValueType.nativeFn) { Call = call; }

            public override string ToString() => Call.ToString() ?? "NativeFnVal";
        }

        public class FunctionVal : RuntimeVal
        {
            public string Name { get; }
            public Type Type_ { get; }
            public VarDeclaration[] Parameters { get; }
            public Environment DeclarationEnv { get; }
            public Stmt[] Body { get; }

            public FunctionVal(string name, Type type, VarDeclaration[] parameters, Environment declarationEnv, Stmt[] body) : base(ValueType.function)
            {
                Name = name;
                Type_ = type;
                Parameters = parameters;
                DeclarationEnv = declarationEnv;
                Body = body;
            }

            public override string ToString() => this.ToJson();
        }
    }
}
