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
            ImportStmt,
            VarDeclaration,
            FunctionDeclaration,
            ObjectDeclaration,
            ReturnDeclaration,
            DeleteDeclaration,
            IfElseDeclaration,
            WhileDeclaration,
            ForDeclaration,

            // EXPRESSIONS
            AssignmentExpr,
            EqualityCheckExpr,
            MemberExpr,
            LambdaExpr,
            CallExpr,
            IndexExpr,
            ObjectConstructorExpr,

            // LITERALS
            Property,
            ArrayLiteral,
            NumericLiteral,
            StringLiteral,
            CharLiteral,
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
            public string FileDir { get; }
            public List<Stmt> Body { get; }

            public Program(string fileDir, List<Stmt> body) : base(NodeType.Program) { FileDir = fileDir; Body = body; }
        }

        public class ImportStmt : Stmt
        {
            public string[] Target { get; }
            public string? Alias { get; }

            public ImportStmt(string[] target, string? alias) : base(NodeType.ImportStmt)
            {
                Target = target;
                Alias = alias;
            }
        }

        public class VarDeclaration : Stmt
        {
            public bool Constant { get; }
            public bool Export { get; }
            public Types.Type Type { get; }
            public string Identifier { get; }
            public Expr? Value { get; }

            public VarDeclaration(bool constant, bool export, Types.Type? type, string identifier, Expr? value) : base(NodeType.VarDeclaration)
            {
                Constant = constant;
                Export = export;
                Type = type ?? Types.Type.Void();
                Identifier = identifier;
                Value = value;
            }
        }

        public class FunctionDeclaration : Stmt
        {
            public bool Export { get; }
            public VarDeclaration[] Parameters { get; }
            public string Name { get; }
            public Types.Type Type { get; }
            public Stmt[] Body { get; }
            public bool InstantReturn { get; }

            public FunctionDeclaration(bool export, VarDeclaration[] parameters, string name, Types.Type? type, Stmt[] body, bool instantReturn) : base(NodeType.FunctionDeclaration)
            {
                Export = export;
                Parameters = parameters;
                Name = name;
                Type = type ?? Types.Type.Void();
                Body = body;
                InstantReturn = instantReturn;
                // TODO: Important keywords like `async` etc.
            }
        }

        public class ObjectDeclaration : Stmt
        {
            public bool Export { get; }
            public string Name { get; }
            public Property[] Properties { get; }

            public ObjectDeclaration(bool export, string name, Property[] properties) : base(NodeType.ObjectDeclaration)
            {
                Export = export;
                Name = name;
                Properties = properties;
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

        public class DeleteDeclaration : Stmt
        {
            public string Value { get; }

            public DeleteDeclaration(string value) : base(NodeType.DeleteDeclaration)
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

        public class IndexExpr : Expr
        {
            public Expr Arg { get; }
            public Expr Caller { get; }

            public IndexExpr(Expr arg, Expr calle) : base(NodeType.IndexExpr)
            {
                Caller = calle;
                Arg = arg;
            }
        }

        public class ObjectConstructorExpr : Expr
        {
            public dynamic TargetVariableIdent { get; }
            public bool TargetVarIdentAsType { get; }
            public Property[] Properties { get; }

            public ObjectConstructorExpr(dynamic targetVariableIdent, bool targetVarIdentAsType, Property[] properties) : base(NodeType.ObjectConstructorExpr)
            {
                TargetVariableIdent = targetVariableIdent;
                TargetVarIdentAsType = targetVarIdentAsType;
                Properties = properties;
            }
        }

        public class MemberExpr : Expr
        {
            public Expr Object { get; }
            public Expr Property { get; }

            public MemberExpr(Expr object_, Expr property) : base(NodeType.MemberExpr)
            {
                Object = object_;
                Property = property;
            }
        }

        public class LambdaExpr : Expr
        {
            public Identifier[] ParamIdents { get; }
            public Stmt[] Body { get; }
            public bool InstantReturn { get; }

            public LambdaExpr(Identifier[] paramIdents, Stmt[] body, bool instantReturn) : base(NodeType.LambdaExpr)
            {
                ParamIdents = paramIdents;
                Body = body;
                InstantReturn = instantReturn;
            }
        }

        public class Identifier : Expr
        {
            public string Symbol { get; }

            public Identifier(string symbol) : base(NodeType.Identifier) { Symbol = symbol; }
        }

        public class ArrayLiteral : Expr
        {
            public Expr[] Value { get; }

            public ArrayLiteral(Expr[] value) : base(NodeType.ArrayLiteral) { Value = value; }
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

        public class CharLiteral : Expr
        {
            public char Value { get; }

            public CharLiteral(char value) : base(NodeType.CharLiteral) { Value = value; }
        }

        public class Property : Expr
        {
            public string Key { get; }
            public Types.Type Type { get; }
            public Expr? Value { get; }

            public Property(string key, Types.Type? type, Expr? value) : base(NodeType.Property) {
                Key = key;
                Type = type ?? Types.Type.Void();
                Value = value;
            }
        }
    }
}
