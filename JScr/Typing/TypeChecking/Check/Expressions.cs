using JScr.Frontend;
using static JScr.Frontend.Ast;

namespace JScr.Typing.TypeChecking.Check;

internal static class Expressions
{
    public static StaticValue? CheckAssignment(AssignmentExpr node, Environment env)
    {
        var a = TypeChecker.CheckTypes(node.Assigne, env);
        var b = TypeChecker.CheckTypes(node.Value, env);

        if (a != b)
            env.ThrowTypeError(node, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Type mismatch. Type `{b}` cannot be assigned to `{a}`."));
        return a;
    }

    public static StaticValue? CheckEqualityCheckExpr(EqualityCheckExpr node, Environment env)
    {
        // TODO: Check if types can be compared and return a boolean type
        return null;
    }
    
    public static StaticValue? CheckBinaryExpr(BinaryExpr binop, Environment env)
    {
        var left = TypeChecker.CheckTypes(binop.Left, env);
        if (left is not ClassValue cleft)
        {
            env.ThrowTypeError(binop, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Tried to define operator on type `{left?.Path}`, but it is not a class."));
            return null;
        }

        var op = cleft.StaticEnvironment?.LookupSymbol(cleft.Path.Append(binop.Operator), binop);
        if (op == null) return null;

        if (op.Kind != SymbolKind.Function)
        {
            env.ThrowTypeError(binop, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Operator must be a function."));
            return null;
        }
        
        var right = TypeChecker.CheckTypes(binop.Right, env);
        var func = (FunctionValue)op.Type;
        if (func.Parameters.Length != 1)
        {
            env.ThrowTypeError(binop, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Operator definition `{binop.Operator}` on `{left.Path}` must only have one parameter."));
            return null;
        } 
        
        if (func.Parameters.First() != right)
        {
            env.ThrowTypeError(binop, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Operator definition `{binop.Operator}` on `{left.Path}` expected parameter of type `{func.Parameters.First().Path}`, but got `{right?.Path}`."));
            return null;
        }

        if (func.Type == env.LookupPrimitiveVoid(binop)?.Type)
        {
            env.ThrowTypeError(binop, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Operator definition `{binop.Operator}` on `{left.Path}` cannot return void."));
            return null;
        }
        
        return func.Type;
    }
    
    public static StaticValue? CheckIndexExpr(IndexExpr expr, Environment env)
    {
        return null;
    }
    
    public static StaticValue? CheckCallExpr(CallExpr expr, Environment env)
    {
        var caller = TypeChecker.CheckTypes(expr, env);

        if (caller is not FunctionValue)
        {
            env.ThrowTypeError(expr, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Only functions can be called."));
            return null;
        }

        FunctionValue newCaller = (FunctionValue)caller;

        if (newCaller.Parameters.Length != expr.Args.Count)
        {
            env.ThrowTypeError(expr, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Amount of parameters in the target function does not match the amount of passed parameters."));
            return null;
        }

        for (int i = 0; i < newCaller.Parameters.Length; i++)
        {
            var param = newCaller.Parameters[i];
            var arg = TypeChecker.CheckTypes(expr.Args[i], env);

            if (param != arg)
            {
                env.ThrowTypeError(expr, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"The type of the passed function parameter at index {i} does not match the type of the function signature."));
                return null;
            }
        }

        return newCaller.Type;
    }

    public static StaticValue? CheckGenericArgsExpr(GenericArgsExpr expr, Environment env)
    {
        return null;
    }
    
    public static StaticValue? CheckObjectConstructorExpr(ObjectConstructorExpr expr, Environment env)
    {
        // TODO: Basically the type of the constructed object (?)
        // TODO: Check type first and then the object constructor params
        
        var type = TypeChecker.CheckTypes(expr.Type!, env); // TODO: ALLOW NULL TYPE (inferred types)
        
        /*var caller = TypeChecker.CheckTypes(expr, env);

        if (caller is not FunctionValue)
        {
            env.ThrowTypeError(expr, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Only functions can be called."));
            return null;
        }

        FunctionValue newCaller = (FunctionValue)caller;

        if (newCaller.Parameters.Length != expr.Args.Length)
        {
            env.ThrowTypeError(expr, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Amount of parameters in the target function does not match the amount of passed parameters."));
            return null;
        }

        for (int i = 0; i < newCaller.Parameters.Length; i++)
        {
            var param = newCaller.Parameters[i];
            var arg = TypeChecker.CheckTypes(expr.Args[i], env);

            if (param != arg)
            {
                env.ThrowTypeError(expr, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"The type of the passed function parameter at index {i} does not match the type of the function signature."));
                return null;
            }
        }

        return newCaller.Type;
        */
        
        // TODO: Add ctor decl first
        
        
        return null;
    }

    public static StaticValue? CheckObjectArrayConstructorExpr(ObjectArrayConstructorExpr node, Environment env)
    {
        return null;
    }
    
    // variable or function
    public static StaticValue? CheckMemberExpr(MemberExpr node, Environment env)
    {
        var obj = TypeChecker.CheckTypes(node.Left, env);
        
        if (node.Right is not Identifier ident)
        {
            env.ThrowTypeError(node.Right, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Right hand side of a member expression must be an identifier."));
            return null;
        }
        
        var leftPath = obj?.Path;
        if (obj is ClassValue { MemberEnvironment: not null } classVal)
        {
            var symbol = classVal.MemberEnvironment.LookupSymbol(leftPath!.Value.Append(ident.Symbol), ident);
            if (symbol == null) return null;
        }
        else if (obj is StructValue { MemberEnvironment: not null } structVal)
        {
            var symbol = structVal.MemberEnvironment.LookupSymbol(leftPath!.Value.Append(ident.Symbol), ident);
            if (symbol == null) return null;
        }

        env.ThrowTypeError(node, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Invalid left hand side expression for member expression."));
        return null;
    }
    
    // (static) class, enum, module
    public static StaticValue? CheckResolutionExpr(ResolutionExpr node, Environment env)
    {
        var left = TypeChecker.CheckTypes(node.Left, env);
        if (left == null) return null;
        
        if (node.Right is not Identifier r) return null;
        var right = r.Symbol;
        
        var leftPath = left.Path;
        if (left is NamespaceValue { Environment: not null } ns)
        {
            var sym = ns.Environment.LookupSymbol(leftPath.Append(right), node);
            return sym?.Type;
        }
        else if (left is ClassValue { StaticEnvironment: not null } cls)
        {
            var sym = cls.StaticEnvironment.LookupSymbol(leftPath.Append(right), node);
            if (sym == null) return null;
        }
        else if (left is StructValue { StaticEnvironment: not null } stc)
        {
            var sym = stc.StaticEnvironment.LookupSymbol(leftPath.Append(right), node);
            if (sym == null) return null;
        }

        env.ThrowTypeError(node, SyntaxErrorData.New(
            SyntaxErrorLevel.Error,
            $"Cannot use resolution operator `::` on type `{left?.Name}`."
        ));
        return null;
    }
    
    public static StaticValue? CheckUnaryExpr(UnaryExpr node, Environment env)
    {
        // 0 (number) (- | +) val
        // ^ BinaryExpr

        return CheckBinaryExpr(new BinaryExpr(node.Range, new NumericLiteral(node.Range, 0), node.Object, node.Operator), env);
    }
    
    public static StaticValue? CheckLambdaExpr(LambdaExpr expr, Environment env)
    {
        return null;
    }

    public static StaticValue? CheckIdentifier(Identifier ident, Environment env)
    {
        // Find Symbol
        var symbol = env.LookupSymbol(new QualifiedName(ident.Symbol), ident);
        return symbol?.Type;

        /*if (symbol != null)
        {
            if (symbol is VariableSymbol)
                return ((VariableSymbol)symbol).Type;
            else if (symbol is FunctionSymbol)
                return ((FunctionSymbol)symbol).FunctionValue;
            else if (symbol is DatatypeSymbol)
                return ((DatatypeSymbol)symbol).DatatypeSymbolValue;
        }*/

        // Find Module
        /*var modName = new List<string>() { ident.Symbol };
        if (env.TryFindModule(modName, out var mod))
            return new StaticModuleVal(modName, mod!.Value);*/

        // Nothing Found
        /*env.ThrowTypeError(ident, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Undefined or invalid symbol `{ident.Symbol}`."));
        return null;*/
    }
}