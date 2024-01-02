using JScr.Frontend;
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

        public static RuntimeVal EvalBinaryExpr(BinaryExpr binop, Environment env)
        {
            var lhs = Interpreter.Evaluate(binop.Left, env);
            var rhs = Interpreter.Evaluate(binop.Right, env);

            if (lhs.Type == ValueType.integer && rhs.Type == ValueType.integer)
            {
                return EvalNumericBinaryExpr(lhs as IntegerVal, rhs as IntegerVal, binop.Operator);
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

        public static RuntimeVal EvalObjectExpr(ObjectLiteral obj, Environment env)
        {
            var object_ = new ObjectVal(new());
            foreach (var kvp in obj.Properties)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                var runtimeVal = (value == null) ? env.LookupVar(key) : Interpreter.Evaluate(value, env);
                object_.Properties[key] = runtimeVal;
            }
            return object_;
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

                // Create the variables for the parameters list
                for (var i = 0; i < func.Parameters.Length; i++)
                {
                    // TODO: Check the bounce here
                    // verify arity of function
                    var variable = func.Parameters[i];
                    scope.DeclareVar(variable.Identifier, args[i], variable.Constant, variable.Type);

                    /*
                     * // TODO: Check the bounce here
                    // verify arity of function
                    var varname = func.Parameters[i];
                    scope.DeclareVar(varname, args[i], false);
                    */

                    //scope.DeclareVar(variable.Identifier, instance ?? new NullVal(), false, variable.Type);
                }

                var result = new NullVal() as RuntimeVal;
                // Evaluate the function body statement by statement.
                foreach (var stmt in func.Body)
                {
                    if (stmt is ReturnDeclaration)
                    {
                        result = Interpreter.Evaluate(stmt, scope);
                        break;
                    }

                    Interpreter.Evaluate(stmt, scope);
                }

                return result;
            }

            throw new RuntimeException($"Cannot call value that is not a function: {fn.ToJson()}");
        }
    }
}
