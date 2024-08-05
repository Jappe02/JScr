using JScr.Frontend;
using JScr.Transpiler;
using JScr.Typing.TypeChecking.Check;
using static JScr.Frontend.Ast;

namespace JScr.Typing.TypeChecking
{
    internal class TypeChecker
    {
        public static void CheckProgramTypes(Program program, Action<SyntaxError> errorCallback)
        {
            Environment env = new(null, errorCallback: errorCallback, filedir: program.FileDir);

            Statements.CheckProgram(program, env);
        }

        public static RuntimeVal CheckTypes(Stmt astNode, Environment env)
        {
            switch (astNode.Kind)
            {
                // Handle expressions
                case NodeType.NumericLiteral:
                    return /*new TPString((astNode as NumericLiteral).Value.ToString())*/ new NullValue();
                case NodeType.FloatLiteral:
                    return /*new TPString((astNode as FloatLiteral).Value.ToString())*/ new NullValue();
                case NodeType.DoubleLiteral:
                    return /*new TPString((astNode as DoubleLiteral).Value.ToString())*/ new NullValue();
                case NodeType.StringLiteral:
                    return /*new TPString("\"" + (astNode as StringLiteral).Value.ToString() + "\"")*/ new NullValue();
                case NodeType.CharLiteral:
                    return /*new TPString("\'" + (astNode as CharLiteral).Value.ToString() + "\'")*/ new NullValue();
                case NodeType.Identifier:
                    return Expressions.CheckIdentifier(astNode as Identifier, env);
                case NodeType.MemberExpr:
                    return Expressions.CheckMemberExpr(astNode as MemberExpr, env);
                case NodeType.UnaryExpr:
                    return Expressions.CheckUnaryExpr(astNode as UnaryExpr, env);
                case NodeType.ObjectConstructorExpr:
                    return Expressions.CheckObjectConstructorExpr(astNode as ObjectConstructorExpr, env);
                case NodeType.ArrayLiteral:
                    return Expressions.CheckArrayExpr(astNode as ArrayLiteral, env);
                case NodeType.CallExpr:
                    return Expressions.CheckCallExpr(astNode as CallExpr, env);
                case NodeType.IndexExpr:
                    return Expressions.CheckIndexExpr(astNode as IndexExpr, env);
                case NodeType.LambdaExpr:
                    return Expressions.CheckLambdaExpr(astNode as LambdaExpr, env);
                case NodeType.AssignmentExpr:
                    return Expressions.CheckAssignment(astNode as AssignmentExpr, env);
                case NodeType.EqualityCheckExpr:
                    return Expressions.CheckEqualityCheckExpr(astNode as EqualityCheckExpr, env);
                case NodeType.BinaryExpr:
                    return Expressions.CheckBinaryExpr(astNode as BinaryExpr, env);
                /*case NodeType.Program:
                    return Statements.TPProgram(astNode as Program);*/

                // Handle statements
                case NodeType.ImportStmt:
                    return Statements.CheckImportStmt(astNode as ImportStmt, env);
                case NodeType.VarDeclaration:
                    return Statements.CheckVarDeclaration(astNode as VarDeclaration, env);
                case NodeType.FunctionDeclaration:
                    return Statements.CheckFunctionDeclaration(astNode as FunctionDeclaration, env);
                case NodeType.StructDeclaration:
                    return Statements.CheckObjectDeclaration(astNode as StructDeclaration, env);
                case NodeType.ClassDeclaration:
                    return Statements.CheckClassDeclaration(astNode as ClassDeclaration, env);
                case NodeType.EnumDeclaration:
                    return Statements.CheckEnumDeclaration(astNode as EnumDeclaration, env);
                case NodeType.ReturnDeclaration:
                    return Statements.CheckReturnDeclaration(astNode as ReturnDeclaration, env);
                case NodeType.DeleteDeclaration:
                    return Statements.CheckDeleteDeclaration(astNode as DeleteDeclaration, env);
                case NodeType.IfElseDeclaration:
                    return Statements.CheckIfElseDeclaration(astNode as IfElseDeclaration, env);
                case NodeType.WhileDeclaration:
                    return Statements.CheckWhileDeclaration(astNode as WhileDeclaration, env);
                case NodeType.ForDeclaration:
                    return Statements.CheckForDeclaration(astNode as ForDeclaration, env);

                // Handle unimplemented ast types as error
                default:
                    throw new Exception($"This type-checker does not support the `{astNode}` AST node.");
            }
        }
    }
}
