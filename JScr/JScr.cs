using JScr.Frontend;
using JScr.Runtime;

namespace JScr
{
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

        public const string fileExtension = ".jscr";

        public string FileDir { get; private set; } = "";
        private List<JScrExternalResource> resources = new();

        private Ast.Program? program;
        private bool isRunning = false;

        private Script() { }
        ~Script() { End(); }

        /// <summary>
        /// Create a script from file, a script file should be the full directory with a file name
        /// and extension.
        /// </summary>
        /// <param name="filedir">The path to the target script file.</param>
        /// <param name="externalResources"></param>
        /// <returns></returns>
        public static Result FromFile(string filedir, List<JScrExternalResource>? externalResources = null)
        {
            Script script = new();
            List<SyntaxError> errors = new();

            script.FileDir = filedir;
            script.resources = externalResources ?? new List<JScrExternalResource>();

            try
            {
                var parser = new Parser();

                var data = File.ReadAllText(script.FileDir);
                script.program = parser.ProduceAST(script.FileDir, data);
            } catch (SyntaxException e)
            {
                errors.Add(e.Error);
            }

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
            else if (isRunning) throw new InvalidOperationException("Cannot execute script while it already is running.");

            isRunning = true;
            if (anotherThread)
                await Task.Run(() => Interpreter.EvaluateProgram(program, ref isRunning));
            else
                Interpreter.EvaluateProgram(program, ref isRunning);

            End();
        }

        /// <summary>
        /// End the execution of this script.
        /// </summary>
        public void End() {
            isRunning = false;
        }
    }

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

        private string internalFile;

        public JScrExternalResourceFile(string internalFile)
        {
            this.internalFile = internalFile;
        }
    }
}
