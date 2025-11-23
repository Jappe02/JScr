using JScr.Typing;
using JScr.Utils;
using Range = JScr.Utils.Range;

namespace JScr.Frontend;

internal static class Lexer
{
    public enum TokenType
    {
        // Literal Types
        Number,
        FloatNumber,
        DoubleNumber,
        String,
        Char,
        Identifier,

        // Keywords
        This,
        Static, Const,
        Private, Protected, Public,
        Abstract, Virtual, Override, Extern,
        Return,
        If, Else, While, For,
        Struct, Enum, Class,
        New, Delete,
        Namespace, Import, As,

        // Gruping * Operators
        BinaryOperator,
        LessThan, MoreThan,
        LessThanOrEquals, MoreThanOrEquals,
        LogicalAnd,
        LogicalOr,
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseOnesComplement,
        BitwiseShiftLeft,
        BitwiseShiftRight,
        Not,
        NotEquals,
        Equals,
        Assignment,
        PtrMemberAccess,
        LambdaArrow,
        Comma, Colon, DoubleColon, Dot, At, Discard,
        Semicolon,
        OpenParen /*(*/, CloseParen /*)*/,
        OpenBrace /*{*/, CloseBrace /*}*/,
        OpenBracket /*[*/, CloseBracket /*]*/,
        EOF, // <-- Signified the end of file.
    }

    static readonly Dictionary<string, TokenType> _keywords = new()
    {
        { "this", TokenType.This },
        { "static", TokenType.Static },
        { "const", TokenType.Const },
        { "priv", TokenType.Private },
        { "prot", TokenType.Protected },
        { "pub", TokenType.Public },
        { "abst", TokenType.Abstract },
        { "virt", TokenType.Virtual },
        { "ovr", TokenType.Override },
        { "extern", TokenType.Extern },
        { "return", TokenType.Return },
        { "if", TokenType.If },
        { "else", TokenType.Else },
        { "while", TokenType.While },
        { "for", TokenType.For },
        { "struct", TokenType.Struct },
        { "enum", TokenType.Enum },
        { "class", TokenType.Class },
        { "new", TokenType.New },
        { "delete", TokenType.Delete },
        { "space", TokenType.Namespace },
        { "use", TokenType.Import },
        { "as", TokenType.As },
    };

    public class Token
    {
        public string Value { get; set; } = "";
        public TokenType Type { get; set; }

        public Token(string value, TokenType type)
        {
            Value = value;
            Type = type;
        }

        public Token(char value, TokenType type)
        {
            Value = value.ToString();
            Type = type;
        }
    }

    static bool IsAlpha(char src) => src.ToString().ToUpper() != src.ToString().ToLower() || src == '_' || src == '$';

    static bool IsSkippable(char src) => src == ' ' || src == '\n' || src == '\t' || src == '\r';

    static bool IsInt(char src)
    {
        var c = (int)src;
        var bounds = new int[]{(int)'0', (int)'9'};
        return c >= bounds[0] && c <= bounds[1];
    }

    public static Dictionary<Token, uint[]> Tokenize(string filedir, string text, Action<SyntaxError> errorCallback)
    {
        var tokens = new Dictionary<Token, uint[]>();
        var src = text.ToCharArray().ToList();

        uint line = 1;
        uint col = 0;

        bool insideComment = false;
        bool insideCommentMultiline = false;

        char Shift()
        {
            col++;
            return src.Shift();
        }

        void Push(Token token)
        {
            if (insideComment) return;

            tokens!.Add(token, [line, col]);
        }

        // Will return true if this is the beginning or the end of a comment.
        // Shifts twice before returning true to get rid of comment beginning and end parts.
        bool CommentModifier()
        {
            if (src.Count < 2) return false;

            if (src[0] == '/')
            {
                if (src[1] == '/') // "//"
                {
                    insideComment = true;
                    insideCommentMultiline = false;
                    Shift(); Shift();
                    return true;
                }
                else if (src[1] == '*') // "/*"
                {
                    insideComment = true;
                    insideCommentMultiline = true;
                    Shift(); Shift();
                    return true;
                }
            }
            else if (src[0] == '*' && src[1] == '/') // "*/"
            {
                insideComment = false;
                insideCommentMultiline = false;
                Shift(); Shift();
                return true;
            }

            return false;
        }

        // Build each token until end of file.
        while (src.Count > 0)
        {
            while (CommentModifier() || insideComment)
            {
                if (src.ElementAtOrDefault(0) == '\n')
                {
                    line++;
                    col = 0;
                    Shift(); // Skip newline

                    if (insideComment && !insideCommentMultiline)
                        insideComment = false;
                }
                else
                {
                    Shift(); // Shift for other comment characters
                }
            }

            if (src.Count == 0)
                break;

            // BEGIN PARSING ONE CHARACTER TOKENS
            if (src[0] == '(')
            {
                Push(new Token(Shift(), TokenType.OpenParen));
            }
            else if (src[0] == ')')
            {
                Push(new Token(Shift(), TokenType.CloseParen));
            }
            else if (src[0] == '{')
            {
                Push(new Token(Shift(), TokenType.OpenBrace));
            }
            else if (src[0] == '}')
            {
                Push(new Token(Shift(), TokenType.CloseBrace));
            }
            else if (src[0] == '[')
            {
                Push(new Token(Shift(), TokenType.OpenBracket));
            }
            else if (src[0] == ']')
            {
                Push(new Token(Shift(), TokenType.CloseBracket));
            }

            // HANDLE BINARY OPERATORS & COMMENTS
            else if (src[0] == '+' || src[0] == '-' || src[0] == '*' || src[0] == '/' || src[0] == '%')
            {
                var tk = Shift();

                if (src[0] == '>' && tk == '-')
                    Push(new Token(tk.ToString() + Shift(), TokenType.PtrMemberAccess));
                else
                    Push(new Token(tk, TokenType.BinaryOperator));
            }

            // HANDLE CONDITIONAL, BITWISE & ASSIGNMENT TOKENS
            else if (src[0] == '=')
            {
                var tk = Shift();

                if (src[0] == '=')
                    Push(new Token(tk.ToString() + Shift(), TokenType.Equals));
                else if (src[0] == '>')
                    Push(new Token(tk.ToString() + Shift(), TokenType.LambdaArrow));
                else
                    Push(new Token(tk, TokenType.Assignment));
            }
            else if (src[0] == ';')
            {
                Push(new Token(Shift(), TokenType.Semicolon));
            }
            else if (src[0] == ':')
            {
                var tk = Shift();

                if (src[0] == ':')
                    Push(new Token(tk.ToString() + Shift(), TokenType.DoubleColon));
                else
                    Push(new Token(tk, TokenType.Colon));
            }
            else if (src[0] == ',')
            {
                Push(new Token(Shift(), TokenType.Comma));
            } 
            else if (src[0] == '.')
            {
                Push(new Token(Shift(), TokenType.Dot));
            } 
            else if (src[0] == '@')
            {
                Push(new Token(Shift(), TokenType.At));
            }
            else if (src[0] == '^')
            {
                Push(new Token(Shift(), TokenType.BitwiseXor));
            }
            else if (src[0] == '~')
            {
                Push(new Token(Shift(), TokenType.BitwiseOnesComplement));
            }
            else if (src[0] == '_' && !IsAlpha(src[1]))
            {
                Push(new Token(Shift(), TokenType.Discard));
            }
            else if (src[0] == '<')
            {
                var tk = Shift();

                if (src[0] == '<')
                    Push(new Token(tk.ToString() + Shift(), TokenType.BitwiseShiftLeft));
                else if (src[0] == '=')
                    Push(new Token(tk.ToString() + Shift(), TokenType.LessThanOrEquals));
                else
                    Push(new Token(tk, TokenType.LessThan));
            } 
            else if (src[0] == '>')
            {
                var tk = Shift();

                if (src[0] == '>')
                    Push(new Token(tk.ToString() + Shift(), TokenType.BitwiseShiftRight));
                else if (src[0] == '=')
                    Push(new Token(tk.ToString() + Shift(), TokenType.MoreThanOrEquals));
                else
                    Push(new Token(tk, TokenType.MoreThan));
            } 
            else if (src[0] == '&')
            {
                var tk = Shift();

                if (src[0] == '&')
                    Push(new Token(tk.ToString() + Shift(), TokenType.LogicalAnd));
                else
                    Push(new Token(tk, TokenType.BitwiseAnd));
            } 
            else if (src[0] == '|')
            {
                var tk = Shift();

                if (src[0] == '|')
                    Push(new Token(tk.ToString() + Shift(), TokenType.LogicalOr));
                else
                    Push(new Token(tk, TokenType.BitwiseOr));
            } 
            else if (src[0] == '!')
            {
                var tk = Shift();

                if (src[0] == '=')
                    Push(new Token(tk.ToString() + Shift(), TokenType.NotEquals));
                else
                    Push(new Token(tk, TokenType.Not));
            }

            // HANDLE MULTICHARACTER KEYWORDS, TOKENS, IDENTIFIERS ETC...
            else
            {
                // Handle multicaracter tokens
                if (IsInt(src[0]))
                {
                    var num = "";
                    bool dot = false;
                    while (src.Count > 0 && (IsInt(src[0]) || src[0] == '.'))
                    {
                        if (dot && src[0] == '.') break;
                        if (src[0] == '.') dot = true;
                        num += Shift();
                    }

                    if (char.ToUpper(src[0]) == char.ToUpper('d'))
                    {
                        Shift();
                        Push(new Token(num, TokenType.DoubleNumber));
                    }
                    else if (char.ToUpper(src[0]) == char.ToUpper('f') || dot)
                    {
                        if (char.ToUpper(src[0]) == char.ToUpper('f'))
                            Shift();
                        Push(new Token(num, TokenType.FloatNumber));
                    }
                    else
                    {
                        Push(new Token(num, TokenType.Number));
                    }
                }
                else if (IsAlpha(src[0]))
                {
                    var ident = "";
                    while (src.Count > 0 && (IsAlpha(src[0]) || IsInt(src[0])))
                    {
                        ident += Shift();
                    }

                    // check for reserved keywords
                    TokenType? reserved = null;
                    if (_keywords.TryGetValue(ident, out var keywordType)) reserved = keywordType;
                    if (Types.GetReservedTypes().TryGetValue(ident, out var primitiveType)) ident = primitiveType.ToString();

                    if (reserved != null)
                    {
                        Push(new Token(ident, (TokenType)reserved));
                    }
                    else
                    {
                        Push(new Token(ident, TokenType.Identifier));
                    }
                }
                else if (IsSkippable(src[0]))
                {
                    if (src[0] == '\n') {
                        line++; col = 0;

                        if (insideComment && !insideCommentMultiline)
                            insideComment = false;
                    }

                    Shift(); // SKIP THE CURRENT CHARACTER
                }
                else if (src[0] == '"') {
                    Shift(); // < begin quote
                    var ident = "";

                    while (src.Count > 0 && src[0] != '"')
                    {
                        ident += Shift();
                    }

                    Shift(); // < end quote

                    Push(new Token(ident, TokenType.String));
                }
                else if (src[0] == '\'')
                {
                    Shift(); // < begin quote
                    char ident = Shift();
                    Shift(); // < end quote

                    Push(new Token(ident, TokenType.Char));
                }
                else
                {
                    errorCallback(new SyntaxError(filedir, new Range(new Position(line, col)), SyntaxErrorData.New(SyntaxErrorLevel.Error, "Unrecognized character found in source.")));
                }
            }
        }

        Push(new Token("EndOfFile", TokenType.EOF));
        return tokens;
    }
}