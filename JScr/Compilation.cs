using JScr.Frontend;

namespace JScr;

public class Compilation
{
    public enum Origin { Source, Metadata, Native }
    public struct Dependency
    {
        public string Name;
        public string FilePath;
        public Origin Origin;
        public string? Mode;
    }
    
    public string FilePath;
    public Ast.Program[] Targets;
    public Dependency[] Dependencies;
    internal Action<SyntaxError> ErrorCallback;
    
    public Compilation(string filePath, Ast.Program[] targets, Dependency[] dependencies, Action<SyntaxError> errorCallback)
    {
        FilePath = filePath;
        Targets = targets;
        Dependencies = dependencies;
        ErrorCallback = errorCallback;
    }
}