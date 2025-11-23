using JScr.Frontend;
using static JScr.Frontend.Ast;

namespace JScr.Typing.TypeChecking;

internal enum ScopeType : byte
{
    Global,
    Class,
    Method,
}
/*
internal abstract class Symbol
{
    public abstract string Identifier { get; }
    public abstract Visibility Visibility { get; }
    public abstract List<StaticTypeSymbolVal> AnnotatedWith { get; }
}

// variable
internal class VariableSymbol : Symbol
{
    public VariableSymbol(string identifier, StaticTypeSymbolVal type, Visibility visibility, List<StaticTypeSymbolVal> annotatedWith, InheritanceModifier? modifier, bool isOverride, bool isConstant)
    {
        Identifier = identifier;
        Type = type;
        Visibility = visibility;
        AnnotatedWith = annotatedWith;
        Modifier = modifier;
        IsOverride = isOverride;
        IsConstant = isConstant;
    }

    public override string Identifier { get; }
    public StaticTypeSymbolVal Type { get; }
    public override Visibility Visibility { get; }
    public override List<StaticTypeSymbolVal> AnnotatedWith { get; }
    public InheritanceModifier? Modifier { get; }
    public bool IsOverride { get; }
    public bool IsConstant { get; }
}

// function
internal class FunctionSymbol : Symbol
{
    public FunctionSymbol(FunctionVal functionVal)
    {
        Identifier = functionVal.Name;
        Visibility = functionVal.Visibility;
        AnnotatedWith = functionVal.AnnotatedWith;
        FunctionValue = functionVal;
    }

    public override string Identifier { get; }
    public override Visibility Visibility { get; }
    public override List<StaticTypeSymbolVal> AnnotatedWith { get; }
    public FunctionVal FunctionValue { get; }
}

// TODO: THIS SHOULD HAVE THE ENVIRONMENT INSTEAD OF StaticTypeSymbolVal.
// TODO: MAKE StaticTypeSymbolVal Type INSTEAD!!! Type SHOULD JUST BE A PATH I GUESS. EVERY FUNCTION IN THE TYPECHECKER SHOULD RETURN A TYPE.
// TODO: OR RETURN SYMBOL STRAIGHT AWAY FROM ALL TYPECHECKER FUNCTIONS.
// class, struct, enum
internal class DatatypeSymbol : Symbol
{
    public readonly static DatatypeSymbol errorType = new();

    public DatatypeSymbol(string identifier, Visibility visibility)
    {
        Identifier = identifier;
        Visibility = visibility;
        AnnotatedWith = datatypeSymbolValue.AnnotatedWith;
        DatatypeSymbolValue = datatypeSymbolValue;
    }

    public override string Identifier { get; }
    public override Visibility Visibility { get; }
    public override List<StaticTypeSymbolVal> AnnotatedWith { get; }
    public StaticTypeSymbolVal DatatypeSymbolValue { get; }
}*/

internal enum SymbolKind // <-- TODO: Can remove
{
    Namespace,
    Class,
    Struct,
    Enum,
    Function,
    Variable,
}

internal class SymbolInfo
{
    public SymbolKind Kind { get; }
    public StaticValue Type { get; }

    public SymbolInfo(SymbolKind kind, StaticValue type)
    {
        Kind = kind;
        Type = type;
    }
}

internal abstract class Environment
{
    public readonly Environment? Parent;
    public QualifiedName Path { get; protected set; }
    public readonly string FilePath;
    public readonly string FullLibDir;
    protected readonly Action<SyntaxError> ErrorCallback;
    private readonly HashSet<Environment> _importedEnvs = [];
    private readonly Dictionary<QualifiedName, SymbolInfo> _symbols = new();
    
    private SymbolInfo? _voidType;
    private SymbolInfo? _boolType;
    private SymbolInfo? _int32Type;
    private SymbolInfo? _stdAnnotationType;

    protected Environment(Environment? parent, Action<SyntaxError>? errorCallback, string? filePath, string? fullLibDir)
    {
        if (parent == null && errorCallback == null)
            throw new InvalidOperationException("errorCallback cannot be null for the root environment.");
        
        if (parent == null && filePath == null)
            throw new InvalidOperationException("filePath cannot be null for the root environment.");
        
        if (parent == null && fullLibDir == null)
            throw new InvalidOperationException("fullLibDir cannot be null for the root environment.");
        
        Parent = parent;
        ErrorCallback = errorCallback ?? parent!.ErrorCallback;
        FilePath = filePath ?? parent!.FilePath;
        FullLibDir = fullLibDir ?? parent!.FullLibDir;
    }
    
    public void ThrowTypeError(Stmt astNode, SyntaxErrorData data)
    {
        ErrorCallback(new SyntaxError(FilePath, astNode.Range, data));
    }
    
    public void AddImport(Stmt astNode, NamespaceEnvironment env)
    {
        if (this is not FileEnvironment && Parent != null)
        {
            Parent.AddImport(astNode, env);
            return;
        }
        else if (this is FileEnvironment)
        {
            _importedEnvs.Add(env); // TODO: error if exists already
        }
        
        ErrorCallback(new SyntaxError(FilePath, astNode.Range, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Import statement cannot be used here.")));
        
        /*
        JScrLibrary.GetProjectRootStructure(root, out _, out var srcdir, out var libdata);

        var libraryFile = File.ReadAllText(libdata);
        library = JsonConvert.DeserializeObject<JScrLibrary>(libraryFile);

        var data = File.ReadAllText(Path.Combine(srcdir, library.Entry));
        program = Parser.ProduceAST(root, data, errorCallback);
        TypeChecker.CheckProgramTypes(program, errorCallback, out _);*/
    }
    
    // TODO: All namespaces should be available even though they are not in _importedEnvs. Contents of imported namespaces should be available as if it was inside of this environment.
    public void DeclareSymbol(Stmt astNode, SymbolInfo data)
    {
        if (_symbols.ContainsKey(data.Type.Path))
            ErrorCallback(new SyntaxError(FilePath, astNode.Range, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"A symbol `{data.Type.Path}` already exists.")));

        _symbols[data.Type.Path] = data;
    }
    
    public void DeclareOrFindSymbolFromThisScope(SymbolInfo data, out SymbolInfo symbolInfo)
    {
        if (_symbols.TryGetValue(data.Type.Path, out var symbol))
        {
            symbolInfo = symbol;
            return;
        }

        _symbols[data.Type.Path] = data;
        symbolInfo = data;
    }

    public SymbolInfo? LookupPrimitiveVoid(Stmt astNode)
    {
        _voidType ??= LookupSymbol(Types.VoidType(), astNode);
        return _voidType;
    }
    
    public SymbolInfo? LookupPrimitiveBoolean(Stmt astNode)
    {
        _boolType ??= LookupSymbol(Types.BooleanType(), astNode);
        return _boolType;
    }
    
    public SymbolInfo? LookupPrimitiveInt32(Stmt astNode)
    {
        _int32Type ??= LookupSymbol(Types.Int32Type(), astNode);
        return _int32Type;
    }
    
    public SymbolInfo? LookupStdAnnotation(Stmt astNode)
    {
        _stdAnnotationType ??= LookupSymbol(Types.StdAnnotationType(), astNode);
        return _stdAnnotationType;
    }
    
    // TODO: QualifiedName should not need to be a full name for imported namespaces.
    public SymbolInfo? LookupSymbol(QualifiedName name, Stmt astNode, bool noError = false)
    {
        var env = ResolveSymbol(name, astNode, noError);
        return env?._symbols[name];
    }

    public Environment? ResolveSymbol(QualifiedName name, Stmt astNode, bool noError = false)
    {
        var ri = ResolveSymbolInImports(name);

        if (ri != null)
            return ri;

        var rp = ResolveSymbolInParents(name);

        if (rp != null)
            return rp;

        if (!noError) ErrorCallback(new SyntaxError(FilePath, astNode.Range, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Cannot resolve symbol with name `{name}`, it does not exist.")));
        return null;
    }

    public Environment? ResolveSymbolInParents(QualifiedName name)
    {
        if (_symbols.ContainsKey(name))
            return this;

        if (Parent == null)
            return null;

        return Parent.ResolveSymbolInParents(name);
    }

    public Environment? ResolveSymbolInImports(QualifiedName name)
    {
        foreach (var import in _importedEnvs)
        {
            bool v = import._symbols.ContainsKey(name) && import._symbols[name].Type.Visibility == Visibility.Public;
            if (v)
                return import;
        }

        return null;
    }
}

internal class RootEnvironment : Environment
{
    public RootEnvironment(Action<SyntaxError> errorCallback) : base(null, errorCallback, null, null)
    {
        
    }
}

internal class GlobalEnvironment : Environment
{
    public GlobalEnvironment(RootEnvironment parent, string filePath, string fullLibDir) : base(parent, null, filePath, fullLibDir)
    {
        
    }
}

internal class NamespaceEnvironment : Environment
{
    public readonly NamespaceValue NamespaceVal;
    
    public NamespaceEnvironment(Environment parent, NamespaceValue namespaceVal) : base(parent, null, null, null)
    {
        NamespaceVal = namespaceVal;

        if (parent is not GlobalEnvironment && parent is not NamespaceEnvironment)
            throw new Exception("NamespaceEnvironment can only be placed inside a GlobalEnvironment or a NamespaceEnvironment! Make sure to check for this before trying to create an environment.");
    }
}

internal class FileEnvironment : Environment
{
    public FileEnvironment(NamespaceEnvironment parent, string filePath) : base(parent, null, filePath, null)
    {

    }
}

internal class ClassEnvironment : Environment
{
    public readonly ClassValue ClassVal;
    
    public ClassEnvironment(Environment parent, ClassValue classVal) : base(parent, null, null, null)
    {
        Path = classVal.Path;
        ClassVal = classVal;
        
        if (parent is not FileEnvironment && parent is not ClassEnvironment)
            throw new Exception("ClassEnvironment can only be placed inside a FileEnvironment or a ClassEnvironment! Make sure to check for this before trying to create an environment.");
    }
}

internal class MemberEnvironment : Environment
{
    public MemberEnvironment(ClassEnvironment parent) : base(parent, null, null, null)
    {

    }
}

internal class StructEnvironment : Environment
{
    public readonly StructValue StructVal;
    
    public StructEnvironment(Environment parent, StructValue structVal) : base(parent, null, null, null)
    {
        Path = structVal.Path;
        StructVal = structVal;
        
        if (parent is not FileEnvironment && parent is not ClassEnvironment)
            throw new Exception("StructEnvironment can only be placed inside a FileEnvironment or a ClassEnvironment! Make sure to check for this before trying to create an environment.");
    }
}

internal class FunctionEnvironment : Environment
{
    public readonly FunctionValue FunctionVal;
    
    public FunctionEnvironment(Environment parent, FunctionValue functionVal) : base(parent, null, null, null)
    {
        Path = functionVal.Path;
        FunctionVal = functionVal;
        
        if (parent is not ClassEnvironment && parent is not StructEnvironment && parent is not FunctionEnvironment && parent is not FileEnvironment)
            throw new Exception("FunctionEnvironment can only be placed inside a ClassEnvironment, StructEnvironment, FunctionEnvironment or a FileEnvironment! Make sure to check for this before trying to create an environment.");
    }
}


/*
internal class Environment
{
    public static Environment Global(
        string fullLibDir,
        string filedir,
        string libname,
        Library.Dependency[] dependencies,
        Action<SyntaxError> errorCallback) => new(
        null,
        ScopeType.Global,
        fullLibDir,
        filedir,
        libname,
        dependencies,
        errorCallback);

    public static Environment Sub(Environment parent, ScopeType scopeType) => new(
        parent,
        scopeType,
        parent.fullLibDir,
        parent.filedir,
        parent.libname,
        parent.dependencies,
        parent.errorCallback);

    public Environment(
        Environment? parent,
        ScopeType scopeType = ScopeType.Global,
        string? fullLibDir = null,
        string? filedir = null,
        string? libname = null,
        Library.Dependency[]? dependencies = null,
        Action<SyntaxError>? errorCallback = null)
    {
        if (parent != null && scopeType == ScopeType.Global)
            throw new InvalidOperationException("Cannot create global scope if it has a parent scope!");

        if (parent == null && fullLibDir == null)
            throw new InvalidOperationException("fullLibDir cannot be null for the root environment.");

        if (parent == null && filedir == null)
            throw new InvalidOperationException("filedir cannot be null for the root environment.");

        if (parent == null && libname == null)
            throw new InvalidOperationException("libname cannot be null for the root environment.");

        if (parent == null && dependencies == null)
            throw new InvalidOperationException("dependencies cannot be null for the root environment.");

        if (parent == null && errorCallback == null)
            throw new InvalidOperationException("Error callback cannot be null for the root environment.");

        this.parent = parent;
        this.scopeType = scopeType;
        this.fullLibDir = fullLibDir ?? parent!.fullLibDir;
        this.filedir = filedir ?? parent!.filedir;
        this.libname = libname ?? parent!.libname;
        this.dependencies = dependencies ?? parent!.dependencies;
        this.errorCallback = errorCallback ?? parent!.errorCallback;

        importedEnvs = parent == null ? new() : parent.importedEnvs;
        symbols = new();
    }

    public readonly Environment? parent;
    public readonly ScopeType scopeType;
    public readonly string fullLibDir;
    public readonly string filedir;
    public readonly string libname;
    public readonly Library.Dependency[] dependencies;
    private readonly Action<SyntaxError> errorCallback;

    // NOTE: imported envs do not have this environment as their parent
    private readonly List<Environment> importedEnvs;

    private readonly Dictionary<string, Symbol> symbols;

    public void ThrowTypeError(Stmt astNode, SyntaxErrorData data)
    {
        errorCallback(new(filedir, astNode.Range, data));
    }
    // TODO
    public void AddImport(ImportStmt importStmt)
    {
        if (parent != null)
        {
            parent.AddImport(importStmt);
            return;
        }
        /*
        JScrLibrary.GetProjectRootStructure(root, out _, out var srcdir, out var libdata);

        var libraryFile = File.ReadAllText(libdata);
        library = JsonConvert.DeserializeObject<JScrLibrary>(libraryFile);

        var data = File.ReadAllText(Path.Combine(srcdir, library.Entry));
        program = Parser.ProduceAST(root, data, errorCallback);
        TypeChecker.CheckProgramTypes(program, errorCallback, out _);*/
    /*}

    public bool TryFindModule(List<string> modulePath, out Module? module)
    {
        module = null;
        if (modulePath.Count == 0)
            return false;

        Library.GetProjectRootStructure(fullLibDir, out var libsdir, out var srcdir, out _);

        bool isLocal = modulePath.First() == libname;

        try
        {
            if (isLocal)
            {
                List<string> pathToCombine = modulePath.GetRange(1, modulePath.Count - 1);
                string combinedPath = Path.Combine(srcdir, Path.Combine(pathToCombine.ToArray()));

                // Check if it's a file (.jscr extension)
                string combinedFilePath = combinedPath + ".jscr";
                if (File.Exists(combinedFilePath))
                {
                    module = new(ModuleType.File, combinedFilePath);
                    return true;
                }

                // Check if it's a directory
                if (Directory.Exists(combinedPath))
                {
                    module = new(ModuleType.Directory, combinedPath);
                    return true;
                }

                return false;
            }
            else
            {
                throw new NotImplementedException();
                // TODO: REVIEW THIS CODE

                var otherLibName = modulePath.First();
                var dep = dependencies.FirstOrDefault(x => Path.GetFileName(Path.GetDirectoryName(x.Path)) == otherLibName);

                if (dep.Equals(default(Library.Dependency)))
                    return false;

                Library.GetProjectRootStructure(Path.Combine(libsdir, dep.Path), out _, out var depSourcedir, out var libdata);
#if false
                    Library.DeserializeLibraryJson(libdata, out var library);
#endif
                List<string> pathToCombine = modulePath.GetRange(1, modulePath.Count - 1);
                string combinedPath = Path.Combine(depSourcedir, Path.Combine(pathToCombine.ToArray()));

                // Check if it's a file (.jscr extension)
                string combinedFilePath = combinedPath + ".jscr";
                if (File.Exists(combinedFilePath))
                {
                    module = new(ModuleType.File, combinedFilePath);
                    return true;
                }

                // Check if it's a directory
                if (Directory.Exists(combinedPath))
                {
                    module = new(ModuleType.Directory, combinedPath);
                    return true;
                }

                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    public void DeclareSymbol(Stmt astNode, Symbol data)
    {
        if (symbols.ContainsKey(data.Identifier))
            errorCallback(new(filedir, astNode.Range, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"A symbol with the name `{data.Identifier}` already exists.")));

        symbols[data.Identifier] = data;
    }

    public Symbol LookupSymbol(string name, Stmt astNode)
    {
        var env = ResolveSymbol(name, astNode);
        return env.symbols[name];
    }

    public DatatypeSymbol LookupDatatypeSymbol(string name, Stmt astNode)
    {
        var env = ResolveSymbol(name, astNode);
        var res = env.symbols[name];

        if (res is DatatypeSymbol)
            return (DatatypeSymbol)res;

        errorCallback(new(filedir, astNode.Range, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Symbol with name `{name}` is not a class, struct or enum declaration.")));
        throw new Exception("Unhandled error.");
    }

    public Environment ResolveSymbol(string name, Stmt astNode)
    {
        var ri = ResolveSymbolInImports(name);

        if (ri != null)
            return ri;

        var rp = ResolveSymbolInParents(name);

        if (rp != null)
            return rp;

        errorCallback(new(filedir, astNode.Range, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Cannot resolve symbol with name `{name}`, it does not exist.")));
        throw new Exception("Unhandled error.");
    }

    public Environment? ResolveSymbolInParents(string name)
    {
        if (symbols.ContainsKey(name))
            return this;

        if (parent == null)
            return null;

        return parent.ResolveSymbolInParents(name);
    }

    public Environment? ResolveSymbolInImports(string name)
    {
        foreach (var import in importedEnvs)
        {
            bool v = import.symbols.ContainsKey(name) && import.symbols[name].Visibility == Visibility.Public;
            if (v)
                return import;
        }

        return null;
    }
}
*/
// TODO: Support env types:
// Namespace
// File
// Datatype
// Method
/*
internal class EnvironmentOld
{
    public static EnvironmentOld Global(
        string fullLibDir,
        string filedir,
        string libname,
        Library.Dependency[] dependencies,
        Action<SyntaxError> errorCallback) => new(
        null,
        ScopeType.Global,
        fullLibDir,
        filedir,
        libname,
        null,
        dependencies,
        errorCallback);

    public static EnvironmentOld Sub(EnvironmentOld parent, ScopeType scopeType, string name) => new(
        parent,
        scopeType,
        parent.fullLibDir,
        parent.filedir,
        parent.libname,
        name,
        parent.dependencies,
        parent.errorCallback);

    public EnvironmentOld(
        EnvironmentOld? parent,
        ScopeType scopeType = ScopeType.Global,
        string? fullLibDir = null,
        string? filedir = null,
        string? libname = null,
        string? envname = null,
        Library.Dependency[]? dependencies = null,
        Action<SyntaxError>? errorCallback = null)
    {
        if (parent != null && scopeType == ScopeType.Global)
            throw new InvalidOperationException("Cannot create global scope if it has a parent scope!");

        if (parent == null && fullLibDir == null)
            throw new InvalidOperationException("fullLibDir cannot be null for the root environment.");

        if (parent == null && filedir == null)
            throw new InvalidOperationException("filedir cannot be null for the root environment.");

        if (parent == null && libname == null)
            throw new InvalidOperationException("libname cannot be null for the root environment.");

        if (parent == null && dependencies == null)
            throw new InvalidOperationException("dependencies cannot be null for the root environment.");

        if (parent == null && errorCallback == null)
            throw new InvalidOperationException("Error callback cannot be null for the root environment.");
        
        if (scopeType == ScopeType.Global && envname != null)
            throw new InvalidOperationException("envname cannot be set for a global environment. Use libname instead.");
        else if (envname == null)
            throw new InvalidOperationException("envname cannot be null for a class or method environment.");
        
        envname = libname;
        this.envname = envname;
        if (parent == null)
        {
            Path = new QualifiedName(envname!);
        }
        else if (parent.Path != null)
        {
            Path = new QualifiedName(envname!);
        }
     // TODO: Do environment naming properly!!!

        this.parent = parent;
        this.scopeType = scopeType;
        this.fullLibDir = fullLibDir ?? parent!.fullLibDir;
        this.filedir = filedir ?? parent!.filedir;
        this.libname = libname ?? parent!.libname;
        this.dependencies = dependencies ?? parent!.dependencies;
        this.errorCallback = errorCallback ?? parent!.errorCallback;

        _importedEnvs = parent == null ? new() : parent._importedEnvs;
        _symbols = new();
    }

    public readonly Environment? parent;
    public readonly ScopeType scopeType;
    public readonly string fullLibDir;
    public readonly string filedir;
    public readonly string libname;
    public readonly string? envname;
    public readonly Library.Dependency[] dependencies;
    public QualifiedName Path { get; private set; }
    private readonly Action<SyntaxError> errorCallback;
    
    private SymbolInfo? _voidType;
    private SymbolInfo? _boolType;
    private SymbolInfo? _int32Type;

    // NOTE: imported envs do not have this environment as their parent
    private readonly List<Environment> _importedEnvs;

    private readonly Dictionary<string, SymbolInfo> _symbols;

    public void ThrowTypeError(Stmt astNode, SyntaxErrorData data)
    {
        errorCallback(new(filedir, astNode.Range, data));
    }
    // TODO
    public void AddImport(ImportStmt importStmt)
    {
        if (parent != null)
        {
            parent.AddImport(importStmt);
            return;
        }
        /*
        JScrLibrary.GetProjectRootStructure(root, out _, out var srcdir, out var libdata);

        var libraryFile = File.ReadAllText(libdata);
        library = JsonConvert.DeserializeObject<JScrLibrary>(libraryFile);

        var data = File.ReadAllText(Path.Combine(srcdir, library.Entry));
        program = Parser.ProduceAST(root, data, errorCallback);
        TypeChecker.CheckProgramTypes(program, errorCallback, out _);*/
    /*}

    public void DeclareSymbol(Stmt astNode, SymbolInfo data)
    {
        if (_symbols.ContainsKey(data.Type.Name))
            errorCallback(new(filedir, astNode.Range, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"A symbol with the name `{data.Type.Name}` already exists.")));

        _symbols[data.Type.Name] = data;
    }

    public SymbolInfo? LookupPrimitiveVoid(Stmt astNode)
    {
        _voidType ??= LookupSymbol(Types.VoidType().ToString(), astNode);
        return _voidType;
    }
    
    public SymbolInfo? LookupPrimitiveBoolean(Stmt astNode)
    {
        _boolType ??= LookupSymbol(Types.BooleanType().ToString(), astNode);
        return _boolType;
    }
    
    public SymbolInfo? LookupPrimitiveInt32(Stmt astNode)
    {
        _int32Type ??= LookupSymbol(Types.Int32Type().ToString(), astNode);
        return _int32Type;
    }

    public SymbolInfo? LookupSymbol(string name, Stmt astNode)
    {
        var env = ResolveSymbol(name, astNode);
        return env?._symbols[name];
    }

    public EnvironmentOld? ResolveSymbol(string name, Stmt astNode)
    {
        var ri = ResolveSymbolInImports(name);

        if (ri != null)
            return ri;

        var rp = ResolveSymbolInParents(name);

        if (rp != null)
            return rp;

        errorCallback(new(filedir, astNode.Range, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Cannot resolve symbol with name `{name}`, it does not exist.")));
        return null;
    }

    public EnvironmentOld? ResolveSymbolInParents(string name)
    {
        if (_symbols.ContainsKey(name))
            return this;

        if (parent == null)
            return null;

        return parent.ResolveSymbolInParents(name);
    }

    public EnvironmentOld? ResolveSymbolInImports(string name)
    {
        foreach (var import in _importedEnvs)
        {
            bool v = import._symbols.ContainsKey(name) && import._symbols[name].Type.Visibility == Visibility.Public;
            if (v)
                return import;
        }

        return null;
    }
}*/