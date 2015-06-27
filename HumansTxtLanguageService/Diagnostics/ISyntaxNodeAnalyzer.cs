using HumansTxtLanguageService.Syntax;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;

namespace HumansTxtLanguageService.Diagnostics
{
    public interface ISyntaxNodeAnalyzer<TSyntaxNode> : IDiagnosticAnalyzer where TSyntaxNode : SyntaxNode
    {
        IEnumerable<ITagSpan<IErrorTag>> Analyze(TSyntaxNode node);
    }
}
