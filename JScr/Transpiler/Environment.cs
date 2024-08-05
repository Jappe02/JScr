using JScr.Frontend;

namespace JScr.Transpiler
{
    internal enum ScopeType : byte
    {
        Global,
        Class,
        Method,
    }

    internal class Environment
    {
        public Environment(Environment? parent, ScopeType scopeType = ScopeType.Global, string? filedir = null, Action<SyntaxError>? errorCallback = null)
        {
            if (parent != null && scopeType == ScopeType.Global)
                throw new InvalidOperationException("Cannot create global scope if it has a parent scope!");

            if (parent == null && errorCallback == null)
                throw new InvalidOperationException("Error callback cannot be null for the root environment.");

            if (parent == null && filedir == null)
                throw new InvalidOperationException("Filedir cannot be null for the root environment.");

            this.parent = parent;
            this.scopeType = scopeType;
            this.filedir = filedir ?? parent!.filedir;
            this.errorCallback = errorCallback ?? parent!.errorCallback;

            top = parent?.top ?? new();
        }

        public readonly Environment? parent;
        public readonly ScopeType scopeType;
        public readonly string filedir;
        public readonly Action<SyntaxError> errorCallback;

        public bool NoSemicolons { get; private set; } = false;

        public readonly List<string> top;

        public Environment WithNoSemicolons()
        {
            Environment environment = this;
            environment.NoSemicolons = true;
            return environment;
        }
    }

    internal class ClassEnvironment : Environment
    {
        public ClassEnvironment(Environment parent, string name) : base(parent, ScopeType.Class, parent.filedir, parent.errorCallback)
        {
            this.name = name;
        }

        public readonly string name;
    }
}
