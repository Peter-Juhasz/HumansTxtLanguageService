using HumansTxtLanguageService.Syntax;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;

namespace HumansTxtLanguageService.Diagnostics
{
    [ExportDiagnosticAnalyzer]
    internal sealed class HumansTxtSectionSyntaxAnalyzer : ISyntaxNodeAnalyzer<HumansTxtSectionSyntax>
    {
        public const string MissingSectionTitle = nameof(MissingSectionTitle);
        public const string MissingSectionClosingBrace = nameof(MissingSectionClosingBrace);

        public IEnumerable<ITagSpan<IErrorTag>> Analyze(HumansTxtSectionSyntax section)
        {
            // title missing
            if (section.TitleToken.IsMissing)
            {
                yield return new TagSpan<IErrorTag>(
                    section.TitleToken.Span.Span,
                    new DiagnosticErrorTag(PredefinedErrorTypeNames.SyntaxError, MissingSectionTitle, $"Section title expected")
                );
            }

            // closing brace missing
            else if (section.ClosingBracketToken.IsMissing)
            {
                yield return new TagSpan<IErrorTag>(
                    section.ClosingBracketToken.Span.Span,
                    new DiagnosticErrorTag(PredefinedErrorTypeNames.SyntaxError, MissingSectionClosingBrace, $"Section title closing brace expected")
                );
            }
        }
    }
}
