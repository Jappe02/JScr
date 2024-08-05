using JScr.Frontend;
using System;
using System.Xml.Linq;
using static JScr.Frontend.Ast;
using static JScr.Runtime.Values;
using static JScr.Script;

namespace JScr.Runtime.Eval
{
    internal static class Statements
    {
        public static RuntimeVal EvalProgram(Program program, Environment env)
        {
            RuntimeVal lastEvaluated = new NullVal();
            foreach (var statement in program.Body)
            {
                try
                {
                    lastEvaluated = Interpreter.Evaluate(statement, env);
                } catch (IStatementThrowable)
                {
                    throw new RuntimeException("Cannot use (some of | any of) return, continue, or break keywords in this scope.");
                }
                
            }
            return lastEvaluated;
        }

        public static RuntimeVal EvalImportStmt(ImportStmt declaration, Environment env)
        {
            if (env.parent != null)
                throw new RuntimeException("Import statements need to be declared in the global scope.");

            void FileLoadError(string otherFileDir, SyntaxException ex)
            {
                throw new RuntimeException($"Failed to import script file because of syntax error. From: {otherFileDir}. Syntax Error: ({ex.Error}).");
            }

            Program program;
            string otherFileDir;
            if (env.externalResources.FirstOrDefault((item) => item is JScrExternalResourceFile && (item as JScrExternalResourceFile).location.SequenceEqual(declaration.Target)) is JScrExternalResourceFile externalFile)
            {
                otherFileDir = Path.Combine("<external>/", string.Join("/", declaration.Target) + ".jscr");
                try
                {
                    var parser = new Parser();

                    var data = externalFile.internalFile;
                    program = parser.ProduceAST(otherFileDir, data, _ => throw new Exception("Runtime parsing exception!"));
                } catch (SyntaxException e)
                {
                    FileLoadError(otherFileDir, e);
                }
            } else
            {
                otherFileDir = Path.Combine(Path.GetDirectoryName(env.FileDir) ?? "", string.Join("/", declaration.Target) + ".jscr");
                try
                {
                    var parser = new Parser();

                    var data = File.ReadAllText(otherFileDir);
                    program = parser.ProduceAST(otherFileDir, data, _ => throw new Exception("Runtime parsing exception!"));
                } catch (SyntaxException e)
                {
                    FileLoadError(otherFileDir, e);
                }
            }

            var evaluationScope = new Environment(env, ScopeType.Global, env.externalResources, true);
            env.DeclareImport(evaluationScope, otherFileDir, declaration.Alias);

            return EvalProgram(program, evaluationScope);
        }

        public static RuntimeVal EvalVarDeclaration(VarDeclaration declaration, Environment env)
        {
            var value = declaration.Value != null ? Interpreter.Evaluate(declaration.Value, env) : new NullVal();
            return env.DeclareVar(declaration.Identifier, value, declaration.Constant, declaration.Visibility, declaration.Type, declaration.AnnotatedWith);
        }

        public static RuntimeVal EvalFunctionDeclaration(FunctionDeclaration declaration, Environment env)
        {
            var fn = new FunctionVal(declaration.Name, declaration.Type, declaration.Parameters, env, declaration.Body, declaration.InstantReturn);

            return env.DeclareVar(declaration.Name, fn, true, declaration.Visibility, declaration.Type, declaration.AnnotatedWith);
        }

        public static RuntimeVal EvalObjectDeclaration(ObjectDeclaration obj, Environment env)
        {
            var props = new List<ObjectVal.Property>();
            ArrayVal? annotationTargets = null;
            foreach (var prop in obj.Properties)
            {
                var key = prop.Key;
                var type = prop.Type;
                var value = prop.Value;

                var runtimeVal = (value == null) ? new NullVal() : Interpreter.Evaluate(value, env);

                if (!Types.RuntimeValMatchesType(type, runtimeVal))
                    throw new RuntimeException("Type and value do not match in object expression.");

                if (key == "targets" && type == Types.Type.Array(Types.Type.Int()))
                {
                    annotationTargets = (ArrayVal?)runtimeVal;
                    continue;
                }

                props.Add(new ObjectVal.Property(key, type, runtimeVal));
            }
            return env.DeclareObject(new ObjectVal(obj.Visibility, obj.Name, props, annotationTargets), obj.AnnotatedWith);
        }

        public static RuntimeVal EvalEnumDeclaration(EnumDeclaration obj, Environment env)
        {
            var entries = new Dictionary<string, ushort>();
            for (ushort i = 0; i < obj.Entries.Length; i++)
            {
                entries.Add(obj.Entries[i], i);
            }
            return env.DeclareEnum(new EnumVal(obj.Visibility, obj.Name, entries), obj.AnnotatedWith);
        }

        public static RuntimeVal EvalClassDeclaration(ClassDeclaration obj, Environment env)
        {
            //var scope = new Environment(env, ScopeType.Class);
            //foreach (var stmt in obj.Body)
            //{
            //    Interpreter.Evaluate(stmt, scope);
            //}

            var derivants = new List<ClassVal>();
            foreach (var d in obj.Derivants)
            {
                env.LookupClass(obj.Name);
            }

            return env.DeclareClass(new ClassVal(obj.Visibility, obj.Name, derivants.ToArray(), obj.Body), obj.AnnotatedWith);
        }

        public static RuntimeVal EvalReturnDeclaration(ReturnDeclaration declaration, Environment env)
        {
            var value = declaration.Value != null ? Interpreter.Evaluate(declaration.Value, env) : new NullVal();

            throw new ThReturnStmt(value);
        }

        public static RuntimeVal EvalDeleteDeclaration(DeleteDeclaration declaration, Environment env)
        {
            var value = env.AssignVar(declaration.Value, new NullVal());

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
                if (!(val as BoolVal)!.Value) continue;

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

            while ((val as BoolVal)!.Value)
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

            while ((condition as BoolVal)!.Value)
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
