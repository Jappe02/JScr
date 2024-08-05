using static JScr.Frontend.Ast;

namespace JScr.Typing.TypeChecking.Check
{
    internal static class Statements
    {
        public static RuntimeVal? CheckProgram(Program program, Environment env)
        {
            RuntimeVal? lastEvaluated = null;
            foreach (var statement in program.Body)
            {
                lastEvaluated = TypeChecker.CheckTypes(statement, env);
            }
            return lastEvaluated;
        }

        public static RuntimeVal CheckImportStmt(ImportStmt declaration, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckVarDeclaration(VarDeclaration declaration, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckFunctionDeclaration(FunctionDeclaration declaration, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckObjectDeclaration(StructDeclaration obj, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckClassDeclaration(ClassDeclaration obj, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckEnumDeclaration(EnumDeclaration obj, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckReturnDeclaration(ReturnDeclaration declaration, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckDeleteDeclaration(DeleteDeclaration declaration, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckIfElseDeclaration(IfElseDeclaration declaration, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckWhileDeclaration(WhileDeclaration declaration, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckForDeclaration(ForDeclaration declaration, Environment env)
        {
            return new NullValue();
        }
    }
}
