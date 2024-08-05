using JScr.Frontend;
using System;
using System.Reflection.Emit;
using System.Xml.Linq;
using static JScr.Frontend.Ast;
using static JScr.Runtime.Values;

namespace JScr.Runtime
{
    internal static class Types
    {
        /// <summary>Operators to compare values.</summary>
        public enum EqualityCheckOp
        {
            Equals,
            NotEquals,
            MoreThan,
            LessThan,
            MoreThanOrEquals,
            LessThanOrEquals,
        }

        /// <summary>List of reserved types and their string identifiers.</summary>
        private static readonly Dictionary<Type, string> types = new()
        {
            { Type.Dynamic(), "dynamic" },
            { Type.Void(),    "void"    }, 
            { Type.Bool(),    "bool"    }, 
            { Type.Int(),     "int"     }, 
            { Type.Float(),   "float"   }, 
            { Type.Double(),  "double"  }, 
            { Type.String(),  "string"  }, 
            { Type.Char(),    "char"    },
        };
        public static IReadOnlyDictionary<Type, string> reservedTypesDict => types;

        #region Type Specific Actions

        public static RuntimeVal Convert(RuntimeVal val, Type to, bool fallbackToOriginalValue = false) // TODO: Use
        {
            switch (val.Type)
            {
                case Values.ValueType.integer:
                {
                    if (to == Type.Float())
                        return new FloatVal((val as IntegerVal).Value);
                    else if (to == Type.Double())
                        return new DoubleVal((val as IntegerVal).Value);
                    else if (to == Type.Bool())
                        return new BoolVal((val as IntegerVal).Value > 0);

                    break;
                }
                case Values.ValueType.float_:
                {
                    if (to == Type.Int())
                        return new IntegerVal((int)(val as FloatVal).Value);
                    else if (to == Type.Double())
                        return new DoubleVal((val as FloatVal).Value);

                    break;
                }
                case Values.ValueType.double_:
                {
                    if (to == Type.Int())
                        return new IntegerVal((int)(val as DoubleVal).Value);
                    else if (to == Type.Float())
                        return new FloatVal((float)(val as DoubleVal).Value);

                    break;
                }
                case Values.ValueType.char_:
                {
                    if (to == Type.Int())
                        return new IntegerVal((val as CharVal).Value);

                    break;
                }
                case Values.ValueType.boolean:
                {
                    if (to == Type.Int())
                        return new IntegerVal((val as BoolVal).Value ? 1 : 0);

                    break;
                }
            }

            if (fallbackToOriginalValue)
                return val;

            throw new RuntimeException($"Failed to convert value of type \"{SuitableType(val)}\" to \"{to}\".");
        }

        /// <summary>
        /// Handles comparison operators for types.
        /// </summary>
        /// <returns>The result of the comparison.</returns>
        public static RuntimeVal Compare(RuntimeVal lhs, RuntimeVal rhs, EqualityCheckOp operator_)
        {
            if (lhs.Type != rhs.Type) return new BoolVal(false);

            if (operator_ == EqualityCheckOp.Equals)
                return new BoolVal(lhs.Equals(rhs));
            else if (operator_ == EqualityCheckOp.NotEquals)
                return new BoolVal(!lhs.Equals(rhs));

            switch (lhs.Type)
            {
                case Values.ValueType.integer:
                {
                    if (operator_ == EqualityCheckOp.LessThan)
                        return new BoolVal((lhs as IntegerVal).Value < (rhs as IntegerVal).Value);
                    else if (operator_ == EqualityCheckOp.LessThanOrEquals)
                        return new BoolVal((lhs as IntegerVal).Value <= (rhs as IntegerVal).Value);
                    else if (operator_ == EqualityCheckOp.MoreThan)
                        return new BoolVal((lhs as IntegerVal).Value > (rhs as IntegerVal).Value);
                    else if (operator_ == EqualityCheckOp.MoreThanOrEquals)
                        return new BoolVal((lhs as IntegerVal).Value >= (rhs as IntegerVal).Value);

                    break;
                }
                case Values.ValueType.float_:
                {
                    if (operator_ == EqualityCheckOp.LessThan)
                        return new BoolVal((lhs as FloatVal).Value < (rhs as FloatVal).Value);
                    else if (operator_ == EqualityCheckOp.LessThanOrEquals)
                        return new BoolVal((lhs as FloatVal).Value <= (rhs as FloatVal).Value);
                    else if (operator_ == EqualityCheckOp.MoreThan)
                        return new BoolVal((lhs as FloatVal).Value > (rhs as FloatVal).Value);
                    else if (operator_ == EqualityCheckOp.MoreThanOrEquals)
                        return new BoolVal((lhs as FloatVal).Value >= (rhs as FloatVal).Value);

                    break;
                }
                case Values.ValueType.double_:
                {
                    if (operator_ == EqualityCheckOp.LessThan)
                        return new BoolVal((lhs as DoubleVal).Value < (rhs as DoubleVal).Value);
                    else if (operator_ == EqualityCheckOp.LessThanOrEquals)
                        return new BoolVal((lhs as DoubleVal).Value <= (rhs as DoubleVal).Value);
                    else if (operator_ == EqualityCheckOp.MoreThan)
                        return new BoolVal((lhs as DoubleVal).Value > (rhs as DoubleVal).Value);
                    else if (operator_ == EqualityCheckOp.MoreThanOrEquals)
                        return new BoolVal((lhs as DoubleVal).Value >= (rhs as DoubleVal).Value);

                    break;
                }
            }

            return new BoolVal(false);
        }

        /// <summary>
        /// Handles member expressions for types.
        /// Like `object.property` for example.
        /// </summary>
        /// <returns>The result of the expression.</returns>
        public static RuntimeVal MemberOf(MemberExpr node, Environment env)
        {
            /// <summary>
            /// Handles member expressions on type identifiers.
            /// Like `int.max` for example.
            /// </summary>
            RuntimeVal? MemberOfType(RuntimeVal obj)
            {
                var type = FromString((node.Object as Identifier)!.Symbol);

                if (type == Type.Int() && node.Property.Kind == NodeType.Identifier)
                {
                    var ident = (node.Property as Identifier)!.Symbol;

                    if (ident == "max")
                    {
                        return new IntegerVal(int.MaxValue);
                    } else if (ident == "min")
                    {
                        return new IntegerVal(int.MinValue);
                    }
                }

                return null;
            }

            /// <summary>
            /// Handles member expressions on variables values.
            /// Like `myVar.prop` for example.
            /// </summary>
            RuntimeVal? MemberOfValue(RuntimeVal obj)
            {
                switch (obj.Type)
                {
                    case Values.ValueType.objectInstance:
                    {
                        if (node.Property.Kind != NodeType.Identifier) break;

                        var p = (obj as ObjectInstanceVal)!.Properties.FirstOrDefault((prop) => prop.Key == (node.Property as Identifier)!.Symbol);

                        if (p == null)
                            throw new RuntimeException($"Property does not exist in object.");

                        return p?.Value ?? new NullVal();
                    }
                    case Values.ValueType.enum_:
                    {
                        if (node.Property.Kind != NodeType.Identifier) break;

                        var p = (obj as EnumVal)!.Entries.FirstOrDefault((prop) => prop.Key == (node.Property as Identifier)!.Symbol);

                        if (p.Key == null)
                            throw new RuntimeException($"Entry does not exist in enum.");

                        return new IntegerVal(p.Value);
                    }
                }

                return null;
            }

            var obj = Interpreter.Evaluate(node.Object, env);
            RuntimeVal? value = null;

            if (node.Object.Kind == NodeType.Identifier)
            {
                value = MemberOfType(obj);
            }
            
            if (value == null) 
            {
                value = MemberOfValue(obj);
            }

            if (value != null) return value;

            throw new RuntimeException($"This declaration type does not support member expressions.");
        }

        #endregion
        #region Methods To Help With Types

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
                return ((FunctionVal)runtimeVal).Type_ ;
            else if (runtimeVal != null && valueType == Values.ValueType.nativeFn)
                return ((NativeFnVal)runtimeVal).Type_ ;

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
                    Type? type = null; // TODO: Array
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
                    return Type.Void() ;
                }
                case Values.ValueType.boolean:
                {
                    return Type.Bool() ;
                }
                case Values.ValueType.integer:
                {
                    return Type.Int() ;
                }
                case Values.ValueType.float_:
                {
                    return Type.Float();
                }
                case Values.ValueType.double_:
                {
                    return Type.Double();
                }
                case Values.ValueType.string_:
                {
                    return Type.String() ;
                }
                case Values.ValueType.char_:
                {
                    return Type.Char() ;
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

        #endregion

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
            public static Type Float(Type[]? lambdaTypes = null) => new(6, lambdaTypes);
            public static Type Double(Type[]? lambdaTypes = null) => new(7, lambdaTypes);
            public static Type String(Type[]? lambdaTypes = null) => new(8, lambdaTypes);
            public static Type Char(Type[]? lambdaTypes = null) => new(9, lambdaTypes);

            public int CompareTo(Type? other)
            {
                return -1;
            }

            // Explicitly overload the == operator
            public static bool operator ==(Type? left, Type? right)
            {
                if (ReferenceEquals(left, null))
                    return ReferenceEquals(right, null);

                return left.Equals(right);
            }

            // Explicitly overload the != operator
            public static bool operator !=(Type? left, Type? right)
            {
                return !(left == right);
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
    }
}
