using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JScr.Frontend
{
    public class SyntaxException : Exception
    {
        public SyntaxError Error { get; private set; }

        public SyntaxException(SyntaxError error) : base(error.ToString())
        {
            Error = error;
        }
    }

    public class SyntaxError
    {
        public string Filedir { get; private set; }
        public uint   Line { get; private set; }
        public uint   Col { get; private set; }
        public long   ErrCode { get; private set; }
        public string Description { get; private set; }

        internal SyntaxError(string filedir, uint line, uint col, string description)
        {
            long errorCode = GenerateErrorCode(description);

            Filedir = Path.GetFullPath(filedir);
            Line = line;
            Col = col;
            ErrCode = errorCode;
            Description = description;

            errCodes.Add(errorCode);
        }

        private static long GenerateErrorCode(string errCode)
        {
            long num = 0;

            foreach (char c in errCode)
            {
                num += (int)c;
            }

            return num;
        }

        private static readonly List<long> errCodes = new();

        public static SyntaxError Unknown(string filedir, uint line, uint col) => new(filedir, line, col, "Unknown syntax error!");

        public override string ToString() => $"Syntax error at: \"{Filedir}\" [{Line}:{Col}] ({ErrCode}) \"{Description}\"";
        public string ToJson() => JsonSerializer.Serialize(this);
    }

    /*
    public class SyntaxError
    {
        public string Filedir { get; private set; }
        public int    Line { get; private set; }
        public int    Col { get; private set; }
        public short  ErrCode { get; private set; }
        public string MoreInfo { get; private set; }

        private SyntaxError(string filedir, int line, int col, string moreInfo)
        {
            short errorCode = (short)(errCodes.Last() + 1);

            Filedir = Path.GetFullPath(filedir);
            Line = line;
            Col = col;
            ErrCode = errorCode;
            MoreInfo = moreInfo;

            errCodes.Add(errorCode);
        }

        private static readonly List<short> errCodes = new();

        #region types
        public static SyntaxError Unknown(string filedir, int line, int col)                                   => new(filedir, line, col, "Unknown syntax error!");
        public static SyntaxError UnrecognizedCharacter(string filedir, int line, int col)                     => new(filedir, line, col, "Unrecognized character found in source file.");
        public static SyntaxError ExpectedAfollowingB(string filedir, int line, int col, string a, string b)   => new(filedir, line, col, $"Expected {a} following {b}.");
        #endregion

        public override string ToString() => $"Syntax error at: \"{Filedir}\" [{Line}:{Col}] ({ErrCode}) \"{MoreInfo}\".";
    }
    */
}
