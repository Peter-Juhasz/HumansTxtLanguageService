using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.Windows.Media;

namespace HumansTxtLanguageService
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "HumansTxt/Delimiter")]
    [Name("HumansTxt/Delimiter")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class HumansTxtDelimiterClassificationFormat : ClassificationFormatDefinition
    {
        public HumansTxtDelimiterClassificationFormat()
        {
            this.DisplayName = "Humans.txt Delimiter"; // Human readable version of the name
            this.ForegroundColor = Colors.Green;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "HumansTxt/SectionName")]
    [Name("HumansTxt/SectionName")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class HumansTxtSectionNameClassificationFormat : ClassificationFormatDefinition
    {
        public HumansTxtSectionNameClassificationFormat()
        {
            this.DisplayName = "Humans.txt Section Name"; // Human readable version of the name
            this.ForegroundColor = Colors.Green;
            this.IsBold = true;
        }
    }
}
