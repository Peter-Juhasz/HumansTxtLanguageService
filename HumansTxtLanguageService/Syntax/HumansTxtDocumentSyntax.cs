using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HumansTxtLanguageService.Syntax
{
    public class HumansTxtDocumentSyntax : SyntaxNode
    {
        public HumansTxtDocumentSyntax(IList<HumansTxtSectionSyntax> sections)
        {
            this.Sections = sections;
            this.LeadingTrivia = new List<SnapshotToken>();
            this.TrailingTrivia = new List<SnapshotToken>();
        }

        public ITextSnapshot Snapshot { get; set; }

        public IList<HumansTxtSectionSyntax> Sections { get; set; }


        public IList<SnapshotToken> LeadingTrivia { get; set; }

        public IList<SnapshotToken> TrailingTrivia { get; set; }


        public override SnapshotSpan Span
        {
            get
            {
                if (!this.Sections.Any())
                    return new SnapshotSpan(this.Snapshot, 0, 0);

                return new SnapshotSpan(
                    this.Sections.First().Span.Start,
                    this.Sections.Last().Span.End
                );
            }
        }

        public override SnapshotSpan FullSpan
        {
            get
            {
                return new SnapshotSpan(this.Snapshot, 0, this.Snapshot.Length);
            }
        }

        public override SyntaxNode Parent
        {
            get
            {
                return null;
            }
        }

        public override IEnumerable<SyntaxNode> Descendants()
        {
            return this.Sections.SelectMany(s => s.DescendantsAndSelf());
        }


        public override IEnumerable<SnapshotToken> GetTokens()
        {
            return (this.LeadingTrivia)
                .Concat(this.Sections.SelectMany(s => s.GetTokens()))
                .Concat(this.TrailingTrivia)
            ;
        }
    }
}
