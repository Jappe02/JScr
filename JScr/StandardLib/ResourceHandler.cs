using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace JScr.StandardLib
{
    internal static class ResourceHandler
    {
        public static string? GetStandardLibResource(string resourceName)
        {
            using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JScr.Resources.StandardLib." + resourceName))
            {
                return stream?.ToString();
            }
        }
    }
}
