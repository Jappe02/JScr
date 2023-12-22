using System.Text.Json;
using static JScr.Frontend.Ast;

namespace JScr.Runtime
{
    internal static class Values
    {
        public enum ValueType
        {
            null_,
            number,
            boolean,
            object_,
            function,
            nativeFn,
        }

        public abstract class RuntimeVal
        {
            public ValueType Type { get; }

            public RuntimeVal(ValueType type) { Type = type; }
        }

        public class NullVal : RuntimeVal
        {
            public dynamic? Value { get; }

            public NullVal() : base(ValueType.null_) { Value = null; }

            public override string ToString() => Value.ToString() ?? "";
        }

        public class BoolVal : RuntimeVal
        {
            public bool Value { get; }

            public BoolVal(bool value = true) : base(ValueType.boolean) { Value = value; }

            public override string ToString() => Value.ToString();
        }

        public class NumberVal : RuntimeVal
        {
            public float Value { get; }

            public NumberVal(float value = 0) : base(ValueType.number) { Value = value; }

            public override string ToString() => Value.ToString();
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
            public string[] Parameters { get; }
            public Environment DeclarationEnv { get; }
            public Stmt[] Body { get; }

            public FunctionVal(string name, string[] parameters, Environment declarationEnv, Stmt[] body) : base(ValueType.function)
            {
                Name = name;
                Parameters = parameters;
                DeclarationEnv = declarationEnv;
                Body = body;
            }

            public override string ToString() => this.ToJson();
        }
    }
}
