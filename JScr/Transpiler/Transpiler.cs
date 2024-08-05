using JScr.Frontend;
using JScr.Transpiler.Transpile;
using static JScr.Frontend.Ast;

namespace JScr.Transpiler
{
    internal class Transpiler
    {
        public static string TranspileProgram(Program program, JScrExternalResource[] resources, Action<SyntaxError> errorCallback)
        {
            Environment env = new(null, errorCallback: errorCallback, filedir: program.FileDir);

            return string.Join("\n", env.top) + "\n" + Statements.TPProgram(program, env).Val;
        }

        public static TPString Transpile(Stmt astNode, Environment env)
        {
            switch (astNode.Kind)
            {
                case NodeType.NumericLiteral:
                    return new TPString((astNode as NumericLiteral).Value.ToString());
                case NodeType.FloatLiteral:
                    return new TPString((astNode as FloatLiteral).Value.ToString());
                case NodeType.DoubleLiteral:
                    return new TPString((astNode as DoubleLiteral).Value.ToString());
                case NodeType.StringLiteral:
                    return new TPString("\"" + (astNode as StringLiteral).Value.ToString() + "\"");
                case NodeType.CharLiteral:
                    return new TPString("\'" + (astNode as CharLiteral).Value.ToString() + "\'");
                case NodeType.Identifier:
                    return Expressions.TPIdentifier(astNode as Identifier, env);
                case NodeType.MemberExpr:
                    return Expressions.TPMemberExpr(astNode as MemberExpr, env);
                case NodeType.UnaryExpr:
                    return Expressions.TPUnaryExpr(astNode as UnaryExpr, env);
                case NodeType.ObjectConstructorExpr:
                    return Expressions.TPObjectConstructorExpr(astNode as ObjectConstructorExpr, env);
                case NodeType.ArrayLiteral:
                    return Expressions.TPArrayExpr(astNode as ArrayLiteral, env);
                case NodeType.CallExpr:
                    return Expressions.TPCallExpr(astNode as CallExpr, env);
                case NodeType.IndexExpr:
                    return Expressions.TPIndexExpr(astNode as IndexExpr, env);
                case NodeType.LambdaExpr:
                    return Expressions.TPLambdaExpr(astNode as LambdaExpr, env);
                case NodeType.AssignmentExpr:
                    return Expressions.TPAssignment(astNode as AssignmentExpr, env);
                case NodeType.EqualityCheckExpr:
                    return Expressions.TPEqualityCheckExpr(astNode as EqualityCheckExpr, env);
                case NodeType.BinaryExpr:
                    return Expressions.TPBinaryExpr(astNode as BinaryExpr, env);
                /*case NodeType.Program:
                    return Statements.TPProgram(astNode as Program);*/

                // Handle statements
                case NodeType.ImportStmt:
                    return Statements.TPImportStmt(astNode as ImportStmt, env);
                case NodeType.VarDeclaration:
                    return Statements.TPVarDeclaration(astNode as VarDeclaration, env);
                case NodeType.FunctionDeclaration:
                    return Statements.TPFunctionDeclaration(astNode as FunctionDeclaration, env, out _);
                case NodeType.StructDeclaration:
                    return Statements.TPObjectDeclaration(astNode as StructDeclaration, env);
                case NodeType.ClassDeclaration:
                    return Statements.TPClassDeclaration(astNode as ClassDeclaration, env);
                case NodeType.EnumDeclaration:
                    return Statements.TPEnumDeclaration(astNode as EnumDeclaration, env);
                case NodeType.ReturnDeclaration:
                    return Statements.TPReturnDeclaration(astNode as ReturnDeclaration, env);
                case NodeType.DeleteDeclaration:
                    return Statements.TPDeleteDeclaration(astNode as DeleteDeclaration, env);
                case NodeType.IfElseDeclaration:
                    return Statements.TPIfElseDeclaration(astNode as IfElseDeclaration, env);
                case NodeType.WhileDeclaration:
                    return Statements.TPWhileDeclaration(astNode as WhileDeclaration, env);
                case NodeType.ForDeclaration:
                    return Statements.TPForDeclaration(astNode as ForDeclaration, env);

                // Handle unimplemented ast types as error
                default:
                    throw new Exception($"This AST Node has not yet been setup for transpiling: {astNode}");
            }
        }
    }
}
