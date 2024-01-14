using JScr.Frontend;
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
        private static IntegerVal EvalNumericBinaryExpr(IntegerVal lhs, IntegerVal rhs, string operator_)
        {
            var result = 0;
            if (operator_ == "+")
                result = lhs.Value + rhs.Value;
            else if (operator_ == "-")
                result = lhs.Value - rhs.Value;
            else if (operator_ == "*")
                result = lhs.Value * rhs.Value;
            else if (operator_ == "/")
                // TODO: Division by zero checks
                result = lhs.Value / rhs.Value;
            else
                result = lhs.Value % rhs.Value;

            return new IntegerVal(result);
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
                return EvalNumericBinaryExpr(lhs as IntegerVal, rhs as IntegerVal, binop.Operator);
            } else if (lhs.Type == ValueType.string_ && rhs.Type == ValueType.string_)
            {
                return EvalStringBinaryExpr(lhs as StringVal, rhs as StringVal, binop.Operator);
            }

            // One or both are NULL
            return new NullVal();
        }

        public static RuntimeVal EvalIdentifier(Identifier ident, Environment env)
        {
            var val = env.LookupVar(ident.Symbol);
            return val;
        }

        public static RuntimeVal EvalAssignment(AssignmentExpr node, Environment env)
        {
            if (node.Assigne.Kind != NodeType.Identifier)
                throw new RuntimeException($"Invalid LMS inside assignment expr: {node.Assigne.ToJson()}");

            var varname = (node.Assigne as Identifier).Symbol;
            return env.AssignVar(varname, Interpreter.Evaluate(node.Value, env));
        }

        // TODO: Allow each type to declare their own ways to compare other types and stuff. (= cleaner code).
        public static RuntimeVal EvalEqualityCheckExpr(EqualityCheckExpr node, Environment env)
        {
            var lhs = Interpreter.Evaluate(node.Left, env);
            var rhs = Interpreter.Evaluate(node.Right, env);

            switch (node.Operator)
            {
                case EqualityCheckExpr.Type.Equals:
                    return new BoolVal(lhs.Equals(rhs));
                case EqualityCheckExpr.Type.NotEquals:
                    return new BoolVal(!lhs.Equals(rhs));
                case EqualityCheckExpr.Type.LessThan:
                    return new BoolVal((lhs as IntegerVal).Value < (rhs as IntegerVal).Value);
                case EqualityCheckExpr.Type.LessThanOrEquals:
                    return new BoolVal((lhs as IntegerVal).Value <= (rhs as IntegerVal).Value);
                case EqualityCheckExpr.Type.MoreThan:
                    return new BoolVal((lhs as IntegerVal).Value > (rhs as IntegerVal).Value);
                case EqualityCheckExpr.Type.MoreThanOrEquals:
                    return new BoolVal((lhs as IntegerVal).Value >= (rhs as IntegerVal).Value);

                case EqualityCheckExpr.Type.And:
                {
                    if (lhs.Type == ValueType.boolean && rhs.Type == ValueType.boolean)
                        return new BoolVal((lhs as BoolVal).Value && (rhs as BoolVal).Value);
                    return new BoolVal(false);
                }
                case EqualityCheckExpr.Type.Or:
                {
                    if (lhs.Type == ValueType.boolean || rhs.Type == ValueType.boolean)
                        return new BoolVal((lhs as BoolVal)!.Value || (rhs as BoolVal)!.Value);
                    return new BoolVal(false);
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
            var obj = Interpreter.Evaluate(node.Object, env);

            if (obj.Type == ValueType.objectInstance && node.Property.Kind == NodeType.Identifier)
            {
                var p = (obj as ObjectInstanceVal)!.Properties.FirstOrDefault((prop) => prop.Key == (node.Property as Identifier)!.Symbol);

                if (p == null)
                    throw new RuntimeException($"Property does not exist in object.");

                return p?.Value ?? new NullVal();
            } else if (node.Object.Kind == NodeType.Identifier)
            {
                var iaEnv = env.LookupImportAlias((node.Object as Identifier)!.Symbol);
                if (iaEnv != null)
                {
                    Interpreter.Evaluate(node.Property, iaEnv);
                }
            }

            throw new RuntimeException($"This declaration type does not support member expressions.");
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

        // TODO: LambdaFn lookup variable or function return type to pass it to the callexpr
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
                        anonymousParams.Add(new(true, false, currType, func.Parameters[i].Identifier, null));
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
                    scope.DeclareVar(variable.Identifier, currentArgValue.Type == ValueType.null_ ? defaultVal : currentArgValue, variable.Constant, false, variable.Type);
                }

                var result = new NullVal() as RuntimeVal;
                // Evaluate the function body statement by statement.
                foreach (var stmt in func.Body)
                {
                    if (stmt is ReturnDeclaration || func.InstantReturn)
                    {
                        result = Interpreter.Evaluate(stmt, scope);
                        break;
                    }

                    Interpreter.Evaluate(stmt, scope);
                }

                // Check type, just for fun
                if (result.Type != ValueType.null_ && func.Type_ != Types.Type.Void())
                {
                    if (Types.SuitableType(result) != func.Type_)
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
                parameters.Add(new(false, false, null, item.Symbol, null));
            }

            return new FunctionVal(null, null, parameters.ToArray(), env, expr.Body, expr.InstantReturn);
        }
    }
}
