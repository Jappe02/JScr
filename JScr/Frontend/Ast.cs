using JScr.Runtime;
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
            IfElseDeclaration,
            WhileDeclaration,
            ForDeclaration,

            // EXPRESSIONS
            AssignmentExpr,
            EqualityCheckExpr,
            MemberExpr,
            CallExpr,

            // LITERALS
            Property,
            ObjectLiteral,
            NumericLiteral,
            StringLiteral,
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
            public Types.Type Type { get; }
            public string Identifier { get; }
            public Expr? Value { get; }

            public VarDeclaration(bool constant, Types.Type? type, string identifier, Expr? value) : base(NodeType.VarDeclaration)
            {
                Constant = constant;
                Type = type ?? Types.Type.Void;
                Identifier = identifier;
                Value = value;
            }
        }

        public class FunctionDeclaration : Stmt
        {
            public VarDeclaration[] Parameters { get; }
            public string Name { get; }
            public Types.Type Type { get; }
            public Stmt[] Body { get; }

            public FunctionDeclaration(VarDeclaration[] parameters, string name, Types.Type? type, Stmt[] body) : base(NodeType.FunctionDeclaration)
            {
                Parameters = parameters;
                Name = name;
                Type = type ?? Types.Type.Void;
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

        public class IfElseDeclaration : Stmt
        {
            public class IfBlock
            {
                public Expr Condition { get; }
                public Stmt[] Body { get; }

                public IfBlock(Expr condition, Stmt[] body)
                {
                    Condition = condition;
                    Body = body;
                }
            }

            public IfBlock[] Blocks { get; }
            public Stmt[]? ElseBody { get; }

            public IfElseDeclaration(IfBlock[] blocks, Stmt[]? elseBody) : base(NodeType.IfElseDeclaration)
            {
                Blocks = blocks;
                ElseBody = elseBody;
            }
        }

        public class WhileDeclaration : Stmt
        {
            public Expr Condition { get; }
            public Stmt[] Body { get; }

            public WhileDeclaration(Expr condition, Stmt[] body) : base(NodeType.WhileDeclaration)
            {
                Condition = condition;
                Body = body;
            }
        }

        public class ForDeclaration : Stmt
        {
            public Stmt Declaration { get; }
            public Expr Condition { get; }
            public Expr Action { get; }
            public Stmt[] Body { get; }

            public ForDeclaration(Stmt declaration, Expr condition, Expr action, Stmt[] body) : base(NodeType.ForDeclaration)
            {
                Declaration = declaration;
                Condition = condition;
                Action = action;
                Body = body;
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

        public class EqualityCheckExpr : Expr
        {
            public enum Type
            {
                Equals, NotEquals, MoreThan, MoreThanOrEquals, LessThan, LessThanOrEquals, And, Or
            }

            public Expr Left { get; }
            public Expr Right { get; }
            public Type Operator { get; }

            public EqualityCheckExpr(Expr left, Expr right, Type operator_) : base(NodeType.EqualityCheckExpr)
            {
                Left = left;
                Right = right;
                Operator = operator_;
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
            public Identifier Property { get; }

            public MemberExpr(Expr object_, Identifier property) : base(NodeType.MemberExpr)
            {
                Object = object_;
                Property = property;
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

        public class StringLiteral : Expr
        {
            public string Value { get; }

            public StringLiteral(string value) : base(NodeType.StringLiteral) { Value = value; }
        }

        public class Property : Expr
        {
            public string Key { get; }
            public Types.Type Type { get; }
            public Expr? Value { get; }

            public Property(string key, Types.Type? type, Expr? value) : base(NodeType.Property) {
                Key = key;
                Type = type ?? Types.Type.Void;
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
