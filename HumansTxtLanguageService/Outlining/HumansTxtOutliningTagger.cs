using HumansTxtLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace HumansTxtLanguageService
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(HumansTxtContentTypeNames.HumansTxt)]
    internal sealed class HumansTxtOutliningTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new HumansTxtOutliningTagger(buffer)
            ) as ITagger<T>;
        }


        private sealed class HumansTxtOutliningTagger : ITagger<IOutliningRegionTag>
        {
            public HumansTxtOutliningTagger(ITextBuffer buffer)
            {
                _buffer = buffer;
                _buffer.ChangedLowPriority += OnBufferChanged;
            }

            private readonly ITextBuffer _buffer;
            
            private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
            {
                if (e.After != _buffer.CurrentSnapshot)
                    return;

                SnapshotSpan? changedSpan = null;

                // examine old version
                SyntaxTree oldSyntaxTree = e.Before.GetSyntaxTree();
                HumansTxtDocumentSyntax oldRoot = oldSyntaxTree.Root as HumansTxtDocumentSyntax;

                // find affected sections
                IReadOnlyCollection<HumansTxtSectionSyntax> oldChangedSections = (
                    from change in e.Changes
                    from section in oldRoot.Sections
                    where section.Span.IntersectsWith(change.OldSpan)
                    orderby section.Span.Start
                    select section
                ).ToList();

                if (oldChangedSections.Any())
                {
                    // compute changed span
                    changedSpan = new SnapshotSpan(
                        oldChangedSections.First().Span.Start,
                        oldChangedSections.Last().Span.End
                    );

                    // translate to new version
                    changedSpan = changedSpan.Value.TranslateTo(e.After, SpanTrackingMode.EdgeInclusive);
                }

                // examine current version
                SyntaxTree syntaxTree = e.After.GetSyntaxTree();
                HumansTxtDocumentSyntax root = syntaxTree.Root as HumansTxtDocumentSyntax;

                // find affected sections
                IReadOnlyCollection<HumansTxtSectionSyntax> changedSections = (
                    from change in e.Changes
                    from section in root.Sections
                    where section.Span.IntersectsWith(change.NewSpan)
                    orderby section.Span.Start
                    select section
                ).ToList();
                
                if (changedSections.Any())
                {
                    // compute changed span
                    SnapshotSpan newChangedSpan = new SnapshotSpan(
                        changedSections.First().Span.Start,
                        changedSections.Last().Span.End
                    );

                    changedSpan = changedSpan == null
                        ? newChangedSpan
                        : new SnapshotSpan(
                            changedSpan.Value.Start < newChangedSpan.Start ? changedSpan.Value.Start : newChangedSpan.Start,
                            changedSpan.Value.End > newChangedSpan.End ? changedSpan.Value.End : newChangedSpan.End
                        )
                    ;
                }

                // notify if any change affects outlining
                if (changedSpan != null)
                    this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(changedSpan.Value));
            }
            

            public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                SyntaxTree syntax = buffer.GetSyntaxTree();
                HumansTxtDocumentSyntax root = syntax.Root as HumansTxtDocumentSyntax;

                return
                    from section in root.Sections
                    where !section.TitleToken.IsMissing && !section.BodyToken.IsMissing
                    where spans.IntersectsWith(section.Span)
                    let collapsibleSpan = new SnapshotSpan(
                        section.ClosingBracketToken.Span.Span.End,
                        section.BodyToken.Span.Span.End
                    )
                    select new TagSpan<IOutliningRegionTag>(
                        collapsibleSpan,
                        new OutliningRegionTag(
                            collapsedForm: "...",
                            collapsedHintForm: collapsibleSpan.GetText().Trim()
                        )
                    )
                ;
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }
    }
}
