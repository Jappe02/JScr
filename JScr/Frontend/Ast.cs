using JScr.Typing;
using Type = JScr.Typing.Type;

namespace JScr.Frontend
{
    internal static class Ast
    {
        public enum NodeType
        {
            // STATEMENTS
            Program,
            NamespaceStmt,
            ImportStmt,
            BlockStmt,
            AnnotationUsageDeclaration,
            VarDeclaration,
            FunctionDeclaration,
            StructDeclaration,
            ClassDeclaration,
            EnumDeclaration,
            ReturnDeclaration,
            DeleteDeclaration,
            IfElseDeclaration,
            WhileDeclaration,
            ForDeclaration,

            // EXPRESSIONS
            AssignmentExpr,
            EqualityCheckExpr,
            MemberExpr,
            StaticMemberExpr,
            UnaryExpr,
            LambdaExpr,
            CallExpr,
            IndexExpr,
            ObjectConstructorExpr,

            // LITERALS
            Property,
            ArrayLiteral,
            NumericLiteral,
            FloatLiteral,
            DoubleLiteral,
            StringLiteral,
            CharLiteral,
            Identifier,
            BinaryExpr,
        }

        public enum Visibility : byte
        {
            Private,
            Protected,
            Public,
        }

        public enum InheritanceModifier : byte
        {
            Abstract,
            Virtual,
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

        public class NamespaceStmt : Stmt
        {
            public string[] Target { get; }

            public NamespaceStmt(string[] target) : base(NodeType.NamespaceStmt)
            {
                Target = target;
            }
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

        public class BlockStmt : Stmt
        {
            public Stmt[] Body { get; }

            public BlockStmt(Stmt[] body) : base(NodeType.BlockStmt)
            {
                Body = body;
            }
        }

        public class AnnotationUsageDeclaration : Stmt
        {
            public string Ident { get; }
            public Expr[] Args { get; }

            public AnnotationUsageDeclaration(string ident, Expr[] args) : base(NodeType.AnnotationUsageDeclaration)
            {
                Ident = ident;
                Args = args;
            }
        }

        public class VarDeclaration : Stmt
        {
            public AnnotationUsageDeclaration[] AnnotatedWith { get; }
            public bool IsConstant { get; }
            public Visibility Visibility { get; }
            public InheritanceModifier? Modifier { get; }
            public bool IsOverride { get; }
            public Type Type { get; }
            public string Identifier { get; }
            public Expr? Value { get; }

            public VarDeclaration(AnnotationUsageDeclaration[] annotatedWith, bool constant, Visibility visibility, InheritanceModifier? modifier, bool override_, Type? type, string identifier, Expr? value) : base(NodeType.VarDeclaration)
            {
                AnnotatedWith = annotatedWith;
                IsConstant = constant;
                Visibility = visibility;
                Modifier = modifier;
                IsOverride = override_;
                Type = type ?? Types.VoidType();
                Identifier = identifier;
                Value = value;
            }
        }

        public class FunctionDeclaration : Stmt
        {
            public AnnotationUsageDeclaration[] AnnotatedWith { get; }
            public Visibility Visibility { get; }
            public InheritanceModifier? Modifier { get; }
            public bool IsOverride { get; }
            public VarDeclaration[] Parameters { get; }
            public string Name { get; }
            public Type Type { get; }
            public Stmt[] Body { get; }
            public bool InstantReturn { get; }

            public FunctionDeclaration(AnnotationUsageDeclaration[] annotatedWith, Visibility visibility, InheritanceModifier? modifier, bool override_, VarDeclaration[] parameters, string name, Type? type, Stmt[] body, bool instantReturn) : base(NodeType.FunctionDeclaration)
            {
                AnnotatedWith = annotatedWith;
                Visibility = visibility;
                Modifier = modifier;
                IsOverride = override_;
                Parameters = parameters;
                Name = name;
                Type = type ?? Types.VoidType();
                Body = body;
                InstantReturn = instantReturn;
                // TODO: Important keywords like `async` etc.
            }
        }

        public class StructDeclaration : Stmt
        {
            public AnnotationUsageDeclaration[] AnnotatedWith { get; }
            public Visibility Visibility { get; }
            public string Name { get; }
            public Property[] Properties { get; }

            public StructDeclaration(AnnotationUsageDeclaration[] annotatedWith, Visibility visibility, string name, Property[] properties) : base(NodeType.StructDeclaration)
            {
                AnnotatedWith = annotatedWith;
                Visibility = visibility;
                Name = name;
                Properties = properties;
            }
        }

        public class ClassDeclaration : Stmt
        {
            public AnnotationUsageDeclaration[] AnnotatedWith { get; }
            public Visibility Visibility { get; }
            public bool IsAbstract { get; }
            public SimpleType Name { get; }
            public SimpleType[] Derivants { get; }
            public Stmt[] Body { get; }

            public ClassDeclaration(AnnotationUsageDeclaration[] annotatedWith, Visibility visibility, bool abstract_, SimpleType name, SimpleType[] derivants, Stmt[] body) : base(NodeType.ClassDeclaration)
            {
                AnnotatedWith = annotatedWith;
                Visibility = visibility;
                IsAbstract = abstract_;
                Name = name;
                Derivants = derivants;
                Body = body;
            }
        }

        public class EnumDeclaration : Stmt
        {
            public AnnotationUsageDeclaration[] AnnotatedWith { get; }
            public Visibility Visibility { get; }
            public string Name { get; }
            public string[] Entries { get; }

            public EnumDeclaration(AnnotationUsageDeclaration[] annotatedWith, Visibility visibility, string name, string[] entries) : base(NodeType.EnumDeclaration)
            {
                AnnotatedWith = annotatedWith;
                Visibility = visibility;
                Name = name;
                Entries = entries;
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
                public Stmt Body { get; }

                public IfBlock(Expr condition, Stmt body)
                {
                    Condition = condition;
                    Body = body;
                }
            }

            public IfBlock[] Blocks { get; }
            public Stmt? ElseBody { get; }

            public IfElseDeclaration(IfBlock[] blocks, Stmt? elseBody) : base(NodeType.IfElseDeclaration)
            {
                Blocks = blocks;
                ElseBody = elseBody;
            }
        }

        public class WhileDeclaration : Stmt
        {
            public Expr Condition { get; }
            public Stmt Body { get; }

            public WhileDeclaration(Expr condition, Stmt body) : base(NodeType.WhileDeclaration)
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
            public Stmt Body { get; }

            public ForDeclaration(Stmt declaration, Expr condition, Expr action, Stmt body) : base(NodeType.ForDeclaration)
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
            public Expr Member { get; }

            public MemberExpr(Expr object_, Expr member) : base(NodeType.MemberExpr)
            {
                Object = object_;
                Member = member;
            }
        }

        public class StaticMemberExpr : Expr
        {
            public Expr Object { get; }
            public Expr Member { get; }

            public StaticMemberExpr(Expr object_, Expr member) : base(NodeType.StaticMemberExpr)
            {
                Object = object_;
                Member = member;
            }
        }

        public class UnaryExpr : Expr
        {
            public Expr Object { get; }
            public string Operator { get; }

            public UnaryExpr(Expr object_, string operator_) : base(NodeType.UnaryExpr)
            {
                Object = object_;
                Operator = operator_;
            }
        }

        public class LambdaExpr : Expr
        {
            public AnnotationUsageDeclaration[] AnnotatedWith { get; }
            public VarDeclaration[] Parameters { get; }
            public Type? ReturnType { get; }
            public Stmt[] Body { get; }
            public bool IsExpressionLambda { get; }

            public LambdaExpr(AnnotationUsageDeclaration[] annotatedWith, VarDeclaration[] parameters, Type? returnType, Stmt[] body, bool isExpressionLambda) : base(NodeType.LambdaExpr)
            {
                AnnotatedWith = annotatedWith;
                Parameters = parameters;
                ReturnType = returnType;
                Body = body;
                IsExpressionLambda = isExpressionLambda;
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

        public class FloatLiteral : Expr
        {
            public float Value { get; }

            public FloatLiteral(float value) : base(NodeType.FloatLiteral) { Value = value; }
        }

        public class DoubleLiteral : Expr
        {
            public double Value { get; }

            public DoubleLiteral(double value) : base(NodeType.DoubleLiteral) { Value = value; }
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
            public Type Type { get; }
            public Expr? Value { get; }

            public Property(string key, Type? type, Expr? value) : base(NodeType.Property) {
                Key = key;
                Type = type ?? Types.VoidType();
                Value = value;
            }
        }
    }
}
