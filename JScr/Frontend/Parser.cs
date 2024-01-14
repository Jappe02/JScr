using JScr.Runtime;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using static JScr.Frontend.Ast;
using static JScr.Frontend.Lexer;
using static JScr.Runtime.Types;
using static JScr.Runtime.Values;

namespace JScr.Frontend
{
    internal class Parser
    {
        class ParseTypeContext
        {
            public Types.Type Type { get; }
            public bool Constant { get; }
            public bool Exported { get; }

            public ParseTypeContext(Types.Type type, bool constant, bool exported) {
                Type = type;
                Constant = constant;
                Exported = exported;
            }
        }

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
        
            return prev!;
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

            var program = new Program(filedir, new List<Stmt>());

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
                case TokenType.Import:
                    return ParseImportStmt();
                case TokenType.Export:
                case TokenType.Const:
                case TokenType.Type:
                    return ParseType<Stmt>()!;
                case TokenType.Object:
                    return ParseObjectStmt();
                case TokenType.Return:
                    return ParseReturnStmt();
                case TokenType.Delete:
                    return ParseDeleteStmt();
                case TokenType.If:
                    return ParseIfElseStmt();
                case TokenType.While:
                    return ParseWhileStmt();
                case TokenType.For:
                    return ParseForStmt();
                case TokenType.Identifier:
                {
                    if ((tokens[1].Type == TokenType.Const || tokens[1].Type == TokenType.Export || tokens[1].Type == TokenType.Identifier) && outline == 0)
                    {
                        return ParseType<Stmt>(true)!;
                    }
                    return ParseExpr();
                }
                default:
                    return ParseExpr();
            }
        }

        private Stmt ParseImportStmt()
        {
            Eat();

            var target = new List<string>();
            string? alias = null;

            while (NotEOF() && At().Type != TokenType.Semicolon)
            {
                target.Add(Eat().Value);

                if (At().Type == TokenType.Dot)
                {
                    Eat();
                } else
                {
                    break;
                }
            }

            if (At().Type == TokenType.As)
            {
                Eat();
                alias = Expect(TokenType.Identifier, "Identifier expected after `as` keyword.").Value;
            }

            Expect(TokenType.Semicolon, "Semicolon expected after import statement.");
            return new ImportStmt(target.ToArray(), alias);
        }

        /// <summary> T: ParseTypeContext | Types.Type | Stmt </summary>
        private T? ParseType<T>(bool identifierType = false) where T : class
        {
            Token? type = null;
            List<Token>? functionTypeListTk = null;
            bool constant = false;
            bool exported = false;

            while (((!identifierType ? At().Type == TokenType.Type : At().Type == TokenType.Identifier) && type == null) || (At().Type == TokenType.Const && !constant) || (At().Type == TokenType.Export && !exported) || (At().Type == TokenType.Function && functionTypeListTk == null))
            {
                if (At().Type == TokenType.Const)
                {
                    constant = true;
                    Eat();
                    continue;
                } else if ((!identifierType ? At().Type == TokenType.Type : At().Type == TokenType.Identifier))
                {
                    type = Eat();
                    if (At().Type == TokenType.OpenBracket) {
                        Eat();
                        Expect(TokenType.CloseBracket, "Closing bracket expected after open bracket in array declaration.");
                        type = new Token(type.Value + "[]", TokenType.Type);
                    }
                    continue;
                } else if (At().Type == TokenType.Export)
                {
                    Eat();
                    exported = true;
                    continue;
                } else if (At().Type == TokenType.Function)
                {
                    Eat();
                    functionTypeListTk = new();

                    outline++;
                    Expect(TokenType.OpenParen, "Open paren expected in lambda function declaration keyword.");

                    if (At().Type == TokenType.CloseParen)
                        continue;

                    functionTypeListTk.Add(Expect(TokenType.Type, "Type expected in lambda function declaration keyword."));

                    while (At().Type == TokenType.Comma && Eat() != null)
                    {
                        functionTypeListTk.Add(Expect(TokenType.Type, "Type expected in lambda function declaration keyword."));
                    }

                    Expect(TokenType.CloseParen, "Close paren expected in lambda function declaration keyword.");
                    outline--;
                    continue;
                }

                break;
            }

            if (type == null)
                ThrowSyntaxError("No declaration type specified.");

            // Do lambda types
            List<Types.Type> functionTypeList = new();
            if (functionTypeListTk != null)
            {
                foreach (var item in functionTypeListTk)
                    functionTypeList.Add(Types.FromString(item.Value) ?? Types.Type.Void());
            }

            // If the requested is only a type
            if (typeof(T) == typeof(Types.Type))
            {
                return ((T)(object)Types.FromString(type!.Value)?.CopyWith(lambdaTypes: functionTypeList.ToArray())!);
            } else if (typeof(T) == typeof(ParseTypeContext))
            {
                return ((T)(object)new ParseTypeContext(Types.FromString(type!.Value)?.CopyWith(lambdaTypes: functionTypeList.ToArray())!, constant, exported));
            } else if (typeof(T) != typeof(Stmt))
            {
                ThrowSyntaxError("Internal Error: Cannot use anything else than ( ParseTypeContext | Types.Type | Stmt ) as a generic type parameter for the `ParseType` method.");
            }


            // Get identifier
            var identifier = Expect(TokenType.Identifier, "Expected identifier after type.");

            ///var finalType = (T)(object)Types.FromString(type!.Value)?.CopyWith(lambdaTypes: functionTypeList.ToArray())!;

            // Return: Function
            if (At().Type == TokenType.OpenParen)
            {
                if (constant) ThrowSyntaxError("Functions cannot be declared constant.");
                return (T)(object)ParseFnDeclaration(type!, identifier, functionTypeList, exported);
            }

            // Return: Variable
            return (T)(object)ParseVarDeclaration(type!, identifier, functionTypeList, constant, exported);
        }

        private Stmt ParseFnDeclaration(Token type, Token name, List<Types.Type> lambdaFnTypelist, bool exposed)
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

            var body = new List<Stmt>();
            var instaret = false;
            if (At().Type == TokenType.OpenBrace)
            {
                Eat();
                while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
                {
                    body.Add(ParseStmt());
                }
                Expect(TokenType.CloseBrace, "Closing brace expected inside function declaration.");
            } else
            {
                instaret = true;
                Expect(TokenType.Equals, "Lambda arrow expected: Equals");
                Expect(TokenType.MoreThan, "Lambda arrow expected: MoreThan");
                body.Add(ParseStmt());
            }

            var fn = new FunctionDeclaration(exposed, args.ToArray(), name.Value, Types.FromString(type.Value)?.CopyWith(lambdaTypes: lambdaFnTypelist.ToArray()), body.ToArray(), instaret);
            return fn;
        }

        private List<VarDeclaration> ParseDeclarativeArgs()
        {
            outline++;

            Expect(TokenType.OpenParen, "Expected open parenthesis inside declarative arguments list.");
            var args = At().Type == TokenType.CloseParen ? new List<VarDeclaration>() : ParseDeclarativeArgsList();
            Expect(TokenType.CloseParen, "Expected closing parenthesis inside declarative arguments list.");

            outline--;
            return args;
        }

        private List<VarDeclaration> ParseDeclarativeArgsList()
        {
            VarDeclaration ParseParamVar()
            {
                var stmt = ParseType<Stmt>();

                if (stmt!.Kind != NodeType.VarDeclaration)
                    ThrowSyntaxError("Variable declaration expected inside declarative parameters list.");

                return stmt as VarDeclaration;
            }

            var args = new List<VarDeclaration>(){ ParseParamVar() };

            while (At().Type == TokenType.Comma && Eat() != null)
            {
                args.Add(ParseParamVar());
            }

            return args;
        }

        private Stmt ParseVarDeclaration(Token type, Token name, List<Types.Type> lambdaFnTypelist, bool constant, bool exposed)
        {
            //var isConstant = Eat().Type == TokenType.Const;
            //var identifier = Expect(TokenType.Identifier, "Expected identifier name following let | const keywords.").Value;

            VarDeclaration MkNoval()
            {
                if (constant)
                    ThrowSyntaxError("Must assign value to constant expression. No value provided.");

                return new VarDeclaration(false, exposed, Types.FromString(type.Value), name.Value, null);
            }

            if (outline == 0 && At().Type == TokenType.Semicolon)
            {
                Eat(); // eat semicolon
                return MkNoval();
            } else if (outline > 0 && At().Type != TokenType.Equals)
            {
                return MkNoval();
            }

            VarDeclaration declaration;
            var valType = Types.FromString(type.Value)?.CopyWith(lambdaTypes: lambdaFnTypelist.ToArray());
            outline++;
            if (At().Type == TokenType.Equals)
            {
                Eat(); // Advance past equal
                declaration = new VarDeclaration(constant, exposed, valType, name.Value, ParseExpr());
            } else
            {
                declaration = new VarDeclaration(constant, exposed, valType, name.Value, ParseObjectConstructorExpr(valType ?? Types.Type.Void(), true));
            }
            if (outline <= 1) Expect(TokenType.Semicolon, "Outline variable declaration statement must end with semicolon.");
            outline--;

            return declaration;
        }

        private Stmt ParseObjectStmt()
        {
            Eat();
            var name = Expect(TokenType.Identifier, "Object identifier expected.");
            Expect(TokenType.OpenBrace, "Open brace expected in object declaration.");

            var objectProperties = new List<Property>();
            bool export = false;
            outline++;
            while (NotEOF() && At().Type != TokenType.CloseBrace)
            {
                var type = ParseType<ParseTypeContext>();
                var key = Expect(TokenType.Identifier, "Object literal key expected.").Value;

                export = type!.Exported;

                // Allows shorthand key: pair -> { key, }.
                if (At().Type == TokenType.Comma)
                {
                    Eat(); // advance past comma
                    objectProperties.Add(new Property(key, type?.Type, null));
                    continue;
                }
                // Allows shorthand key: pair -> { key }.
                else if (At().Type == TokenType.CloseBrace)
                {
                    objectProperties.Add(new Property(key, type?.Type, null));
                    continue;
                }

                // { key: val }
                Expect(TokenType.Colon, "Missing colon following identifier in ObjectExpr.");
                var value = ParseExpr();

                objectProperties.Add(new Property(key, type?.Type, value));
                if (At().Type != TokenType.CloseBrace)
                {
                    Expect(TokenType.Comma, "Expected comma or closing bracket following property.");
                }
            }
            outline--;

            Expect(TokenType.CloseBrace, "Object declaration missing closing brace.");
            return new ObjectDeclaration(export, name.Value, objectProperties.ToArray());
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

        private Stmt ParseDeleteStmt()
        {
            Eat();

            outline++;
            var val = new DeleteDeclaration(Expect(TokenType.Identifier, "Delete identifier expected.").Value);
            Expect(TokenType.Semicolon, "Delete statement must end with semicolon.");
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
                var body = new List<Stmt>();
                if (At().Type == TokenType.OpenBrace)
                {
                    Eat();
                    while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
                    {
                        body.Add(ParseStmt());
                    }
                    Expect(TokenType.CloseBrace, "Closing brace expected inside 'if' statement.");
                } else
                {
                    body.Add(ParseStmt());
                }

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
                    // `else` Body
                    if (At().Type == TokenType.OpenBrace)
                    {
                        Eat();
                        while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
                        {
                            elseBody = new()
                            {
                                ParseStmt()
                            };
                        }
                        Expect(TokenType.CloseBrace, "Closing brace expected inside 'else' statement.");
                    } else
                    {
                        elseBody = new()
                        {
                            ParseStmt()
                        };
                    }
                }
            }

            return new IfElseDeclaration(blocks.ToArray(), elseBody?.ToArray());
        }

        private Stmt ParseWhileStmt()
        {
            Eat(); // eat while keyword

            // Condition
            outline++;
            Expect(TokenType.OpenParen, "Open paren expected after 'while' keyword.");
            var condition = ParseExpr();
            Expect(TokenType.CloseParen, "Close paren expected after 'while' condition.");
            outline--;

            // Body
            var body = new List<Stmt>();
            if (At().Type == TokenType.OpenBrace)
            {
                Eat();
                while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
                {
                    body.Add(ParseStmt());
                }
                Expect(TokenType.CloseBrace, "Closing brace expected inside 'while' statement.");
            } else
            {
                body.Add(ParseStmt());
            }

            return new WhileDeclaration(condition, body.ToArray());
        }

        private Stmt ParseForStmt()
        {
            Eat(); // eat for keyword

            // Condition
            outline++;
            
            Expect(TokenType.OpenParen, "Open paren expected after 'for' keyword.");
            var variableDecl = ParseStmt();
            Expect(TokenType.Semicolon, "Semicolon expected after variable declaration in 'for' loop.");

            var condition = ParseExpr();
            Expect(TokenType.Semicolon, "Semicolon expected after condition in 'for' loop.");
            
            var action = ParseExpr();
            Expect(TokenType.CloseParen, "Close paren expected after 'for' condition.");
            
            outline--;

            // Body
            var body = new List<Stmt>();
            if (At().Type == TokenType.OpenBrace)
            {
                Eat();
                while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
                {
                    body.Add(ParseStmt());
                }
                Expect(TokenType.CloseBrace, "Closing brace expected inside 'while' statement.");
            } else
            {
                body.Add(ParseStmt());
            }

            return new ForDeclaration(variableDecl, condition, action, body.ToArray());
        }

        private Expr ParseExpr()
        {
            return ParseAssignmentExpr();
        }

        private Expr ParseAssignmentExpr()
        {
            var left = ParseArrayExpr();

            if (At().Type == TokenType.Equals)
            {
                Eat(); // Advance past equal
                outline++;
                var value = ParseAssignmentExpr();
                if (outline <= 1) Expect(TokenType.Semicolon, "Semicolon expected after outline assignment expr.");
                outline--;
                return new AssignmentExpr(left, value);
            } else if (At().Type == TokenType.OpenBrace)
            {
                outline++;
                var value = ParseObjectConstructorExpr(left);
                if (outline <= 1) Expect(TokenType.Semicolon, "Semicolon expected after outline assignment expr.");
                outline--;
                return new AssignmentExpr(left, value);
            }

            return left;
        }

        private Expr ParseObjectConstructorExpr(dynamic targetVariableIdentifier, bool tviAsType = false)
        {
            if (!tviAsType && targetVariableIdentifier is not Identifier)
            {
                ThrowSyntaxError("Object constructor assignment only works for identifiers.");
            }

            Expect(TokenType.OpenBrace, "Open brace expected in object constructor.");

            var objectProperties = new List<Property>();
            while (NotEOF() && At().Type != TokenType.CloseBrace)
            {
                var key = Expect(TokenType.Identifier, "Object constructor key expected.").Value;

                // { key: val }
                Expect(TokenType.Colon, "Missing colon following identifier in ObjectConstructorExpr.");
                var value = ParseExpr();

                objectProperties.Add(new Property(key, null, value));
                if (At().Type != TokenType.CloseBrace)
                {
                    Expect(TokenType.Comma, "Expected comma or closing bracket following property.");
                }
            }

            Expect(TokenType.CloseBrace, "Object constructor missing closing brace.");
            return new ObjectConstructorExpr(targetVariableIdentifier, tviAsType, objectProperties.ToArray());
        }

        private Expr ParseArrayExpr()
        {
            if (At().Type != TokenType.OpenBrace)
            {
                return ParseLambdaFuncExpr();
            }

            Eat(); // advance past open brace
            var arrayElements = new List<Expr>();

            while (NotEOF() && At().Type != TokenType.CloseBrace)
            {
                var value = ParseExpr();
                arrayElements.Add(value);
                if (At().Type != TokenType.CloseBrace)
                {
                    Expect(TokenType.Comma, "Expected comma or closing bracket following array element.");
                }
            }

            Expect(TokenType.CloseBrace, "Array literal missing closing brace.");
            return new ArrayLiteral(arrayElements.ToArray());
        }

        private Expr ParseLambdaFuncExpr()
        {
            if (At().Type != TokenType.Lambda)
            {
                return ParseBoolExpr();
            }

            List<Identifier> ParseIdentList()
            {
                List<Identifier> list = new();

                void Add(Expr e)
                {
                    if (e.Kind != NodeType.Identifier)
                    {
                        ThrowSyntaxError("Identifier required as a param in lambda expression.");
                    }

                    list.Add(e as Identifier);
                }

                outline++;
                Eat();
                Expect(TokenType.OpenParen, "Open paren expected in lambda expression.");
                if (At().Type == TokenType.CloseParen)
                    return list;

                Add(ParsePrimaryExpr());

                while (At().Type == TokenType.Comma && Eat() != null)
                {
                    Add(ParsePrimaryExpr());
                }

                Expect(TokenType.CloseParen, "Close paren expected in lambda expression.");
                outline--;

                return list;
            }

            var identList = ParseIdentList();
            var body = new List<Stmt>();
            var instaret = false;
            if (At().Type == TokenType.OpenBrace)
            {
                outline--;
                Eat();
                while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
                {
                    body.Add(ParseStmt());
                }
                Expect(TokenType.CloseBrace, "Closing brace expected inside lambda declaration.");
                outline++;
            } else
            {
                instaret = true;
                Expect(TokenType.Equals, "Lambda arrow expected: Equals");
                Expect(TokenType.MoreThan, "Lambda arrow expected: MoreThan");
                body.Add(ParseStmt());
            }

            return new LambdaExpr(identList.ToArray(), body.ToArray(), instaret);
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
            } else if (At().Type == TokenType.OpenBracket)
            {
                return ParseIndexExpr(member);
            }

            return member;
        }

        private Expr ParseIndexExpr(Expr caller)
        {
            outline++;
            Expect(TokenType.OpenBracket, "Open bracket expected inside index expression.");
            Expr callExpr = new IndexExpr(ParseExpr(), caller);
            Expect(TokenType.CloseBracket, "Closing bracket expected inside index expression.");
            outline--;

            if (At().Type == TokenType.OpenBracket)
            {
                callExpr = ParseIndexExpr(callExpr);
            }

            return callExpr;
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

            Expect(TokenType.OpenParen, "Expected open parenthesis inside arguments list.");
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
                
                object_ = new MemberExpr(object_, property);
            }

            return object_;
        }

        private Expr ParsePrimaryExpr()
        {
            var tk = At().Type;
        
            switch (tk)
            {
                case TokenType.Identifier:
                {
                    return new Identifier(Eat().Value);
                }
                case TokenType.Number:
                    return new NumericLiteral(int.Parse(Eat().Value));
                case TokenType.String:
                    return new StringLiteral(Eat().Value);
                case TokenType.Char:
                    return new CharLiteral(Eat().Value.ToCharArray()[0]);
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
