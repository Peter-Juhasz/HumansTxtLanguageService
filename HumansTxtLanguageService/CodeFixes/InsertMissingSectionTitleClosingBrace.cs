using HumansTxtLanguageService.CodeRefactorings;
using HumansTxtLanguageService.Diagnostics;
using HumansTxtLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace HumansTxtLanguageService.CodeFixes
{
    [Export(typeof(ICodeFixProvider))]
    internal sealed class InsertMissingSectionTitleClosingBrace : ICodeFixProvider
    {
        private static readonly IReadOnlyCollection<string> FixableIds = new string[]
        {
            HumansTxtSectionSyntaxAnalyzer.MissingSectionClosingBrace
        };

        public IEnumerable<string> FixableDiagnosticIds
        {
            get { return FixableIds; }
        }

        public IEnumerable<CodeAction> GetFixes(SnapshotSpan span)
        {
            ITextBuffer buffer = span.Snapshot.TextBuffer;
            SyntaxTree syntax = buffer.GetSyntaxTree();
            HumansTxtDocumentSyntax root = syntax.Root as HumansTxtDocumentSyntax;

            // find section
            HumansTxtSectionSyntax section = root.Sections
                .Where(s => !s.TitleToken.IsMissing)
                .TakeWhile(s => s.TitleToken.Span.Span.End <= span.Start)
                .Last();
            
            yield return new CodeAction(
                $"Fix syntax error: Insert missing '{HumansTxtSyntaxFacts.SectionEnd}'",
                () => Fix(section)
            );
        }
        
        public ITextEdit Fix(HumansTxtSectionSyntax section)
        {
            ITextBuffer buffer = section.Document.Snapshot.TextBuffer;

            ITextEdit edit = buffer.CreateEdit();
            edit.Insert(section.TitleToken.Span.Span.End, HumansTxtSyntaxFacts.SectionEnd);

            return edit;
        }
    }
}
