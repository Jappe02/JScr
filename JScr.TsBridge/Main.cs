namespace JScr.TsBridge
{
    public class SyntaxError
    {
        public required string fileDir;
        public required uint line;
        public required uint col;
        public required long errorCode;
        public required string description;
    }

    public class Result
    {
        public required Script script;
        public required SyntaxError[] errors;
    }

    public class Script
    {
        public static async Task<object> FromFile(dynamic d)
        {
            var res = JScr.Script.FromFile((string)d.filedir);
            return res;
        }
    }
}
