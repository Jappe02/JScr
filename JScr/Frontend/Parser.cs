using JScr.Typing.TypeChecking;
using JScr.Utils;
using static JScr.Frontend.Ast;
using static JScr.Frontend.Lexer;
using Range = JScr.Utils.Range;
using Type = JScr.Typing.Type;

namespace JScr.Frontend;

// TODO: NEW OPERATORS
public class Parser
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
        public bool Static { get; }
        public Visibility Visibility { get; }
        public InheritanceModifier? Modifier { get; }
        public AnnotationUsageDeclaration[] Annotations { get; }
        public Expr Type { get; }

        public ParseTypeCtxVar(bool constant, bool static_, Visibility visibility, InheritanceModifier? modifier, Expr type, AnnotationUsageDeclaration[] annotations)
        {
            Constant = constant;
            Static = static_;
            Visibility = visibility;
            Modifier = modifier;
            Annotations = annotations;
            Type = type;
        }
    }

    /// <summary>
    /// constructors
    /// </summary>
    class ParseTypeCtxConstr : ParseTypeCtx
    {
        public bool Constant { get; }
        public Visibility Visibility { get; }
        public AnnotationUsageDeclaration[] Annotations { get; }
        public Identifier Type { get; }

        public ParseTypeCtxConstr(Visibility visibility, Identifier type)
        {
            Constant = false;
            Visibility = visibility;
            Annotations = [];
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
        public TypeDefinitionStmt TypeDef { get; }

        public ParseTypeCtxObjOrEnum(bool constant, Visibility visibility, bool abstract_, Token type, TypeDefinitionStmt typedef, AnnotationUsageDeclaration[] annotations)
        {
            Constant = constant;
            Visibility = visibility;
            IsAbstract = abstract_;
            Annotations = annotations;
            Type = type;
            TypeDef = typedef;
        }
    }

    /// <summary>Never access items using the f[] operator, use the At(int offset = 0) function instead.</summary>
    private List<Token> tokens = [];
    /// <summary>Never access items using the f[] operator, use the AtLine() or AtCol() functions instead.</summary>
    private List<uint[]> linesAndCols = [];
    private Action<SyntaxError>? errorCallback;

    private string filedir = "";

    private bool insideUndoArea = false;
    private int undoAreaOffset = 0;
    private Action<SyntaxError>? undoAreaErrorHandler = null;

    /// <summary>
    /// Returns 0 if the current token is not between any type of equal sign or a semicolon,
    /// or inside a function call.
    /// Scope {} does not matter!
    /// </summary>
    private int outline = 0;

    private bool NotEOF() => At().Type != TokenType.EOF;

    private int FirstTokenOffset() => !insideUndoArea ? 0 : undoAreaOffset;
    private Token At(int offset = 0) => tokens[FirstTokenOffset() + offset];

    private uint AtLine() => linesAndCols[FirstTokenOffset()][0];
    private uint AtCol() => linesAndCols[FirstTokenOffset()][1];
    private Position AtPos() => new(AtLine(), AtCol());

    private Token Eat()
    {
        if (!NotEOF())
            ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Unexpected end of file."));

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
        
    private Token Expect(TokenType type, SyntaxErrorData data, Range? syntaxErrorRange = null)
    {
        var currentTk = At();
        if (currentTk.Type != type || currentTk == null)
        {
            ThrowSyntaxError(data, default, syntaxErrorRange);
        }
        Eat();
        return currentTk!;
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
        insideUndoArea = false;
        for (int i = 0; i < undoAreaOffset; i++)
            Eat();

        undoAreaErrorHandler = null;
        undoAreaOffset = 0;
    }

    private Identifier CombinePossibleTildeAndIdentifier(bool doNotEatIdentifier = false)
    {
        var _beginPos = AtPos();
        string tilde = string.Empty;
        if (At().Type == TokenType.BitwiseOnesComplement)
        {
            tilde = At().Value;
            Eat();
        }

        if (At().Type != TokenType.Identifier)
            ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Identifier expected."));

        var identifier = At().Value;
        if (!doNotEatIdentifier)
            Eat();

        return new Identifier(new Range(_beginPos, AtPos()), tilde + identifier);
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

    private void ThrowSyntaxError(SyntaxErrorData data, bool canContinue = false, Range? range = null)
    {
        var beginPos = range.HasValue ? range.Value.from : AtPos();
        var endPos   = range.HasValue ? range.Value.to   : new Position(AtLine(), AtCol() + (uint)At().Value.Length);

        var err = new SyntaxError(filedir, new Range(beginPos, endPos), data);

        if (undoAreaErrorHandler != null)
        {
            undoAreaErrorHandler?.Invoke(err);
            return;
        }
            
        errorCallback!(err);

        if (!canContinue)
            throw new SyntaxException(err);
    }

    private Parser() { }

    public static Program ProduceAST(string filedir, string sourceCode, Action<SyntaxError> errorCallback)
    {
        Parser parser = new();

        parser.errorCallback = errorCallback;

        var tokenDictionary = Tokenize(filedir, sourceCode, parser.errorCallback);
        parser.tokens = tokenDictionary.Keys.ToList();
        parser.linesAndCols = tokenDictionary.Values.ToList();
        parser.linesAndCols.Add(parser.linesAndCols.Last()); // <-- Add duplicate of last item to prevent index out of range exception if syntax error on last token.

        var program = new Program(
            new Range(
                new Position(0, 0),
                new Position(parser.linesAndCols.Last()[0], parser.linesAndCols.Last()[1])
            ),
            filedir,
            false,
            []
        );

        parser.filedir = filedir;

        // Parse until end of file
        try
        {
            while (parser.NotEOF())
            {
                program.Body.Add(parser.ParseStmt());
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
            case TokenType.Static:
            case TokenType.Const:
            case TokenType.Class:
            case TokenType.Struct:
            case TokenType.Enum:
            case TokenType.At:
                return ParseDeclStmt(true);
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
            case TokenType.BitwiseOnesComplement when At(1).Type == TokenType.Identifier:
            {
                var newIdent = CombinePossibleTildeAndIdentifier(true).Symbol;
                At().Value = newIdent;

                goto case TokenType.Identifier;
            }
            case TokenType.Identifier:
            {
                if ((At(1).Type == TokenType.Identifier || At(1).Type == TokenType.LessThan || At(1).Type == TokenType.DoubleColon) && outline == 0)
                {
                    return ParseDeclStmt();
                }
                //return ParseExpr();
                goto default;
            }
            case TokenType.OpenBrace:
                return ParseBlockStmt();
            default:
                var expr = ParseExpr();
                Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after expression."));
                return expr;
        }
    }

    // use for typenames for definitions of classes, structs, or enums
    /*private TypeDefinitionStmt GetTypeDefinitionFromExpr(Expr expr)
    {
        var _beginPos = AtPos();

        string name;
        List<Expr> generics = new();

        / TOD0

        return new TypeDefinitionStmt(new(_beginPos, AtPos()), "TestName", new());
    }*/

    private Stmt ParseNamespaceStmt()
    {
        var _beginPos = AtPos();

        Eat();

        List<string> idents = [];
        while (At().Type == TokenType.Identifier)
        {
            idents.Add(Eat().Value);
            if (At().Type == TokenType.DoubleColon) Eat();
        }

        Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after namespace statement."));
        return new NamespaceStmt(new Range(_beginPos, AtPos()), idents.ToArray());
    }

    private Stmt ParseImportStmt()
    {
        var _beginPos = AtPos();

        Eat();

        var target = ParseExpr();
        string? alias = null;

        if (At().Type == TokenType.As)
        {
            Eat();
            alias = Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Identifier expected after `as` keyword.")).Value;
        }

        Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after import statement."));
        return new ImportStmt(new Range(_beginPos, AtPos()), target, alias);
    }

    private List<AnnotationUsageDeclaration> ParseTypeAnnotations()
    {
        var _beginPos = AtPos();

        List<AnnotationUsageDeclaration> annotations = [];

        while (At().Type == TokenType.At)
        {
            Eat();

            var type = ParseExpr();
            var identifier = Expect(
                TokenType.Identifier,
                SyntaxErrorData.New(SyntaxErrorLevel.Error, "Annotation type expected.")
                    .AddHint("Try adding the name of a type after the `@` token.")
            );
            List<Expr> args = [];

            if (At().Type == TokenType.OpenParen)
            {
                args = ParseArgs();
            }

            var range = new Range(_beginPos, AtPos());
            annotations.Add(new AnnotationUsageDeclaration(range, new ObjectConstructorExpr(range, new DualType<Expr, StaticValue?>(type), args.ToArray())));
        }

        return annotations;
    }

    /*
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
    }*/

    // Example: "pub const bool" | "pub class MyClass"
    private ParseTypeCtx ParseTypeWithModifiers()
    {
        List<AnnotationUsageDeclaration> annotations = [];
        bool? constant = null;
        bool? @static = null;
        bool? @abstract = null;
        bool? @virtual = null;
        Visibility? visibility = null;
        InheritanceModifier? modifier = null;
        Token? objectTk = null;
        DualType<TypeDefinitionStmt, Expr>? type = null;

        // Annotations
        annotations = ParseTypeAnnotations();

        // Modifiers
        bool ConstCheck() => At().Type == TokenType.Const;
        bool StaticCheck() => At().Type == TokenType.Static;
        bool VisibilityCheck() => At().Type == TokenType.Private || At().Type == TokenType.Protected || At().Type == TokenType.Public;
        bool AbstractCheck() => At().Type == TokenType.Abstract;
        bool VirtualCheck() => At().Type == TokenType.Virtual;

        while
        (
            (constant == null && ConstCheck()) ||
            (@static == null && StaticCheck()) ||
            (visibility == null && VisibilityCheck()) ||
            (@abstract == null && AbstractCheck()) ||
            (@virtual == null && VirtualCheck())
        )
        {
            if (ConstCheck())
            {
                constant = true;
                Eat();
                continue;
            }
            else if (StaticCheck())
            {
                @static = true;
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
                @abstract = true;
                Eat();
                continue;
            }
            else if (VirtualCheck())
            {
                @virtual = true;
                Eat();
                continue;
            }

            break;
        }

        constant ??= false;
        @static ??= false;
        visibility ??= Visibility.Private;
        @abstract ??= false;
        @virtual ??= false;

        if ((bool)@abstract && (bool)@virtual)
            ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Both the abstract and virtual keywords cannot be used for the same item at the same time."), true);

        if ((bool)@abstract)
            modifier = InheritanceModifier.Abstract;
        else if ((bool)@virtual)
            modifier = InheritanceModifier.Virtual;

        // Object declaration token
        if (At().Type == TokenType.Struct || At().Type == TokenType.Enum || At().Type == TokenType.Class)
            objectTk = Eat();

        // Constructor
        if (objectTk == null && !(bool)constant && !(bool)@static && modifier == null)
        {
            if (At().Type == TokenType.Identifier && At(1).Type == TokenType.OpenParen)
            {
                var beginPos = AtPos();
                var constructType = Eat().Value;
                return new ParseTypeCtxConstr((Visibility)visibility, new Identifier(new Range(beginPos, AtPos()), constructType));
            }
            else if (At().Type == TokenType.BitwiseOnesComplement && At(1).Type == TokenType.Identifier && At(2).Type == TokenType.OpenParen)
            {
                var constructType = CombinePossibleTildeAndIdentifier();
                return new ParseTypeCtxConstr((Visibility)visibility, constructType);
            }
        }

        // Return

        if (objectTk != null)
        {
            // Parse type definition of class, struct or enum.
            {
                var beginPos = AtPos();
                var name = Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Type name expected.")).Value;
                var genericArgs = new List<Expr>();

                if (At().Type == TokenType.LessThan)
                    genericArgs = ParseGenericArgs();

                type = new DualType<TypeDefinitionStmt, Expr>(new TypeDefinitionStmt(new Range(beginPos, AtPos()), name, genericArgs));
            }

            if (modifier != InheritanceModifier.Abstract && modifier != null)
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Classes, structs and enums cannot be virtual."), true);
            else if ((bool)@static)
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Classes, structs and enums cannot be marked static."), true);
            else
                return new ParseTypeCtxObjOrEnum((bool)constant, (Visibility)visibility, modifier == InheritanceModifier.Abstract, objectTk, type, annotations.ToArray());
        }

        // Parse type usage for variables and functions.
        type = new DualType<TypeDefinitionStmt, Expr>(ParseExpr());
        return new ParseTypeCtxVar((bool)constant, (bool)@static, (Visibility)visibility, modifier, type, annotations.ToArray());
    }
    
    private Stmt ParseDeclStmt(bool requireSemicolon = false)
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
        else if (type is ParseTypeCtxConstr constr)
        {
            return ParseConstructorDeclaration(constr);
        }

        // Get identifier
        var identifier = Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected identifier after type for function/variable declarations."));

        // Return: Function
        if (At().Type == TokenType.OpenParen)
        {
            if (type.Constant)
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Functions cannot be declared constant.").AddHint("Try removing the `const` keyword."), true);

            return ParseFnDeclaration((type as ParseTypeCtxVar)!, identifier, requireSemicolon);
        }

        // Return: Variable
        return ParseVarDeclaration((type as ParseTypeCtxVar)!, identifier, requireSemicolon);
    }

    private Stmt ParseConstructorDeclaration(ParseTypeCtxConstr type)
    {
        var _beginPos = AtPos();
        var shorthandAssignments = new List<string>();
        var args = new List<VarDeclaration>();
        bool isDestructor = type.Type.Symbol.First() == '~';

        // Parse parameters
        Expect(TokenType.OpenParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected open parenthesis inside declarative arguments list."));
        if (At().Type != TokenType.CloseParen && !isDestructor)
        {
            VarDeclaration? ParseParamVar()
            {
                if (At().Type == TokenType.This)
                {
                    Eat();
                    Expect(TokenType.Dot, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected dot in constructor parameter shorthand."));
                    var ident = Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected identifier in constructor parameter shorthand."));
                    shorthandAssignments.Add(ident.Value);
                    return null;
                }

                var stmt = ParseDeclStmt();

                if (stmt?.Kind != NodeType.VarDeclaration)
                    ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Variable declaration or constructor parameter shorthand expected inside declarative parameters list."), range: new Range(_beginPos, AtPos()));

                return (stmt as VarDeclaration)!;
            }

            var first = ParseParamVar();
            if (first != null)
                args.Add(first);

            while (At().Type == TokenType.Comma && Eat() != null)
            {
                var decl = ParseParamVar();
                if (decl != null)
                    args.Add(decl);
            }
        }
        Expect(TokenType.CloseParen, 
            SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected closing parenthesis inside declarative arguments list.")
                .AddHintIf(isDestructor, "Destructors cannot have parameters."));

        if (At().Type == TokenType.Semicolon)
        {
            if (isDestructor)
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Warning, "Bodyless destructor can be removed."), true, new Range(_beginPos, AtPos()));

            Eat();
            return new ConstructorDeclaration(new Range(_beginPos, AtPos()), type.Visibility, isDestructor, args.ToArray(), shorthandAssignments.ToArray(), type.Type, null);
        }

        // Parse body
        var body = new List<Stmt>();
        Expect(TokenType.OpenBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Opening brace expected inside constructor body declaration."));
        while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
        {
            body.Add(ParseStmt());
        }
        Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Closing brace expected inside constructor body declaration."));

        if (body.Count == 0)
            ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Warning, "Empty constructor or destructor body can be removed."), true, new Range(_beginPos, AtPos()));

        return new ConstructorDeclaration(new Range(_beginPos, AtPos()), type.Visibility, isDestructor, args.ToArray(), shorthandAssignments.ToArray(), type.Type, body.ToArray());
    }

    private Stmt ParseFnDeclaration(ParseTypeCtxVar type, Token name, bool requireSemicolon)
    {
        var _beginPos = AtPos();

        string? operator_ = null;
        if (name.Value == "operator")
        {
            if (At().Type == TokenType.BinaryOperator ||
                At().Type == TokenType.BitwiseXor ||
                At().Type == TokenType.BitwiseOnesComplement ||
                At().Type == TokenType.BitwiseAnd ||
                At().Type == TokenType.BitwiseOr ||
                At().Type == TokenType.BitwiseShiftLeft ||
                At().Type == TokenType.BitwiseShiftRight ||
                At().Type == TokenType.LessThan ||
                At().Type == TokenType.MoreThan ||
                At().Type == TokenType.LessThanOrEquals ||
                At().Type == TokenType.MoreThanOrEquals ||
                At().Type == TokenType.Equals ||
                At().Type == TokenType.NotEquals ||
                At().Type == TokenType.Not)
                operator_ = Eat().Value;
        }

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

        List<Stmt>? body = null;
        bool expr = false;
        if (type.Modifier == InheritanceModifier.Abstract)
        {
            if (requireSemicolon)
                Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after function declaration."));
            goto SkipBody;
        }

        body = [];
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
            expr = true;
            Expect(TokenType.LambdaArrow, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Lambda arrow expected inside function declaration.").AddHint("Try inserting an `=>`."));
            body.Add(ParseStmt());
        }

        SkipBody:
        var fn = new FunctionDeclaration(new Range(_beginPos, AtPos()), type.Annotations, type.Visibility, type.Modifier, type.Static, override_, args.ToArray(), operator_ ?? name.Value, new DualType<Expr, StaticValue?>(type.Type), body?.ToArray(), expr);
        if (operator_ != null)
            return new OperatorDeclaration(fn.Range, operator_, fn);
        return fn;
    }

    private List<VarDeclaration> ParseDeclarativeArgs()
    {
        List<VarDeclaration> args;

        Expect(TokenType.OpenParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected open parenthesis inside declarative arguments list."));
        args = At().Type == TokenType.CloseParen ? [] : ParseDeclarativeArgsList();
        Expect(TokenType.CloseParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected closing parenthesis inside declarative arguments list."));

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

    private Stmt ParseVarDeclaration(ParseTypeCtxVar type, Token name, bool requireSemicolon)
    {
        var _beginPos = AtPos();

        bool override_ = false;

        VarDeclaration MkNoval()
        {
            if (type.Constant)
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Must assign value to constant expression. No value provided."));

            if (requireSemicolon)
                Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected semicolon after variable declaration."));
            return new VarDeclaration(new Range(_beginPos, AtPos()), type.Annotations, false, type.Static, type.Visibility, type.Modifier, override_, new DualType<Expr, StaticValue?>(type.Type), name.Value, null);
        }

        if (At().Type == TokenType.Override)
        {
            Eat();
            override_ = true;
        }

        if (At().Type != TokenType.Assignment)
        {
            return MkNoval();
        }

        VarDeclaration? declaration = null;
      
        Expect(TokenType.Assignment, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected assignment operator for variable declaration."));

        var value = ParseExpr();
        declaration = new VarDeclaration(new Range(_beginPos, AtPos()), type.Annotations, type.Constant, type.Static, type.Visibility, type.Modifier, override_, new DualType<Expr, StaticValue?>(type.Type), name.Value, value);

        return declaration!;
    }

    private Stmt ParseStructStmt(ParseTypeCtxObjOrEnum data)
    {
        var _beginPos = AtPos();

        Expect(TokenType.OpenBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open brace expected in struct declaration."));

        var objectProperties = new List<Property>();
        var tp = data.TypeDef;

        while (NotEOF() && At().Type != TokenType.CloseBrace)
        {
            var _propBeginPos = AtPos();

            var type = ParseTypeWithModifiers();
            var key = Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Struct declaration key expected.")).Value;

            if (type is ParseTypeCtxObjOrEnum)
                ThrowSyntaxError(SyntaxErrorData.New(SyntaxErrorLevel.Error, "Cannot declare class, struct or enum inside struct declaration."));

            //export = type!.Exported;

            // Allows shorthand key: pair -> { key, }.
            if (At().Type == TokenType.Comma)
            {
                Eat(); // advance past comma
                objectProperties.Add(new Property(new Range(_propBeginPos, AtPos()), key, new DualType<Expr, StaticValue?>((type as ParseTypeCtxVar)!.Type), null));
                continue;
            }
            // Allows shorthand key: pair -> { key }.
            else if (At().Type == TokenType.CloseBrace)
            {
                objectProperties.Add(new Property(new Range(_propBeginPos, AtPos()), key, new DualType<Expr, StaticValue?>((type as ParseTypeCtxVar)!.Type), null));
                continue;
            }

            // { key: val }
            Expect(TokenType.Colon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Missing colon following identifier inside struct declaration."));
            var value = ParseExpr();

            objectProperties.Add(new Property(new Range(_propBeginPos, AtPos()), key, new DualType<Expr, StaticValue?>((type as ParseTypeCtxVar)!.Type), value));
            if (At().Type != TokenType.CloseBrace)
            {
                Expect(TokenType.Comma, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected comma or closing bracket following property."));
            }
        }
     
        Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Struct declaration missing closing brace."));
        return new StructDeclaration(new Range(_beginPos, AtPos()), data.Annotations, data.Visibility, tp, objectProperties.ToArray());
    }

    private Stmt ParseEnumStmt(ParseTypeCtxObjOrEnum data)
    {
        var _beginPos = AtPos();

        Expect(TokenType.OpenBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open brace expected in enum declaration."));

        var objectProperties = new List<string>();
        var tp = data.TypeDef;

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
        
        Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Enum declaration missing closing brace."));
        return new EnumDeclaration(new Range(_beginPos, AtPos()), data.Annotations, data.Visibility, tp, objectProperties.ToArray());
    }

    // TODO
    private Stmt ParseClassStmt(ParseTypeCtxObjOrEnum data)
    {
        var _beginPos = AtPos();

        var tp = data.TypeDef;
        var derivants = new List<Expr>();
        if (At().Type == TokenType.Colon)
        {
            Eat();

            derivants.Add(ParseExpr());
            while (At().Type == TokenType.Comma)
            {
                Eat();
                derivants.Add(ParseExpr());
            }
        }

        Expect(TokenType.OpenBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open brace expected in class declaration."));

        var body = new List<Stmt>();
        while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
        {
            body.Add(ParseStmt());
        }

        Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Close brace expected in class declaration."));

        return new ClassDeclaration(new Range(_beginPos, AtPos()), data.Annotations, data.Visibility, data.IsAbstract, tp, derivants.ToArray(), body.ToArray());
    }

    private Stmt ParseReturnStmt()
    {
        var _beginPos = AtPos();

        Eat();

        ReturnDeclaration? declaration = null;

        var value = ParseExpr();
        declaration = new ReturnDeclaration(new Range(_beginPos, AtPos()), value);
        Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after return statement."));

        return declaration!;
    }

    private Stmt ParseDeleteStmt()
    {
        var _beginPos = AtPos();

        Eat();

        DeleteDeclaration? declaration = null;

        var value = Expect(TokenType.Identifier, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Delete identifier expected.")).Value;
        declaration = new DeleteDeclaration(new Range(_beginPos, AtPos()), value);
        Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after delete statement."));

        return declaration!;
    }

    private Stmt ParseIfElseStmt()
    {
        var _beginPos = AtPos();

        IfElseDeclaration.IfBlock ParseElseIf()
        {
            Eat(); // eat if keyword

            // Condition
            Expr? condition = null;
             
            Expect(TokenType.OpenParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open paren expected after 'if' keyword."));
            condition = ParseExpr();
            Expect(TokenType.CloseParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Close paren expected after 'if' condition."));

            // Body
            var body = ParseStmt();

            return new IfElseDeclaration.IfBlock(condition!, body);
        }

        List<IfElseDeclaration.IfBlock> blocks = [];
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

        return new IfElseDeclaration(new Range(_beginPos, AtPos()), blocks.ToArray(), elseBody);
    }

    private Stmt ParseWhileStmt()
    {
        var _beginPos = AtPos();

        Eat(); // eat while keyword

        // Condition
        Expr? condition = null;
          
        Expect(TokenType.OpenParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open paren expected after 'while' keyword."));
        condition = ParseExpr();
        Expect(TokenType.CloseParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Close paren expected after 'while' condition."));
          
        // Body
        var body = ParseStmt();

        return new WhileDeclaration(new Range(_beginPos, AtPos()), condition!, body);
    }

    private Stmt ParseForStmt()
    {
        var _beginPos = AtPos();

        Eat(); // eat for keyword

        // Condition
        Stmt? variableDecl = null;
        Expr? condition = null;
        Expr? action = null;
       
        Expect(TokenType.OpenParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open paren expected after 'for' keyword."));
        variableDecl = ParseStmt();
        Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after variable declaration in 'for' loop."));

        condition = ParseExpr();
        Expect(TokenType.Semicolon, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Semicolon expected after condition in 'for' loop."));

        action = ParseExpr();
        Expect(TokenType.CloseParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Close paren expected after 'for' condition."));
      

        // Body
        var body = ParseStmt();

        return new ForDeclaration(new Range(_beginPos, AtPos()), variableDecl!, condition!, action!, body);
    }

    private Stmt ParseBlockStmt()
    {
        var _beginPos = AtPos();

        Eat();

        var body = new List<Stmt>();
        while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
        {
            body.Add(ParseStmt());
        }

        Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Closing brace expected in block statement."));

        return new BlockStmt(new Range(_beginPos, AtPos()), body.ToArray());
    }

    #endregion
    #region Expressions

    private Expr ParseExpr()
    {
        return ParseAssignmentExpr();
    }

    private Expr ParseAssignmentExpr()
    {
        var _beginPos = AtPos();

        var left = ParseObjectConstructorExpr();

        if (At().Type == TokenType.Assignment)
        {
            Eat(); // Advance past equal
            Expr? value = null;
             
            value = ParseAssignmentExpr();

            return new AssignmentExpr(new Range(_beginPos, AtPos()), left, value!);
        }

        return left;
    }

    private Expr ParseObjectConstructorExpr()
    {
        if (At().Type != TokenType.New)
        {
            return ParseLambdaFuncExpr();
        }

        List<Expr> ParseArrayElements()
        {
            var arrayElements = new List<Expr>();

            Expect(TokenType.OpenBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Array literal missing opening brace."));

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

            return arrayElements;
        }

        var _beginPos = AtPos();

        Eat();
        if (At().Type == TokenType.OpenParen)
        {
            var args = ParseArgs().ToArray();
            return new ObjectConstructorExpr(new Range(_beginPos, AtPos()), null, args);
        }
        else if (At().Type == TokenType.OpenBracket)
        {
            var el = ParseArrayElements().ToArray();
            return new ObjectArrayConstructorExpr(new Range(_beginPos, AtPos()), null, el);
        }

        var type = ParseExpr();

        if (type.Kind == NodeType.IndexExpr)
        {
            var el = ParseArrayElements().ToArray();
            return new ObjectArrayConstructorExpr(new Range(_beginPos, AtPos()), type, el);
        }

        var args2 = ParseArgs().ToArray();
        return new ObjectConstructorExpr(new Range(_beginPos, AtPos()), new DualType<Expr, StaticValue?>(type), args2);
    }

    private Expr ParseLambdaFuncExpr()
    {
        BeginUndoArea((e) => throw new SyntaxException(e));

        List<AnnotationUsageDeclaration> annotations;
        List<VarDeclaration> args;
        Expr? returnType;

        var _beginPos = AtPos();
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

                returnType = ParseExpr();
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
           
            Eat();
            while (At().Type != TokenType.EOF && At().Type != TokenType.CloseBrace)
            {
                body.Add(ParseStmt());
            }
            Expect(TokenType.CloseBrace, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Closing brace expected inside lambda declaration."));              
        }
        else
        {
            expr = true;
            Expect(TokenType.LambdaArrow, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Lambda arrow expected inside lambda declaration.").AddHint("Try inserting an `=>`."));
            body.Add(ParseStmt());
        }

        return new LambdaExpr(new Range(_beginPos, AtPos()), annotations.ToArray(), args.ToArray(), new DualType<Expr?, Type>(returnType), body.ToArray(), expr);
    }

    private Expr ParseBoolExpr()
    {
        var _beginPos = AtPos();

        var left = ParseComparisonExpr();

        if (At().Type == TokenType.LogicalOr)
        {
            Eat();
            var p = ParseBoolExpr();
            return new EqualityCheckExpr(new Range(_beginPos, AtPos()), left, p, EqualityCheckExpr.Type.Or);
        }
        else if (At().Type == TokenType.LogicalAnd)
        {
            Eat();
            var p = ParseBoolExpr();
            return new EqualityCheckExpr(new Range(_beginPos, AtPos()), left, p, EqualityCheckExpr.Type.And);
        }

        return left;
    }

    private Expr ParseComparisonExpr()
    {
        var _beginPos = AtPos();

        var left = ParseAdditiveExpr();

        if (At().Type == TokenType.Equals)
        {
            Eat();
            var p = ParseAdditiveExpr();
            return new EqualityCheckExpr(new Range(_beginPos, AtPos()), left, p, EqualityCheckExpr.Type.Equals);
        }
        else if (At().Type == TokenType.NotEquals)
        {
            Eat();
            var p = ParseAdditiveExpr();
            return new EqualityCheckExpr(new Range(_beginPos, AtPos()), left, p, EqualityCheckExpr.Type.NotEquals);
        }
        else if (At().Type == TokenType.LessThan)
        {
            Eat();
            var p = ParseAdditiveExpr();
            return new EqualityCheckExpr(new Range(_beginPos, AtPos()), left, p, EqualityCheckExpr.Type.LessThan);
        }
        else if (At().Type == TokenType.LessThanOrEquals)
        {
            Eat();
            var p = ParseAdditiveExpr();
            return new EqualityCheckExpr(new Range(_beginPos, AtPos()), left, p, EqualityCheckExpr.Type.LessThanOrEquals);
        }
        else if (At().Type == TokenType.MoreThan)
        {
            Eat();
            var p = ParseAdditiveExpr();
            return new EqualityCheckExpr(new Range(_beginPos, AtPos()), left, p, EqualityCheckExpr.Type.MoreThan);
        }
        else if (At().Type == TokenType.MoreThanOrEquals)
        {
            Eat();
            var p = ParseAdditiveExpr();
            return new EqualityCheckExpr(new Range(_beginPos, AtPos()), left, p, EqualityCheckExpr.Type.MoreThanOrEquals);
        }

        return left;
    }

    private Expr ParseAdditiveExpr()
    {
        var _beginPos = AtPos();

        var left = ParseMultiplicitaveExpr();
        
        while (At().Value == "+" || At().Value == "-")
        {
            var operator_ = Eat().Value;
            var right = ParseMultiplicitaveExpr();
            left = new BinaryExpr(new Range(_beginPos, AtPos()), left, right, operator_);
        }
        
        return left;
    }
        
    private Expr ParseMultiplicitaveExpr()
    {
        var _beginPos = AtPos();

        var left = ParseBitwiseExpr();
        
        while (At().Value == "/" || At().Value == "*" || At().Value == "%")
        {
            var operator_ = Eat().Value;
            var right = ParseBitwiseExpr();
            left = new BinaryExpr(new Range(_beginPos, AtPos()), left, right, operator_);
        }
        
        return left;
    }

    private Expr ParseBitwiseExpr()
    {
        var _beginPos = AtPos();

        var left = ParseUnaryExpr();

        while (At().Type == TokenType.BitwiseAnd || At().Type == TokenType.BitwiseOr || At().Type == TokenType.BitwiseXor || At().Type == TokenType.BitwiseShiftLeft || At().Type == TokenType.BitwiseShiftRight)
        {
            var operator_ = Eat().Value;
            var right = ParseUnaryExpr();
            left = new BinaryExpr(new Range(_beginPos, AtPos()), left, right, operator_);
        }

        return left;
    }

    private Expr ParseUnaryExpr()
    {
        var _beginPos = AtPos();

        string operator_;
        Expr obj;

        if (At().Value == "+" || At().Value == "-" || At().Value == "!" || At().Value == "~" || At().Value == "&")
        {
            operator_ = Eat().Value;
            obj = ParseCallMemberExpr();
            return new UnaryExpr(new Range(_beginPos, AtPos()), obj, operator_);
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
        else if (At().Type == TokenType.LessThan)
        {
            return ParseGenericExpr(member);
        }

        return member;
    }

    private Expr ParseIndexExpr(Expr caller)
    {
        var _beginPos = AtPos();

        Expr? callExpr = null;
       
        Expect(TokenType.OpenBracket, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Open bracket expected inside index expression."));
        var arg = ParseExpr();
        callExpr = new IndexExpr(new Range(_beginPos, AtPos()), arg, caller);
        Expect(TokenType.CloseBracket, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Closing bracket expected inside index expression."));
     
        if (At().Type == TokenType.OpenBracket)
        {
            callExpr = ParseIndexExpr(callExpr!);
        }

        return callExpr!;
    }

    private Expr ParseCallExpr(Expr caller)
    {
        var _beginPos = AtPos();

        var args = ParseArgs();
        Expr callExpr = new CallExpr(new Range(_beginPos, AtPos()), args, caller);

        if (At().Type == TokenType.OpenParen)
        {
            callExpr = ParseCallExpr(callExpr);
        }

        return callExpr;
    }

    private Expr ParseGenericExpr(Expr caller)
    {
        var _beginPos = AtPos();

        var args = ParseGenericArgs();
        Expr callExpr = new GenericArgsExpr(new Range(_beginPos, AtPos()), args, caller);

        if (At().Type == TokenType.LessThan)
        {
            callExpr = ParseGenericExpr(callExpr);
        }

        return callExpr;
    }

    /// <param name="preEnd">Make something execute after the closing paren.</param>
    /// <returns>The args.</returns>
    private List<Expr> ParseArgs()
    {
        List<Expr>? args = null;
       
        Expect(TokenType.OpenParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected open parenthesis inside arguments list."));
        args = At().Type == TokenType.CloseParen ? [] : ParseArgumentsList();
        Expect(TokenType.CloseParen, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected closing parenthesis inside arguments list."));

        return args!;
    }

    private List<Expr> ParseGenericArgs()
    {
        List<Expr>? args = null;
         
        Expect(TokenType.LessThan, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected less-than sign inside generic arguments list."));
        args = At().Type == TokenType.MoreThan ? [] : ParseArgumentsList();
        Expect(TokenType.MoreThan, SyntaxErrorData.New(SyntaxErrorLevel.Error, "Expected more-than sign inside generic arguments list."));

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
        var _beginPos = AtPos();

        var object_ = ParsePrimaryExpr();

        while (At().Type == TokenType.Dot || At().Type == TokenType.DoubleColon)
        {
            if (At().Type == TokenType.Dot)
            {
                Eat(); // Consume the '.'
                Expr property = ParsePrimaryExpr();
                object_ = new MemberExpr(new Range(_beginPos, AtPos()), object_, property);
            }
            else if (At().Type == TokenType.DoubleColon)
            {
                Eat(); // Consume the '::'
                Expr staticMember = ParsePrimaryExpr();
                object_ = new ResolutionExpr(new Range(_beginPos, AtPos()), object_, staticMember);
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
        var _beginPos = AtPos();
        var tk = At().Type;
        
        switch (tk)
        {
            case TokenType.This:
            case TokenType.Identifier:
                var eat0 = Eat().Value;
                return new Identifier(new Range(_beginPos, AtPos()), eat0);
            case TokenType.Number:
                var eat1 = Eat().Value;

                if (int.TryParse(eat1, null, out var res1))
                    return new NumericLiteral(new Range(_beginPos, AtPos()), res1);

                goto default;
            case TokenType.FloatNumber:
                var eat2 = Eat().Value;

                if (float.TryParse(eat2, null, out var res2))
                    return new FloatLiteral(new Range(_beginPos, AtPos()), res2);

                goto default;
            case TokenType.DoubleNumber:
                var eat3 = Eat().Value;

                if (double.TryParse(eat3, null, out var res3))
                    return new DoubleLiteral(new Range(_beginPos, AtPos()), res3);

                goto default;
            case TokenType.String:
                var eat4 = Eat().Value;
                return new StringLiteral(new Range(_beginPos, AtPos()), eat4);
            case TokenType.Char:
                var eat5 = Eat().Value;
                return new CharLiteral(new Range(_beginPos, AtPos()), eat5.ToCharArray()[0]);
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