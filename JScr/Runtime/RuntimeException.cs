using System.Text.Json;

namespace JScr.Frontend
{
    public class RuntimeException : Exception
    {
        public RuntimeException(string message) : base(message)
        {
            
        }
    }
}
