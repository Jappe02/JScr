using JScr;
using JScr.Frontend;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace TestApp;

class Program
{
    // FOR INTERPRETED VERSION
    /*
    static void Main(string[] args)
    {
        Console.WriteLine("Loading script file...");

        var script = Script.FromFile(@"C:\Test\test.jscr");

        Console.WriteLine("Script file loaded, beginning execution.");

        if (script.IsSuccess)
        {
            try
            {
                var exec = script.Script!.Execute(false);
                exec.Wait();
            } catch (RuntimeException ex)
            {
                Console.WriteLine("> Code failed to execute due to runtime error: <");
                Console.WriteLine($"\n  {ex}");
                Console.WriteLine($"\n\n");

                Console.WriteLine("Exiting with runtime error.");
                Console.ReadLine();
                Environment.Exit(1);
            }
        } else
        {
            Console.WriteLine("> Syntax errors found in code: <");

            foreach (var err in script.Errors)
            {
                Console.WriteLine($"\n  {err}");
            }

            Console.WriteLine($"\n\n");

            Console.WriteLine("Exiting with syntax error.");
            Console.ReadLine();
            Environment.Exit(1);
        }

        Console.WriteLine("Execution succeeded! Press any key to quit.");
        Console.ReadLine();
    }
    */

    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Compiling library...");

        if (!Compile(@"C:\Test\", out var library))
        {
            Console.WriteLine("Compilation failed with an error. Press any key to quit.");
            Console.ReadLine();
            return -1;
        }

        if (library.Type != Library.LibraryType.Executable)
        {
            Console.WriteLine("Compilation succeeded! Library is not an executable. Press any key to quit.");
            Console.ReadLine();
            return 0;
        }

        Console.WriteLine("Compilation succeeded! Library is an executable, would you like to run it in the interpreter? [Y/N]");
        bool shouldExecute = YesNo();

        if (!shouldExecute)
        {
            Console.WriteLine("Executable will not be run in the interpreter. Press any key to quit.");
            Console.ReadLine();
            return 0;
        }

        Console.WriteLine("Running executable...");

        // TODO: RUN IN THE INTERPRETER

        return 0;
    }

    static bool Compile(string libpath, out Library lib)
    {
        lib = default;
        bool error = false;
        try
        {
            Library.Load(libpath, out lib);
            Library.Compile(lib, Library.LibraryBuildType.Interpretable, null, (err) =>
            {
                Console.WriteLine();
                Console.WriteLine($"> {err}");
                Console.WriteLine();
                error = true;
            });
        }
        catch (Exception ex)
        {
            error = true;
            Console.WriteLine("An exception occurred while compiling or closing library:");
            Console.WriteLine(ex);
            Console.WriteLine();
        }

        return !error;
    }

    static bool YesNo()
    {
        var line = Console.ReadLine();

        if (line == "Y" || line == "y")
            return true;

        return false;
    }
}