using System.Windows.Documents;
using System.Windows.Media;

namespace HumansTxtLanguageService.QuickInfo
{
    public class QuickInfoContent
    {
        public ImageSource Glyph { get; set; }

        public Inline Signature { get; set; }

        public string Documentation { get; set; }
    }
}
