using static JScr.Frontend.Ast;

namespace JScr.Typing.TypeChecking.Check
{
    internal static class Expressions
    {
        public static RuntimeVal CheckBinaryExpr(BinaryExpr binop, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckIdentifier(Identifier ident, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckAssignment(AssignmentExpr node, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckEqualityCheckExpr(EqualityCheckExpr node, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckArrayExpr(ArrayLiteral obj, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckMemberExpr(MemberExpr node, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckUnaryExpr(UnaryExpr node, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckObjectConstructorExpr(ObjectConstructorExpr node, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckCallExpr(CallExpr expr, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckIndexExpr(IndexExpr expr, Environment env)
        {
            return new NullValue();
        }

        public static RuntimeVal CheckLambdaExpr(LambdaExpr expr, Environment env)
        {
            return new NullValue();
        }
    }
}
