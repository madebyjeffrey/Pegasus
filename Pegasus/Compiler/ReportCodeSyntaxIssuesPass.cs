﻿// -----------------------------------------------------------------------
// <copyright file="ReportCodeSyntaxIssuesPass.cs" company="(none)">
//   Copyright © 2014 John Gietzen.  All Rights Reserved.
//   This source is subject to the MIT license.
//   Please see license.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Pegasus.Compiler
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Threading;
    using Pegasus.Expressions;
    using Pegasus.Properties;
    using Roslyn.Compilers;
    using Roslyn.Compilers.CSharp;

    internal class ReportCodeSyntaxIssuesPass : CompilePass
    {
        public override IList<string> BlockedByErrors
        {
            get { return new string[0]; }
        }

        public override IList<string> ErrorsProduced
        {
            get { return new[] { "CS0000" }; }
        }

        public override void Run(Grammar grammar, CompileResult result)
        {
            new CodeSyntaxTreeWalker(result).WalkGrammar(grammar);
        }

        private class CodeSyntaxTreeWalker : ExpressionTreeWalker
        {
            private readonly CompileResult result;

            public CodeSyntaxTreeWalker(CompileResult result)
            {
                this.result = result;
            }

            protected override void WalkCodeExpression(CodeExpression codeExpression)
            {
                var ix = (codeExpression.CodeType == CodeType.Error || codeExpression.CodeType == CodeType.Result)
                    ? Tuple.Create("state =>", "")
                    : Tuple.Create("state => {", "}");
                var prefix = ix.Item1;
                var suffix = ix.Item2;

                var startCursor = codeExpression.CodeSpan.Start;
                var code = (prefix + codeExpression.CodeSpan.Code + suffix).TrimEnd();
                var expression = Syntax.ParseExpression(code);

                if (expression.ContainsDiagnostics)
                {
                    foreach (var diag in new DummySyntaxTree().GetDiagnostics(expression))
                    {
                        var cursor = diag.Location.IsInSource
                            ? startCursor.Advance(-prefix.Length + diag.Location.SourceSpan.Start)
                            : startCursor;

                        this.result.Errors.Add(new CompilerError(startCursor.FileName, cursor.Line, cursor.Column, diag.Info.MessageIdentifier, diag.Info.GetMessage()) { IsWarning = diag.Info.Severity == DiagnosticSeverity.Warning });
                    }
                }

                if (expression.Span.Length != code.Length)
                {
                    var sliced = code.Substring(expression.Span.Length);
                    var trimmed = sliced.TrimStart();
                    this.result.AddError(startCursor.Advance(-prefix.Length + expression.Span.Length + (sliced.Length - trimmed.Length)), () => Resources.CS1026_UNEXPECTED_CHARACTER, trimmed[0]);
                }
            }
        }

        private class DummySyntaxTree : SyntaxTree
        {
            private readonly CompilationUnitSyntax node;

            public DummySyntaxTree()
            {
                this.node = this.CloneNodeAsRoot<CompilationUnitSyntax>(Syntax.ParseCompilationUnit(string.Empty, 0, null));
            }

            public override string FilePath
            {
                get { return string.Empty; }
            }

            public override int Length
            {
                get { return 0; }
            }

            public override ParseOptions Options
            {
                get { return ParseOptions.Default; }
            }

            public override FileLinePositionSpan GetLineSpan(TextSpan span, bool usePreprocessorDirectives, CancellationToken cancellationToken = new CancellationToken())
            {
                return new FileLinePositionSpan();
            }

            public override SyntaxReference GetReference(SyntaxNode node)
            {
                return null;
            }

            public override CompilationUnitSyntax GetRoot(CancellationToken cancellationToken)
            {
                return this.node;
            }

            public override IText GetText(CancellationToken cancellationToken)
            {
                return new StringText(string.Empty);
            }

            public override bool TryGetRoot(out CompilationUnitSyntax root)
            {
                root = this.node;
                return true;
            }

            public override bool TryGetText(out IText text)
            {
                text = new StringText(string.Empty);
                return true;
            }

            public override SyntaxTree WithChangedText(IText newText)
            {
                throw new NotImplementedException();
            }
        }
    }
}