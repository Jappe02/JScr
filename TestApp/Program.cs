using JScr;
using JScr.Frontend;
using System.Runtime.CompilerServices;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Loading script file...");

            var script = Script.FromFile(@"C:\Test\test.jscr");

            Console.WriteLine("Script file loaded, beginning execution.");

            if (script.IsSuccess)
            {
                try
                {
                    _ = script.Script.Execute();
                } catch (RuntimeException ex)
                {
                    Console.WriteLine("> Code failed to execute due to runtime error: <");
                    Console.WriteLine($"\n  {ex}");
                    Console.WriteLine($"\n\n");
                }

                Console.WriteLine("Exiting with runtime error.");
                Console.ReadLine();
                Environment.Exit(1);
                
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
    }
}