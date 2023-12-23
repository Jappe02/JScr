using System.Text.Json;
using static JScr.Runtime.Values;

namespace JScr.Frontend
{
    internal static class Ast
    {
        public enum NodeType
        {
            // STATEMENTS
            Program,
            VarDeclaration,
            FunctionDeclaration,
            ReturnDeclaration,

            // EXPRESSIONS
            AssignmentExpr,
            MemberExpr,
            CallExpr,

            // LITERALS
            Property,
            ObjectLiteral,
            NumericLiteral,
            Identifier,
            BinaryExpr,
        }

        public abstract class Stmt
        {
            public NodeType Kind { get; }

            public Stmt(NodeType kind) { Kind = kind; }
        }

        public class Program : Stmt
        {
            public List<Stmt> Body { get; }

            public Program(List<Stmt> body) : base(NodeType.Program) { Body = body; }
        }

        public class VarDeclaration : Stmt
        {
            public bool Constant { get; }
            public Type Type { get; }
            public string Identifier { get; }
            public Expr? Value { get; }

            public VarDeclaration(bool constant, Type type, string identifier, Expr? value) : base(NodeType.VarDeclaration)
            {
                Constant = constant;
                Type = type;
                Identifier = identifier;
                Value = value;
            }
        }

        public class FunctionDeclaration : Stmt
        {
            public VarDeclaration[] Parameters { get; }
            public string Name { get; }
            public Type Type { get; }
            public Stmt[] Body { get; }

            public FunctionDeclaration(VarDeclaration[] parameters, string name, Type type, Stmt[] body) : base(NodeType.FunctionDeclaration)
            {
                Parameters = parameters;
                Name = name;
                Type = type;
                Body = body;
                // TODO: Important keywords like `async` etc.
            }
        }

        public class ReturnDeclaration : Stmt
        {
            public Expr Value { get; }

            public ReturnDeclaration(Expr value) : base(NodeType.ReturnDeclaration)
            {
                Value = value;
            }
        }

        public abstract class Expr : Stmt
        {
            public Expr(NodeType kind) : base(kind) { }
        }

        public class AssignmentExpr : Expr
        {
            public Expr Assigne { get; }
            public Expr Value { get; }

            public AssignmentExpr(Expr assigne, Expr value) : base(NodeType.AssignmentExpr) {
                Assigne = assigne;
                Value = value;
            }
        }

        public class BinaryExpr : Expr
        {
            public Expr Left { get; }
            public Expr Right { get; }
            public string Operator { get; }

            public BinaryExpr(Expr left, Expr right, string operator_) : base(NodeType.BinaryExpr)
            {
                Left = left;
                Right = right;
                Operator = operator_;
            }
        }

        public class CallExpr : Expr
        {
            public List<Expr> Args { get; }
            public Expr Caller { get; }

            public CallExpr(List<Expr> args, Expr calle) : base(NodeType.CallExpr)
            {
                Caller = calle;
                Args = args;
            }
        }

        public class MemberExpr : Expr
        {
            public Expr Object { get; }
            public Expr Property { get; }
            public bool Computed { get; }

            public MemberExpr(Expr object_, Expr property, bool computed) : base(NodeType.MemberExpr)
            {
                Object = object_;
                Property = property;
                Computed = computed;
            }
        }

        public class Identifier : Expr
        {
            public string Symbol { get; }

            public Identifier(string symbol) : base(NodeType.Identifier) { Symbol = symbol; }
        }

        public class NumericLiteral : Expr
        {
            public int Value { get; }

            public NumericLiteral(int value) : base(NodeType.NumericLiteral) { Value = value; }
        }

        public class Property : Expr
        {
            public string Key { get; }
            public Expr? Value { get; }

            public Property(string key, Expr? value) : base(NodeType.Property) {
                Key = key;
                Value = value;
            }
        }

        public class ObjectLiteral : Expr
        {
            public Property[] Properties { get; }

            public ObjectLiteral(Property[] properties) : base(NodeType.ObjectLiteral) { Properties = properties; }
        }
    }
}
