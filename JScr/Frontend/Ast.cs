using JScr.Typing.TypeChecking;
using JScr.Utils;
using Type = JScr.Typing.Type;
using Range = JScr.Utils.Range;

// TODO: Typed AST
namespace JScr.Frontend;

/// Represents a type in the AST. A type after parsing may be an Ast.Expr, but after type checking it may be Type.
using AstType = DualType<Ast.Expr, StaticValue?>;

public static class Ast
{
    public enum NodeType
    {
        // STATEMENTS
        Program,
        NamespaceStmt,
        ImportStmt,
        BlockStmt,
        TypeDefinitionStmt,
        AnnotationUsageDeclaration,
        ConstructorDeclaration,
        VarDeclaration,
        FunctionDeclaration,
        OperatorDeclaration,
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
        ResolutionExpr,
        UnaryExpr,
        LambdaExpr,
        IndexExpr,
        CallExpr,
        GenericArgsExpr,
        ObjectConstructorExpr,
        ObjectArrayConstructorExpr,

        // LITERALS
        Property,
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
        public Range Range { get; }

        public Stmt(NodeType kind, Range range) { Kind = kind; Range = range; }
    }

    public class Program : Stmt
    {
        public string FileDir { get; }
        public bool IsTyped { get; }
        public List<Stmt> Body { get; }

        public Program(Range range, string fileDir, bool isTyped, List<Stmt> body) : base(NodeType.Program, range) { FileDir = fileDir; IsTyped = isTyped; Body = body; }
    }

    public class NamespaceStmt : Stmt
    {
        public string[] Target { get; }

        public NamespaceStmt(Range range, string[] target) : base(NodeType.NamespaceStmt, range)
        {
            Target = target;
        }
    }

    public class ImportStmt : Stmt
    {
        public Expr Target { get; }
        public string? Alias { get; }

        public ImportStmt(Range range, Expr target, string? alias) : base(NodeType.ImportStmt, range)
        {
            Target = target;
            Alias = alias;
        }
    }

    public class BlockStmt : Stmt
    {
        public Stmt[] Body { get; }

        public BlockStmt(Range range, Stmt[] body) : base(NodeType.BlockStmt, range)
        {
            Body = body;
        }
    }

    public class TypeDefinitionStmt : Stmt // TODO: Does this need to be an stmt?
    {
        public TypeDefinitionStmt(Range range, string name, List<Expr> genericParameters) : base(NodeType.TypeDefinitionStmt, range)
        {
            Name = name;
            GenericParameters = genericParameters;
        }

        public string Name { get; }
        public List<Expr> GenericParameters { get; }
    }

    /*public class AnnotationUsageDeclaration : Stmt
    {
        public AstType Type { get; }
        public Expr[] Args { get; }

        public AnnotationUsageDeclaration(Range range, AstType type, Expr[] args) : base(NodeType.AnnotationUsageDeclaration, range)
        {
            Type = type;
            Args = args;
        }
    }*/
    
    public class AnnotationUsageDeclaration : Stmt
    {
        public ObjectConstructorExpr Constructor { get; }

        public AnnotationUsageDeclaration(Range range, ObjectConstructorExpr constructor) : base(NodeType.AnnotationUsageDeclaration, range)
        {
            Constructor = constructor;
        }
    }

    public class VarDeclaration : Stmt
    {
        public AnnotationUsageDeclaration[] AnnotatedWith { get; }
        public bool IsConstant { get; }
        public bool IsStatic { get; }
        public Visibility Visibility { get; }
        public InheritanceModifier? Modifier { get; }
        public bool IsOverride { get; }
        public AstType Type { get; set; }
        public string Identifier { get; }
        public Expr? Value { get; }

        public VarDeclaration(Range range, AnnotationUsageDeclaration[] annotatedWith, bool constant, bool @static, Visibility visibility, InheritanceModifier? modifier, bool @override, AstType type, string identifier, Expr? value) : base(NodeType.VarDeclaration, range)
        {
            AnnotatedWith = annotatedWith;
            IsConstant = constant;
            IsStatic = @static;
            Visibility = visibility;
            Modifier = modifier;
            IsOverride = @override;
            Type = type;
            Identifier = identifier;
            Value = value;
        }
    }

    public class ConstructorDeclaration : Stmt
    {
        public Visibility Visibility { get; }
        public bool IsDestructor { get; }
        public VarDeclaration[] Parameters { get; }
        public string[] Shorthands { get; }
        public Identifier Ident { get; }
        public Stmt[]? Body { get; }

        public ConstructorDeclaration(Range range, Visibility visibility, bool destructor, VarDeclaration[] parameters, string[] shorthands, Identifier ident, Stmt[]? body) : base(NodeType.ConstructorDeclaration, range)
        {
            Visibility = visibility;
            IsDestructor = destructor;
            Parameters = parameters;
            Shorthands = shorthands;
            Ident = ident;
            Body = body;
        }
    }

    public class FunctionDeclaration : Stmt
    {
        public AnnotationUsageDeclaration[] AnnotatedWith { get; }
        public Visibility Visibility { get; }
        public InheritanceModifier? Modifier { get; }
        public bool IsStatic { get; }
        public bool IsOverride { get; }
        public VarDeclaration[] Parameters { get; }
        public string Name { get; }
        public AstType Type { get; }
        public Stmt[]? Body { get; }
        public bool InstantReturn { get; }

        public FunctionDeclaration(Range range, AnnotationUsageDeclaration[] annotatedWith, Visibility visibility, InheritanceModifier? modifier, bool @static, bool @override, VarDeclaration[] parameters, string name, AstType type, Stmt[]? body, bool instantReturn) : base(NodeType.FunctionDeclaration, range)
        {
            AnnotatedWith = annotatedWith;
            Visibility = visibility;
            Modifier = modifier;
            IsStatic = @static;
            IsOverride = @override;
            Parameters = parameters;
            Name = name;
            Type = type;
            Body = body;
            InstantReturn = instantReturn;
            // TODO: Important keywords like `async` etc.
        }
    }

    public class OperatorDeclaration : Stmt
    {
        public string Operator { get; }
        public FunctionDeclaration FunctionDeclaration { get; }

        public OperatorDeclaration(Range range, string @operator, FunctionDeclaration declaration) : base(NodeType.OperatorDeclaration, range)
        {
            Operator = @operator;
            FunctionDeclaration = declaration;
        }
    }

    public class StructDeclaration : Stmt
    {
        public AnnotationUsageDeclaration[] AnnotatedWith { get; }
        public Visibility Visibility { get; }
        public TypeDefinitionStmt Name { get; }
        public Property[] Properties { get; }

        public StructDeclaration(Range range, AnnotationUsageDeclaration[] annotatedWith, Visibility visibility, TypeDefinitionStmt name, Property[] properties) : base(NodeType.StructDeclaration, range)
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
        public TypeDefinitionStmt Name { get; }
        public Expr[] Derivants { get; }
        public Stmt[] Body { get; }

        public ClassDeclaration(Range range, AnnotationUsageDeclaration[] annotatedWith, Visibility visibility, bool @abstract, TypeDefinitionStmt name, Expr[] derivants, Stmt[] body) : base(NodeType.ClassDeclaration, range)
        {
            AnnotatedWith = annotatedWith;
            Visibility = visibility;
            IsAbstract = @abstract;
            Name = name;
            Derivants = derivants;
            Body = body;
        }
    }

    public class EnumDeclaration : Stmt
    {
        public AnnotationUsageDeclaration[] AnnotatedWith { get; }
        public Visibility Visibility { get; }
        public TypeDefinitionStmt Name { get; }
        public string[] Entries { get; }

        public EnumDeclaration(Range range, AnnotationUsageDeclaration[] annotatedWith, Visibility visibility, TypeDefinitionStmt name, string[] entries) : base(NodeType.EnumDeclaration, range)
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

        public ReturnDeclaration(Range range, Expr value) : base(NodeType.ReturnDeclaration, range)
        {
            Value = value;
        }
    }

    public class DeleteDeclaration : Stmt
    {
        public string Value { get; }

        public DeleteDeclaration(Range range, string value) : base(NodeType.DeleteDeclaration, range)
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

        public IfElseDeclaration(Range range, IfBlock[] blocks, Stmt? elseBody) : base(NodeType.IfElseDeclaration, range)
        {
            Blocks = blocks;
            ElseBody = elseBody;
        }
    }

    public class WhileDeclaration : Stmt
    {
        public Expr Condition { get; }
        public Stmt Body { get; }

        public WhileDeclaration(Range range, Expr condition, Stmt body) : base(NodeType.WhileDeclaration, range)
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

        public ForDeclaration(Range range, Stmt declaration, Expr condition, Expr action, Stmt body) : base(NodeType.ForDeclaration, range)
        {
            Declaration = declaration;
            Condition = condition;
            Action = action;
            Body = body;
        }
    }

    public abstract class Expr : Stmt
    {
        public Expr(NodeType kind, Range range) : base(kind, range) { }
    }

    public class AssignmentExpr : Expr
    {
        public Expr Assigne { get; }
        public Expr Value { get; }

        public AssignmentExpr(Range range, Expr assigne, Expr value) : base(NodeType.AssignmentExpr, range) {
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

        public EqualityCheckExpr(Range range, Expr left, Expr right, Type @operator) : base(NodeType.EqualityCheckExpr, range)
        {
            Left = left;
            Right = right;
            Operator = @operator;
        }
    }

    public class BinaryExpr : Expr
    {
        public Expr Left { get; }
        public Expr Right { get; }
        public string Operator { get; }

        public BinaryExpr(Range range, Expr left, Expr right, string @operator) : base(NodeType.BinaryExpr, range)
        {
            Left = left;
            Right = right;
            Operator = @operator;
        }
    }

    public class IndexExpr : Expr
    {
        public Expr Arg { get; }
        public Expr Caller { get; }

        public IndexExpr(Range range, Expr arg, Expr calle) : base(NodeType.IndexExpr, range)
        {
            Caller = calle;
            Arg = arg;
        }
    }

    public class CallExpr : Expr
    {
        public List<Expr> Args { get; }
        public Expr Caller { get; }

        public CallExpr(Range range, List<Expr> args, Expr calle) : base(NodeType.CallExpr, range)
        {
            Caller = calle;
            Args = args;
        }
    }

    public class GenericArgsExpr : Expr
    {
        public List<Expr> Args { get; }
        public Expr Caller { get; }

        public GenericArgsExpr(Range range, List<Expr> args, Expr calle) : base(NodeType.CallExpr, range)
        {
            Caller = calle;
            Args = args;
        }
    }

    public class ObjectConstructorExpr : Expr
    {
        public AstType? Type { get; }
        public Expr[] Args { get; }

        public ObjectConstructorExpr(Range range, AstType? type, Expr[] args) : base(NodeType.ObjectConstructorExpr, range)
        {
            Type = type;
            Args = args;
        }
    }

    public class ObjectArrayConstructorExpr : Expr
    {
        public Expr? Type { get; }
        public Expr[] Items { get; }

        public ObjectArrayConstructorExpr(Range range, Expr? type, Expr[] items) : base(NodeType.ObjectArrayConstructorExpr, range)
        {
            Type = type;
            Items = items;
        }
    }

    public class MemberExpr : Expr
    {
        public Expr Left { get; }
        public Expr Right { get; }

        public MemberExpr(Range range, Expr left, Expr right) : base(NodeType.MemberExpr, range)
        {
            Left = left;
            Right = right;
        }
    }

    public class ResolutionExpr : Expr
    {
        public Expr Left { get; }
        public Expr Right { get; }

        public ResolutionExpr(Range range, Expr left, Expr right) : base(NodeType.ResolutionExpr, range)
        {
            Left = left;
            Right = right;
        }
    }

    public class UnaryExpr : Expr
    {
        public Expr Object { get; }
        public string Operator { get; }

        public UnaryExpr(Range range, Expr @object, string @operator) : base(NodeType.UnaryExpr, range)
        {
            Object = @object;
            Operator = @operator;
        }
    }

    public class LambdaExpr : Expr
    {
        public AnnotationUsageDeclaration[] AnnotatedWith { get; }
        public VarDeclaration[] Parameters { get; }
        public DualType<Expr?, Type> ReturnType { get; }
        public Stmt[] Body { get; }
        public bool IsExpressionLambda { get; }

        public LambdaExpr(Range range, AnnotationUsageDeclaration[] annotatedWith, VarDeclaration[] parameters, DualType<Expr?, Type> returnType, Stmt[] body, bool isExpressionLambda) : base(NodeType.LambdaExpr, range)
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

        public Identifier(Range range, string symbol) : base(NodeType.Identifier, range) { Symbol = symbol; }
    }

    public class NumericLiteral : Expr
    {
        public int Value { get; }

        public NumericLiteral(Range range, int value) : base(NodeType.NumericLiteral, range) { Value = value; }
    }

    public class FloatLiteral : Expr
    {
        public float Value { get; }

        public FloatLiteral(Range range, float value) : base(NodeType.FloatLiteral, range) { Value = value; }
    }

    public class DoubleLiteral : Expr
    {
        public double Value { get; }

        public DoubleLiteral(Range range, double value) : base(NodeType.DoubleLiteral, range) { Value = value; }
    }

    public class StringLiteral : Expr
    {
        public string Value { get; }

        public StringLiteral(Range range, string value) : base(NodeType.StringLiteral, range) { Value = value; }
    }

    public class CharLiteral : Expr
    {
        public char Value { get; }

        public CharLiteral(Range range, char value) : base(NodeType.CharLiteral, range) { Value = value; }
    }

    public class Property : Expr // TODO: Maybe not expr if used only in structs
    {
        public string Key { get; }
        public AstType Type { get; }
        public Expr? Value { get; }

        public Property(Range range, string key, AstType type, Expr? value) : base(NodeType.Property, range) 
        {
            Key = key;
            Type = type;
            Value = value;
        }
    }
}