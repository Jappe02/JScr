using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JScr.Frontend
{
    internal static class Lexer
    {
        public enum TokenType
        {
            // Literal Types
            Number,
            Identifier,

            // Keywords
            Let, Const, Fn,

            // Gruping * Operators
            BinaryOperator,
            Equals,
            Comma, Colon, Dot,
            Semicolon,
            OpenParen /*(*/, CloseParen /*)*/,
            OpenBrace /*{*/, CloseBrace /*}*/,
            OpenBracket /*[*/, CloseBracket /*]*/,
            EOF, // <-- Signified the end of file.
        }

        static readonly Dictionary<string, TokenType> KEYWORDS = new() {
            { "let", TokenType.Let},
            { "const", TokenType.Const},
            { "fn", TokenType.Fn},
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

            public override string ToString() => JsonSerializer.Serialize(this);
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

            char Shift()
            {
                col++;
                return src.Shift();
            }

            void Push(Token token)
            {
                tokens!.Add(token, new uint[] { line, col });
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

                // HANDLE BINARY OPERATORS
                else if (src[0] == '+' || src[0] == '-' || src[0] == '*' || src[0] == '/' || src[0] == '%')
                {
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
                        TokenType? reserved;
                        if (KEYWORDS.TryGetValue(ident, out var keywordType)) reserved = keywordType;
                        else reserved = null;

                        if (reserved is TokenType.Number)
                        {
                            Push(new Token(ident, (TokenType)reserved));
                        } else
                        {
                            Push(new Token(ident, TokenType.Identifier));
                        }
                    } else if (IsSkippable(src[0]))
                    {
                        if (src[0] == '\n') { line++; col = 0; }
                        Shift(); // SKIP THE CURRENT CHARACTER
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
