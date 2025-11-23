using JScr.Frontend;

namespace JScr;

public struct InterpreterExecutable
{
    public const string FILE_EXTENSION = "iexec";

    public string FilePath { get; private set; }
    public bool IsRunning { get; internal set; } = false;

    internal Dictionary<string, Ast.Program> Program { get; } = new();

    public InterpreterExecutable(string filePath)
    {
        FilePath = filePath;
    }

    public int Execute(string[]? args = null)
    {
        if (IsRunning)
            throw new InvalidOperationException("The program is already running.");

        string[] newArgs = args ?? [];

        // TODO: Evaluate through interpreter
        throw new NotImplementedException();
    }

    public Task<int> ExecuteAsync(string[]? args = null)
    {
        var t = this;
        return Task.Run(() => t.Execute(args));
    }

    public bool Terminate(bool kill = false)
    {
        if (!IsRunning)
            return false;

        // TODO: Terminate interpreter process

        return true;
    }
}