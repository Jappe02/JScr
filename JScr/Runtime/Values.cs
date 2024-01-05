﻿using System.Text.Json;
using static JScr.Frontend.Ast;

namespace JScr.Runtime
{
    internal static class Values
    {
        public enum ValueType
        {
            null_,
            array,
            integer,
            string_,
            char_,
            boolean,
            object_,
            function,
            nativeFn,
        }

        public abstract class RuntimeVal
        {
            public ValueType Type { get; }

            public RuntimeVal(ValueType type) { Type = type; }

            public override string ToString()
            {
                return "RuntimeVal";
            }
        }

        public class NullVal : RuntimeVal
        {
            public dynamic? Value => null;

            public NullVal() : base(ValueType.null_) { }

            public override string ToString() => "null";
        }

        public class ArrayVal : RuntimeVal
        {
            public RuntimeVal[] Value { get; }

            public ArrayVal(RuntimeVal[] value) : base(ValueType.array) { Value = value; }

            public override string ToString() => Value.ToJson();
        }

        public class BoolVal : RuntimeVal
        {
            public bool Value { get; }

            public BoolVal(bool value = true) : base(ValueType.boolean) { Value = value; }

            public override string ToString() => Value.ToString();
        }

        public class IntegerVal : RuntimeVal
        {
            public int Value { get; }

            public IntegerVal(int value = 0) : base(ValueType.integer) { Value = value; }

            public override string ToString() => Value.ToString();
        }

        public class StringVal : RuntimeVal
        {
            public string Value { get; }

            public StringVal(string value = "") : base(ValueType.string_) { Value = value; }

            public override string ToString() => Value.ToString();
        }

        public class CharVal : RuntimeVal
        {
            public char Value { get; }

            public CharVal(char value) : base(ValueType.char_) { Value = value; }

            public override string ToString() => Value.ToString();
        }

        public class ObjectVal : RuntimeVal
        {
            public class Property
            {
                public string Key { get; }
                public Types.Type Type { get; }
                public RuntimeVal Value { get; }

                public Property(string key, Types.Type? type, RuntimeVal value)
                {
                    Key = key;
                    Type = type ?? Types.Type.Void;
                    Value = value;
                }

                public override string ToString() => this.ToJson();
            }

            public List<Property> Properties { get; }

            public ObjectVal(List<Property> properties) : base(ValueType.object_) { Properties = properties; }

            public override string ToString() => Properties.ToJson();
        }

        public delegate RuntimeVal FunctionCall(RuntimeVal[] args, Environment env);

        public class NativeFnVal : RuntimeVal
        {
            public Types.Type Type_ { get; }
            public FunctionCall Call { get; }

            public NativeFnVal(Types.Type type, FunctionCall call) : base(ValueType.nativeFn) { Type_ = type; Call = call; }

            public override string ToString() => Call.ToString() ?? "NativeFnVal";
        }

        public class FunctionVal : RuntimeVal
        {
            public string Name { get; }
            public Types.Type Type_ { get; }
            public VarDeclaration[] Parameters { get; }
            public Environment DeclarationEnv { get; }
            public Stmt[] Body { get; }

            public FunctionVal(string name, Types.Type type, VarDeclaration[] parameters, Environment declarationEnv, Stmt[] body) : base(ValueType.function)
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
