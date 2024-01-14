using JScr.Frontend;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using static JScr.Frontend.Ast;
using static JScr.Runtime.Types;
using static JScr.Runtime.Values;

namespace JScr.Runtime
{
    internal class Environment
    {
        #region statics
        public static Environment CreateGlobalEnv(string fileDir, JScrExternalResource[] externalResources)
        {
            var env = new Environment(null, externalResources);
            env.FileDir = fileDir;

            // Setup default environment
            env.DeclareVar("true", new BoolVal(true), true, false, Types.Type.Bool());
            env.DeclareVar("false", new BoolVal(false), true, false, Types.Type.Bool());
            env.DeclareVar("null", new NullVal(), true, false, Types.Type.Dynamic());

            // Define a native builtin method
            env.DeclareVar("print", new NativeFnVal(Types.Type.Void(), (args, scope) =>
            {
                // TODO
                Console.WriteLine(string.Join(' ', args.AsEnumerable()));
                return new NullVal();
            }), true, false, Types.Type.Void());

            return env;
        }
        #endregion

        #region env
        public string FileDir { get; private set; } = "";
        public readonly Environment? parent;
        public readonly JScrExternalResource[] externalResources;
        public readonly bool isImported;
        private readonly Dictionary<Environment, string> importedEnvs;
        private readonly Dictionary<string, Environment> importAliases;

        private readonly Dictionary<string, ObjectVal> objects;
        private readonly HashSet<string> publicObjects;

        private readonly Dictionary<string, RuntimeVal> variables;
        private readonly Dictionary<string, Types.Type> types;
        private readonly HashSet<string> constants;
        private readonly HashSet<string> publicVariables;

        public Environment(Environment? parentEnv, JScrExternalResource[]? externalResources = null, bool isImported = false)
        {
            var global = parentEnv != null;
            parent = parentEnv;
            this.externalResources = (externalResources ?? parent?.externalResources) ?? Array.Empty<JScrExternalResource>();
            importedEnvs = new Dictionary<Environment, string>();
            importAliases = new Dictionary<string, Environment>();
            objects = new Dictionary<string, ObjectVal>();
            publicObjects = new HashSet<string>();
            variables = new Dictionary<string, RuntimeVal>();
            types = new Dictionary<string, Types.Type>();
            constants = new HashSet<string>();
            publicVariables = new HashSet<string>();
            this.isImported = isImported;
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
        #region Objects

        public ObjectVal DeclareObject(ObjectVal val)
        {
            if (objects.ContainsKey(val.Name))
            {
                throw new RuntimeException($"Cannot declare object \"{val.Name}\". As it already is defined.");
            }

            objects.Add(val.Name, val);

            if (val.Export)
            {
                publicObjects.Add(val.Name);
            }

            return val;
        }

        public ObjectVal LookupObject(string objname)
        {
            var env = ResolveObject(objname);
            return env.objects[objname];
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
                bool v = import.Key.publicObjects.Contains(objname);
                if (v)
                    return import.Key;
            }

            return null;
        }

        #endregion
        #region Variables

        public RuntimeVal DeclareVar(string varname, RuntimeVal value, bool constant, bool exposed, Types.Type type)
        {
            if (variables.ContainsKey(varname))
            {
                throw new RuntimeException($"Cannot declare variable \"{varname}\". As it already is defined.");
            }

            if (!Types.RuntimeValMatchesType(type, value))
            {
                throw new RuntimeException($"Cannot declare variable \"{varname}\". Type and initial value do not match.");
            }

            variables.Add(varname, value);
            types.Add(varname, type);
            if (constant)
            {
                constants.Add(varname);
            }
            if (exposed)
            {
                publicVariables.Add(varname);
            }
            return value;
        }

        public RuntimeVal AssignVar(string varname, RuntimeVal value)
        {
            var env = ResolveVar(varname);
            var type = env.types[varname];
            
            // Cannot assign to constant
            if (env.constants.Contains(varname))
            {
                throw new RuntimeException($"Cannot resign to variable \"{varname}\" as it was declared constant.");
            }

            if (!Types.RuntimeValMatchesType(type, value))
            {
                throw new RuntimeException($"Cannot resign to variable \"{varname}\". Type and new value do not match.");
            }

            env.variables[varname] = value;
            return value;
        }

        public RuntimeVal LookupVar(string varname)
        {
            var env = ResolveVar(varname);
            return env.variables[varname];
        }

        public Types.Type LookupVarType(string varname)
        {
            var env = ResolveVar(varname);
            return env.types[varname];
        }

        public Environment ResolveVar(string varname)
        {
            var ri = ResolveVarInImports(varname);

            if (ri != null)
                return ri;

            var rp = ResolveVarInParents(varname);

            if (rp != null)
                return rp;

            throw new RuntimeException($"Cannot resolve \"{varname}\" as it does not exist.");
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
                bool v = import.Key.publicVariables.Contains(varname);
                if (v)
                    return import.Key;
            }

            return null;
        }
        #endregion
        #endregion
    }
}
