using JScr.Frontend;
using static JScr.Frontend.Ast;

namespace JScr.Typing.TypeChecking
{
    internal enum ScopeType : byte
    {
        Global,
        Class,
        Method,
    }

    internal readonly struct SymbolData
    {
        public SymbolData(Visibility visibility)
        {
            Visibility = visibility;
        }

        public Visibility Visibility { get; }
    }

    internal class Environment
    {
        public static Environment Global(string filedir, Action<SyntaxError> errorCallback) => new(null, ScopeType.Global, filedir, errorCallback);
        public static Environment Sub(Environment parent, ScopeType scopeType) => new(parent, scopeType, parent.filedir, parent.errorCallback);

        public Environment(Environment? parent, /*bool isImported = false,*/ ScopeType scopeType = ScopeType.Global, string? filedir = null, Action<SyntaxError>? errorCallback = null)
        {
            if (parent != null && scopeType == ScopeType.Global)
                throw new InvalidOperationException("Cannot create global scope if it has a parent scope!");

            /*if (isImported && scopeType != ScopeType.Global)
                throw new InvalidOperationException("isImported cannot be true for a non-global scope.");*/

            if (parent == null && filedir == null)
                throw new InvalidOperationException("Filedir cannot be null for the root environment.");

            if (parent == null && errorCallback == null)
                throw new InvalidOperationException("Error callback cannot be null for the root environment.");

            this.parent = parent;
            //this.isImported = isImported;
            this.scopeType = scopeType;
            this.filedir = filedir ?? parent!.filedir;
            this.errorCallback = errorCallback ?? parent!.errorCallback;

            importedEnvs = new();
            symbols = new();
        }

        public readonly Environment? parent;
        public readonly ScopeType scopeType;
        public readonly string filedir;
        public readonly Action<SyntaxError> errorCallback;

        //public readonly bool isImported;
        private readonly List<Environment> importedEnvs;

        private readonly Dictionary<string, SymbolData> symbols;

        public void DeclareSymbol(string name, SymbolData data)
        {
            if (symbols.ContainsKey(name))
                errorCallback(new(filedir, 0, 0, $"A symbol with the name `{name}` already exists."));

            symbols[name] = data;
        }

        public SymbolData LookupSymbol(string name)
        {
            var env = ResolveSymbol(name);
            return env.symbols[name];
        }

        public Environment ResolveSymbol(string name)
        {
            var ri = ResolveSymbolInImports(name);

            if (ri != null)
                return ri;

            var rp = ResolveSymbolInParents(name);

            if (rp != null)
                return rp;

            errorCallback(new(filedir, 0, 0, $"Cannot resolve symbol with name `{name}`, it does not exist."));
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
}
