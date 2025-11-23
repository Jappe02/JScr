using System.Reflection;

namespace JScr.BuildTools;

static class Program
{
    public const int APPLICATION_EXIT_ERROR = -1;
    public const int APPLICATION_EXIT_INVALID_PARAMETERS = -3;

    static int Main(string[] args)
    {
        // Check if there are any arguments
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments provided. Showing help instead.");
            ShowHelp();
            return 0;
        }

        // Parse the arguments
        Dictionary<string, string> arguments = ParseArguments(args);
        string? action = args[0];

        // Map args string to their actions. The optional parameters of methods should be optional in the args too.
        // The action function should have the same name as the action arg.
        // The parameters of the action functions should have same names as the ones we get from the `arguments` map.
        switch (action)
        {
            case "h":
            case "help":
            {
                ShowHelp();
                return 0;
            }

            case "v":
            case "ver":
            case "version":
            {
                ShowVersion();
                return 0;
            }

            case "build":
            {
                arguments.TryGetValue(new[] { "target", "t" }, out string? target);
                arguments.TryGetValue(new[] { "out", "o" }, out string? outp);

                if (target == null)
                {
                    Console.Error.WriteLine("No target provided.");
                    return APPLICATION_EXIT_INVALID_PARAMETERS;
                }

                return Actions.Build(target, outp);
            }

            case "run":
            {
                arguments.TryGetValue(new[] { "target", "t" }, out string? target);

                if (target == null)
                {
                    Console.Error.WriteLine("No target provided.");
                    return APPLICATION_EXIT_INVALID_PARAMETERS;
                }

                return Actions.Run(target);
            }

            default:
            {
                Console.WriteLine("Invalid action.");
                return -2;
            }
        }
    }

    static Dictionary<string, string> ParseArguments(string[] args)
    {
        var arguments = new Dictionary<string, string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith('-'))
            {
                string key = args[i].TrimStart('-');
                string? value = (i + 1 < args.Length && !args[i + 1].StartsWith('-')) ? args[i + 1] : null;
                arguments[key] = value ?? "";
            }
        }

        return arguments;
    }

    static void ShowHelp()
    {
        static string HelpLine(Delegate func, string description)
        {
            string str = "";
            str += "\t";
            str += $"{func.Method.Name.ToLower()}: ";

            foreach (var param in func.Method.GetParameters())
            {
                bool hasDefaultVal = param.HasDefaultValue;

                if (hasDefaultVal)
                    str += "[";
                else
                    str += "<";

                str += param.Name;

                if (hasDefaultVal)
                    str += "]";
                else
                    str += ">";
            }

            str += "\n\t\t";
            str += description;
            str += "\n";

            return str;
        }

        Console.WriteLine("JSCR BUILDTOOLS HELP:");
        {
            Console.WriteLine(HelpLine(Actions.Build, "Build the target file to an executable."));
            Console.WriteLine(HelpLine(Actions.Run, "Runs the target file through the interpreter."));
        }

        Console.WriteLine("");
        ShowVersion();
    }

    static void ShowVersion()
    {
        Version appVersion = Assembly.GetExecutingAssembly().GetName().Version!;
        string ver = "v" + appVersion.Major + "." + appVersion.Minor + "." + appVersion.Build + ".";

        Console.WriteLine("JScr BuildTools Version: " + ver);
        Console.WriteLine("By JappeStudios.");
    }
}

static class Actions
{
    /// <summary>
    /// Build the target file to an executable.
    /// </summary>
    /// <param name="target">The target file to build.</param>
    /// <param name="outp">The output file.</param>
    /// <returns>Exit code.</returns>
    public static int Build(string target, string? outp = null)
    {
        outp ??= target;

        return 0;
    }

    /// <summary>
    /// Runs the target file through the interpreter.
    /// </summary>
    /// <param name="target">Target file to run.</param>
    /// <returns>Exit code.</returns>
    public static int Run(string target)
    {
        const string PREFIX = "run> ";

        Script.Result script = Script.FromFile(target);

        Console.WriteLine(PREFIX + "Parsing ...");

        if (!script.IsSuccess)
        {
            Console.Error.WriteLine(PREFIX + "Failed to run script file: parsing failed.");

            foreach (var err in script.Errors)
            {
                Console.Error.WriteLine(PREFIX + ">\t" + err.ToString());
            }

            return Program.APPLICATION_EXIT_ERROR;
        }

        Console.WriteLine(PREFIX + "Executing program.");

        try
        {
            _ = script.Script!.Execute(false);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(PREFIX + "Program execution failed due to a runtime error!");
            Console.Error.WriteLine(PREFIX + ">\t" + e.ToString());
        }

        Console.WriteLine(PREFIX + "Execution done!");

        return 0;
    }
}