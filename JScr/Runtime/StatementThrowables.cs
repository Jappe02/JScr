using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JScr.Runtime.Values;

namespace JScr.Runtime
{
    internal abstract class IStatementThrowable : Exception { }

    internal class ThReturnStmt : IStatementThrowable
    {
        public RuntimeVal ReturnValue { get; }

        public ThReturnStmt(RuntimeVal returnValue) { ReturnValue = returnValue; }
    }

    internal class ThContinueStmt : IStatementThrowable
    {
        public ThContinueStmt() {}
    }

    internal class ThBreakStmt : IStatementThrowable
    {
        public ThBreakStmt() { }
    }
}
