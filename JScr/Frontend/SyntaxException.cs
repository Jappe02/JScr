using Range = JScr.Utils.Range;

namespace JScr.Frontend;

public enum SyntaxErrorLevel
{
    Message,
    Warning,
    Error,
}

/// <summary>An exception that wraps a [SyntaxError].</summary>
public class SyntaxException : Exception
{
    public SyntaxError Error { get; private set; }

    public SyntaxException(SyntaxError error) : base(error.ToString())
    {
        Error = error;
    }

    public string ToJson() => Error.ToJson();
}

public class SyntaxError
{
    public string          Filedir { get; private set; }
    public Range           Pos { get; private set; }
    public long            ErrCode { get; private set; }
    public SyntaxErrorData Data { get; private set; }

    private readonly string asString;

    internal SyntaxError(string filedir, Range pos, SyntaxErrorData data)
    {
        long errorCode = GenerateErrorCode(data.Description);

        Filedir = Path.GetFullPath(filedir);
        Pos = pos;
        ErrCode = errorCode;
        Data = data;

        errCodes.Add(errorCode);

        asString = CreateString();
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

    private static readonly List<long> errCodes = [];

    //public static SyntaxError Unknown(string filedir, uint line, uint col) => new(filedir, line, col, "Unknown syntax error!");

    private string CreateString()
    {
        var startPos = Pos.from;
        string linestr = startPos.line.ToString();

        string LineNumber(bool hideNum = false)
        {
            string ret = string.Empty;

            if (hideNum)
            {
                for (int i = 0; i < linestr.Length; i++)
                    ret += " ";
            }
            else
            {
                ret = linestr;
            }

            ret += " | ";
            return ret;
        }

        string ReadLine()
        {
            string err = "Failed to output error message to the console.";
            string? line;

            try
            {
                line = File.ReadLines(Filedir).Skip((int)startPos.line - 1).FirstOrDefault();
            }
            catch (Exception e)
            {
                throw new Exception(err + " : " + e);
            }

            if (line == null)
                line = $"BUG: Failed to read line {startPos.line} for error message.";

            return line;
        }

        string fullLine = ReadLine();

        string str = $"\n{Data.Level.ToString().ToLower()}({ErrCode}): {Data.Description}";
        str += $"\n --> {Filedir} [{startPos.line}:{startPos.col}]";
        str += $"\n{LineNumber(true)}";
        str += $"\n{LineNumber()}{fullLine}";
        str += $"\n{LineNumber(true)}";

        for (int i = 1; i < startPos.col; i++)
            str += "-";

        str += "^";

        foreach (string hint in Data.Hints)
        {
            str += "\n --> hint: " + hint;
        }

        return str;
    }

    public override string ToString() => asString;
}

public class SyntaxErrorData
{
    public static SyntaxErrorData New(SyntaxErrorLevel level, string description)
        => new(level, description);

    public SyntaxErrorLevel Level { get; private set; }
    public string Description { get; private set; }
    public IReadOnlyList<string> Hints => hints.AsReadOnly();

    private List<string> hints = [];

    private SyntaxErrorData(SyntaxErrorLevel level, string description)
    {
        Level = level;
        Description = description;
    }

    public SyntaxErrorData AddHint(string hint)
    {
        hints.Add(hint);
        return this;
    }

    public SyntaxErrorData AddHintIf(bool predicate, string hint)
    {
        if (predicate)
            hints.Add(hint);
        return this;
    }
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