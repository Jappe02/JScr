using System.Reflection;

namespace JScr.StandardLib
{
    internal static class ResourceHandler
    {
        public static string? GetStandardLibResource(string resourceName)
        {
            using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JScr.Resources.StandardLib." + resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
