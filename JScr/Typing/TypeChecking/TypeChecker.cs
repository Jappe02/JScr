using JScr.Frontend;
using JScr.Typing.TypeChecking.Check;
using static JScr.Frontend.Ast;

namespace JScr.Typing.TypeChecking;

internal static class TypeChecker
{
    // TODO: Pass program as ref and modify the AST so that it becomes typed and can then be used in interpreter.
    public static void CheckProgramTypes(Compilation compilation, out RootEnvironment environment)
    {
        environment = new RootEnvironment(compilation.ErrorCallback);
        var projectEnv = new GlobalEnvironment(environment, compilation.FilePath, compilation.FilePath);
        foreach (var target in compilation.Targets)
        {
            Statements.CheckProgram(target, projectEnv);
        }

        // TODO: Check deps
    }

    /// <summary>
    /// Checks types of the specified AST node.
    /// </summary>
    /// <param name="astNode">The AST node to check.</param>
    /// <param name="env">The current environment.</param>
    /// <param name="requireDatatypeDecl">Whether to require the return value to be an instance of [StaticTypeSymbolVal] (a type a.k.a class, struct, enum)</param>
    /// <returns>The type.</returns>
    /// <exception cref="Exception"></exception>
    public static StaticValue? CheckTypes(Stmt astNode, Environment env, bool requireDatatypeDecl = false)
    {
        StaticValue? ret;

        switch (astNode.Kind)
        {
            // Handle expressions
            case NodeType.AssignmentExpr:
                ret = Expressions.CheckAssignment(astNode as AssignmentExpr, env);
                break;
            case NodeType.EqualityCheckExpr:
                ret = Expressions.CheckEqualityCheckExpr(astNode as EqualityCheckExpr, env);
                break;
            case NodeType.BinaryExpr:
                ret = Expressions.CheckBinaryExpr(astNode as BinaryExpr, env);
                break;
            case NodeType.IndexExpr:
                ret = Expressions.CheckIndexExpr(astNode as IndexExpr, env);
                break;
            case NodeType.CallExpr:
                ret = Expressions.CheckCallExpr(astNode as CallExpr, env);
                break;
            case NodeType.GenericArgsExpr:
                ret = Expressions.CheckGenericArgsExpr(astNode as GenericArgsExpr, env);
                break;
            case NodeType.ObjectConstructorExpr:
                ret = Expressions.CheckObjectConstructorExpr(astNode as ObjectConstructorExpr, env);
                break;
            case NodeType.ObjectArrayConstructorExpr:
                ret = Expressions.CheckObjectArrayConstructorExpr(astNode as ObjectArrayConstructorExpr, env);
                break;
            case NodeType.MemberExpr:
                ret = Expressions.CheckMemberExpr(astNode as MemberExpr, env);
                break;
            case NodeType.ResolutionExpr:
                ret = Expressions.CheckResolutionExpr(astNode as ResolutionExpr, env);
                break;
            case NodeType.UnaryExpr:
                ret = Expressions.CheckUnaryExpr(astNode as UnaryExpr, env);
                break;
            case NodeType.LambdaExpr:
                ret = Expressions.CheckLambdaExpr(astNode as LambdaExpr, env);
                break;
            case NodeType.Identifier:
                ret = Expressions.CheckIdentifier(astNode as Identifier, env);
                break;
            case NodeType.NumericLiteral:
                ret = /*new TPString((astNode as NumericLiteral).Value.ToString())*/ null;
                break;
            case NodeType.FloatLiteral:
                ret = /*new TPString((astNode as FloatLiteral).Value.ToString())*/ null;
                break;
            case NodeType.DoubleLiteral:
                ret = /*new TPString((astNode as DoubleLiteral).Value.ToString())*/ null;
                break;
            case NodeType.StringLiteral:
                ret = /*new TPString("\"" + (astNode as StringLiteral).Value.ToString() + "\"")*/ null;
                break;
            case NodeType.CharLiteral:
                ret = /*new TPString("\'" + (astNode as CharLiteral).Value.ToString() + "\'")*/ null;
                break;
            
            /*case NodeType.Program:
                ret =  Statements.TPProgram(astNode as Program);*/

            // Handle statements
            case NodeType.NamespaceStmt:
                ret = Statements.CheckNamespaceStmt(astNode as NamespaceStmt, env);
                break;
            case NodeType.ImportStmt:
                ret = Statements.CheckImportStmt(astNode as ImportStmt, env);
                break;
            case NodeType.BlockStmt:
                ret = Statements.CheckBlockStmt(astNode as BlockStmt, env);
                break;
            case NodeType.AnnotationUsageDeclaration:
                ret = Statements.CheckAnnotationUsageDeclaration(astNode as AnnotationUsageDeclaration, env);
                break;
            case NodeType.VarDeclaration:
                ret = Statements.CheckVarDeclaration(astNode as VarDeclaration, env);
                break;
            case NodeType.ConstructorDeclaration:
                ret = Statements.CheckConstructorDeclaration(astNode as ConstructorDeclaration, env);
                break;
            case NodeType.FunctionDeclaration:
                ret = Statements.CheckFunctionDeclaration(astNode as FunctionDeclaration, env);
                break;
            case NodeType.OperatorDeclaration:
                ret = Statements.CheckOperatorDeclaration(astNode as OperatorDeclaration, env);
                break;
            case NodeType.StructDeclaration:
                ret = Statements.CheckStructDeclaration(astNode as StructDeclaration, env);
                break;
            case NodeType.ClassDeclaration:
                ret = Statements.CheckClassDeclaration(astNode as ClassDeclaration, env);
                break;
            case NodeType.EnumDeclaration:
                ret = Statements.CheckEnumDeclaration(astNode as EnumDeclaration, env);
                break;
            case NodeType.ReturnDeclaration:
                ret = Statements.CheckReturnDeclaration(astNode as ReturnDeclaration, env);
                break;
            case NodeType.DeleteDeclaration:
                ret = Statements.CheckDeleteDeclaration(astNode as DeleteDeclaration, env);
                break;
            case NodeType.IfElseDeclaration:
                ret = Statements.CheckIfElseDeclaration(astNode as IfElseDeclaration, env);
                break;
            case NodeType.WhileDeclaration:
                ret = Statements.CheckWhileDeclaration(astNode as WhileDeclaration, env);
                break;
            case NodeType.ForDeclaration:
                ret = Statements.CheckForDeclaration(astNode as ForDeclaration, env);
                break;
            

            // Handle unimplemented ast types as error
            default:
                throw new Exception($"This type-checker does not support the `{astNode}` AST node.");
        }
       
        if (requireDatatypeDecl && (ret is not ClassValue || ret is not StructValue || ret is not EnumValue))
        {
            env.ThrowTypeError(astNode, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Invalid symbol type found.").AddHint("Make sure that the type of the symbol being referenced is correct; either a class, struct, enum, function or a variable."));
            return null;
        }
        
        return ret;
    }
}