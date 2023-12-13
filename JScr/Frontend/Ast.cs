using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            public readonly NodeType Kind;

            public Stmt(NodeType kind) { Kind = kind; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class Program : Stmt
        {
            public readonly List<Stmt> Body;

            public Program(List<Stmt> body) : base(NodeType.Program) { Body = body; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class VarDeclaration : Stmt
        {
            public readonly bool Constant;
            public readonly string Identifier;
            public readonly Expr? Value;

            public VarDeclaration(bool constant, string identifier, Expr? value) : base(NodeType.VarDeclaration)
            {
                Constant = constant;
                Identifier = identifier;
                Value = value;
            }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class FunctionDeclaration : Stmt
        {
            public readonly string[] Parameters;
            public readonly string Name;
            public readonly Stmt[] Body;

            public FunctionDeclaration(string[] parameters, string name, Stmt[] body) : base(NodeType.FunctionDeclaration)
            {
                Parameters = parameters;
                Name = name;
                Body = body;
                // TODO: Important keywords like `async` etc.
            }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public abstract class Expr : Stmt
        {
            public Expr(NodeType kind) : base(kind) { }
            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class AssignmentExpr : Expr
        {
            public readonly Expr Assigne;
            public readonly Expr Value;

            public AssignmentExpr(Expr assigne, Expr value) : base(NodeType.AssignmentExpr) {
                Assigne = assigne;
                Value = value;
            }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class BinaryExpr : Expr
        {
            public readonly Expr Left;
            public readonly Expr Right;
            public readonly string Operator;

            public BinaryExpr(Expr left, Expr right, string operator_) : base(NodeType.BinaryExpr)
            {
                Left = left;
                Right = right;
                Operator = operator_;
            }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class CallExpr : Expr
        {
            public readonly List<Expr> Args;
            public readonly Expr Caller;

            public CallExpr(List<Expr> args, Expr calle) : base(NodeType.CallExpr)
            {
                Caller = calle;
                Args = args;
            }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class MemberExpr : Expr
        {
            public readonly Expr Object;
            public readonly Expr Property;
            public readonly bool Computed;

            public MemberExpr(Expr object_, Expr property, bool computed) : base(NodeType.MemberExpr)
            {
                Object = object_;
                Property = property;
                Computed = computed;
            }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class Identifier : Expr
        {
            public readonly string Symbol;

            public Identifier(string symbol) : base(NodeType.Identifier) { Symbol = symbol; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class NumericLiteral : Expr
        {
            public readonly float Value;

            public NumericLiteral(float value) : base(NodeType.NumericLiteral) { Value = value; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class Property : Expr
        {
            public readonly string Key;
            public readonly Expr? Value;

            public Property(string key, Expr? value) : base(NodeType.Property) {
                Key = key;
                Value = value;
            }

            public override string ToString() => JsonSerializer.Serialize(this);
        }

        public class ObjectLiteral : Expr
        {
            public readonly Property[] Properties;

            public ObjectLiteral(Property[] properties) : base(NodeType.ObjectLiteral) { Properties = properties; }

            public override string ToString() => JsonSerializer.Serialize(this);
        }
    }
}
