using System.Text.RegularExpressions;

namespace JScr.Transpiler
{
    internal class TPString
    {
        public string Val { get; private set; }
        private readonly bool space = false;

        public TPString(bool space = false)
        {
            Val = "";
            this.space = space;

            if (space)
                Space();
        }

        public TPString(string val)
        {
            Val = ReplaceMultipleSpaces(val.Trim());
        }

        public TPString(bool space, string val)
        {
            this.space = space;
            Val = (space ? " " : null) + ReplaceMultipleSpaces(val.Trim());
        }

        public void Space()
        {
            if (Val.Last() != ' ')
                Val += " ";
        }

        public void Append(string? str)
        {
            if (str == null) return;

            if (space && Val.Length > 1)
                Val = Val.Remove(Val.Length - 1);

            Val += str.Trim();
            Val = ReplaceMultipleSpaces(Val);

            if (space)
                Space();
        }

        public override string ToString() => Val;

        private static string ReplaceMultipleSpaces(string input)
        {
            // Use regular expression to replace multiple spaces with a single space
            string pattern = @"\s+";
            string replacement = " ";
            Regex regex = new(pattern);
            string result = regex.Replace(input, replacement);

            return result;
        }

        public static TPString operator +(TPString left, TPString right)
        {
            var str = left;
            str.Append(right?.Val ?? "");
            return str;
        }

        public static TPString operator +(TPString left, string? right)
        {
            var str = left;
            str.Append(right ?? "");
            return str;
        }
    }
}
