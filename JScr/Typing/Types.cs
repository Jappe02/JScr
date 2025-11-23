using JScr.Typing.TypeChecking;

namespace JScr.Typing;

internal static class Types
{
    /// <summary>Builtin default types that can be used by the aliases.</summary>
    private static readonly Dictionary<string, QualifiedName> _reservedTypes = new()
    {
        { "function", new QualifiedName("JScr::Core::Func")    },
        { "void",     new QualifiedName(VoidType())              },
        { "byte",     new QualifiedName("JScr::Core::Byte")    },
        { "bool",     new QualifiedName(BooleanType())           },
        { "char",     new QualifiedName("JScr::Core::Char")    },
        { "short",    new QualifiedName("JScr::Core::Int16")   },
        { "ushort",   new QualifiedName("JScr::Core::UInt16")  },
        { "int",      new QualifiedName(Int32Type())             },
        { "uint",     new QualifiedName("JScr::Core::UInt32")  },
        { "long",     new QualifiedName("JScr::Core::Int64")   },
        { "ulong",    new QualifiedName("JScr::Core::UInt64")  },
        { "float",    new QualifiedName("JScr::Core::Float")   },
        { "double",   new QualifiedName("JScr::Core::Double")  },
        { "string",   new QualifiedName("JScr::Core::String")  },
        { "object",   new QualifiedName("JScr::Core::Object")  },
    };

    public static IReadOnlyDictionary<string, QualifiedName> GetReservedTypes() => _reservedTypes;

    /*public static string ValidateTypename(string type)
    {
        string valid = type.Replace(" ", "").Trim();

        if (_reservedTypes.TryGetValue(valid, out var alias))
            return alias;

        if (valid == string.Empty)
            return "<ErrorType>";

        return valid;
    }*/

    public static QualifiedName VoidType() => new("JScr::Core::Void");
    public static QualifiedName BooleanType() => new("JScr::Core::Boolean");
    public static QualifiedName Int32Type() => new("JScr::Core::Int32");
    
    public static QualifiedName StdAnnotationType() => new("JScr::Core::Annotation");
}

public abstract class Type
{

}

// TODO: Change from name to path
/*internal class SimpleType : Type
{
    public SimpleType(string typename, Type[]? genericTypeParameters = null)
    {
        this.typename = Types.ValidateTypename(typename);
        this.genericTypeParameters = genericTypeParameters ?? Array.Empty<Type>();
    }

    public readonly string typename;
    public readonly Type[] genericTypeParameters;
}*/