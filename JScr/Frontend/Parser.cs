﻿using static JScr.Frontend.Ast;
using static JScr.Frontend.Lexer;

namespace JScr.Frontend
{
    internal class Parser
    {
        private List<Token> tokens = new();
        private List<uint[]> linesAndCols = new();

        private string filedir = "";

        /// <summary>
        /// Returns 0 if the current token is not between any type of equal sign or a semicolon,
        /// or inside a function call.
        /// Scope {} does not matter!
        /// </summary>
        private uint outline = 0;

        private bool NotEOF() => tokens[0].Type != TokenType.EOF;
        
        private Token At() => tokens[0];

        private uint AtLine() => linesAndCols[0][0];
        private uint AtCol() => linesAndCols[0][1];

        private Token Eat() { linesAndCols.Shift(); return tokens.Shift(); }
        
        private Token Expect(TokenType type, string syntaxExceptionDescription)
        {
            var prev = Eat();
            if (prev == null || prev.Type != type)
            {
                ThrowSyntaxError(syntaxExceptionDescription);
            }
        
            return prev;
        }

        private void ThrowSyntaxError(string description)
        {
            throw new SyntaxException(new(filedir, AtLine(), AtCol(), description));
        }
        
        public Program ProduceAST(string filedir, string sourceCode)
        {
            var tokenDictionary = Tokenize(filedir, sourceCode);
            tokens = tokenDictionary.Keys.ToList();
            linesAndCols = tokenDictionary.Values.ToList();
            linesAndCols.Add(linesAndCols.Last()); // <-- Add duplicate of last item to prevent index out of range exception if syntax error on last token.

            var program = new Program(new List<Stmt>());

            this.filedir = filedir;

            // Parse until end of file
            while (NotEOF())
            {
                program.Body.Add(ParseStmt());
            }
        
            return program;
        }
        
        private Stmt ParseStmt()
        {
            switch (At().Type)
            {
                case TokenType.Let:
                case TokenType.Const:
                    return ParseVarDeclaration();
                case TokenType.Func:
                    return ParseFnDeclaration();
                case TokenType.Return:
                    return ParseReturnStmt();
                default:
                    return ParseExpr();
            }
        }

        private Stmt ParseFnDeclaration()
        {
            Eat(); // eat the fn keyword
            var name = Expect(TokenType.Identifier, "Expected function name following func keyword.").Value;
            var args = ParseArgs();
            var params_ = new List<string>();
            foreach (var arg in args)
            {
                if (arg.Kind != NodeType.Identifier)
                {
                    ThrowSyntaxError("Inside function declaration expected parameters to be of type string.");
                }

                params_.Add((arg as Identifier).Symbol);
            }

            Expect(TokenType.OpenBrace, "Expected function body following declaration.");
            var body = new List<Stmt>();

            while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
            {
                body.Add(ParseStmt());
            }

            Expect(TokenType.CloseBrace, "Closing brace expected inside function declaration.");
            var fn = new FunctionDeclaration(params_.ToArray(), name, body.ToArray());

            return fn;
        }

        private Stmt ParseVarDeclaration()
        {
            var isConstant = Eat().Type == TokenType.Const;
            var identifier = Expect(TokenType.Identifier, "Expected identifier name following let | const keywords.").Value;

            if (At().Type == TokenType.Semicolon)
            {
                Eat(); // expect semicolon
                if (isConstant)
                    ThrowSyntaxError("Must assign value to constant expression. No value provided.");

                return new VarDeclaration(false, identifier, null);
            }

            Expect(TokenType.Equals, "Expected equals token following identifier in var declaration.");
            outline++;
            var declaration = new VarDeclaration(isConstant, identifier, ParseExpr());
            Expect(TokenType.Semicolon, "Variable declaration statement must end with semicolon.");
            outline--;

            return declaration;
        }

        private Stmt ParseReturnStmt()
        {
            Eat();

            outline++;
            var val = new ReturnDeclaration(ParseExpr());
            Expect(TokenType.Semicolon, "Return statement must end with semicolon.");
            outline--;

            return val;
        }

        private Expr ParseExpr()
        {
            return ParseAssignmentExpr();
        }

        private Expr ParseAssignmentExpr()
        {
            var left = ParseObjectExpr();

            if (At().Type == TokenType.Equals)
            {
                Eat(); // Advance past equal
                var value = ParseAssignmentExpr();
                return new AssignmentExpr(left, value);
            }

            return left;
        }

        private Expr ParseObjectExpr()
        {
            if (At().Type != TokenType.OpenBrace)
            {
                return ParseAdditiveExpr();
            }

            Eat(); // advance past open brace
            var properties = new List<Property>();

            while (NotEOF() && At().Type != TokenType.CloseBrace)
            {
                var key = Expect(TokenType.Identifier, "Object literal key expected.").Value;

                // Allows shorthand key: pair -> { key, }.
                if (At().Type == TokenType.Comma)
                {
                    Eat(); // advance past comma
                    properties.Add(new Property(key, null));
                    continue;
                }
                // Allows shorthand key: pair -> { key }.
                else if (At().Type == TokenType.CloseBrace)
                {
                    properties.Add(new Property(key, null));
                    continue;
                }

                // { key: val }
                Expect(TokenType.Colon, "Missing colon following identifier in ObjectExpr.");
                var value = ParseExpr();

                properties.Add(new Property(key, value));
                if (At().Type != TokenType.CloseBrace)
                {
                    Expect(TokenType.Comma, "Expected comma or closing bracket following property.");
                }
            }

            Expect(TokenType.CloseBrace, "Object literal missing closing brace.");
            return new ObjectLiteral(properties.ToArray());
        }

        private Expr ParseAdditiveExpr()
        {
            var left = ParseMultiplicitaveExpr();
        
            while (At().Value == "+" || At().Value == "-")
            {
                var operator_ = Eat().Value;
                var right = ParseMultiplicitaveExpr();
                left = new BinaryExpr(left, right, operator_);
            }
        
            return left;
        }
        
        private Expr ParseMultiplicitaveExpr()
        {
            var left = ParseCallMemberExpr();
        
            while (At().Value == "/" || At().Value == "*" || At().Value == "%")
            {
                var operator_ = Eat().Value;
                var right = ParseCallMemberExpr();
                left = new BinaryExpr(left, right, operator_);
            }
        
            return left;
        }

        private Expr ParseCallMemberExpr()
        {
            var member = ParseMemberExpr();

            if (At().Type == TokenType.OpenParen)
            {
                return ParseCallExpr(member);
            }

            return member;
        }

        private Expr ParseCallExpr(Expr caller)
        {
            Expr callExpr = new CallExpr(ParseArgs(() => {
                if (outline <= 1) Expect(TokenType.Semicolon, "Semicolon expected after outline call expression.");
            }), caller);

            if (At().Type == TokenType.OpenParen)
            {
                callExpr = ParseCallExpr(callExpr);
            }

            return callExpr;
        }

        private List<Expr> ParseArgs(Action? preEnd = null)
        {
            outline++;

            Expect(TokenType.OpenParen, "Expected open parenthesis.");
            var args = At().Type == TokenType.CloseParen ? new List<Expr>() : ParseArgumentsList();

            Expect(TokenType.CloseParen, "Expected closing parenthesis inside arguments list.");
            preEnd?.Invoke();

            outline--;
            return args;
        }

        private List<Expr> ParseArgumentsList()
        {
            var args = new List<Expr>(){ ParseAssignmentExpr() };

            while (At().Type == TokenType.Comma && Eat() != null)
            {
                args.Add(ParseAssignmentExpr());
            }

            return args;
        }

        private Expr ParseMemberExpr()
        {
            var object_ = ParsePrimaryExpr();

            while (At().Type == TokenType.Dot || At().Type == TokenType.OpenBracket)
            {
                var operator_ = Eat();
                Expr property;
                bool computed;

                // non-computed values aka obj.expr
                if (operator_.Type == TokenType.Dot)
                {
                    computed = false;
                    // get identifier
                    property = ParsePrimaryExpr();

                    if (property.Kind != NodeType.Identifier)
                    {
                        ThrowSyntaxError("Cannot use dot operator without right hand side being an identifier.");
                    }
                } else
                { // This allows obj[computedValue]
                    computed = true;
                    property = ParseExpr();
                    Expect(TokenType.CloseBracket, "Missing closing bracket in computed value.");
                }

                object_ = new MemberExpr(object_, property, computed);
            }

            return object_;
        }

        private Expr ParsePrimaryExpr()
        {
            var tk = At().Type;
        
            switch (tk)
            {
                case TokenType.Identifier:
                    return new Identifier(Eat().Value);
                case TokenType.Number:
                    return new NumericLiteral(float.Parse(Eat().Value));
                case TokenType.OpenParen:
                    Eat();
                    var value = ParseExpr();
                    Expect(TokenType.CloseParen, "Unexpected token found inside parenthesised expression. Expected closing parenthesis.");
                    return value;
        
                default:
                    ThrowSyntaxError($"Unexpected token found while parsing! {At()}");
                    return null; // <-- Never reached^
            }
        }
    }
}
