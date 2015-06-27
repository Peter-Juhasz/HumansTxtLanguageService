using HumansTxtLanguageService.Diagnostics;
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
    [TagType(typeof(IErrorTag))]
    [ContentType(HumansTxtContentTypeNames.HumansTxt)]
    internal sealed class IniErrorTaggerProvider : ITaggerProvider
    {
#pragma warning disable 649

        [ImportMany]
        private IEnumerable<IDiagnosticAnalyzer> analyzers;

#pragma warning restore 649


        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new IniErrorTagger(buffer, analyzers)
            ) as ITagger<T>;
        }


        private sealed class IniErrorTagger : ITagger<IErrorTag>
        {
            public IniErrorTagger(ITextBuffer buffer, IEnumerable<IDiagnosticAnalyzer> analyzers)
            {
                _analyzers = analyzers;

                buffer.ChangedLowPriority += OnBufferChanged;
            }

            private readonly IEnumerable<IDiagnosticAnalyzer> _analyzers;
            

            public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                SyntaxTree syntax = buffer.GetSyntaxTree();

                return
                    // find intersecting nodes
                    from node in syntax.Root.DescendantsAndSelf()
                    where spans.IntersectsWith(node.Span)
                    let type = node.GetType()
                    
                    // find analyzers for node
                    from analyzer in _analyzers
                    from @interface in analyzer.GetType().GetInterfaces()
                    where @interface.IsGenericType
                       && @interface.GetGenericTypeDefinition() == typeof(ISyntaxNodeAnalyzer<>)
                    let analyzerNodeType = @interface.GetGenericArguments().Single()
                    where analyzerNodeType.IsAssignableFrom(type)

                    // analyze node
                    from diagnostic in typeof(ISyntaxNodeAnalyzer<>)
                        .MakeGenericType(analyzerNodeType)
                        .GetMethod("Analyze")
                        .Invoke(analyzer, new [] { node }) as IEnumerable<ITagSpan<IErrorTag>>
                    select diagnostic
                ;
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;


            private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
            {
                ITextBuffer buffer = sender as ITextBuffer;
                if (e.After != buffer.CurrentSnapshot)
                    return;

                SnapshotSpan? changedSpan = null;

                // examine old version
                SyntaxTree oldSyntaxTree = e.Before.GetSyntaxTree();
                HumansTxtDocumentSyntax oldRoot = oldSyntaxTree.Root as HumansTxtDocumentSyntax;

                // find affected sections
                IReadOnlyCollection<HumansTxtSectionSyntax> oldChangedSections = (
                    from change in e.Changes
                    from record in oldRoot.Sections
                    where record.Span.IntersectsWith(change.OldSpan)
                    orderby record.Span.Start
                    select record
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
                    from record in root.Sections
                    where record.Span.IntersectsWith(change.NewSpan)
                    orderby record.Span.Start
                    select record
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
        }
    }
}
