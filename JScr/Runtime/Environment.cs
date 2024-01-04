using JScr.Frontend;
using System.Text.Json;
using System.Text.Json.Serialization;
using static JScr.Runtime.Types;
using static JScr.Runtime.Values;

namespace JScr.Runtime
{
    internal class Environment
    {
        #region statics
        public static Environment CreateGlobalEnv()
        {
            var env = new Environment(null);
            // Setup default environment
            env.DeclareVar("true", new BoolVal(true), true, Types.Type.Bool);
            env.DeclareVar("false", new BoolVal(false), true, Types.Type.Bool);
            env.DeclareVar("null", new NullVal(), true, Types.Type.Dynamic);

            // Define a native builtin method
            env.DeclareVar("print", new NativeFnVal(Types.Type.Void, (args, scope) =>
            {
                // TODO
                Console.WriteLine(string.Join(' ', args.AsEnumerable()));
                return new NullVal();
            }), true, Types.Type.Void);

            return env;
        }
        #endregion

        #region env
        private readonly Environment? parent;
        private readonly Dictionary<string, RuntimeVal> variables;
        private readonly Dictionary<string, Types.Type> types;
        private readonly HashSet<string> constants;

        public Environment(Environment? parentEnv)
        {
            var global = parentEnv != null;
            parent = parentEnv;
            variables = new Dictionary<string, RuntimeVal>();
            types = new Dictionary<string, Types.Type>();
            constants = new HashSet<string>();
        }

        public RuntimeVal DeclareVar(string varname, RuntimeVal value, bool constant, Types.Type type)
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
            return value;
        }

        public RuntimeVal AssignVar(string varname, RuntimeVal value)
        {
            var env = Resolve(varname);
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
            var env = Resolve(varname);
            return env.variables[varname];
        }

        public Environment Resolve(string varname)
        {
            if (variables.ContainsKey(varname))
                return this;

            if (parent == null)
                throw new RuntimeException($"Cannot resolve \"{varname}\" as it does not exist.");

            return parent.Resolve(varname);
        }
        #endregion
    }
}
