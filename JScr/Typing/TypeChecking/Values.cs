using JScr.Typing;
using static JScr.Frontend.Ast;

namespace JScr.Typing.TypeChecking;

// TODO: Type comparisons and stuff
// TODO: Separation between symbols and vals, maybe vals having only a string to find type (like "$TJScr::Core::Int64") and symbols holding the envs and other stuff (in Environment). Maybe check old interpreter code.
// TODO: Fully make the AST typed in the type checker. [AstType] Expr -> Type/StaticTypeSymbolVal. In the interpreter, the only thing needed about types is really their path ig (same path == same type).

public readonly struct QualifiedName : IEquatable<QualifiedName>
{
    public const string NAMESPACE_SEPARATOR = "::";
    
    public static implicit operator string[](QualifiedName d) => d.Name;

    public static string ValidatePart(string input)
    {
        var valid = input.Replace(" ", "").Trim();
        return valid == string.Empty ? "<ErrorType>" : valid;
    }
    
    public readonly string[] Name;

    public QualifiedName(string[] name)
    {
        var validated = new string[name.Length];

        for (var i = 0; i < name.Length; i++)
            validated[i] = ValidatePart(name[i]);
        
        Name = validated;
    }
    
    public QualifiedName(string path)
    {
        Name = path.Split(NAMESPACE_SEPARATOR);
        
        for (var i = 0; i < Name.Length; i++)
            Name[i] = ValidatePart(Name[i]);
    }

    public QualifiedName Append(QualifiedName other)
    {
        var result = new string[Name.Length + other.Name.Length];
        Name.AsSpan().CopyTo(result);
        other.Name.AsSpan().CopyTo(result.AsSpan(Name.Length));
        return new QualifiedName(result);
    }
    
    public QualifiedName Append(string other)
    {
        var result = new string[Name.Length + 1];
        Name.AsSpan().CopyTo(result);
        result[^1] = other;
        return new QualifiedName(result);
    }
    
    public QualifiedName Trim(int length)
    {
        return new QualifiedName(Name[..length]);
    }
    
    public override string ToString() => string.Join(NAMESPACE_SEPARATOR, Name);

    public bool Equals(QualifiedName other) => Name.Equals(other.Name);

    public override bool Equals(object? obj) => obj is QualifiedName other && Equals(other);

    public override int GetHashCode() => Name.GetHashCode();
}

public abstract class StaticValue
{
    public static bool operator==(StaticValue? a, StaticValue? b) => a?.Equals(b) ?? false;

    public static bool operator !=(StaticValue? a, StaticValue? b) => !a?.Equals(b) ?? false;
    
    // TODO: PROPER TYPE EQUALITY CHECKING
    public abstract QualifiedName Path { get; }
    public abstract Visibility Visibility { get; }
    
    public string Name => Path.Name.Last();
    
    public override string ToString() => Path.ToString();
    
    public override bool Equals(object? obj) => (obj is StaticValue stval && stval.Path.Equals(Path)) || obj is null;

    public override int GetHashCode() => Path.GetHashCode();
}

internal interface IStaticable
{
    public bool IsStatic { get; }
}

internal class NamespaceValue : StaticValue
{
    public override QualifiedName Path { get; }
    public override Visibility Visibility => Visibility.Public;
    public NamespaceEnvironment? Environment { get; internal set; }

    public NamespaceValue(QualifiedName path)
    {
        Path = path;
    }
}

internal class EnumValue : StaticValue
{
    public override QualifiedName Path { get; }
    public override Visibility Visibility { get; }
    public StaticValue[] Annotations { get; }
    public string[] Entries { get; }

    public EnumValue(QualifiedName path, Visibility visibility, StaticValue[] annotations, string[] entries)
    {
        Path = path;
        Visibility = visibility;
        Annotations = annotations;
        Entries = entries;
    }
}

internal class StructValue : StaticValue
{
    public override QualifiedName Path { get; }
    public override Visibility Visibility { get; }
    public StaticValue[] Annotations { get; }
    public StructEnvironment? MemberEnvironment { get; internal set; }
    public StructEnvironment? StaticEnvironment { get; internal set; }

    public StructValue(QualifiedName path, Visibility visibility, StaticValue[] annotations)
    {
        Path = path;
        Visibility = visibility;
        Annotations = annotations;
    }
}

internal class ClassValue : StaticValue
{
    public override QualifiedName Path { get; }
    public override Visibility Visibility { get; }
    public StaticValue[] Annotations { get; }
    public StaticValue[] Derivants { get; }
    public MemberEnvironment? MemberEnvironment { get; internal set; }
    public ClassEnvironment? StaticEnvironment { get; internal set; }

    public ClassValue(QualifiedName path, Visibility visibility, StaticValue[] annotations, StaticValue[] derivants)
    {
        Path = path;
        Visibility = visibility;
        Annotations = annotations;
        Derivants = derivants;
    }
}

internal class VariableValue : StaticValue, IStaticable
{
    public override QualifiedName Path { get; }
    public override Visibility Visibility { get; }

    public StaticValue Type { get; }
    public StaticValue[] Annotations { get; }
    public bool IsConst { get; }
    public bool IsStatic { get; }
    public bool IsOverride { get; }
    public InheritanceModifier? Modifier { get; }

    public VariableValue(
        QualifiedName path,
        Visibility visibility,
        StaticValue type,
        StaticValue[] annotations,
        bool isConst,
        bool isStatic,
        InheritanceModifier? modifier = null,
        bool isOverride = false)
    {
        Path = path;
        Visibility = visibility;
        Type = type;
        Annotations = annotations;

        IsConst = isConst;
        IsStatic = isStatic;
        Modifier = modifier;
        IsOverride = isOverride;
    }
}


internal class FunctionValue : StaticValue, IStaticable
{
    public override QualifiedName Path { get; }
    public override Visibility Visibility { get; }
    public StaticValue Type { get;  }
    public StaticValue[] Annotations { get; }
    public InheritanceModifier? Modifier { get; }
    public bool IsStatic { get; }
    public bool IsOverride { get; }
    public StaticValue[] Parameters { get; }
    private FunctionEnvironment? _body;
    public FunctionEnvironment? Body { get => Modifier != InheritanceModifier.Abstract ? _body : null; internal set => _body = value; }
    
    public FunctionValue(QualifiedName path, Visibility visibility, StaticValue type, StaticValue[] annotations, InheritanceModifier? modifier, bool isStatic, bool isOverride, StaticValue[] parameters)
    {
        Path = path;
        Visibility = visibility;
        Type = type;
        Annotations = annotations;
        Modifier = modifier;
        IsStatic = isStatic;
        IsOverride = isOverride;
        Parameters = parameters;
    }
}


/*internal static class Values
{
    public abstract class StaticVal
    {
        public override string ToString() => "<unspecified_value>";
    }

    /// <summary>Every defined type is an instance of this class. When type checking a type, like "string", it should return a value that is an instance of a StaticTypeSymbolVal.</summary>
    public abstract class StaticTypeSymbolVal : StaticVal
    {
        public string Name { get; }
        public List<StaticTypeSymbolVal> GenericParams { get; }
        public Visibility Visibility { get; }
        public List<StaticTypeSymbolVal> AnnotatedWith { get; }

        public StaticTypeSymbolVal(string name, List<StaticTypeSymbolVal> genericParams, Visibility visibility, List<StaticTypeSymbolVal> annotatedWith)
        {
            Name = name;
            GenericParams = genericParams;
            Visibility = visibility;
            AnnotatedWith = annotatedWith;
        }
    }

    public class ClassVal : StaticTypeSymbolVal
    {
        public List<StaticTypeSymbolVal> Derivants { get; }
        public Environment ClassEnv { get; }

        public ClassVal(string name, List<StaticTypeSymbolVal> genericParams, Visibility visibility, List<StaticTypeSymbolVal> annotatedWith, List<StaticTypeSymbolVal> derivants, Environment classEnv) : base(name, genericParams, visibility, annotatedWith)
        {
            Derivants = derivants;
            ClassEnv = classEnv;
        }
    }

    public class StructVal : StaticTypeSymbolVal
    {
        public class Property
        {
            public string Key { get; }
            public StaticTypeSymbolVal Type { get; }
            public StaticVal Value { get; }

            public Property(string key, StaticTypeSymbolVal type, StaticVal value)
            {
                Key = key;
                Type = type;
                Value = value;
            }
        }

        public List<Property> Properties { get; }

        public StructVal(string name, List<StaticTypeSymbolVal> genericParams, Visibility visibility, List<StaticTypeSymbolVal> annotatedWith, List<Property> properties) : base(name, genericParams, visibility, annotatedWith)
        {
            Properties = properties;
        }
    }

    public class EnumVal : StaticTypeSymbolVal
    {
        public string[] Entries { get; }

        public EnumVal(string name, Visibility visibility, List<StaticTypeSymbolVal> annotatedWith, string[] entries) : base(name, new(), visibility, annotatedWith)
        {
            Entries = entries;
        }
    }

    public class FunctionVal : StaticVal
    {
        public string Name { get; }
        public StaticTypeSymbolVal Type { get; }
        public Visibility Visibility { get; }
        public List<StaticTypeSymbolVal> AnnotatedWith { get; }
        public InheritanceModifier? Modifier { get; }
        public bool IsOverride { get; }
        public VarDeclaration[] Parameters { get; }
        public Stmt[]? Body { get; }

        public FunctionVal(string name, StaticTypeSymbolVal type, Visibility visibility, List<StaticTypeSymbolVal> annotatedWith, InheritanceModifier? modifier, bool isOverride, VarDeclaration[] parameters, Stmt[]? body)
        {
            Name = name;
            Type = type;
            Visibility = visibility;
            AnnotatedWith = annotatedWith;
            Modifier = modifier;
            IsOverride = isOverride;
            Parameters = parameters;
            Body = body;
        }
    }

    public class StaticModuleVal : StaticVal
    {
        public List<string> Name { get; }
        public Module Module { get; }

        public StaticModuleVal(List<string> name, Module module)
        {
            Name = name;
            Module = module;
        }
    }

    /*
    public class TypeVal : StaticVal
    {
        public TypeVal()
        {

        }

        public string Identifier { get; private set; }
        public List<TypeVal> Types { get; private set; }
    }*/

/*public class NullVal : StaticVal
{

}

public class ErrorVal : StaticVal
{
    public override string ToString() => "<error_value>";
}
/*
public class CharLiteralValue : StaticVal<char>
{
    public CharLiteralValue(char value) : base(value) {}

    internal override Type Type { get => new SimpleType("char"); }
}

public class IntLiteralValue : StaticVal<int>
{
    public IntLiteralValue(int value) : base(value) { }

    internal override Type Type { get => new SimpleType("int"); }
}*//*
}*/