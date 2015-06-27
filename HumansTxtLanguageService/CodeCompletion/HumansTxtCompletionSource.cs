using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using HumansTxtLanguageService.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using HumansTxtLanguageService.Documentation;

namespace HumansTxtLanguageService.CodeCompletion
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(HumansTxtContentTypeNames.HumansTxt)]
    [Name("HumansTxtCompletion")]
    internal sealed class HumansTxtCompletionSourceProvider : ICompletionSourceProvider
    {
#pragma warning disable 649

        [Import]
        private IGlyphService glyphService;

#pragma warning restore 649


        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new HumansTxtCompletionSource(textBuffer, glyphService);
        }


        private sealed class HumansTxtCompletionSource : ICompletionSource
        {
            public HumansTxtCompletionSource(ITextBuffer buffer, IGlyphService glyphService)
            {
                _buffer = buffer;
                _glyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphKeyword, StandardGlyphItem.GlyphItemPublic);
            }

            private readonly ITextBuffer _buffer;
            private readonly ImageSource _glyph;
            private bool _disposed = false;

            private static readonly IReadOnlyCollection<string> WellKnownSectionTitles = new[] { "team", "thanks", "site" };


            public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
            {
                if (_disposed)
                    return;
                
                // get snapshot
                ITextSnapshot snapshot = _buffer.CurrentSnapshot;
                var triggerPoint = session.GetTriggerPoint(snapshot);
                if (triggerPoint == null)
                    return;

                // get or compute syntax tree
                SyntaxTree syntaxTree = snapshot.GetSyntaxTree();
                HumansTxtDocumentSyntax root = syntaxTree.Root as HumansTxtDocumentSyntax;

                // find section
                var section = root.Sections
                    .FirstOrDefault(s =>
                        s.OpeningBracketToken.Span.Span.End <= triggerPoint.Value &&
                        (triggerPoint.Value <= s.ClosingBracketToken.Span.Span.Start || s.ClosingBracketToken.IsMissing)
                    );

                if (section != null)
                {
                    // compute applicable to span
                    var applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(triggerPoint.Value, triggerPoint.Value), SpanTrackingMode.EdgeInclusive);

                    if (!section.NameToken.IsMissing)
                        applicableTo = snapshot.CreateTrackingSpan(section.NameToken.Span.Span, SpanTrackingMode.EdgeInclusive);

                    // gather all other section titles
                    var otherSections = root.Sections.Where(s => s != section);
                    var otherSectionTitles = otherSections
                        .Where(s => !s.NameToken.IsMissing)
                        .Select(s => s.NameToken.Value)
                        .ToList();

                    // recommend
                    IList<Completion> completions = WellKnownSectionTitles
                        .Except(otherSectionTitles, StringComparer.InvariantCultureIgnoreCase)
                        .Select(t => new Completion(t, t.ToUpperInvariant(), HumansTxtDocumentation.GetDocumentation(t), _glyph, t))
                        .ToList();

                    completionSets.Add(
                        new CompletionSet("All", "All", applicableTo, completions, Enumerable.Empty<Completion>())
                    );
                }
            }
            

            public void Dispose()
            {
                _disposed = true;
            }
        }
    }
}
