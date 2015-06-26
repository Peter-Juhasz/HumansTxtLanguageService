using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace HumansTxtLanguageService
{
    /// <summary>
    /// Classification type definition export for IniClassifier
    /// </summary>
    internal static class HumansTxtClassifierClassificationDefinitions
    {
#pragma warning disable 169
        
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("HumansTxt/Delimiter")]
        private static ClassificationTypeDefinition delimiter;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("HumansTxt/SectionName")]
        private static ClassificationTypeDefinition sectionName;
        
#pragma warning restore 169
    }
}
