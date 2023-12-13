using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JScr.Frontend
{
    public class RuntimeException : Exception
    {
        public RuntimeException(string message) : base(message)
        {
            
        }

        public override string ToString() => JsonSerializer.Serialize(this);
    }
}
