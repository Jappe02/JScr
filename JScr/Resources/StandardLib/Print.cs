using JScr.CsImpl;

namespace JScr.StandardExternalRes
{
    [JScrClassTarget("")]
    internal class Print
    {
        [JScrMethodTarget("print")]
        public static void WriteLine(dynamic obj) {
            Console.WriteLine(obj);
        }
    }
}
