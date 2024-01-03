using JScr.Frontend;
using System;
using static JScr.Frontend.Ast;
using static JScr.Runtime.Values;
using static JScr.Script;

namespace JScr.Runtime.Eval
{
    internal static class Statements
    {
        public static RuntimeVal EvalProgram(Program program, Environment env, ref bool running)
        {
            RuntimeVal lastEvaluated = new NullVal();
            foreach (var statement in program.Body)
            {
                if (!running) return new NullVal();

                lastEvaluated = Interpreter.Evaluate(statement, env);
            }
            return lastEvaluated;
        }

        public static RuntimeVal EvalVarDeclaration(VarDeclaration declaration, Environment env)
        {
            var value = declaration.Value != null ? Interpreter.Evaluate(declaration.Value, env) : new NullVal();
            return env.DeclareVar(declaration.Identifier, value, declaration.Constant, declaration.Type);
        }

        public static RuntimeVal EvalFunctionDeclaration(FunctionDeclaration declaration, Environment env)
        {
            var fn = new FunctionVal(declaration.Name, declaration.Type, declaration.Parameters, env, declaration.Body);

            return env.DeclareVar(declaration.Name, fn, true, declaration.Type);
        }

        public static RuntimeVal EvalReturnDeclaration(ReturnDeclaration declaration, Environment env)
        {
            var value = declaration.Value != null ? Interpreter.Evaluate(declaration.Value, env) : new NullVal();

            return value;
        }

        public static RuntimeVal EvalIfElseDeclaration(IfElseDeclaration declaration, Environment env)
        {
            foreach (var block in declaration.Blocks) {
                var val = Interpreter.Evaluate(block.Condition, env);

                // Verify valid bool
                if (val.Type != Values.ValueType.boolean)
                    throw new RuntimeException("If statement condition needs to be a boolean.");

                // Continue to next iteration if the value is false
                if (!(val as BoolVal).Value) continue;

                // If the value is true, execute statement body and return
                var scope = new Environment(env);
                foreach (var stmt in block.Body)
                {
                    Interpreter.Evaluate(stmt, scope);
                }

                return new NullVal();
            }

            // Execute else statement if there is one
            if (declaration.ElseBody != null)
            {
                var scope = new Environment(env);
                foreach (var stmt in declaration.ElseBody)
                {
                    Interpreter.Evaluate(stmt, scope);
                }
            }

            return new NullVal();
        }
    }
}
