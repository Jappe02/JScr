using static JScr.Frontend.Ast;

namespace JScr.Typing
{
    internal static class Types
    {
        /// <summary>Builtin default types that can be used by the aliases.</summary>
        private static readonly Dictionary<string, string> reservedTypes = new()
        {
            { "function", "JScr::Core::Func"    },
            { "void",     "JScr::Core::Void"    },
            { "byte",     "JScr::Core::Byte"    },
            { "bool",     "JScr::Core::Boolean" },
            { "char",     "JScr::Core::Char"    },
            { "short",    "JScr::Core::Int32"   },
            { "ushort",   "JScr::Core::UInt32"  },
            { "int",      "JScr::Core::Int64"   },
            { "uint",     "JScr::Core::UInt64"  },
            { "long",     "JScr::Core::Int128"  },
            { "ulong",    "JScr::Core::UInt128" },
            { "float",    "JScr::Core::Float"   },
            { "double",   "JScr::Core::Double"  },
            { "string",   "JScr::Core::String"  },
            { "object",   "JScr::Core::Object"  },
        };

        public static IReadOnlyDictionary<string, string> GetReservedTypes() => reservedTypes;

        public static string ValidateTypename(string type)
        {
            string valid = type.Replace(" ", "").Trim();

            if (reservedTypes.TryGetValue(valid, out var alias))
                return alias;

            if (valid == string.Empty)
                return "<ErrorType>";

            return valid;
        }

        public static Type VoidType() => new SimpleType("void");
    }

    internal abstract class Type
    {

    }

    internal class SimpleType : Type
    {
        public SimpleType(string typename, Type[]? genericTypeParameters = null)
        {
            this.typename = Types.ValidateTypename(typename);
            this.genericTypeParameters = genericTypeParameters ?? Array.Empty<Type>();
        }

        public readonly string typename;
        public readonly Type[] genericTypeParameters;
    }
}
