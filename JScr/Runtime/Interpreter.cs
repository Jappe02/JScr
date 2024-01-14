using JScr.Frontend;
using JScr.Runtime.Eval;
using static JScr.Frontend.Ast;
using static JScr.Runtime.Values;

namespace JScr.Runtime
{
    internal static class Interpreter
    {
        public static RuntimeVal EvaluateProgram(Program program, JScrExternalResource[] resources)
        {
            var env = Environment.CreateGlobalEnv(program.FileDir, resources);

            return Statements.EvalProgram(program, env);
        }

        public static RuntimeVal Evaluate(Stmt astNode, Environment env)
        {
            switch (astNode.Kind) {
                case NodeType.NumericLiteral:
                    return new IntegerVal((astNode as NumericLiteral).Value);
                case NodeType.StringLiteral:
                    return new StringVal((astNode as StringLiteral).Value);
                case NodeType.CharLiteral:
                    return new CharVal((astNode as CharLiteral).Value);
                case NodeType.Identifier:
                    return Expressions.EvalIdentifier(astNode as Identifier, env);
                case NodeType.MemberExpr:
                    return Expressions.EvalMemberExpr(astNode as MemberExpr, env);
                case NodeType.ObjectConstructorExpr:
                    return Expressions.EvalObjectConstructorExpr(astNode as ObjectConstructorExpr, env);
                case NodeType.ArrayLiteral:
                    return Expressions.EvalArrayExpr(astNode as ArrayLiteral, env);
                case NodeType.CallExpr:
                    return Expressions.EvalCallExpr(astNode as CallExpr, env);
                case NodeType.IndexExpr:
                    return Expressions.EvalIndexExpr(astNode as IndexExpr, env);
                case NodeType.LambdaExpr:
                    return Expressions.EvalLambdaExpr(astNode as LambdaExpr, env);
                case NodeType.AssignmentExpr:
                    return Expressions.EvalAssignment(astNode as AssignmentExpr, env);
                case NodeType.EqualityCheckExpr:
                    return Expressions.EvalEqualityCheckExpr(astNode as EqualityCheckExpr, env);
                case NodeType.BinaryExpr:
                    return Expressions.EvalBinaryExpr(astNode as BinaryExpr, env);
                /*case NodeType.Program:
                    return Statements.EvalProgram(astNode as Program, env);*/

                // Handle statements
                case NodeType.ImportStmt:
                    return Statements.EvalImportStmt(astNode as ImportStmt, env);
                case NodeType.VarDeclaration:
                    return Statements.EvalVarDeclaration(astNode as VarDeclaration, env);
                case NodeType.FunctionDeclaration:
                    return Statements.EvalFunctionDeclaration(astNode as FunctionDeclaration, env);
                case NodeType.ObjectDeclaration:
                    return Statements.EvalObjectDeclaration(astNode as ObjectDeclaration, env);
                case NodeType.ReturnDeclaration:
                    return Statements.EvalReturnDeclaration(astNode as ReturnDeclaration, env);
                case NodeType.DeleteDeclaration:
                    return Statements.EvalDeleteDeclaration(astNode as DeleteDeclaration, env);
                case NodeType.IfElseDeclaration:
                    return Statements.EvalIfElseDeclaration(astNode as IfElseDeclaration, env);
                case NodeType.WhileDeclaration:
                    return Statements.EvalWhileDeclaration(astNode as WhileDeclaration, env);
                case NodeType.ForDeclaration:
                    return Statements.EvalForDeclaration(astNode as ForDeclaration, env);

                // Handle unimplemented ast types as error
                default: // TODO: Eval member expr to make objects work
                    throw new RuntimeException($"This AST Node has not yet been setup for interpretation: {astNode}");
            }
        }
    }
}
