using HumansTxtLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HumansTxtLanguageService
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(ITextMarkerTag))]
    [ContentType(HumansTxtContentTypeNames.HumansTxt)]
    internal sealed class HumansTxtBraceMatchingTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return textView.Properties.GetOrCreateSingletonProperty(
                creator: () => new HumansTxtBraceMatchingTagger(textView)
            ) as ITagger<T>;
        }
        

        private sealed class HumansTxtBraceMatchingTagger : ITagger<ITextMarkerTag>
        {
            public HumansTxtBraceMatchingTagger(ITextView view)
            {
                _view = view;

                _view.Caret.PositionChanged += OnCaretPositionChanged;
            }

            private readonly ITextView _view;

            private static readonly ITextMarkerTag Tag = new TextMarkerTag("bracehighlight");


            private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
            {
                // TODO: optimize changed spans
                this.TagsChanged?.Invoke(this,
                    new SnapshotSpanEventArgs(new SnapshotSpan(e.TextView.TextBuffer.CurrentSnapshot, 0, e.TextView.TextBuffer.CurrentSnapshot.Length))
                );
            }
            
            public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                SyntaxTree syntax = buffer.GetSyntaxTree();
                HumansTxtDocumentSyntax root = syntax.Root as HumansTxtDocumentSyntax;

                SnapshotPoint caret = _view.Caret.Position.BufferPosition;

                HumansTxtSectionSyntax section = root.Sections
                    .Where(
                        s => !s.OpeningBracketToken.IsMissing
                          && !s.ClosingBracketToken.IsMissing
                    )
                    .FirstOrDefault(
                        s => (s.OpeningBracketToken.Span.Span.Start <= caret && caret < s.OpeningBracketToken.Span.Span.End)
                          || (s.ClosingBracketToken.Span.Span.Start < caret && caret <= s.ClosingBracketToken.Span.Span.End)
                    );

                if (section != null)
                {
                    yield return new TagSpan<ITextMarkerTag>(section.OpeningBracketToken.Span.Span, Tag);
                    yield return new TagSpan<ITextMarkerTag>(section.ClosingBracketToken.Span.Span, Tag);
                }
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }
    }
}
