using JScr.Typing;
using static JScr.Frontend.Ast;
using static JScr.Frontend.Lexer;
using Type = JScr.Typing.Type;

namespace JScr.Frontend
{
    // TODO: NEW OPERATORS
    internal class Parser
    {
        interface ParseTypeCtx
        {
            public bool Constant { get; }
            public Visibility Visibility { get; }
            public AnnotationUsageDeclaration[] Annotations { get; }
        }

        /// <summary>
        /// variables, functions
        /// </summary>
        class ParseTypeCtxVar : ParseTypeCtx
        {
            public bool Constant { get; }
            public Visibility Visibility { get; }
            public InheritanceModifier? Modifier { get; }
            public AnnotationUsageDeclaration[] Annotations { get; }
            public Type Type { get; }

            public ParseTypeCtxVar(bool constant, Visibility visibility, InheritanceModifier? modifier, Type type, AnnotationUsageDeclaration[] annotations)
            {
                Constant = constant;
                Visibility = visibility;
                Modifier = modifier;
                Annotations = annotations;
                Type = type;
            }
        }

        /// <summary>
        /// classes, structs, enums
        /// </summary>
        class ParseTypeCtxObjOrEnum : ParseTypeCtx
        {
            public bool Constant { get; }
            public Visibility Visibility { get; }
            public bool IsAbstract { get; }
            public AnnotationUsageDeclaration[] Annotations { get; }
            public Token Type { get; }
            public Type IdentifierT { get; }

            public ParseTypeCtxObjOrEnum(bool constant, Visibility visibility, bool abstract_, Token type, Type identifierT, AnnotationUsageDeclaration[] annotations)
            {
                Constant = constant;
                Visibility = visibility;
                IsAbstract = abstract_;
                Annotations = annotations;
                Type = type;
                IdentifierT = identifierT;
            }
        }


        //class ParseTypeContext
        //{
        //    public Types.Type Type { get; }
        //    public bool Constant { get; }
        //    public bool Exported { get; }
        //
        //    public ParseTypeContext(Types.Type type, bool constant, bool exported) {
        //        Type = type;
        //        Constant = constant;
        //        Exported = exported;
        //    }
        //}

        /// <summary>Never access items using the f[] operator, use the At(int offset = 0) function instead.</summary>
        private List<Token> tokens = new();
        /// <summary>Never access items using the f[] operator, use the AtLine() or AtCol() functions instead.</summary>
        private List<uint[]> linesAndCols = new();
        private Action<SyntaxError>? errorCallback;

        private string filedir = "";

        private bool insideUndoArea = false;
        private int undoAreaOffset = 0;
        private Action<SyntaxError>? undoAreaErrorHandler = null;
        //private List<Token> undoTokens = new();
        //private List<uint[]> undoLinesAndCols = new();

        /// <summary>
        /// Returns 0 if the current token is not between any type of equal sign or a semicolon,
        /// or inside a function call.
        /// Scope {} does not matter!
        /// </summary>
        private int outline = 0;

        private bool NotEOF() => At().Type != TokenType.EOF;
        private bool SemicolonNeeded() => outline < 1;

        private void AddOutline(Action action)
        {
            outline++;

            try
            {
                action();
            }
            finally
            {
                outline--;
            }
        }

        private void SubtractOutline(Action action)
        {
            outline--;

            try
            {
                action();
            }
            finally
            {
                outline++;
            }
        }

        private int FirstTokenOffset() => !insideUndoArea ? 0 : undoAreaOffset;
        private Token At(int offset = 0) => tokens[FirstTokenOffset() + offset];

        private uint AtLine() => linesAndCols[FirstTokenOffset()][0];
        private uint AtCol() => linesAndCols[FirstTokenOffset()][1];

        private Token Eat()
        {
            if (insideUndoArea)
            {
                var tk = tokens[undoAreaOffset];
                undoAreaOffset++;
                return tk;
            }
            else
            {
                linesAndCols.Shift();
                return tokens.Shift();
            }
        }
        
        private Token Expect(TokenType type, SyntaxErrorData data)
        {
            var prev = Eat();
            if (prev == null || prev.Type != type)
            {
                ThrowSyntaxError(data);
            }
        
            return prev!;
        }

        /// <summary>Continue parsing, but token eaten after this function can be undone.</summary>
        /// <param name="errorHandler">An error handler that catches all errors while parsing. Leave null to not handle them.</param>
        private void BeginUndoArea(Action<SyntaxError>? errorHandler = null)
        {
            if (insideUndoArea)
                throw new Exception("BUG: Cannot call BeginUndoArea before the undo area has been closed.");

            undoAreaErrorHandler = errorHandler;
            undoAreaOffset = 0;
            insideUndoArea = true;
        }

        /// <summary>Put back all "removed" tokens.</summary>
        private void CancelUndoArea()
        {
            undoAreaErrorHandler = null;
            undoAreaOffset = 0;
            insideUndoArea = false;
        }

        /// <summary>Deletes every token "removed" in the current undo area permanently.</summary>
        private void PurgeUndoArea()
        {
            for (int i = 0; i < undoAreaOffset; i++)
            {
                tokens.Shift();
                linesAndCols.Shift();
            }

            undoAreaErrorHandler = null;
            undoAreaOffset = 0;
            insideUndoArea = false;
        }

        //private void BeginUndoArea()
        //{
        //    undoTokens.Clear();
        //    undoLinesAndCols.Clear();
        //    insideUndoArea = true;
        //}
        //
        //private void CancelUndoArea()
        //{
        //    undoTokens.Clear();
        //    undoLinesAndCols.Clear();
        //    insideUndoArea = false;
        //}
        //
        //private void UndoUndoArea()
        //{
        //    for (int i = undoTokens.Count - 1; i >= 0; i--)
        //        tokens.Insert(0, undoTokens[i]);
        //
        //    for (int i = undoLinesAndCols.Count - 1; i >= 0; i--)
        //        linesAndCols.Insert(0, undoLinesAndCols[i]);
        //
        //    undoTokens.Clear();
        //    undoLinesAndCols.Clear();
        //    insideUndoArea = false;
        //}

        private void ThrowSyntaxError(SyntaxErrorData data, bool canContinue = false)
        {
            var err = new SyntaxError(filedir, AtLine(), AtCol(), data);

            if (undoAreaErrorHandler != null)
            {
                undoAreaErrorHandler?.Invoke(err);
                return;
            }
            
            errorCallback!(err);

            if (!canContinue)
                throw new SyntaxException(err);
        }

        public Program ProduceAST(string filedir, string sourceCode, Action<SyntaxError> errorCallback)
        {
            this.errorCallback = errorCallback;

            var tokenDictionary = Tokenize(filedir, sourceCode, this.errorCallback);
            tokens = tokenDictionary.Keys.ToList();
            linesAndCols = tokenDictionary.Values.ToList();
            linesAndCols.Add(linesAndCols.Last()); // <-- Add duplicate of last item to prevent index out of range exception if syntax error on last token.

            var program = new Program(filedir, new List<Stmt>());

            this.filedir = filedir;

            // Parse until end of file
            try
            {
                while (NotEOF())
                {
                    program.Body.Add(ParseStmt());
                }
            }
            catch (SyntaxException)
            {

            }
            
            return program;
        }

        #region Statements

        private Stmt ParseStmt()
        {
            switch (At().Type)
            {
                case TokenType.Namespace:
                    return ParseNamespaceStmt();
                case TokenType.Import:
                    return ParseImportStmt();
                case TokenType.Private:
                case TokenType.Protected:
                case TokenType.Public:
                case TokenType.Abstract:
                case TokenType.Virtual:
                case TokenType.Const:
                case TokenType.Class:
                case TokenType.Struct:
                case TokenType.Enum:
                case TokenType.At:
                    return ParseDeclStmt();
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
                    if ((At(1).Type == TokenType.Identifier || At(1).Type == TokenType.LessThan) && outline == 0)
                    {
                        return ParseDeclStmt();
                    }
                    return ParseExpr();
                }
                case TokenType.OpenBrace:
                    return ParseBlockStmt();
                default:
                    return ParseExpr();
            }
        }

        private List<string> ParseStaticNamespaceOrTypeName()
        {
            var err = "Namespace or type name expected.";
            var target = new List<string>();

            target.Add(Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, err)).Value);

            while (NotEOF() && At().Type == TokenType.DoubleColon)
            {
                Eat();

                target.Add(Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, err)).Value);
            }

            return target;
        }

        private Stmt ParseNamespaceStmt()
        {
            Eat();

            var target = ParseStaticNamespaceOrTypeName();

            Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after namespace statement."));
            return new NamespaceStmt(target.ToArray());
        }

        private Stmt ParseImportStmt()
        {
            Eat();

            var target = ParseStaticNamespaceOrTypeName();
            string? alias = null;

            if (At().Type == TokenType.As)
            {
                Eat();
                alias = Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Identifier expected after `as` keyword.")).Value;
            }

            Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after import statement."));
            return new ImportStmt(target.ToArray(), alias);
        }

        private List<AnnotationUsageDeclaration> ParseTypeAnnotations()
        {
            List<AnnotationUsageDeclaration> annotations = new();

            while (At().Type == TokenType.At)
            {
                Eat();

                var identifier = Expect(
                    TokenType.Identifier,
                    SyntaxErrorData.New(SyntaxErrorLevel.Error, "Annotation type expected.")
                        .AddHint("Try adding the name of a type after the `@` token.")
                );
                List<Expr> args = new();

                if (At().Type == TokenType.OpenParen)
                {
                    args = ParseArgs();
                }

                annotations.Add(new(identifier.Value, args.ToArray()));
            }

            return annotations;
        }

        private Type ParseType()
        {
            SimpleType SimpleType()
            {
                string typename = string.Empty;
                List<Type> genericArgs = new();

                if (At().Type == TokenType.Identifier)
                    typename = Eat().Value;
                else
                    ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Type expected."));

                if (At().Type == TokenType.LessThan)
                {
                    Eat();

                    genericArgs.Add(SimpleType());
                    while (At().Type == TokenType.Comma && Eat() != null)
                    {
                        genericArgs.Add(SimpleType());
                    }

                    Expect(TokenType.MoreThan, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected end of type parameter list."));
                }

                return new SimpleType(typename, genericArgs.ToArray());
            }

            return SimpleType();
        }

        // Example: "pub const bool" | "pub class MyClass"
        private ParseTypeCtx ParseTypeWithModifiers()
        {
            List<AnnotationUsageDeclaration> annotations = new();
            bool? constant = null;
            bool? abstract_ = null;
            bool? virtual_ = null;
            Visibility? visibility = null;
            InheritanceModifier? modifier = null;
            Token? objectTk = null;
            Type? type = null;

            // Annotations
            annotations = ParseTypeAnnotations();

            // Modifiers
            bool ConstCheck() => At().Type == TokenType.Const;
            bool VisibilityCheck() => At().Type == TokenType.Private || At().Type == TokenType.Protected || At().Type == TokenType.Public;
            bool AbstractCheck() => At().Type == TokenType.Abstract;
            bool VirtualCheck() => At().Type == TokenType.Virtual;

            while
            (
                (constant == null && ConstCheck()) ||
                (visibility == null && VisibilityCheck()) ||
                (abstract_ == null && AbstractCheck()) ||
                (virtual_ == null && VirtualCheck())
            )
            {
                if (ConstCheck())
                {
                    constant = true;
                    Eat();
                    continue;
                }
                else if (VisibilityCheck())
                {
                    switch (At().Type)
                    {
                        case TokenType.Private:   visibility = Visibility.Private; break;
                        case TokenType.Protected: visibility = Visibility.Protected; break;
                        case TokenType.Public:    visibility = Visibility.Public; break;
                    }

                    Eat();
                    continue;
                }
                else if (AbstractCheck())
                {
                    abstract_ = true;
                    Eat();
                    continue;
                }
                else if (VirtualCheck())
                {
                    virtual_ = true;
                    Eat();
                    continue;
                }

                break;
            }

            constant ??= false;
            visibility ??= Visibility.Private;
            abstract_ ??= false;
            virtual_ ??= false;

            if ((bool)abstract_ && (bool)virtual_)
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Both the abstract and virtual keywords cannot be used for the same item at the same time."), true);

            if ((bool)abstract_)
                modifier = InheritanceModifier.Abstract;
            else if ((bool)virtual_)
                modifier = InheritanceModifier.Virtual;

            // Object declaration token
            if (At().Type == TokenType.Struct || At().Type == TokenType.Enum || At().Type == TokenType.Class)
                objectTk = Eat();

            // Type
            type = ParseType();

            if (objectTk != null && type is not SimpleType)
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Invalid type declaration."), true);

            // Return

            if (objectTk != null)
                if (modifier != InheritanceModifier.Abstract && modifier != null)
                    ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Classes, structs and enums cannot be virtual."), true);
                else
                    return new ParseTypeCtxObjOrEnum((bool)constant, (Visibility)visibility, modifier == InheritanceModifier.Abstract, objectTk, type, annotations.ToArray());

            return new ParseTypeCtxVar((bool)constant, (Visibility)visibility, modifier, type, annotations.ToArray());
        }
    
        private Stmt ParseDeclStmt()
        {
            var type = ParseTypeWithModifiers();

            if (type is ParseTypeCtxObjOrEnum tp)
            {
                if (tp.Constant)
                    ThrowSyntaxError(
                        SyntaxErrorData.New(SyntaxErrorLevel.Error, "Cannot declare class, struct or enum as constant.")
                            .AddHint("Try removing the `const` keyword.")
                       , true);

                switch (tp.Type.Type)
                {
                    case TokenType.Struct:
                        if (tp.IsAbstract)
                            ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Structs cannot be abstract.").AddHint("Try removing the `abst` keyword."), true);

                        return ParseStructStmt(tp);
                    case TokenType.Enum:
                        if (tp.IsAbstract)
                            ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Enums cannot be abstract.").AddHint("Try removing the `abst` keyword."), true);

                        return ParseEnumStmt(tp);
                    case TokenType.Class:
                        return ParseClassStmt(tp);
                }

                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Internal Error: Invalid type."));
            }

            // Get identifier
            var identifier = Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected identifier after type for function/variable declarations."));

            // Return: Function
            if (At().Type == TokenType.OpenParen)
            {
                if (type.Constant)
                    ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Functions cannot be declared constant.").AddHint("Try removing the `const` keyword."), true);

                return ParseFnDeclaration((type as ParseTypeCtxVar)!, identifier);
            }

            // Return: Variable
            return ParseVarDeclaration((type as ParseTypeCtxVar)!, identifier);
        }

        private Stmt ParseFnDeclaration(ParseTypeCtxVar type, Token name)
        {
            var args = ParseDeclarativeArgs();
            foreach (var arg in args)
            {
                if (arg.Kind != NodeType.VarDeclaration)
                {
                    ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Inside function declaration expected parameters to be variable declarations."));
                }
            }

            bool override_ = false;
            if (At().Type == TokenType.Override)
            {
                Eat();
                override_ = true;
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
                Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Closing brace expected inside function declaration."));
            }
            else
            {
                instaret = true;
                Expect(TokenType.LambdaArrow, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Lambda arrow expected inside function declaration.").AddHint("Try inserting an `=>`."));
                body.Add(ParseStmt());
            }

            var fn = new FunctionDeclaration(type.Annotations, type.Visibility, type.Modifier, override_, args.ToArray(), name.Value, type.Type, body.ToArray(), instaret);
            return fn;
        }

        private List<VarDeclaration> ParseDeclarativeArgs()
        {
            List<VarDeclaration> args = new();

            AddOutline(() =>
            {
                Expect(TokenType.OpenParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected open parenthesis inside declarative arguments list."));
                args = At().Type == TokenType.CloseParen ? new List<VarDeclaration>() : ParseDeclarativeArgsList();
                Expect(TokenType.CloseParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected closing parenthesis inside declarative arguments list."));
            });
            
            return args;
        }

        private List<VarDeclaration> ParseDeclarativeArgsList()
        {
            VarDeclaration ParseParamVar()
            {
                var stmt = ParseDeclStmt();

                if (stmt?.Kind != NodeType.VarDeclaration)
                    ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Variable declaration expected inside declarative parameters list."));

                return (stmt as VarDeclaration)!;
            }

            var args = new List<VarDeclaration>(){ ParseParamVar() };

            while (At().Type == TokenType.Comma && Eat() != null)
            {
                args.Add(ParseParamVar());
            }

            return args;
        }

        private Stmt ParseVarDeclaration(ParseTypeCtxVar type, Token name)
        {
            bool override_ = false;

            VarDeclaration MkNoval()
            {
                if (type.Constant)
                    ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Must assign value to constant expression. No value provided."));

                return new VarDeclaration(type.Annotations, false, type.Visibility, type.Modifier, override_, type.Type, name.Value, null);
            }

            if (At().Type == TokenType.Override)
            {
                Eat();
                override_ = true;
            }

            if (SemicolonNeeded() && At().Type == TokenType.Semicolon)
            {
                Eat(); // eat semicolon
                return MkNoval();
            } 
            else if (!SemicolonNeeded() && At().Type != TokenType.Assignment)
            {
                return MkNoval();
            }

            VarDeclaration? declaration = null;
            AddOutline(() =>
            {
                Expect(TokenType.Assignment, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected assignment operator for variable declaration."));
                declaration = new(type.Annotations, type.Constant, type.Visibility, type.Modifier, override_, type.Type, name.Value, ParseExpr());
            });
            if (SemicolonNeeded()) Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Outline variable declaration statement must end with semicolon."));

            return declaration!;
        }

        private Stmt ParseStructStmt(ParseTypeCtxObjOrEnum data)
        {
            if (data.IdentifierT is not SimpleType)
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Invalid typename for struct."));

            Expect(TokenType.OpenBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open brace expected in struct declaration."));

            var objectProperties = new List<Property>();

            AddOutline(() => 
            {
                while (NotEOF() && At().Type != TokenType.CloseBrace)
                {
                    var type = ParseTypeWithModifiers();
                    var key = Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Struct declaration key expected.")).Value;

                    if (type is ParseTypeCtxObjOrEnum)
                        ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Cannot declare class, struct or enum inside struct declaration."));

                    //export = type!.Exported;

                    // Allows shorthand key: pair -> { key, }.
                    if (At().Type == TokenType.Comma)
                    {
                        Eat(); // advance past comma
                        objectProperties.Add(new Property(key, (type as ParseTypeCtxVar)!.Type, null));
                        continue;
                    }
                    // Allows shorthand key: pair -> { key }.
                    else if (At().Type == TokenType.CloseBrace)
                    {
                        objectProperties.Add(new Property(key, (type as ParseTypeCtxVar)!.Type, null));
                        continue;
                    }

                    // { key: val }
                    Expect(TokenType.Colon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Missing colon following identifier inside struct declaration."));
                    var value = ParseExpr();

                    objectProperties.Add(new Property(key, (type as ParseTypeCtxVar)!.Type, value));
                    if (At().Type != TokenType.CloseBrace)
                    {
                        Expect(TokenType.Comma, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected comma or closing bracket following property."));
                    }
                }
            });

            Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Struct declaration missing closing brace."));
            return new StructDeclaration(data.Annotations, data.Visibility, ((SimpleType)data.IdentifierT).typename, objectProperties.ToArray());
        }

        private Stmt ParseEnumStmt(ParseTypeCtxObjOrEnum data)
        {
            if (data.IdentifierT is not SimpleType)
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Invalid typename for enum."));

            Expect(TokenType.OpenBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open brace expected in enum declaration."));

            var objectProperties = new List<string>();
            AddOutline(() =>
            {
                while (NotEOF() && At().Type != TokenType.CloseBrace)
                {
                    var key = Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Enum entry expected.")).Value;
                    // Allows shorthand key: pair -> { key, }.
                    if (At().Type == TokenType.Comma)
                    {
                        Eat(); // advance past comma
                        objectProperties.Add(key);
                        continue;
                    }
                    // Allows shorthand key: pair -> { key }.
                    else if (At().Type == TokenType.CloseBrace)
                    {
                        objectProperties.Add(key);
                        continue;
                    }

                    if (At().Type != TokenType.CloseBrace)
                    {
                        Expect(TokenType.Comma, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected comma or closing bracket following enum entry."));
                    }
                }
            });

            Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Enum declaration missing closing brace."));
            return new EnumDeclaration(data.Annotations, data.Visibility, ((SimpleType)data.IdentifierT).typename, objectProperties.ToArray());
        }

        // TODO
        private Stmt ParseClassStmt(ParseTypeCtxObjOrEnum data)
        {
            if (data.IdentifierT is not SimpleType)
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Invalid typename for class."));

            var derivants = new List<SimpleType>();
            if (At().Type == TokenType.Colon)
            {
                Eat();

                void Add()
                {
                    var type = ParseType();

                    if (type is not SimpleType)
                        ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Invalid type to derive from."));

                    derivants.Add((SimpleType)type);
                }

                Add();
                while (At().Type == TokenType.Comma)
                {
                    Eat();
                    Add();
                }
            }

            Expect(TokenType.OpenBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open brace expected in class declaration."));

            var body = new List<Stmt>();
            while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
            {
                body.Add(ParseStmt());
            }

            Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Close brace expected in class declaration."));

            return new ClassDeclaration(data.Annotations, data.Visibility, data.IsAbstract, (SimpleType)data.IdentifierT, derivants.ToArray(), body.ToArray());
        }

        private Stmt ParseReturnStmt()
        {
            Eat();

            ReturnDeclaration? declaration = null;
            AddOutline(() =>
            {
                declaration = new ReturnDeclaration(ParseExpr());
            });
            if (SemicolonNeeded()) Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Return statement must end with semicolon."));

            return declaration!;
        }

        private Stmt ParseDeleteStmt()
        {
            Eat();

            DeleteDeclaration? declaration = null;
            AddOutline(() =>
            {
                declaration = new DeleteDeclaration(Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Delete identifier expected.")).Value);
            });
            if (SemicolonNeeded()) Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Delete statement must end with semicolon."));

            return declaration!;
        }

        private Stmt ParseIfElseStmt()
        {
            IfElseDeclaration.IfBlock ParseElseIf()
            {
                Eat(); // eat if keyword

                // Condition
                Expr? condition = null;
                AddOutline(() =>
                {
                    Expect(TokenType.OpenParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open paren expected after 'if' keyword."));
                    condition = ParseExpr();
                    Expect(TokenType.CloseParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Close paren expected after 'if' condition."));
                });

                // Body
                var body = ParseStmt();

                return new IfElseDeclaration.IfBlock(condition!, body);
            }

            List<IfElseDeclaration.IfBlock> blocks = new();
            Stmt? elseBody = null;

            blocks.Add(ParseElseIf());

            if (At().Type == TokenType.Else)
            {
                Eat();

                if (At().Type == TokenType.If)
                {
                    blocks.Add(ParseElseIf());
                }
                else
                {
                    elseBody = ParseStmt();
                }
            }

            return new IfElseDeclaration(blocks.ToArray(), elseBody);
        }

        private Stmt ParseWhileStmt()
        {
            Eat(); // eat while keyword

            // Condition
            Expr? condition = null;
            AddOutline(() =>
            {
                Expect(TokenType.OpenParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open paren expected after 'while' keyword."));
                condition = ParseExpr();
                Expect(TokenType.CloseParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Close paren expected after 'while' condition."));
            });

            // Body
            var body = ParseStmt();

            return new WhileDeclaration(condition!, body);
        }

        private Stmt ParseForStmt()
        {
            Eat(); // eat for keyword

            // Condition
            Stmt? variableDecl = null;
            Expr? condition = null;
            Expr? action = null;
            AddOutline(() =>
            {
                Expect(TokenType.OpenParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open paren expected after 'for' keyword."));
                variableDecl = ParseStmt();
                Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after variable declaration in 'for' loop."));

                condition = ParseExpr();
                Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after condition in 'for' loop."));

                action = ParseExpr();
                Expect(TokenType.CloseParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Close paren expected after 'for' condition."));
            });

            // Body
            var body = ParseStmt();

            return new ForDeclaration(variableDecl!, condition!, action!, body);
        }

        private Stmt ParseBlockStmt()
        {
            Eat();

            var body = new List<Stmt>();
            while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
            {
                body.Add(ParseStmt());
            }

            Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Closing brace expected in block statement."));

            return new BlockStmt(body.ToArray());
        }

        #endregion
        #region Expressions

        private Expr ParseExpr()
        {
            return ParseAssignmentExpr();
        }

        private Expr ParseAssignmentExpr()
        {
            var left = ParseArrayExpr();

            if (At().Type == TokenType.Assignment)
            {
                Eat(); // Advance past equal
                Expr? value = null;
                AddOutline(() =>
                {
                    value = ParseAssignmentExpr();
                });
                if (SemicolonNeeded()) Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after outline assignment expr."));
                return new AssignmentExpr(left, value!);
            }

            return left;
        }

        private Expr ParseObjectConstructorExpr(dynamic targetVariableIdentifier, bool tviAsType = false)
        {
            if (!tviAsType && targetVariableIdentifier is not Identifier)
            {
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Object constructor assignment only works for identifiers."));
            }

            Expect(TokenType.OpenBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open brace expected in object constructor."));

            var objectProperties = new List<Property>();
            while (NotEOF() && At().Type != TokenType.CloseBrace)
            {
                var key = Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Object constructor key expected.")).Value;

                // { key: val }
                Expect(TokenType.Colon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Missing colon following identifier in ObjectConstructorExpr."));
                var value = ParseExpr();

                objectProperties.Add(new Property(key, null, value));
                if (At().Type != TokenType.CloseBrace)
                {
                    Expect(TokenType.Comma, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected comma or closing bracket following property."));
                }
            }

            Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Object constructor missing closing brace."));
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
                    Expect(TokenType.Comma, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected comma or closing bracket following array element."));
                }
            }

            Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Array literal missing closing brace."));
            return new ArrayLiteral(arrayElements.ToArray());
        }

        private Expr ParseLambdaFuncExpr()
        {
            BeginUndoArea((e) => throw new SyntaxException(e));

            List<AnnotationUsageDeclaration> annotations;
            List<VarDeclaration> args;
            Type? returnType;

            try
            {
                // Annotations
                annotations = ParseTypeAnnotations();

                // Args
                args = ParseDeclarativeArgs();
                foreach (var arg in args)
                {
                    if (arg.Kind != NodeType.VarDeclaration)
                    {
                        ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Inside lambda declaration expected parameters to be variable declarations."));
                    }
                }

                // Return type
                returnType = null;
                if (At().Type == TokenType.PtrMemberAccess)
                {
                    Eat();

                    returnType = ParseType();
                }
            }
            catch (SyntaxException)
            {
                CancelUndoArea();
                return ParseBoolExpr();
            }

            PurgeUndoArea();

            // Body
            var body = new List<Stmt>();
            var expr = false;
            if (At().Type == TokenType.OpenBrace)
            {
                SubtractOutline(() =>
                {
                    Eat();
                    while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
                    {
                        body.Add(ParseStmt());
                    }
                    Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Closing brace expected inside lambda declaration."));
                });
            }
            else
            {
                expr = true;
                Expect(TokenType.LambdaArrow, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Lambda arrow expected inside lambda declaration.").AddHint("Try inserting an `=>`."));
                body.Add(ParseStmt());
            }

            return new LambdaExpr(annotations.ToArray(), args.ToArray(), returnType, body.ToArray(), expr);
        }

        private Expr ParseBoolExpr()
        {
            var left = ParseComparisonExpr();

            if (At().Type == TokenType.LogicalOr)
            {
                Eat(); Eat();
                return new EqualityCheckExpr(left, ParseBoolExpr(), EqualityCheckExpr.Type.Or);
            }
            else if (At().Type == TokenType.LogicalAnd)
            {
                Eat(); Eat();
                return new EqualityCheckExpr(left, ParseBoolExpr(), EqualityCheckExpr.Type.And);
            }

            return left;
        }

        private Expr ParseComparisonExpr()
        {
            var left = ParseAdditiveExpr();

            if (At().Type == TokenType.Assignment && At(1).Type == TokenType.Assignment)
            {
                Eat(); Eat();
                return new EqualityCheckExpr(left, ParseAdditiveExpr(), EqualityCheckExpr.Type.Equals);
            }
            else if (At().Type == TokenType.Not && At(1).Type == TokenType.Assignment)
            {
                Eat(); Eat();
                return new EqualityCheckExpr(left, ParseAdditiveExpr(), EqualityCheckExpr.Type.NotEquals);
            }
            else if (At().Type == TokenType.LessThan)
            {
                Eat();
                return new EqualityCheckExpr(left, ParseAdditiveExpr(), EqualityCheckExpr.Type.LessThan);
            }
            else if (At().Type == TokenType.LessThan && At(1).Type == TokenType.Assignment)
            {
                Eat(); Eat();
                return new EqualityCheckExpr(left, ParseAdditiveExpr(), EqualityCheckExpr.Type.LessThanOrEquals);
            }
            else if (At().Type == TokenType.MoreThan)
            {
                Eat();
                return new EqualityCheckExpr(left, ParseAdditiveExpr(), EqualityCheckExpr.Type.MoreThan);
            }
            else if (At().Type == TokenType.MoreThan && At(1).Type == TokenType.Assignment)
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
            var left = ParseUnaryExpr();
        
            while (At().Value == "/" || At().Value == "*" || At().Value == "%")
            {
                var operator_ = Eat().Value;
                var right = ParseUnaryExpr();
                left = new BinaryExpr(left, right, operator_);
            }
        
            return left;
        }

        private Expr ParseUnaryExpr()
        {
            string operator_;
            Expr obj;

            if (At().Value == "+" || At().Value == "-")
            {
                operator_ = Eat().Value;
                obj = ParseCallMemberExpr();
                return new UnaryExpr(obj, operator_);
            }

            return ParseCallMemberExpr();
        }

        private Expr ParseCallMemberExpr()
        {
            var member = ParseMemberExpr();

            if (At().Type == TokenType.OpenParen)
            {
                return ParseCallExpr(member);
            }
            else if (At().Type == TokenType.OpenBracket)
            {
                return ParseIndexExpr(member);
            }

            if (SemicolonNeeded()) Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after outline member expression."));

            return member;
        }

        private Expr ParseIndexExpr(Expr caller)
        {
            Expr? callExpr = null;
            AddOutline(() =>
            {
                Expect(TokenType.OpenBracket, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open bracket expected inside index expression."));
                callExpr = new IndexExpr(ParseExpr(), caller);
                Expect(TokenType.CloseBracket, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Closing bracket expected inside index expression."));
            });

            if (At().Type == TokenType.OpenBracket)
            {
                callExpr = ParseIndexExpr(callExpr!);
            }

            return callExpr!;
        }

        private Expr ParseCallExpr(Expr caller)
        {
            Expr callExpr = new CallExpr(ParseArgs(), caller);

            if (At().Type == TokenType.OpenParen)
            {
                callExpr = ParseCallExpr(callExpr);
            }

            return callExpr;
        }

        /// <param name="preEnd">Make something execute after the closing paren.</param>
        /// <returns>The args.</returns>
        private List<Expr> ParseArgs()
        {
            List<Expr>? args = null;
            AddOutline(() =>
            {
                Expect(TokenType.OpenParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected open parenthesis inside arguments list."));
                args = At().Type == TokenType.CloseParen ? new List<Expr>() : ParseArgumentsList();
                Expect(TokenType.CloseParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected closing parenthesis inside arguments list."));
            });

            return args!;
        }

        private List<Expr> ParseArgumentsList()
        {
            var args = new List<Expr>(){ ParseExpr() };

            while (At().Type == TokenType.Comma && Eat() != null)
            {
                args.Add(ParseExpr());
            }

            return args;
        }

        private Expr ParseMemberExpr()
        {
            var object_ = ParsePrimaryExpr();

            while (At().Type == TokenType.Dot || At().Type == TokenType.DoubleColon)
            {
                if (At().Type == TokenType.Dot)
                {
                    Eat(); // Consume the '.'
                    Expr property = ParsePrimaryExpr();
                    object_ = new MemberExpr(object_, property);
                }
                else if (At().Type == TokenType.DoubleColon)
                {
                    Eat(); // Consume the '::'
                    Expr staticMember = ParsePrimaryExpr();
                    object_ = new StaticMemberExpr(object_, staticMember);
                }
            }

            return object_;
        }

        //private Expr ParseStaticPathExpr()
        //{
        //    var err = "Namespace or type name expected.";
        //    var target = new List<string>();
        //
        //    target.Add(Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, err)).Value);
        //
        //    while (NotEOF() && At().Type == TokenType.DoubleColon)
        //    {
        //        Eat();
        //
        //        target.Add(Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, err)).Value);
        //    }
        //
        //    return target;
        //}

        private Expr ParsePrimaryExpr()
        {
            var tk = At().Type;
        
            switch (tk)
            {
                case TokenType.Identifier:
                    return new Identifier(Eat().Value);
                case TokenType.Number:
                    return new NumericLiteral(int.Parse(Eat().Value));
                case TokenType.FloatNumber:
                    return new FloatLiteral(float.Parse(Eat().Value));
                case TokenType.DoubleNumber:
                    return new DoubleLiteral(double.Parse(Eat().Value));
                case TokenType.String:
                    return new StringLiteral(Eat().Value);
                case TokenType.Char:
                    return new CharLiteral(Eat().Value.ToCharArray()[0]);
                case TokenType.OpenParen:
                    Eat();
                    var value = ParseExpr();
                    Expect(TokenType.CloseParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Unexpected token found inside parenthesised expression. Expected closing parenthesis."));
                    return value;
        
                default:
                    ThrowSyntaxError(
                        SyntaxErrorData.New(SyntaxErrorLevel.Error, $"Unexpected token found while parsing! Type: {At().Type}, Value: {At().Value}.")
                            .AddHint("Check the provided position of this error for typos.")
                            .AddHint("If the syntax is correct and this error is still occurring, please file an issue on Github.")
                    );
                    return null!; // <-- Never reached^
            }
        }
        #endregion
    }
}
