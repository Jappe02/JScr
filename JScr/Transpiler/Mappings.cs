namespace JScr.Transpiler
{
    internal static class Mappings
    {
        private static readonly Dictionary<string, string> mappings = new()
        {
            // Std_Threading_Thread_StartInternal
            { "Std::Threading::Thread.StartInternal",            "src/Threading/Thread.h" },

            // Std_Threading_Thread_JoinInternal
            { "Std::Threading::Thread.JoinInternal",             "src/Threading/Thread.h" },

            // Std_Threading_Thread_AbortInternal
            { "Std::Threading::Thread.AbortInternal",            "src/Threading/Thread.h" },

            // Std_Threading_Thread_Sleep
            { "Std::Threading::Thread::Sleep",                   "src/Threading/Thread.h" }
        };

        public static bool TryMap(string functionPath, string[] paramIdents, out string? headerPath, out string? functionBody)
        {
            if (mappings.TryGetValue(functionPath, out var localHeaderPath))
            {
                headerPath = localHeaderPath;

                var funcCall = functionPath.Replace("$", "_").Replace("::", "_");
                funcCall += "(";
                for (int i = 0; i < paramIdents.Length; i++)
                {
                    if (i != 0)
                        funcCall += ", ";
                    funcCall += paramIdents[i];
                }
                funcCall += ");";

                functionBody = funcCall;
                return true;
            }

            headerPath = null;
            functionBody = null;
            return false;
        }
    }
}
