using JScr.Frontend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
            env.DeclareVar("true", new BoolVal(true), true);
            env.DeclareVar("false", new BoolVal(false), true);
            env.DeclareVar("null", new NullVal(), true);

            // Define a native builtin method
            env.DeclareVar("print", new NativeFnVal((args, scope) =>
            {
                // TODO
                Console.WriteLine(args);
                return new NullVal();
            }), true);

            return env;
        }
        #endregion

        #region env
        private readonly Environment? parent;
        private readonly Dictionary<string, RuntimeVal> variables;
        private readonly HashSet<string> constants;

        public Environment(Environment? parentEnv)
        {
            var global = parentEnv != null;
            parent = parentEnv;
            variables = new Dictionary<string, RuntimeVal>();
            constants = new HashSet<string>();
        }

        public RuntimeVal DeclareVar(string varname, RuntimeVal value, bool constant)
        {
            if (variables.ContainsKey(varname))
            {
                throw new RuntimeException($"Cannot declare variable \"{varname}\". As it already is defined.");
            }

            variables.Add(varname, value);
            if (constant)
            {
                constants.Add(varname);
            }
            return value;
        }

        public RuntimeVal AssignVar(string varname, RuntimeVal value)
        {
            var env = Resolve(varname);
            
            // Cannot assign to constant
            if (env.constants.Contains(varname))
            {
                throw new RuntimeException($"Cannot resign to variable \"{varname}\" as it was declared constant.");
            }

            env.variables.Add(varname, value);
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

        public override string ToString() => JsonSerializer.Serialize(this);
        #endregion
    }
}
