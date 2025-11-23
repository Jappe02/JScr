using JScr.Frontend;
using Newtonsoft.Json;

namespace JScr;

public enum ModuleType
{
    File,
    Directory
}

public readonly struct Module
{
    public ModuleType Type { get; }
    public string Path { get; }

    internal Module(ModuleType type, string path)
    {
        Type = type;
        Path = path;
    }
}

internal struct JScrLoadedLibrary
{
    public Library Library { get; }
    public string SourcesDir { get; }
    public string LibsDir { get; }
    public Dictionary<string, Ast.Program> Program { get; } = new();
    [JsonIgnore]
    public bool IsRunning { get; internal set; } = false;

    public JScrLoadedLibrary(Library library, string sourcesDir, string libsDir)
    {
        Library = library;
        SourcesDir = sourcesDir;
        LibsDir = libsDir;
    }
}