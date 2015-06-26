using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace HumansTxtLanguageService
{
    internal static class HumansTxtContentTypeDefinition
    {
#pragma warning disable 649

        [Export]
        [Name("HumansTxt")]
        [BaseDefinition("text")]
        internal static ContentTypeDefinition contentTypeDefinition;
        
#pragma warning restore 649
    }

    internal static class HumansTxtContentTypeNames
    {
        public const string HumansTxt = "HumansTxt";
    }
}
