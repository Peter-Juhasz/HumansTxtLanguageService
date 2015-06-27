using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Language.StandardClassification;

namespace HumansTxtLanguageService
{
    /// <summary>
    /// Classification type definition export for HumansTxtClassifier
    /// </summary>
    internal static class HumansTxtClassifierClassificationDefinitions
    {
#pragma warning disable 169
        
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("HumansTxt/Delimiter")]
        [BaseDefinition(PredefinedClassificationTypeNames.NaturalLanguage)]
        private static ClassificationTypeDefinition delimiter;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("HumansTxt/SectionTitle")]
        [BaseDefinition(PredefinedClassificationTypeNames.NaturalLanguage)]
        private static ClassificationTypeDefinition sectionTitle;
        
#pragma warning restore 169
    }
}
