using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HumansTxtLanguageService.Syntax
{
    public class HumansTxtSectionSyntax : SyntaxNode
    {
        public HumansTxtSectionSyntax()
        {
            this.LeadingTrivia = new List<SnapshotToken>();
            this.TrailingTrivia = new List<SnapshotToken>();
        }

        public HumansTxtDocumentSyntax Document { get; set; }
        

        public SnapshotToken OpeningBracketToken { get; set; }

        public SnapshotToken TitleToken { get; set; }

        public SnapshotToken ClosingBracketToken { get; set; }


        public SnapshotToken BodyToken { get; set; }


        public IList<SnapshotToken> LeadingTrivia { get; set; }

        public IList<SnapshotToken> TrailingTrivia { get; set; }


        public override SnapshotSpan Span
        {
            get
            {
                return new SnapshotSpan(
                    this.OpeningBracketToken.Span.Span.Start,
                    this.BodyToken.Span.Span.End
                );
            }
        }

        public override SnapshotSpan FullSpan
        {
            get
            {
                return new SnapshotSpan(
                    (this.LeadingTrivia.FirstOrDefault() ?? this.OpeningBracketToken).Span.Span.Start,
                    (this.TrailingTrivia.LastOrDefault()
                        ?? this.TrailingTrivia.LastOrDefault()
                        ?? this.BodyToken
                    ).Span.Span.End
                );
            }
        }

        public override SyntaxNode Parent
        {
            get
            {
                return this.Document;
            }
        }

        public override IEnumerable<SyntaxNode> Descendants()
        {
            yield break;
        }


        public override IEnumerable<SnapshotToken> GetTokens()
        {
            foreach (SnapshotToken token in this.LeadingTrivia)
                yield return token;

            yield return this.OpeningBracketToken;
            yield return this.TitleToken;
            yield return this.ClosingBracketToken;

            foreach (SnapshotToken token in this.TrailingTrivia)
                yield return token;
        }
    }
}
