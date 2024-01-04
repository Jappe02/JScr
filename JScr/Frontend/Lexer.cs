using System.Linq;
using System.Text.Json;
using static JScr.Runtime.Types;
using static JScr.Runtime.Values;

namespace JScr.Frontend
{
    internal static class Lexer
    {
        public enum TokenType
        {
            Type,

            // Literal Types
            Number,
            String,
            Identifier,

            // Keywords
            Const, Return, If, Else, While, For,

            // Gruping * Operators
            BinaryOperator,
            LessThan, MoreThan,
            And, Or,
            Not,
            Equals,
            Comma, Colon, Dot,
            Semicolon,
            OpenParen /*(*/, CloseParen /*)*/,
            OpenBrace /*{*/, CloseBrace /*}*/,
            OpenBracket /*[*/, CloseBracket /*]*/,
            EOF, // <-- Signified the end of file.
        }

        static readonly Dictionary<string, TokenType> KEYWORDS = new() {
            { "const", TokenType.Const},
            { "return", TokenType.Return},
            { "if", TokenType.If},
            { "else", TokenType.Else},
            { "while", TokenType.While},
            { "for", TokenType.For},
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

        static bool IsAlpha(char src) => src.ToString().ToUpper() != src.ToString().ToLower();

        static bool IsSkippable(char src) => src == ' ' || src == '\n' || src == '\t' || src == '\r';

        static bool IsInt(char src)
        {
            var c = (int)src;
            var bounds = new int[]{(int)'0', (int)'9'};
            return c >= bounds[0] && c <= bounds[1];
        }

        public static Dictionary<Token, uint[]> Tokenize(string filedir, string text)
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

                tokens!.Add(token, new uint[] { line, col });
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
                    } else if (src[1] == '*') // "/*"
                    {
                        insideComment = true;
                        insideCommentMultiline = true;
                        Shift(); Shift();
                        return true;
                    }
                } else if (src[0] == '*')
                {
                    if (src[1] == '/') // "*/"
                    {
                        insideComment = false;
                        insideCommentMultiline = false;
                        Shift(); Shift();
                        return true;
                    }
                }

                return false;
            }

            // Build each token until end of file.
            while (src.Count > 0)
            {
                // BEGIN PARSING ONE CHARACTER TOKENS
                if (src[0] == '(')
                {
                    Push(new Token(Shift(), TokenType.OpenParen));
                } else if (src[0] == ')')
                {
                    Push(new Token(Shift(), TokenType.CloseParen));
                } else if (src[0] == '{')
                {
                    Push(new Token(Shift(), TokenType.OpenBrace));
                } else if (src[0] == '}')
                {
                    Push(new Token(Shift(), TokenType.CloseBrace));
                } else if (src[0] == '[')
                {
                    Push(new Token(Shift(), TokenType.OpenBracket));
                } else if (src[0] == ']')
                {
                    Push(new Token(Shift(), TokenType.CloseBracket));
                }

                // HANDLE BINARY OPERATORS & COMMENTS
                else if (src[0] == '+' || src[0] == '-' || src[0] == '*' || src[0] == '/' || src[0] == '%')
                {
                    if (!CommentModifier())
                        Push(new Token(Shift(), TokenType.BinaryOperator));
                }

                // HANDLE CONDITIONAL & ASSIGNMENT TOKENS
                else if (src[0] == '=')
                {
                    Push(new Token(Shift(), TokenType.Equals));
                } else if (src[0] == ';')
                {
                    Push(new Token(Shift(), TokenType.Semicolon));
                } else if (src[0] == ':')
                {
                    Push(new Token(Shift(), TokenType.Colon));
                } else if (src[0] == ',')
                {
                    Push(new Token(Shift(), TokenType.Comma));
                } else if (src[0] == '.')
                {
                    Push(new Token(Shift(), TokenType.Dot));
                } else if (src[0] == '<')
                {
                    Push(new Token(Shift(), TokenType.LessThan));
                } else if (src[0] == '>')
                {
                    Push(new Token(Shift(), TokenType.MoreThan));
                } else if (src[0] == '&')
                {
                    Push(new Token(Shift(), TokenType.And));
                } else if (src[0] == '|')
                {
                    Push(new Token(Shift(), TokenType.Or));
                } else if (src[0] == '!')
                {
                    Push(new Token(Shift(), TokenType.Not));
                }

                // HANDLE MULTICHARACTER KEYWORDS, TOKENS, IDENTIFIERS ETC...
                else
                {
                    // Handle multicaracter tokens
                    if (IsInt(src[0]))
                    {
                        var num = "";
                        while (src.Count > 0 && IsInt(src[0]))
                        {
                            num += Shift();
                        }

                        Push(new Token(num, TokenType.Number));
                    } else if (IsAlpha(src[0]))
                    {
                        var ident = "";
                        while (src.Count > 0 && IsAlpha(src[0]))
                        {
                            ident += Shift();
                        }

                        // check for reserved keywords
                        TokenType? reserved = null;
                        if (KEYWORDS.TryGetValue(ident, out var keywordType)) reserved = keywordType;
                        else if (reservedTypesDict.Values.Contains(ident)) reserved = TokenType.Type;

                        if (reserved != null)
                        {
                            Push(new Token(ident, (TokenType)reserved));
                        } else
                        {
                            Push(new Token(ident, TokenType.Identifier));
                        }
                    } else if (IsSkippable(src[0]))
                    {
                        if (src[0] == '\n') {
                            line++; col = 0;

                            if (insideComment && !insideCommentMultiline)
                                insideComment = false;
                        }

                        Shift(); // SKIP THE CURRENT CHARACTER
                    } else if (src[0] == '"') {
                        Shift();
                        var ident = "";

                        while (src.Count > 0 && src[0] != '"')
                        {
                            ident += Shift();
                        }

                        Push(new Token(ident, TokenType.String));
                    } else
                    {
                        throw new SyntaxException(new(filedir, line, col, "Unrecognized character found in source."));
                    }
                }
            }

            Push(new Token("EndOfFile", TokenType.EOF));
            return tokens;
        }
    }
}
