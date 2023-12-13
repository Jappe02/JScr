using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
            public readonly ValueType Type;

            public RuntimeVal(ValueType type) { Type = type; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class NullVal : RuntimeVal
        {
            public readonly dynamic? Value;

            public NullVal() : base(ValueType.null_) { Value = null; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class BoolVal : RuntimeVal
        {
            public readonly bool Value;

            public BoolVal(bool value = true) : base(ValueType.boolean) { Value = value; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class NumberVal : RuntimeVal
        {
            public readonly float Value;

            public NumberVal(float value = 0) : base(ValueType.number) { Value = value; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class ObjectVal : RuntimeVal
        {
            public readonly Dictionary<string, RuntimeVal> Properties;

            public ObjectVal(Dictionary<string, RuntimeVal> properties) : base(ValueType.object_) { Properties = properties; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public delegate RuntimeVal FunctionCall(RuntimeVal[] args, Environment env);

        public class NativeFnVal : RuntimeVal
        {
            public readonly FunctionCall Call;

            public NativeFnVal(FunctionCall call) : base(ValueType.nativeFn) { Call = call; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class FunctionVal : RuntimeVal
        {
            public readonly string Name;
            public readonly string[] Parameters;
            public readonly Environment DeclarationEnv;
            public readonly Stmt[] Body;

            public FunctionVal(string name, string[] parameters, Environment declarationEnv, Stmt[] body) : base(ValueType.function)
            {
                Name = name;
                Parameters = parameters;
                DeclarationEnv = declarationEnv;
                Body = body;
            }

            public override string ToString() => JsonSerializer.Serialize(this);
        }
    }
}
