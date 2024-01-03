using JScr.Runtime;
using static JScr.Frontend.Ast;
using static JScr.Frontend.Lexer;
using static JScr.Runtime.Types;
using static JScr.Runtime.Values;

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
                case TokenType.Const:
                case TokenType.Type:
                    return ParseType(); // <-- TODO
                case TokenType.Return:
                    return ParseReturnStmt();
                case TokenType.If:
                    return ParseIfElseStmt();
                default:
                    return ParseExpr();
            }
        }

        private Stmt ParseType()
        {
            Token? type = null;
            bool constant = false;

            while (At().Type == TokenType.Type || At().Type == TokenType.Const)
            {
                if (At().Type == TokenType.Const && !constant)
                {
                    constant = true;
                    Eat();
                } else if (At().Type == TokenType.Type && type == null)
                {
                    type = Eat();
                }
            }

            if (type == null)
                ThrowSyntaxError("No declaration type specified.");

            var identifier = Expect(TokenType.Identifier, "Expected identifier after type.");

            // Function
            if (At().Type == TokenType.OpenParen)
            {
                if (constant) ThrowSyntaxError("Functions cannot be declared constant.");
                return ParseFnDeclaration(type!, identifier);
            }

            return ParseVarDeclaration(type!, identifier, constant);
        }

        private Stmt ParseFnDeclaration(Token type, Token name)
        {
            //Eat(); // eat the fn keyword
            //var name = Expect(TokenType.Identifier, "Expected function name following func keyword.").Value;
            var args = ParseDeclarativeArgs();
            foreach (var arg in args)
            {
                if (arg.Kind != NodeType.VarDeclaration)
                {
                    ThrowSyntaxError("Inside function declaration expected parameters to be variable declarations.");
                }
            }

            Expect(TokenType.OpenBrace, "Expected function body following declaration.");
            var body = new List<Stmt>();

            while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
            {
                body.Add(ParseStmt());
            }

            Expect(TokenType.CloseBrace, "Closing brace expected inside function declaration.");
            var fn = new FunctionDeclaration(args.ToArray(), name.Value, Types.FromString(type.Value), body.ToArray());

            return fn;
        }

        private List<VarDeclaration> ParseDeclarativeArgs()
        {
            outline++;

            Expect(TokenType.OpenParen, "Expected open parenthesis.");
            var args = At().Type == TokenType.CloseParen ? new List<VarDeclaration>() : ParseDeclarativeArgsList();
            Expect(TokenType.CloseParen, "Expected closing parenthesis inside arguments list.");

            outline--;
            return args;
        }

        private List<VarDeclaration> ParseDeclarativeArgsList()
        {
            VarDeclaration ParseCustomParameterVDecl()
            {
                var type = Expect(TokenType.Type, "Type expected inside declarative arguments list.");
                var ident = Expect(TokenType.Identifier, "Identifier expected after type inside declarative arguments list.");

                return new VarDeclaration(false, Types.FromString(type.Value), ident.Value, null);
            }

            var args = new List<VarDeclaration>(){ ParseCustomParameterVDecl() };

            while (At().Type == TokenType.Comma && Eat() != null)
            {
                args.Add(ParseCustomParameterVDecl());
            }

            return args;
        }

        private Stmt ParseVarDeclaration(Token type, Token name, bool constant)
        {
            //var isConstant = Eat().Type == TokenType.Const;
            //var identifier = Expect(TokenType.Identifier, "Expected identifier name following let | const keywords.").Value;

            if (At().Type == TokenType.Semicolon)
            {
                Eat(); // expect semicolon
                if (constant)
                    ThrowSyntaxError("Must assign value to constant expression. No value provided.");

                return new VarDeclaration(false, Types.FromString(type.Value), name.Value, null);
            }

            Expect(TokenType.Equals, "Expected equals token following identifier in var declaration.");
            outline++;
            var declaration = new VarDeclaration(constant, Types.FromString(type.Value), name.Value, ParseExpr());
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

        private Stmt ParseIfElseStmt()
        {
            IfElseDeclaration.IfBlock ParseElseIf()
            {
                Eat(); // eat if keyword

                // Condition
                outline++;
                Expect(TokenType.OpenParen, "Open paren expected after 'if' keyword.");
                var condition = ParseExpr();
                Expect(TokenType.CloseParen, "Close paren expected after 'if' condition.");
                outline--;

                // Body
                Expect(TokenType.OpenBrace, "Expected 'if' statement body following declaration.");
                var body = new List<Stmt>();
                while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
                {
                    body.Add(ParseStmt());
                }
                Expect(TokenType.CloseBrace, "Closing brace expected inside 'if' statement.");

                return new IfElseDeclaration.IfBlock(condition, body.ToArray());
            }

            List<IfElseDeclaration.IfBlock> blocks = new();
            List<Stmt>? elseBody = null;

            blocks.Add(ParseElseIf());

            if (At().Type == TokenType.Else)
            {
                Eat();

                if (At().Type == TokenType.If)
                {
                    blocks.Add(ParseElseIf());
                } else
                {
                    elseBody = new();
                    // `else` Body
                    Expect(TokenType.OpenBrace, "Expected 'else' statement body following declaration.");
                    while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
                    {
                        elseBody.Add(ParseStmt());
                    }
                    Expect(TokenType.CloseBrace, "Closing brace expected inside 'else' statement.");
                }
            }

            return new IfElseDeclaration(blocks.ToArray(), elseBody?.ToArray());
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
                return ParseBoolExpr();
            }

            Eat(); // advance past open brace
            var properties = new List<Property>();

            while (NotEOF() && At().Type != TokenType.CloseBrace)
            {
                var type = Expect(TokenType.Type, "Type expected before object literal key.");
                var key = Expect(TokenType.Identifier, "Object literal key expected.").Value;

                // Allows shorthand key: pair -> { key, }.
                if (At().Type == TokenType.Comma)
                {
                    Eat(); // advance past comma
                    properties.Add(new Property(key, Types.FromString(type.Value), null));
                    continue;
                }
                // Allows shorthand key: pair -> { key }.
                else if (At().Type == TokenType.CloseBrace)
                {
                    properties.Add(new Property(key, Types.FromString(type.Value), null));
                    continue;
                }

                // { key: val }
                Expect(TokenType.Colon, "Missing colon following identifier in ObjectExpr.");
                var value = ParseExpr();

                properties.Add(new Property(key, Types.FromString(type.Value), value));
                if (At().Type != TokenType.CloseBrace)
                {
                    Expect(TokenType.Comma, "Expected comma or closing bracket following property.");
                }
            }

            Expect(TokenType.CloseBrace, "Object literal missing closing brace.");
            return new ObjectLiteral(properties.ToArray());
        }

        private Expr ParseBoolExpr()
        {
            var left = ParseComparisonExpr();

            if (At().Type == TokenType.Or && tokens[1].Type == TokenType.Or)
            {
                Eat(); Eat();
                return new EqualityCheckExpr(left, ParseBoolExpr(), EqualityCheckExpr.Type.Or);
            } else if (At().Type == TokenType.And && tokens[1].Type == TokenType.And)
            {
                Eat(); Eat();
                return new EqualityCheckExpr(left, ParseBoolExpr(), EqualityCheckExpr.Type.And);
            }

            return left;
        }

        private Expr ParseComparisonExpr()
        {
            var left = ParseAdditiveExpr();

            if (At().Type == TokenType.Equals && tokens[1].Type == TokenType.Equals)
            {
                Eat(); Eat();
                return new EqualityCheckExpr(left, ParseAdditiveExpr(), EqualityCheckExpr.Type.Equals);
            } else if (At().Type == TokenType.Not && tokens[1].Type == TokenType.Equals)
            {
                Eat(); Eat();
                return new EqualityCheckExpr(left, ParseAdditiveExpr(), EqualityCheckExpr.Type.NotEquals);
            } else if (At().Type == TokenType.LessThan)
            {
                Eat();
                return new EqualityCheckExpr(left, ParseAdditiveExpr(), EqualityCheckExpr.Type.LessThan);
            } else if (At().Type == TokenType.LessThan && tokens[1].Type == TokenType.Equals)
            {
                Eat(); Eat();
                return new EqualityCheckExpr(left, ParseAdditiveExpr(), EqualityCheckExpr.Type.LessThanOrEquals);
            } else if (At().Type == TokenType.MoreThan)
            {
                Eat();
                return new EqualityCheckExpr(left, ParseAdditiveExpr(), EqualityCheckExpr.Type.MoreThan);
            } else if (At().Type == TokenType.MoreThan && tokens[1].Type == TokenType.Equals)
            {
                Eat(); Eat();
                return new EqualityCheckExpr(left, ParseAdditiveExpr(), EqualityCheckExpr.Type.MoreThanOrEquals);
            }

            return left;
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

        /// <param name="preEnd">Make something execute after the closing paren.</param>
        /// <returns>The args.</returns>
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

            while (At().Type == TokenType.Dot)
            {
                Eat();
                Expr property;

                // get identifier
                property = ParsePrimaryExpr();

                if (property.Kind != NodeType.Identifier)
                {
                    ThrowSyntaxError("Cannot use dot operator without right hand side being an identifier.");
                }
                
                object_ = new MemberExpr(object_, property as Identifier);
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
                    return new NumericLiteral(int.Parse(Eat().Value));
                case TokenType.String:
                    return new StringLiteral(Eat().Value);
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
