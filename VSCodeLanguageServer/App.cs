using JScr;
using LanguageServer;
using LanguageServer.Client;
using LanguageServer.Parameters;
using LanguageServer.Parameters.General;
using LanguageServer.Parameters.TextDocument;
using LanguageServer.Parameters.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Range = LanguageServer.Parameters.Range;

namespace VSCodeLanguageServer
{
    public class App : ServiceConnection
    {
        private Uri _workerSpaceRoot;
        private int _maxNumberOfProblems = 1000;
        private TextDocumentManager _documents;

        public App(Stream input, Stream output)
            : base(input, output)
        {
            _documents = new TextDocumentManager();
            _documents.Changed += Documents_Changed;
        }

        private void Documents_Changed(object sender, TextDocumentChangedEventArgs e)
        {
            ValidateTextDocument(e.Document);
        }

        protected override Result<InitializeResult, ResponseError<InitializeErrorData>> Initialize(InitializeParams @params)
        {
            _workerSpaceRoot = @params.rootUri;
            var result = new InitializeResult
            {
                capabilities = new ServerCapabilities
                {
                    textDocumentSync = TextDocumentSyncKind.Full,
                    completionProvider = new CompletionOptions
                    {
                        resolveProvider = true
                    }
                }
            };
            return Result<InitializeResult, ResponseError<InitializeErrorData>>.Success(result);
        }

        protected override void DidOpenTextDocument(DidOpenTextDocumentParams @params)
        {
            _documents.Add(@params.textDocument);
            Logger.Instance.Log($"{@params.textDocument.uri} opened.");
        }

        protected override void DidChangeTextDocument(DidChangeTextDocumentParams @params)
        {
            _documents.Change(@params.textDocument.uri, @params.textDocument.version, @params.contentChanges);
            Logger.Instance.Log($"{@params.textDocument.uri} changed.");
        }

        protected override void DidCloseTextDocument(DidCloseTextDocumentParams @params)
        {
            _documents.Remove(@params.textDocument.uri);
            Logger.Instance.Log($"{@params.textDocument.uri} closed.");
        }

        protected override void DidChangeConfiguration(DidChangeConfigurationParams @params)
        {
            _maxNumberOfProblems = @params?.settings?.languageServerExample?.maxNumberOfProblems ?? _maxNumberOfProblems;
            Logger.Instance.Log($"maxNumberOfProblems is set to {_maxNumberOfProblems}.");
            foreach (var document in _documents.All)
            {
                ValidateTextDocument(document);
            }
        }

        private void ValidateTextDocument(TextDocumentItem document)
        {
            var diagnostics = new List<Diagnostic>();
            var script = Script.FromFile(document.uri.ToString());

            foreach (var error in script.Errors)
            {
                diagnostics.Add(new Diagnostic() {
                    source = "jscr", severity = DiagnosticSeverity.Error,
                    range = new() { start = new() { line = error.Line, character = error.Col }, end = new() { line = error.Line, character = error.Col + 1 } },
                    message = error.Description
                });
            }

            Proxy.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                uri = document.uri,
                diagnostics = diagnostics.ToArray()
            });
        }

        protected override void DidChangeWatchedFiles(DidChangeWatchedFilesParams @params)
        {
            Logger.Instance.Log("We received an file change event");
        }

        protected override Result<CompletionResult, ResponseError> Completion(CompletionParams @params)
        {
            var array = new[]
            {
                new CompletionItem
                {
                    label = "true",
                    kind = CompletionItemKind.Keyword,
                    data = 0
                },
                new CompletionItem
                {
                    label = "false",
                    kind = CompletionItemKind.Keyword,
                    data = 1
                }
            };
            return Result<CompletionResult, ResponseError>.Success(array);
        }

        protected override Result<CompletionItem, ResponseError> ResolveCompletionItem(CompletionItem @params)
        {
            return Result<CompletionItem, ResponseError>.Success(@params);
        }

        protected override VoidResult<ResponseError> Shutdown()
        {
            Logger.Instance.Log("Language Server is about to shutdown.");
            // WORKAROUND: Language Server does not receive an exit notification.
            Task.Delay(1000).ContinueWith(_ => Environment.Exit(0));
            return VoidResult<ResponseError>.Success();
        }
    }
}