using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace HumansTxtLanguageService.CodeRefactorings
{
    public interface ICodeRefactoringProvider
    {
        IEnumerable<CodeAction> GetRefactorings(SnapshotSpan span);
    }
}
