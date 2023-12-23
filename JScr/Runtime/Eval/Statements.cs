using static JScr.Frontend.Ast;
using static JScr.Runtime.Values;

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
    }
}
