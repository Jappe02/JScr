using static JScr.Frontend.Ast;

namespace JScr.Transpiler.Transpile
{
    internal static class Statements
    {
        public static TPString TPProgram(Program program, Environment env)
        {
            TPString lastEvaluated = new();
            foreach (var statement in program.Body)
            {
                lastEvaluated = Transpiler.Transpile(statement, env);
            }
            return lastEvaluated;
        }

        public static TPString TPImportStmt(ImportStmt declaration, Environment env)
        {
            return new();
        }

        public static TPString TPVarDeclaration(VarDeclaration declaration, Environment env)
        {
            var returnVal = new TPString(" ");

            string classname = string.Empty;
            if (env is ClassEnvironment cenv)
            {
                classname = cenv.name;
            }
            else
            {
                env.errorCallback(new(env.filedir, 0, 0, "Cannot declare variable outside of a class."));
            }

            if (declaration.IsConstant)
            {
                returnVal += "const";
                returnVal.Space();
            }

            returnVal += declaration.Type.ToString();
            returnVal.Space();
            returnVal += Helpers.MemberNameFromData(new() { classname = classname, membervis = declaration.Visibility, membername = declaration.Identifier });

            if (declaration.Value != null)
            {
                returnVal += "=";
                returnVal += Transpiler.Transpile(declaration.Value, env).Val;
            }
            else
            {
                if (declaration.IsConstant)
                    env.errorCallback(new(env.filedir, 0, 0, "Cannot declare constant variable without a value."));
            }

            if (!env.NoSemicolons)
                returnVal += ";";

            return returnVal;
        }

        public static TPString TPFunctionDeclaration(FunctionDeclaration declaration, Environment env, out string declName)
        {
            var returnVal = new TPString(" ");

            Environment? p = env;
            string classname = string.Empty;
            while (p != null)
            {
                if (p is ClassEnvironment cenv)
                {
                    classname = cenv.name;
                    break;
                }

                p = p.parent;
            }

            if (classname == string.Empty)
                env.errorCallback(new(env.filedir, 0, 0, "Cannot declare function outside of a class."));

            returnVal += declaration.Type.ToString();
            returnVal.Space();
            string name = Helpers.MemberNameFromData(new() { classname = classname, membervis = declaration.Visibility, membername = declaration.Name });
            returnVal += name;

            // Params
            returnVal += "(";
            bool first = true;

            {
                first = false;
                returnVal += $"struct {classname}* this";
            }

            foreach (var stmt in declaration.Parameters)
            {
                if (!first)
                    returnVal += ",";
                returnVal += Transpiler.Transpile(stmt, env.WithNoSemicolons()).Val;
                first = false;
            }
            returnVal += ")";

            // Body
            returnVal += "{";
            var scope = new Environment(env, ScopeType.Method);
            foreach (var stmt in declaration.Body)
                returnVal += Transpiler.Transpile(stmt, scope).Val;
            returnVal += "}";

            declName = name;
            return returnVal;
        }

        public static TPString TPObjectDeclaration(StructDeclaration obj, Environment env)
        {
            return new();
        }

        public static TPString TPClassDeclaration(ClassDeclaration obj, Environment env)
        {
            var returnVal = new TPString(" ");
            var variables = new List<VarDeclaration>();
            var functions = new List<FunctionDeclaration>();

            foreach (var stmt in obj.Body)
            {
                if (stmt.Kind == NodeType.VarDeclaration)
                    variables.Add((VarDeclaration)stmt);
                else if (stmt.Kind == NodeType.FunctionDeclaration)
                    functions.Add((FunctionDeclaration)stmt);
                else
                    env.errorCallback(new(env.filedir, 0, 0, "Classes can only contain variables and functions."));
            }

            var scope = new ClassEnvironment(env, obj.Name.typename); // TODO: Generics

            returnVal += "typedef struct";
            returnVal += "{";

            foreach (var v in variables)
                returnVal += Transpiler.Transpile(v, scope);

            returnVal += "}";
            returnVal += obj.Name.typename;
            returnVal += ";";

            foreach (var f in functions)
                returnVal += Transpiler.Transpile(f, scope);

            return returnVal;
        }

        public static TPString TPEnumDeclaration(EnumDeclaration obj, Environment env)
        {
            var returnVal = new TPString("");

            returnVal += "enum";
            returnVal.Space();
            returnVal += obj.Name;
            returnVal += "{";
            foreach (var ent in obj.Entries)
                returnVal += ent + ",";
            returnVal += "}";

            return returnVal;
        }

        public static TPString TPReturnDeclaration(ReturnDeclaration declaration, Environment env)
        {
            var returnVal = new TPString("");

            returnVal += "return";
            returnVal.Space();
            returnVal += Transpiler.Transpile(declaration.Value, env).Val;
            returnVal += ";";

            return returnVal;
        }

        public static TPString TPDeleteDeclaration(DeleteDeclaration declaration, Environment env)
        {
            return new(Funcs.Free(env, declaration.Value));
        }

        public static TPString TPIfElseDeclaration(IfElseDeclaration declaration, Environment env)
        {
            var returnVal = new TPString("");

            for (int i = 0; i < declaration.Blocks.Count(); i++)
            {
                var block = declaration.Blocks[i];

                if (i > 0)
                    returnVal += "else";

                returnVal.Space();
                returnVal += "if";
                returnVal += "(";
                returnVal += Transpiler.Transpile(block.Condition, env).Val;
                returnVal += ")";
                returnVal += Transpiler.Transpile(block.Body, env).Val;
            }

            if (declaration.ElseBody != null)
            {
                returnVal += "else";
                returnVal.Space();
                returnVal += Transpiler.Transpile(declaration.ElseBody, env).Val;
            }

            return returnVal;
        }

        public static TPString TPWhileDeclaration(WhileDeclaration declaration, Environment env)
        {
            var returnVal = new TPString("");

            returnVal += "while";

            returnVal += "(";
            returnVal += Transpiler.Transpile(declaration.Condition, env).Val;
            returnVal += ")";

            returnVal += Transpiler.Transpile(declaration.Body, env).Val;

            return returnVal;
        }

        public static TPString TPForDeclaration(ForDeclaration declaration, Environment env)
        {
            var returnVal = new TPString("");

            returnVal += "for";

            returnVal += "(";
            returnVal += Transpiler.Transpile(declaration.Declaration, env.WithNoSemicolons()).Val;
            returnVal += ";";
            returnVal += Transpiler.Transpile(declaration.Condition, env).Val;
            returnVal += ";";
            returnVal += Transpiler.Transpile(declaration.Action, env).Val;
            returnVal += ")";
            returnVal += Transpiler.Transpile(declaration.Body, env).Val;

            return returnVal;
        }
    }
}
