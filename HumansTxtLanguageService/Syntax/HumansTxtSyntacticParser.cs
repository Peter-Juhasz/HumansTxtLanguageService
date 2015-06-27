using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace HumansTxtLanguageService.Syntax
{
    [Export("HumansTxt", typeof(ISyntacticParser))]
    internal sealed class HumansTxtSyntacticParser : ISyntacticParser
    {
        [ImportingConstructor]
        public HumansTxtSyntacticParser(IClassificationTypeRegistryService registry)
        {
            _delimiterType = registry.GetClassificationType("HumansTxt/Delimiter");
            _sectionTitleType = registry.GetClassificationType("HumansTxt/SectionTitle");
            _sectionBodyType = registry.GetClassificationType(PredefinedClassificationTypeNames.NaturalLanguage);
        }
        
        private readonly IClassificationType _delimiterType;
        private readonly IClassificationType _sectionTitleType;
        private readonly IClassificationType _sectionBodyType;


        public SyntaxTree Parse(ITextSnapshot snapshot)
        {
            IList<HumansTxtSectionSyntax> sections = new List<HumansTxtSectionSyntax>();
            HumansTxtDocumentSyntax root = new HumansTxtDocumentSyntax(sections) { Snapshot = snapshot };

            SnapshotPoint cursor = new SnapshotPoint(snapshot, 0);
            snapshot.ReadWhiteSpace(ref cursor);

            while (cursor < snapshot.Length)
            {
                if (snapshot.IsAtSectionStart(cursor))
                {
                    SnapshotToken openingBracket = new SnapshotToken(snapshot.ReadSectionStart(ref cursor), _delimiterType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken sectionName = new SnapshotToken(snapshot.ReadSectionName(ref cursor), _sectionTitleType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken closingBracket = new SnapshotToken(snapshot.ReadSectionEnd(ref cursor), _delimiterType);
                    snapshot.ReadWhiteSpace(ref cursor);
                    SnapshotToken body = new SnapshotToken(snapshot.ReadSectionBody(ref cursor), _sectionBodyType);
                    snapshot.ReadWhiteSpace(ref cursor);

                    var section = new HumansTxtSectionSyntax
                    {
                        Document = root,

                        OpeningBracketToken = openingBracket,
                        TitleToken = sectionName,
                        ClosingBracketToken = closingBracket,
                        BodyToken = body,
                    };
                    sections.Add(section);
                }
                else
                {
                    SnapshotToken body = new SnapshotToken(snapshot.ReadSectionBody(ref cursor), _sectionBodyType);
                    snapshot.ReadWhiteSpace(ref cursor);

                    var section = new HumansTxtSectionSyntax
                    {
                        BodyToken = body,
                    };
                    sections.Add(section);
                }
            }
            
            return new SyntaxTree(snapshot, root);
        }
    }

    internal static class HumansTxtScanner
    {
        public static bool IsAtSectionStart(this ITextSnapshot snapshot, SnapshotPoint point)
        {
            return snapshot.IsAtExact(point, HumansTxtSyntaxFacts.SectionStart);
        }


        public static SnapshotSpan ReadSectionStart(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadExact(ref point, HumansTxtSyntaxFacts.SectionStart);
        }
        public static SnapshotSpan ReadSectionEnd(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadExact(ref point, HumansTxtSyntaxFacts.SectionEnd);
        }

        public static SnapshotSpan ReadSectionName(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadTo(ref point, HumansTxtSyntaxFacts.SectionEnd);
        }
        public static SnapshotSpan ReadSectionBody(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadTo(ref point, HumansTxtSyntaxFacts.SectionStart);
        }
    }
}
