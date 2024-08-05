using JScr.Frontend;

namespace JScr
{
    public static class JScrSourceFile
    {
        public static async Task CompileAsync(string filedir, string? outdir, Action<SyntaxError> errorCallback)
        {
            string sourcesFilename = "build.sources";
            string outSourceDir = outdir != null ? Path.Join(outdir, sourcesFilename) : Path.Join(filedir, "build", sourcesFilename);

            await Task.Run(() =>
            {
                var parser = new Parser();
                var data = File.ReadAllText(filedir);
                var program = parser.ProduceAST(filedir, data, errorCallback);/*
                var result = Transpiler.Transpiler.TranspileProgram(program, Array.Empty<JScrExternalResource>(), errorCallback);
                var file = File.CreateText(outSourceDir);
                file.Write(result);
                */
                // TODO: use C compiler to compile.
            });
        }
    }
    /*
    public class Script
    {
        public struct Result
        {
            public Script? Script { get; private set; }
            public List<SyntaxError> Errors { get; private set; }
            public readonly bool IsSuccess => Script != null;

            public Result(Script? script, List<SyntaxError> errors)
            {
                #region assertions
                // Check that if script is null, errors must have more than zero items
                if (script == null && (errors == null || errors.Count == 0))
                {
                    throw new ArgumentException("If script is null, errors must have more than zero items");
                }

                // Check that if script is not null, errors must be empty
                if (script != null && errors != null && errors.Count > 0)
                {
                    throw new ArgumentException("If script is not null, errors must be empty");
                }
                #endregion

                Script = script;
                Errors = errors ?? new List<SyntaxError>();
            }
        }

        private static void BuildStandardLibraryResources(ref Script script)
        {
            // Math

            script.resources.Add(new JScrExternalResourceFile(new[] { "jscr", "math" }, ResourceHandler.GetStandardLibResource("Math.math.jscr")!));

            // Lang

            script.resources.Add(new JScrExternalResourceFile(new[] { "jscr", "lang", "annotation_target" }, ResourceHandler.GetStandardLibResource("Lang.annotation_target.jscr")!));
        }

        public const string fileExtension = ".jscr";

        public string FileDir { get; private set; } = "";
        public bool IsRunning { get; private set; } = false;
        
        private List<JScrExternalResource> resources = new();
        private Ast.Program? program;

        private Script() { }
        ~Script() {
            while (IsRunning);
        }

        /// <summary>
        /// Create a script from file, a script file should be the full directory with a file name
        /// and extension.
        /// </summary>
        /// <param name="filedir">The path to the target script file.</param>
        /// <param name="externalResources"></param>
        /// <returns></returns>
        public static Result FromFile(string filedir, Action<SyntaxError>? errorCallback = null, List<JScrExternalResource>? externalResources = null)
        {
            Script script = new();
            List<SyntaxError> errors = new();

            var onError = (SyntaxError err) =>
            {
                errors.Add(err);
                errorCallback?.Invoke(err);
            };

            script.FileDir = filedir;
            script.resources = externalResources ?? new List<JScrExternalResource>();

            BuildStandardLibraryResources(ref script);
            
            var parser = new Parser();
            var data = File.ReadAllText(script.FileDir);
            script.program = parser.ProduceAST(script.FileDir, data, onError);

            return new Result(errors.Count < 1 ? script : null, errors);
        }

        /// <summary>
        /// Execute a script file. May take forever to complete, when this object is destroyed,
        /// End() will be called automatically.
        /// </summary>
        /// <param name="anotherThread">Whether to run this action on another thread.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="RuntimeException"></exception>
        public async Task Execute(bool anotherThread = true) {
            if (program == null) throw new InvalidOperationException("Cannot execute script while 'program' is null! The script needs to be initialised first.");
            else if (IsRunning) throw new InvalidOperationException("Cannot execute script while it already is running.");

            IsRunning = true;
            if (anotherThread)
                await Task.Run(() => Interpreter.EvaluateProgram(program, resources.ToArray()));
            else
                Interpreter.EvaluateProgram(program, resources.ToArray());

            IsRunning = false;
        }
    }*/

    /// <summary>
    /// These externals need to be defined when the Script object is created.
    /// </summary>
    public interface JScrExternalResource { }

    /// <summary>
    /// Allows scripts to access static methods and fields from a C# class T.
    /// These externals need to be defined when the Script object is created.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JScrExternalResourceNative<T> : JScrExternalResource
    {
        public Type Type => typeof(T);

        public JScrExternalResourceNative()
        {
        }
    }

    /// <summary>
    /// Allows scripts to access this internal file that may not have a path on disk.
    /// This file needs a namespace.
    /// These externals need to be defined when the Script object is created.
    /// </summary>
    public class JScrExternalResourceFile : JScrExternalResource
    {
        // TODO: LOAD STANDARD LIBRARY FILES USING RESX OR SOMETHING. (need to be files embedded in this .dll).
        //

        internal string[] location;
        internal string internalFile;

        public JScrExternalResourceFile(string[] location, string internalFile)
        {
            this.location = location;
            this.internalFile = internalFile;
        }
    }
}
