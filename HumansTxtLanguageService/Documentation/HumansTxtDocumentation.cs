using System;
using System.Collections.Generic;

namespace HumansTxtLanguageService.Documentation
{
    internal static class HumansTxtDocumentation
    {
        private static readonly IReadOnlyDictionary<string, string> Documentation = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "team", "Members of the team. Name, title, contact, etc..." },
            { "thanks", "List of people who deserve special thanks." },
            { "site", "Information about the site. Technology, standards, last updated, etc..." },
        };

        public static string GetDocumentation(string sectionTitle)
        {
            string documentation = null;

            Documentation.TryGetValue(sectionTitle, out documentation);

            return documentation;
        }
    }
}
