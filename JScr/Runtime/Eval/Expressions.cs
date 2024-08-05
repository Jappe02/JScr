using JScr.Frontend;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Security;
using System.Xml.Linq;
using static JScr.Frontend.Ast;
using static JScr.Runtime.Values;
using ValueType = JScr.Runtime.Values.ValueType;

namespace JScr.Runtime.Eval
{
    internal static class Expressions
    {
        private static double EvalNumericBinaryExpr(double lhs, double rhs, string operator_)
        {
            var result = 0d;
            if (operator_ == "+")
                result = lhs + rhs;
            else if (operator_ == "-")
                result = lhs - rhs;
            else if (operator_ == "*")
                result = lhs * rhs;
            else if (operator_ == "/")
                // TODO: Division by zero checks
                result = lhs / rhs;
            else
                result = lhs % rhs;

            return result;
        }

        private static StringVal EvalStringBinaryExpr(StringVal lhs, StringVal rhs, string operator_)
        {
            var result = "";
            if (operator_ == "+")
                result = lhs.Value + rhs.Value;

            return new StringVal(result);
        }

        public static RuntimeVal EvalBinaryExpr(BinaryExpr binop, Environment env)
        {
            var lhs = Interpreter.Evaluate(binop.Left, env);
            var rhs = Interpreter.Evaluate(binop.Right, env);

            if (lhs.Type == ValueType.integer && rhs.Type == ValueType.integer)
            {
                return new IntegerVal(Convert.ToInt32(EvalNumericBinaryExpr((lhs as IntegerVal)!.Value, (rhs as IntegerVal)!.Value, binop.Operator)));
            } else if (lhs.Type == ValueType.float_ || lhs.Type == ValueType.float_)
            {
                return new FloatVal((float)EvalNumericBinaryExpr((lhs as FloatVal)!.Value, (rhs as FloatVal)!.Value, binop.Operator));
            } else if (lhs.Type == ValueType.double_ || lhs.Type == ValueType.double_)
            {
                return new DoubleVal(EvalNumericBinaryExpr((lhs as DoubleVal)!.Value, (rhs as DoubleVal)!.Value, binop.Operator));
            } else if (lhs.Type == ValueType.string_ && rhs.Type == ValueType.string_)
            {
                return EvalStringBinaryExpr(lhs as StringVal, rhs as StringVal, binop.Operator);
            }

            // One or both are NULL
            return new NullVal();
        }

        // Lookup variables & static access to objects + enums.
        public static RuntimeVal EvalIdentifier(Identifier ident, Environment env)
        {
            var variable = env.LookupVar(ident.Symbol, true);

            if (variable != null)
                return variable;

            var enum_ = env.LookupEnum(ident.Symbol);

            if (enum_ != null)
                return enum_;

            var object_ = env.LookupObject(ident.Symbol);

            if (object_ != null)
                return object_;

            throw new RuntimeException($"The identifier \"{ident.Symbol}\" does not reference anything.");
        }

        public static RuntimeVal EvalAssignment(AssignmentExpr node, Environment env)
        {
            if (node.Assigne.Kind != NodeType.Identifier)
                throw new RuntimeException($"Invalid LMS inside assignment expr: {node.Assigne.ToJson()}");

            var varname = (node.Assigne as Identifier)!.Symbol;
            return env.AssignVar(varname, Interpreter.Evaluate(node.Value, env));
        }

        public static RuntimeVal EvalEqualityCheckExpr(EqualityCheckExpr node, Environment env)
        {
            var lhs = Interpreter.Evaluate(node.Left, env);
            var rhs = Interpreter.Evaluate(node.Right, env);

            switch (node.Operator)
            {
                case EqualityCheckExpr.Type.Equals:
                    return Types.Compare(lhs, rhs, Types.EqualityCheckOp.Equals);
                case EqualityCheckExpr.Type.NotEquals:
                    return Types.Compare(lhs, rhs, Types.EqualityCheckOp.NotEquals);
                case EqualityCheckExpr.Type.LessThan:
                    return Types.Compare(lhs, rhs, Types.EqualityCheckOp.LessThan);
                case EqualityCheckExpr.Type.LessThanOrEquals:
                    return Types.Compare(lhs, rhs, Types.EqualityCheckOp.LessThanOrEquals);
                case EqualityCheckExpr.Type.MoreThan:
                    return Types.Compare(lhs, rhs, Types.EqualityCheckOp.MoreThan);
                case EqualityCheckExpr.Type.MoreThanOrEquals:
                    return Types.Compare(lhs, rhs, Types.EqualityCheckOp.MoreThanOrEquals);

                case EqualityCheckExpr.Type.And:
                {
                    if (lhs.Type == ValueType.boolean && rhs.Type == ValueType.boolean)
                        return new BoolVal((lhs as BoolVal)!.Value && (rhs as BoolVal)!.Value);
                    throw new RuntimeException("The `and` operator can only be used within booleans.");
                }
                case EqualityCheckExpr.Type.Or:
                {
                    if (lhs.Type == ValueType.boolean || rhs.Type == ValueType.boolean)
                        return new BoolVal((lhs as BoolVal)!.Value || (rhs as BoolVal)!.Value);
                    throw new RuntimeException("The `or` operator can only be used within booleans.");
                }
            }

            return new BoolVal(false);
        }

        public static RuntimeVal EvalArrayExpr(ArrayLiteral obj, Environment env)
        {
            var elements = new List<RuntimeVal>();
            foreach (var item in obj.Value)
            {
                var runtimeVal = (item == null) ? new NullVal() : Interpreter.Evaluate(item, env);
                elements.Add(runtimeVal);
            }
            return new ArrayVal(elements.ToArray());
        }

        public static RuntimeVal EvalMemberExpr(MemberExpr node, Environment env)
        {
            return Types.MemberOf(node, env);
        }

        private static double EvalNumericUnaryExpr(double obj, string operator_)
        {
            var result = 0d;
            if (operator_ == "+")
                result = +obj;
            else
                result = -obj;

            return result;
        }

        public static RuntimeVal EvalUnaryExpr(UnaryExpr node, Environment env)
        {
            var obj = Interpreter.Evaluate(node.Object, env);

            if (obj.Type == ValueType.integer)
            {
                return new IntegerVal(Convert.ToInt32(EvalNumericUnaryExpr((obj as IntegerVal)!.Value, node.Operator)));
            } else if (obj.Type == ValueType.float_)
            {
                return new FloatVal((float)EvalNumericUnaryExpr((obj as FloatVal)!.Value, node.Operator));
            } else if (obj.Type == ValueType.double_)
            {
                return new DoubleVal(EvalNumericUnaryExpr((obj as DoubleVal)!.Value, node.Operator));
            }

            return new NullVal();
        }

        public static RuntimeVal EvalObjectConstructorExpr(ObjectConstructorExpr node, Environment env)
        {
            Types.Type type = node.TargetVarIdentAsType ? (node.TargetVariableIdent as Types.Type)! : env.LookupVarType((node.TargetVariableIdent as Identifier)!.Symbol);

            if (type.Data == null)
                throw new RuntimeException("Invalid object type.");

            var refr = env.LookupObject(type.Data);
            var newProps = new List<ObjectVal.Property>();

            for (int i = 0; i < refr.Properties.Count; i++)
            {
                var item = refr.Properties[i];
                var constrexprItem = node.Properties.ElementAtOrDefault(i)?.Value != null ? Interpreter.Evaluate(node.Properties.ElementAtOrDefault(i)!.Value!, env) : null;
                newProps.Add(new ObjectVal.Property(item.Key, item.Type, constrexprItem ?? item.Value));
            }

            return new ObjectInstanceVal(type.Data, newProps);
        }

        public static RuntimeVal EvalCallExpr(CallExpr expr, Environment env)
        {
            var args = expr.Args.Select(arg => Interpreter.Evaluate(arg, env)).ToList();
            var fn = Interpreter.Evaluate(expr.Caller, env);

            if (fn.Type == ValueType.nativeFn)
            {
                var result = (fn as NativeFnVal).Call(args.ToArray(), env);
                return result;
            } else if (fn.Type == ValueType.function)
            {
                var func = fn as FunctionVal; // <-- target function to call
                var scope = new Environment(func.DeclarationEnv);
                var isAnonymous = func.Name == null;
                var anonymousParams = new List<VarDeclaration>();

                // If the function is anonymous, handle its parameters.
                if (isAnonymous)
                {
                    for (int i = 0; i < func.Parameters.Length; i++)
                    {
                        var currType = Types.SuitableType(args.ElementAtOrDefault(i));
                        anonymousParams.Add(new(Array.Empty<AnnotationUsageDeclaration>(), true, Visibility.Private, currType, func.Parameters[i].Identifier, null));
                    }
                }
                
                // Create the variables for the parameters list
                for (var i = 0; i < func.Parameters.Length; i++)
                {
                    // TODO: Check the bounce here
                    // verify arity of function
                    var variable = !isAnonymous ? func.Parameters[i] : anonymousParams[i];
                    var defaultVal = variable.Value != null ? Interpreter.Evaluate(variable.Value, scope) : new NullVal();
                    var currentArgValue = args.ElementAtOrDefault(i) ?? new NullVal();
                    scope.DeclareVar(variable.Identifier, currentArgValue.Type == ValueType.null_ ? defaultVal : currentArgValue, variable.Constant, Visibility.Private, variable.Type);
                }

                RuntimeVal? result = null;
                // Evaluate the function body statement by statement.
                foreach (var stmt in func.Body)
                {
                    if (func.InstantReturn)
                    {
                        result = Interpreter.Evaluate(stmt, scope);
                        break;
                    }

                    try
                    {
                        Interpreter.Evaluate(stmt, scope);
                    } catch (ThReturnStmt rs)
                    {
                        result = rs.ReturnValue;
                        break;
                    }
                }

                if (func.Type_ != Types.Type.Void() && result == null) {
                    throw new RuntimeException("Not all code paths return a value.");
                }

                result ??= new NullVal();

                // Check type, just for fun
                if (result!.Type != ValueType.null_ && func.Type_ != Types.Type.Void())
                {
                    if (!Types.SuitableType(result).Equals(func.Type_))
                        throw new RuntimeException("Returned type does not match function declaration type.");
                }

                return result;
            }

            throw new RuntimeException($"Cannot call value that is not a function: {fn.ToJson()}");
        }

        public static RuntimeVal EvalIndexExpr(IndexExpr expr, Environment env)
        {
            var a = Interpreter.Evaluate(expr.Arg, env);
            var v = Interpreter.Evaluate(expr.Caller, env);

            if (a.Type != ValueType.integer)
                throw new RuntimeException("Integer expected as index.");

            if (v.Type == ValueType.array)
            {
                var array = v as ArrayVal; // <-- target array to get value of index from
                var arg = a as IntegerVal;

                return array!.Value[arg!.Value];
            }

            throw new RuntimeException($"Cannot use index expression on a non-array object: {v.ToJson()}");
        }

        public static RuntimeVal EvalLambdaExpr(LambdaExpr expr, Environment env)
        {
            var parameters = new List<VarDeclaration>();

            foreach (var item in expr.ParamIdents)
            {
                parameters.Add(new(Array.Empty<AnnotationUsageDeclaration>(), false, Visibility.Private, null, item.Symbol, null));
            }

            return new FunctionVal(null, null, parameters.ToArray(), env, expr.Body, expr.IsExpressionLambda);
        }
    }
}
