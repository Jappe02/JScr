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
            foreach (var block in declaration.Blocks)
            {
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

        public static RuntimeVal EvalWhileDeclaration(WhileDeclaration declaration, Environment env)
        {
            var val = Interpreter.Evaluate(declaration.Condition, env);

            if (val.Type != Values.ValueType.boolean)
                throw new RuntimeException("While statement condition needs to be a boolean.");

            while ((val as BoolVal).Value)
            {
                val = Interpreter.Evaluate(declaration.Condition, env);

                var scope = new Environment(env);
                foreach (var stmt in declaration.Body)
                {
                    Interpreter.Evaluate(stmt, scope);
                }
            }

            return new NullVal();
        }

        public static RuntimeVal EvalForDeclaration(ForDeclaration declaration, Environment env)
        {
            var scope = new Environment(env);
            Interpreter.Evaluate(declaration.Declaration, scope);
            var condition = Interpreter.Evaluate(declaration.Condition, scope);

            if (condition.Type != Values.ValueType.boolean)
                throw new RuntimeException("For statement condition needs to be a boolean.");

            while ((condition as BoolVal).Value)
            {
                foreach (var stmt in declaration.Body)
                {
                    Interpreter.Evaluate(stmt, scope);
                }
                Interpreter.Evaluate(declaration.Action, scope);
                condition = Interpreter.Evaluate(declaration.Condition, scope);
            }

            return new NullVal();
        }
    }
}
