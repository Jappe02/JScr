using JScr.CsImpl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
