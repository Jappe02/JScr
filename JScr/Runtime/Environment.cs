using JScr.Frontend;
using static JScr.Frontend.Ast;
using static JScr.Runtime.Types;
using static JScr.Runtime.Values;

namespace JScr.Runtime
{
    internal enum ScopeType : byte
    {
        Global,
        Class,
        Method,
    }

    internal class Environment
    {
        struct EnumData
        {
            public EnumVal Val;
            public AnnotationUsageDeclaration[] Annotations;
            public Visibility Visibility;
        }

        struct StructData
        {
            public ObjectVal Val;
            public AnnotationUsageDeclaration[] Annotations;
            public Visibility Visibility;
        }

        struct ClassData
        {
            public ClassVal Val;
            public AnnotationUsageDeclaration[] Annotations;
            public Visibility Visibility;
        }

        struct VariableData
        {
            public RuntimeVal Val;
            public AnnotationUsageDeclaration[] Annotations;
            public Visibility Visibility;
            public Types.Type Type;
            public bool IsConstant;
        }

        #region statics
        public static Environment CreateGlobalEnv(string fileDir, JScrExternalResource[] externalResources)
        {
            var env = new Environment(null, ScopeType.Global, externalResources);
            env.FileDir = fileDir;

            // Setup default environment
            env.DeclareVar("true", new BoolVal(true), true, Visibility.Private, Types.Type.Bool());
            env.DeclareVar("false", new BoolVal(false), true, Visibility.Private, Types.Type.Bool());
            env.DeclareVar("null", new NullVal(), true, Visibility.Private, Types.Type.Dynamic());

            // Define a native builtin method
            env.DeclareVar("print", new NativeFnVal(Types.Type.Void(), (args, scope) =>
            {
                // TODO
                Console.WriteLine(string.Join(' ', args.AsEnumerable()));
                return new NullVal();
            }), true, Visibility.Private, Types.Type.Void());

            return env;
        }
        #endregion

        #region env
        public string FileDir { get; private set; } = "";
        public readonly Environment? parent;
        public readonly ScopeType scopeType;
        public readonly JScrExternalResource[] externalResources;
        public readonly bool isImported;
        private readonly Dictionary<Environment, string> importedEnvs;
        private readonly Dictionary<string, Environment> importAliases;

        private readonly Dictionary<string, EnumData>     enums;
        private readonly Dictionary<string, StructData>   objects;
        private readonly Dictionary<string, ClassData>    classes;
        private readonly Dictionary<string, VariableData> variables;

        public Environment(Environment? parentEnv, ScopeType scopeType = ScopeType.Global, JScrExternalResource[]? externalResources = null, bool isImported = false)
        {
            if (parentEnv != null && !isImported && scopeType == ScopeType.Global)
            {
                throw new InvalidOperationException("Cannot create global non-imported scope if it has a parent scope!");
            }

            this.parent = parentEnv;
            this.scopeType = scopeType;
            this.externalResources = (externalResources ?? parent?.externalResources) ?? Array.Empty<JScrExternalResource>();
            this.isImported = isImported;

            importedEnvs = new Dictionary<Environment, string>();
            importAliases = new Dictionary<string, Environment>();

            enums = new();
            objects = new();
            classes = new();
            variables = new();

        }

        #region Imports

        public void DeclareImport(Environment otherScriptEnv, string otherScriptFiledir, string? aliasIdentifier = null)
        {
            if (importedEnvs.ContainsValue(otherScriptFiledir) || (!isImported && FileDir == otherScriptFiledir))
                throw new RuntimeException("Failed to import `otherScriptFiledir`, circular dependency.");
            
            if (!importedEnvs.ContainsKey(otherScriptEnv))
                importedEnvs.Add(otherScriptEnv, otherScriptFiledir);

            if (aliasIdentifier != null)
            {
                if (importAliases.ContainsKey(aliasIdentifier))
                    throw new RuntimeException($"Cannot declare import alias \"{aliasIdentifier}\", since it already exists.");

                importAliases.Add(aliasIdentifier, otherScriptEnv);
            }
        }

        public Environment? LookupImportAlias(string identifier)
        {
            var env = ResolveImportAlias(identifier);
            return env?.importAliases[identifier];
        }

        public Environment? ResolveImportAlias(string identifier)
        {
            if (objects.ContainsKey(identifier))
                return this;

            if (parent == null)
                return null;

            return parent.ResolveImportAlias(identifier);
        }

        #endregion
        #region Enums

        public EnumVal DeclareEnum(EnumVal val, AnnotationUsageDeclaration[]? annotations = null)
        {
            if (enums.ContainsKey(val.Name))
            {
                throw new RuntimeException($"Cannot declare object \"{val.Name}\". As it already is defined.");
            }

            List<AnnotationUsageDeclaration> newAnnotations = annotations?.ToList() ?? new();
            foreach (var annotation in newAnnotations)
            {
                var obj = LookupObject(annotation.Ident);

                if (!obj.IsAnnotation)
                    throw new RuntimeException($"The referenced object is not an annotation.");

                var ann = (obj.AnnotationTargets?.Value.FirstOrDefault((item) => (item as IntegerVal)!.Value == 3))
                        ?? throw new RuntimeException($"This annotation cannot be used for this enum.");
            }

            enums.Add(val.Name, new() { Val = val, Annotations = newAnnotations.ToArray(), Visibility = val.Visibility });

            return val;
        }

        public EnumVal LookupEnum(string objname)
        {
            var env = ResolveEnum(objname);
            return env.enums[objname].Val;
        }

        public Environment ResolveEnum(string objname)
        {
            var ri = ResolveEnumInImports(objname);

            if (ri != null)
                return ri;

            var rp = ResolveEnumInParents(objname);

            if (rp != null)
                return rp;

            throw new RuntimeException($"Cannot resolve \"{objname}\" as it does not exist.");
        }

        public Environment? ResolveEnumInParents(string objname)
        {
            if (enums.ContainsKey(objname))
                return this;

            if (parent == null)
                return null;

            return parent.ResolveEnumInParents(objname);
        }

        public Environment? ResolveEnumInImports(string objname)
        {
            foreach (var import in importedEnvs)
            {
                bool v = import.Key.enums.ContainsKey(objname) && import.Key.enums[objname].Visibility == Visibility.Public;
                if (v)
                    return import.Key;
            }

            return null;
        }

        #endregion
        #region Objects

        public ObjectVal DeclareObject(ObjectVal val, AnnotationUsageDeclaration[]? annotations = null)
        {
            if (objects.ContainsKey(val.Name))
            {
                throw new RuntimeException($"Cannot declare object \"{val.Name}\". As it already is defined.");
            }

            List<AnnotationUsageDeclaration> newAnnotations = annotations?.ToList() ?? new();
            foreach (var annotation in newAnnotations)
            {
                var obj = LookupObject(annotation.Ident);

                if (!obj.IsAnnotation)
                    throw new RuntimeException($"The referenced object is not an annotation.");

                var ann = (obj.AnnotationTargets?.Value.FirstOrDefault((item) => (item as IntegerVal)!.Value == 2))
                        ?? throw new RuntimeException($"This annotation cannot be used for this object.");
            }

            objects.Add(val.Name, new() { Val = val, Annotations = newAnnotations.ToArray(), Visibility = val.Visibility });

            return val;
        }

        public ObjectVal LookupObject(string objname)
        {
            var env = ResolveObject(objname);
            return env.objects[objname].Val;
        }

        public Environment ResolveObject(string objname)
        {
            var ri = ResolveObjectInImports(objname);

            if (ri != null)
                return ri;

            var rp = ResolveObjectInParents(objname);

            if (rp != null)
                return rp;

            throw new RuntimeException($"Cannot resolve \"{objname}\" as it does not exist.");
        }

        public Environment? ResolveObjectInParents(string objname)
        {
            if (objects.ContainsKey(objname))
                return this;

            if (parent == null)
                return null;

            return parent.ResolveObjectInParents(objname);
        }

        public Environment? ResolveObjectInImports(string objname)
        {
            foreach (var import in importedEnvs)
            {
                bool v = import.Key.objects.ContainsKey(objname) && import.Key.objects[objname].Visibility == Visibility.Public;
                if (v)
                    return import.Key;
            }

            return null;
        }

        #endregion
        #region Classes

        public ClassVal DeclareClass(ClassVal val, AnnotationUsageDeclaration[]? annotations = null)
        {
            if (objects.ContainsKey(val.Name))
            {
                throw new RuntimeException($"Cannot declare object \"{val.Name}\". As it already is defined.");
            }

            List<AnnotationUsageDeclaration> newAnnotations = annotations?.ToList() ?? new();
            foreach (var annotation in newAnnotations)
            {
                var obj = LookupObject(annotation.Ident);

                if (!obj.IsAnnotation)
                    throw new RuntimeException($"The referenced object is not an annotation.");

                var ann = (obj.AnnotationTargets?.Value.FirstOrDefault((item) => (item as IntegerVal)!.Value == 2))
                        ?? throw new RuntimeException($"This annotation cannot be used for this object.");
            }

            classes.Add(val.Name, new() { Val = val, Annotations = newAnnotations.ToArray(), Visibility = val.Visibility });

            return val;
        }

        public ClassVal LookupClass(string objname)
        {
            var env = ResolveClass(objname);
            return env.classes[objname].Val;
        }

        public Environment ResolveClass(string objname)
        {
            var ri = ResolveClassInImports(objname);

            if (ri != null)
                return ri;

            var rp = ResolveClassInParents(objname);

            if (rp != null)
                return rp;

            throw new RuntimeException($"Cannot resolve \"{objname}\" as it does not exist.");
        }

        public Environment? ResolveClassInParents(string objname)
        {
            if (classes.ContainsKey(objname))
                return this;

            if (parent == null)
                return null;

            return parent.ResolveClassInParents(objname);
        }

        public Environment? ResolveClassInImports(string objname)
        {
            foreach (var import in importedEnvs)
            {
                bool v = import.Key.classes.ContainsKey(objname) && import.Key.classes[objname].Visibility == Visibility.Public;
                if (v)
                    return import.Key;
            }

            return null;
        }

        #endregion
        #region Variables

        public RuntimeVal DeclareVar(string varname, RuntimeVal value, bool constant, Visibility visibility, Types.Type type, AnnotationUsageDeclaration[]? annotations = null)
        {
            if (variables.ContainsKey(varname))
            {
                throw new RuntimeException($"Cannot declare variable \"{varname}\". As it already is defined.");
            }

            if (!Types.RuntimeValMatchesType(type, value))
            {
                throw new RuntimeException($"Cannot declare variable \"{varname}\". Type and initial value do not match.");
            }

            List<AnnotationUsageDeclaration> newAnnotations = annotations?.ToList() ?? new();
            foreach (var annotation in newAnnotations)
            {
                var obj = LookupObject(annotation.Ident);

                if (!obj.IsAnnotation)
                    throw new RuntimeException($"The referenced object is not an annotation.");

                if (value.Type == Values.ValueType.function)
                {
                    var a = (obj.AnnotationTargets?.Value.FirstOrDefault((item) => (item as IntegerVal)!.Value == 0))
                        ?? throw new RuntimeException($"This annotation cannot be used for this variable.");
                }

                var ann = (obj.AnnotationTargets?.Value.FirstOrDefault((item) => (item as IntegerVal)!.Value == 1))
                        ?? throw new RuntimeException($"This annotation cannot be used for this variable.");
            }

            variables.Add(varname, new() { Val = value, Annotations = newAnnotations.ToArray(), Type = type, Visibility = visibility, IsConstant = constant });

            return value;
        }

        public RuntimeVal AssignVar(string varname, RuntimeVal value)
        {
            var env = ResolveVar(varname);
            var variable = env.variables[varname];
            var type = variable.Type;
            
            // Cannot assign to constant
            if (variable.IsConstant)
            {
                throw new RuntimeException($"Cannot resign to variable \"{varname}\" as it was declared constant.");
            }

            if (!Types.RuntimeValMatchesType(type, value))
            {
                throw new RuntimeException($"Cannot resign to variable \"{varname}\". Type and new value do not match.");
            }

            variable.Val = value;
            env.variables[varname] = variable;
            return value;
        }

        public RuntimeVal? LookupVar(string varname, bool canReturnNull = false)
        {
            var env = ResolveVar(varname, canReturnNull);
            return env?.variables[varname].Val;
        }

        public Types.Type? LookupVarType(string varname, bool canReturnNull = false)
        {
            var env = ResolveVar(varname, canReturnNull);
            return env.variables[varname].Type;
        }

        public Environment? ResolveVar(string varname, bool canReturnNull = false)
        {
            var ri = ResolveVarInImports(varname);

            if (ri != null)
                return ri;

            var rp = ResolveVarInParents(varname);

            if (rp != null)
                return rp;

            if (!canReturnNull)
                throw new RuntimeException($"Cannot resolve \"{varname}\" as it does not exist.");
            else return null;
        }

        public Environment? ResolveVarInParents(string varname)
        {
            if (variables.ContainsKey(varname))
                return this;

            if (parent == null)
                return null;

            return parent.ResolveVarInParents(varname);
        }

        public Environment? ResolveVarInImports(string varname)
        {
            foreach (var import in importedEnvs)
            {
                bool v = import.Key.variables.ContainsKey(varname) && import.Key.variables[varname].Visibility == Visibility.Public;
                if (v)
                    return import.Key;
            }

            return null;
        }
        #endregion
        #endregion
    }
}
