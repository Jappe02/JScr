using JScr.Frontend;

namespace JScr.Transpiler
{
    internal static class Helpers
    {
        public const string identifierDataSeparator = "$";

        public struct MemberData
        {
            public string classname;
            public Ast.Visibility membervis;
            public string membername;
        }
        /*
        public static TPString TypeToString(Types.Type type)
        {
            TPString str = new();

            if (type.IsLambda)
            {
                str.Append("std::function<", true);

                var typeStr = Types.reservedTypesDict.GetValueOrDefault(type);
                if (typeStr != null)
                    str.Append(typeStr, true);

                if (type.Data != null)
                    str.Append(type.Data, true);
                else if (type == Types.Type.Array(type.Child ?? Types.Type.Void()))
                    str.Append(Types.reservedTypesDict.GetValueOrDefault(type.Child), true);


                str.Append(">", true);
            }

            return new();
        }*/

        public static string RandomName(string prefix)
        {
            var random = new Random();
            var rint = random.Next(1000, 9999 + 1);
            return "__" + prefix.Trim().ToLower() + rint;
        }

        public static string Cast(string typename, string evaluated)
        {
            return $"({typename})" + evaluated;
        }

        public static string MemberNameFromData(MemberData data)
        {
            return data.classname + identifierDataSeparator + (byte)data.membervis + identifierDataSeparator + data.membername;
        }

        public static MemberData? DataFromMemberName(string fullMemberName)
        {
            string[] split = fullMemberName.Split(identifierDataSeparator);
            if (split.Length < 3)
                return null;

            if (!byte.TryParse(split[1], out var vis))
                return null;

            return new MemberData { classname = split[0], membervis = (Ast.Visibility)vis, membername = split[2] };
        }
    }
}
