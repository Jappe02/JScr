using JScr.Frontend;
using JScr.Utils;
using static JScr.Frontend.Ast;

namespace JScr.Typing.TypeChecking.Check;

// TODO: add ConstructorDeclaration
// TODO: check valid annotation usage (targets on classVal etc.)

internal static class Statements
{
    public static StaticValue? CheckProgram(Program program, GlobalEnvironment env)
    {
        var i = 0;
        foreach (var statement in program.Body)
        {
            if (i == 0)
            {
                if (statement is not NamespaceStmt nm)
                {
                    env.ThrowTypeError(statement, SyntaxErrorData.New(SyntaxErrorLevel.Error, "First statement in a file must be a namespace statement."));
                    i++;
                    continue;
                }

                var namespaceEnvironment = GetOrCreateNamespace(new QualifiedName(nm.Target), env);
                // TODO: FileEnv etc.
            }
            
            var v = TypeChecker.CheckTypes(statement, env);
            i++;
        }
        return null;
    }

    private static NamespaceEnvironment? GetOrCreateNamespace(QualifiedName fullName, GlobalEnvironment assemblyEnv)
    {
        if (fullName.Name.Length == 0) return null;
        
        Environment parent = assemblyEnv;
        for (var i = 0; i < fullName.Name.Length; i++)
        {
            var nmval = new NamespaceValue(fullName.Trim(i + 1));
            var sym = new SymbolInfo(SymbolKind.Namespace, nmval);
            parent.DeclareOrFindSymbolFromThisScope(sym, out sym);
            if (ReferenceEquals(nmval, sym.Type))
            {
                nmval.Environment = new NamespaceEnvironment(parent, nmval);
                parent = nmval.Environment;
            }
        }

        return (NamespaceEnvironment)parent;
    }
    
    public static StaticValue? CheckNamespaceStmt(NamespaceStmt declaration, Environment env)
    {
        return null;
    }
    
    public static StaticValue? CheckImportStmt(ImportStmt declaration, Environment env)
    {
        if (env is not FileEnvironment)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Import statements are not supported within this scope."));
            return null;
        }
        
        var tc = TypeChecker.CheckTypes(declaration.Target, env);

        if (tc is not NamespaceValue namespaceValue)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Imported expression does not evaluate to a namespace."));
            return null;
        }

        env.AddImport(declaration, namespaceValue.Environment);
        return null;
    }

    public static StaticValue? CheckBlockStmt(BlockStmt declaration, Environment env)
    {
        if (env is not FunctionEnvironment)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "A block statement can only be placed inside functions.").AddHint("Try removing the block statement."));
            return null;
        }
        
        foreach (var it in declaration.Body)
            TypeChecker.CheckTypes(it, env);
            
        return null;
    }
    
    public static StaticValue? CheckAnnotationUsageDeclaration(AnnotationUsageDeclaration declaration, Environment env)
    {
        // TODO: Find actual type and check that it is a class and an annotation, check input params just like constructor. Then return type.
        
        var type = TypeChecker.CheckTypes(declaration.Constructor, env);
        var annotationType = env.LookupStdAnnotation(declaration)?.Type;
        if (type == null || type == annotationType) return null;

        if (type is not ClassValue cls)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Annotation must be a class."));
            return null;
        }

        if (!cls.Annotations.Contains(annotationType))
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"The class `{cls}` is not a valid annotation. It must be annotated with `{annotationType}`."));
            return null;
        }

        return null;
    }

    public static StaticValue? CheckVarDeclaration(VarDeclaration declaration, Environment env)
    {
        if (env is not ClassEnvironment && env is not StructEnvironment && env is not FunctionEnvironment && env is not FileEnvironment)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Variables can only be declared in files, classes, structs or functions.").AddHint("Try moving the variable inside a valid scope."));
            return null;
        }
        
        var value = declaration.Value != null ? TypeChecker.CheckTypes(declaration.Value, env) : null;
        var type = TypeChecker.CheckTypes(declaration.Type, env, requireDatatypeDecl: true);
        declaration.Type = new DualType<Expr, StaticValue?>(type);
        
        if (type == null)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Invalid type in variable declaration."));
            return null;
        }

        if (value != type)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "The type of the value does not match declaration type in variable declaration."));
            return null;
        }

        List<StaticValue> annotations = [];
        foreach (var ann in declaration.AnnotatedWith)
        {
            var t = TypeChecker.CheckTypes(ann, env, requireDatatypeDecl: true);
            if (t == null) continue;
            annotations.Add(t);
        }

        env.DeclareSymbol(declaration, new SymbolInfo(
            kind: SymbolKind.Variable,
            type: new VariableValue(
                path: env.Path.Append(declaration.Identifier),
                visibility: declaration.Visibility,
                type: type,
                annotations: annotations.ToArray(),
                isOverride: declaration.IsOverride,
                isConst: declaration.IsConstant,
                isStatic: declaration.IsStatic,
                modifier: declaration.Modifier
            )
        ));

        return type;
    }

    public static StaticValue? CheckConstructorDeclaration(ConstructorDeclaration declaration, Environment env)
    {
        return null;
    }

    public static StaticValue? CheckFunctionDeclaration(FunctionDeclaration declaration, Environment env)
    {
        if (env is not ClassEnvironment && env is not StructEnvironment && env is not FunctionEnvironment && env is not FileEnvironment)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Functions can only be declared in files, classes or nested inside other functions.").AddHint("Try moving the function inside a valid scope."));
            return null;
        }
        
        if (env is ClassEnvironment cls && !declaration.IsStatic)
            env = cls.ClassVal.MemberEnvironment!;
        
        // Type
        var type = TypeChecker.CheckTypes(declaration.Type, env, requireDatatypeDecl: true);
        if (type == null) return null;
        
        // Annotations
        List<StaticValue> annotations = [];
        foreach (var ann in declaration.AnnotatedWith)
        {
            var t = TypeChecker.CheckTypes(ann, env, requireDatatypeDecl: true);
            if (t == null) continue;
            annotations.Add(t);
        }
        
        // Parameters
        List<StaticValue> parameters = [];
        foreach (var param in declaration.Parameters)
        {
            var t = TypeChecker.CheckTypes(param, env, requireDatatypeDecl: true);
            if (t == null) continue;
            parameters.Add(t);
        }
        
        // Value
        var fnval = new FunctionValue(
            env.Path.Append(declaration.Name),
            declaration.Visibility,
            type,
            annotations.ToArray(),
            declaration.Modifier,
            declaration.IsStatic,
            declaration.IsOverride,
            parameters.ToArray()
        );
        
        // Body
        if (declaration.Modifier != InheritanceModifier.Abstract)
        {
            if (declaration.Body == null)
            {
                env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Non-abstract methods must have a body."));
                return null;
            }
            
            var functionEnv = new FunctionEnvironment(env, fnval);
            fnval.Body = functionEnv;
            foreach (var stmt in declaration.Body!)
            {
                TypeChecker.CheckTypes(stmt, functionEnv);
            }
        }

        env.DeclareSymbol(declaration, new SymbolInfo(SymbolKind.Function, fnval));
        return null;
    }
    
    public static StaticValue? CheckOperatorDeclaration(OperatorDeclaration declaration, Environment env)
    {
        return null;
    }
    
    // TODO: Struct static members?
    public static StaticValue? CheckStructDeclaration(StructDeclaration obj, Environment env)
    {
        if (env is not FileEnvironment && env is not ClassEnvironment)
        {
            env.ThrowTypeError(obj, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Structs can only be declared in files or nested inside classes.").AddHint("Try moving the struct inside a valid scope."));
            return null;
        }
        
        var typename = obj.Name.Name;
        List<StaticValue> genericParams = []; // TODO: MAKE GENERICS WORK!!!
        foreach (var gp in obj.Name.GenericParameters)
        {
            var val = TypeChecker.CheckTypes(gp, env, requireDatatypeDecl: true);
            if (val == null)  continue;
            genericParams.Add(val);
        }

        List<StaticValue> annotations = [];
        foreach (var ann in obj.AnnotatedWith)
        {
            var t = TypeChecker.CheckTypes(ann, env, requireDatatypeDecl: true);
            if (t == null) continue;
            annotations.Add(t);
        }
        
        var structVal = new StructValue(env.Path.Append(typename), obj.Visibility, annotations.ToArray());
        StructEnvironment? memberEnvironment = null;
        if (obj.Properties.Length > 0)
        {
            memberEnvironment = new StructEnvironment(env, structVal);
            foreach (var prop in obj.Properties)
            {
                var key = prop.Key;
                var type = TypeChecker.CheckTypes(prop.Type, env, requireDatatypeDecl: true);
                var value = prop.Value != null ? TypeChecker.CheckTypes(prop.Value, env) : null;
                
                if (type == null)
                {
                    env.ThrowTypeError(prop, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Invalid type in struct property declaration."));
                    continue;
                }

                if (value != type)
                {
                    env.ThrowTypeError(prop, SyntaxErrorData.New(SyntaxErrorLevel.Error, "The type of the value does not match declaration type in struct property declaration."));
                    continue;
                }

                memberEnvironment.DeclareSymbol(prop, new SymbolInfo(
                    kind: SymbolKind.Variable,
                    type: new VariableValue(
                        path: env.Path.Append(key),
                        visibility: Visibility.Public,
                        type: type,
                        annotations: [],
                        isOverride: false,
                        isConst: true,
                        isStatic: false,
                        modifier: null
                    )
                ));
            }
        }
        
        structVal.MemberEnvironment = memberEnvironment;
        env.DeclareSymbol(obj, new SymbolInfo(SymbolKind.Struct, structVal));
        return null;
    }

    public static StaticValue? CheckClassDeclaration(ClassDeclaration obj, Environment env)
    {
        if (env is not FileEnvironment && env is not ClassEnvironment)
        {
            env.ThrowTypeError(obj, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Classes can only be declared in files or nested inside other classes.").AddHint("Try moving the class inside a valid scope."));
            return null;
        }

        var typename = obj.Name.Name;
        List<StaticValue> genericParams = []; // TODO: MAKE GENERICS WORK!!!
        foreach (var gp in obj.Name.GenericParameters)
        {
            var val = TypeChecker.CheckTypes(gp, env, requireDatatypeDecl: true);
            if (val == null)  continue;
            genericParams.Add(val);
        }

        List<StaticValue> annotations = [];
        foreach (var ann in obj.AnnotatedWith)
        {
            var t = TypeChecker.CheckTypes(ann, env, requireDatatypeDecl: true);
            if (t == null) continue;
            annotations.Add(t);
        }
        
        List<StaticValue> derivants = [];
        foreach (var der in obj.AnnotatedWith)
        {
            var t = TypeChecker.CheckTypes(der, env, requireDatatypeDecl: true);
            if (t == null) continue;
            derivants.Add(t);
        }
        
        var classVal = new ClassValue(env.Path.Append(typename), obj.Visibility, annotations.ToArray(), derivants.ToArray());
        classVal.StaticEnvironment = new ClassEnvironment(env, classVal);
        classVal.MemberEnvironment = new MemberEnvironment(classVal.StaticEnvironment);
        foreach (var stmt in obj.Body)
        {
            TypeChecker.CheckTypes(stmt, env);
        }
        
        env.DeclareSymbol(obj, new SymbolInfo(SymbolKind.Class, classVal));
        return null;
    }

    public static StaticValue? CheckEnumDeclaration(EnumDeclaration obj, Environment env)
    {
        if (env is not FileEnvironment && env is not ClassEnvironment)
        {
            env.ThrowTypeError(obj, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Enums can only be declared in files or nested inside classes.").AddHint("Try moving the enum inside a valid scope."));
            return null;
        }
        
        var typename = obj.Name.Name;
        if (obj.Name.GenericParameters.Count > 0)
        {
            env.ThrowTypeError(obj, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Enums cannot have generic type parameters."));
            return null;
        }

        List<StaticValue> annotations = [];
        foreach (var ann in obj.AnnotatedWith)
        {
            var t = TypeChecker.CheckTypes(ann, env, requireDatatypeDecl: true);
            if (t == null) continue;
            annotations.Add(t);
        }
        
        env.DeclareSymbol(obj, new SymbolInfo(
            kind: SymbolKind.Enum,
            type: new EnumValue(
                path: env.Path.Append(typename),
                visibility: obj.Visibility,
                annotations: annotations.ToArray(),
                entries: obj.Entries
            )
        ));

        return null;
    }

    public static StaticValue? CheckReturnDeclaration(ReturnDeclaration declaration, Environment env)
    {
        if (env is not FunctionEnvironment funEnv)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Return statements can only be used inside functions."));
            return null;
        }
        
        var type = TypeChecker.CheckTypes(declaration.Value, env);
        if (type != funEnv.FunctionVal.Type)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Cannot return value of type `{type}`, function expects `{funEnv.FunctionVal.Type}`."));
            return null;
        }
        
        return null;
    }
    
    // TODO
    public static StaticValue? CheckDeleteDeclaration(DeleteDeclaration declaration, Environment env)
    {
        return null;
    }

    public static StaticValue? CheckIfElseDeclaration(IfElseDeclaration declaration, Environment env)
    {
        if (env is not FunctionEnvironment)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "If-statements can only be used inside functions."));
            return null;
        }

        foreach (var block in declaration.Blocks)
        {
            var cond = TypeChecker.CheckTypes(block.Condition, env);
            if (cond != env.LookupPrimitiveBoolean(declaration)?.Type)
            {
                env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "If-statements must be provided with a boolean condition."));
                continue;
            }

            TypeChecker.CheckTypes(block.Body, env);
        }
        
        if (declaration.ElseBody != null)
            TypeChecker.CheckTypes(declaration.ElseBody, env);
        
        return null;
    }

    public static StaticValue? CheckWhileDeclaration(WhileDeclaration declaration, Environment env)
    {
        if (env is not FunctionEnvironment)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "While-statements can only be used inside functions."));
            return null;
        }
        
        var cond = TypeChecker.CheckTypes(declaration.Condition, env);
        if (cond != env.LookupPrimitiveBoolean(declaration)?.Type)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "While-statements must be provided with a boolean condition."));
            return null;
        }
        
        TypeChecker.CheckTypes(declaration.Body, env);
        
        return null;
    }

    public static StaticValue? CheckForDeclaration(ForDeclaration declaration, Environment env)
    {
        if (env is not FunctionEnvironment)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "For-statements can only be used inside functions."));
            return null;
        }
        
        var decl = TypeChecker.CheckTypes(declaration.Declaration, env);
        if (decl is not VariableValue)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "For-statements must be provided with a valid variable declaration."));
            return null;
        }
        
        var cond = TypeChecker.CheckTypes(declaration.Condition, env);
        if (cond != env.LookupPrimitiveBoolean(declaration)?.Type)
        {
            env.ThrowTypeError(declaration, SyntaxErrorData.New(SyntaxErrorLevel.Error, "For-statements must be provided with a boolean condition."));
            return null;
        }
        
        TypeChecker.CheckTypes(declaration.Action, env);
        TypeChecker.CheckTypes(declaration.Body, env);
        
        return null;
    }
}