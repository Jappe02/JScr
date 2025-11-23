using JScr.Frontend;
using Newtonsoft.Json;

namespace JScr;

[Serializable]
public struct Library
{
    public enum LibraryBuildType
    {
        Interpretable,
        Transpiled,
    }

    public enum LibraryType
    {
        Executable,
        Library,
    }

    [Serializable]
    public readonly struct Dependency
    {
        public enum DependencySource
        {
            Local,
            Git,
        }

        public DependencySource Source { get; }
        public string Path { get; }
    }

    public static void Load(string root, out Library library)
    {
        root = Path.GetFullPath(root);
        library = new Library(string.Empty, string.Empty, string.Empty, [], default, string.Empty, [], root);
        DeserializeLibraryJson(ref library);
    }

    public static async Task CompileAsync(Library lib, LibraryBuildType buildType, string[]? filepaths, Action<SyntaxError> errorCallback)
    {
        await Task.Run(() => Compile(lib, buildType, filepaths, errorCallback));
    }

    // TODO: Optional output dir
    public static bool Compile(in Library lib, LibraryBuildType buildType, string[]? filepaths, Action<SyntaxError> errorCallback)
    {
        throw new NotImplementedException();
#if false

            var outputDir = GetOptimalBuildOutputDirectory(lib.rootPath, buildType);

            // TODO: Use filepaths and/or compile all files.
            var filedir = Path.GetFullPath(Path.Combine(lib.sourcesPath, lib.Entry));
            if (lib.Program.ContainsKey(filedir))
                return false;

            var data = File.ReadAllText(filedir); // <-- TODO | NOTE :
            var ast = Parser.ProduceAST(filedir, data, errorCallback);
            lib.Program.Add(filedir, ast);

#endif
        // TODO
        //TypeChecker.CheckProgramTypes(program, errorCallback, out _);

        return true;
    }

    public static string GetOptimalBuildOutputDirectory(string libroot, LibraryBuildType buildType)
    {
        return Path.Combine(libroot, "out/", buildType.ToString().ToLower());
    }

    public static void GetProjectRootStructure(string from, out string libsdir, out string sourcedir, out string libdata)
    {
        libsdir = Path.Combine(from, "libs/");
        sourcedir = Path.Combine(from, "src/");
        libdata = Path.Combine(from, "lib.json");
    }

    internal static void DeserializeLibraryJson(ref Library obj)
    {
        var libraryFile = File.ReadAllText(obj.libJsonPath);
        JsonConvert.PopulateObject(libraryFile, obj);
    }

    public string Name { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public string[] Authors { get; set; }
    public LibraryType Type { get; set; }
    public string Entry { get; set; }
    public Dependency[] Dependencies { get; set; }

    public readonly string rootPath;
    public readonly string libsPath, sourcesPath, libJsonPath;

    internal Library(string name, string version, string description, string[] authors, LibraryType type, string entry, Dependency[] dependencies, string rootPath)
    {
        Name = name;
        Version = version;
        Description = description;
        Authors = authors;
        Type = type;
        Entry = entry;
        Dependencies = dependencies;

        this.rootPath = rootPath;
        GetProjectRootStructure(rootPath, out libsPath, out sourcesPath, out libJsonPath);
    }
}