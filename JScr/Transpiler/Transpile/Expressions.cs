using static JScr.Frontend.Ast;

namespace JScr.Transpiler.Transpile
{
    internal static class Expressions
    {
        public static TPString TPBinaryExpr(BinaryExpr binop, Environment env)
        {
            return new(Transpiler.Transpile(binop.Left, env).ToString() + binop.Operator + Transpiler.Transpile(binop.Right, env).ToString());
        }

        public static TPString TPIdentifier(Identifier ident, Environment env)
        {
            return new(ident.Symbol);
        }

        public static TPString TPAssignment(AssignmentExpr node, Environment env)
        {
            return new(Transpiler.Transpile(node.Assigne, env).ToString() + "=" + Transpiler.Transpile(node.Value, env).ToString());
        }

        public static TPString TPEqualityCheckExpr(EqualityCheckExpr node, Environment env)
        {
            string opstr = string.Empty;

            switch (node.Operator)
            {
                case EqualityCheckExpr.Type.And: opstr = "&&"; break;
                case EqualityCheckExpr.Type.Or: opstr = "||"; break;
                case EqualityCheckExpr.Type.Equals: opstr = "=="; break;
                case EqualityCheckExpr.Type.NotEquals: opstr = "!="; break;
                case EqualityCheckExpr.Type.LessThan: opstr = "<"; break;
                case EqualityCheckExpr.Type.LessThanOrEquals: opstr = "<="; break;
                case EqualityCheckExpr.Type.MoreThan: opstr = ">"; break;
                case EqualityCheckExpr.Type.MoreThanOrEquals: opstr = ">="; break;
            }

            return new(Transpiler.Transpile(node.Left, env).ToString() + opstr + Transpiler.Transpile(node.Right, env).ToString());
        }
        /*
        public static TPString TPArrayExpr(ArrayLiteral obj, Environment env)
        {
            return new();
        }
        */
        public static TPString TPMemberExpr(MemberExpr node, Environment env)
        {
            return new(Transpiler.Transpile(node.Left, env).ToString() + "." + Transpiler.Transpile(node.Right, env).ToString());
        }

        public static TPString TPUnaryExpr(UnaryExpr node, Environment env)
        {
            return new(node.Operator + Transpiler.Transpile(node.Object, env).ToString());
        }

        public static TPString TPObjectConstructorExpr(ObjectConstructorExpr node, Environment env)
        {
            return new();
        }

        public static TPString TPCallExpr(CallExpr expr, Environment env)
        {
            var returnVal = new TPString("");

            returnVal += Transpiler.Transpile(expr.Caller, env);

            returnVal += "(";
            var scope = env.WithNoSemicolons();
            for (int i = 0; i < expr.Args.Count; i++)
            {
                var arg = expr.Args[i];
                if (i > 0)
                    returnVal += ",";

                returnVal += Transpiler.Transpile(arg, scope);
            }
            returnVal += ")";

            if (!env.NoSemicolons)
                returnVal += ";";

            return returnVal;
        }

        public static TPString TPIndexExpr(IndexExpr expr, Environment env)
        {
            return new();
        }

        public static TPString TPLambdaExpr(LambdaExpr expr, Environment env)
        {
            var returnVal = new TPString("");
            string functionName;

            {
                var funcdecl = new TPString("");
                var type = expr.ReturnType;
                funcdecl += Statements.TPFunctionDeclaration(new FunctionDeclaration(expr.Range, expr.AnnotatedWith, Visibility.Private, null, false, false, expr.Parameters, Helpers.RandomName("lambda"), type, expr.Body, expr.IsExpressionLambda), env, out var funcname);
                functionName = funcname;

                env.top.Add(funcdecl.ToString());
            }

            // TODO: use function pointers
            returnVal += "&" + functionName;

            return returnVal;
        }
    }
}
