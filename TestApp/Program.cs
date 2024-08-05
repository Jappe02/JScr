using JScr;
using JScr.Frontend;
using System.Runtime.CompilerServices;

namespace TestApp
{
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

        static async Task Main(string[] args)
        {
            Console.WriteLine("Loading script file...");

            bool error = false;
            await JScrSourceFile.CompileAsync(@"C:\Test\test.jscr", null, (err) => {
                Console.WriteLine();
                Console.WriteLine($"> ERROR: {err}");
                Console.WriteLine();
                error = true;
            });

            if (error)
            {
                Console.WriteLine("Compilation failed with an error. Press any key to quit.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Compilation succeeded! Press any key to quit.");
            Console.ReadLine();
        }
    }
}