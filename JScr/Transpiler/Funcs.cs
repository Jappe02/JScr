namespace JScr.Transpiler
{
    /// <summary>Functions that the transpiler may need to append to C code.</summary>
    internal static class Funcs
    {
        private const string stdlib = "#include <stdlib.h>";

        public static string Sizeof(Environment env, string typename)
        {
            return "sizeof(" + typename + ")";
        }

        public static string Malloc(Environment env, string evaluated)
        {
            if (!env.top.Contains(stdlib))
                env.top.Add(stdlib);

            return "malloc(" + evaluated + ")" + (env.NoSemicolons ? null : ";");
        }

        public static string Free(Environment env, string evaluated)
        {
            if (!env.top.Contains(stdlib))
                env.top.Add(stdlib);

            return "free(" + evaluated + ")" + (env.NoSemicolons ? null : ";");
        }
    }
}
