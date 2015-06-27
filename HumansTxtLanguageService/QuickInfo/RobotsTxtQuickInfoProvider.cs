using HumansTxtLanguageService.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using HumansTxtLanguageService.Documentation;

namespace HumansTxtLanguageService.QuickInfo
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("Humans.Txt Quick Info Provider")]
    [ContentType(HumansTxtContentTypeNames.HumansTxt)]
    internal sealed class HumansTxtQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
#pragma warning disable 649

        [Import]
        private IGlyphService glyphService;

        [Import]
        private IClassificationTypeRegistryService classificationRegistry;

        [Import]
        private IClassificationFormatMapService classificationFormatMapService;

#pragma warning restore 649


        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new HumansTxtQuickInfoSource(
                    textBuffer,
                    glyphService,
                    classificationFormatMapService, 
                    classificationRegistry
                )
            );
        }


        private sealed class HumansTxtQuickInfoSource : IQuickInfoSource
        {
            public HumansTxtQuickInfoSource(
                ITextBuffer buffer,
                IGlyphService glyphService,
                IClassificationFormatMapService classificationFormatMapService,
                IClassificationTypeRegistryService classificationRegistry
            )
            {
                
                _buffer = buffer;
                _glyphService = glyphService;
                _classificationFormatMapService = classificationFormatMapService;
                _classificationRegistry = classificationRegistry;
            }

            private readonly ITextBuffer _buffer;
            private readonly IGlyphService _glyphService;
            private readonly IClassificationFormatMapService _classificationFormatMapService;
            private readonly IClassificationTypeRegistryService _classificationRegistry;

            private static readonly DataTemplate Template;


            public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
            {
                ITextSnapshot snapshot = _buffer.CurrentSnapshot;
                ITrackingPoint triggerPoint = session.GetTriggerPoint(_buffer);
                SnapshotPoint point = triggerPoint.GetPoint(snapshot);

                SyntaxTree syntax = snapshot.GetSyntaxTree();
                HumansTxtDocumentSyntax root = syntax.Root as HumansTxtDocumentSyntax;

                applicableToSpan = null;

                // find section
                HumansTxtSectionSyntax section = root.Sections
                    .FirstOrDefault(s => s.NameToken.Span.Span.Contains(point));
                
                if (section != null)
                {
                    IClassificationFormatMap formatMap = _classificationFormatMapService.GetClassificationFormatMap(session.TextView);

                    string fieldName = section.NameToken.Value;
                    
                    // get glyph
                    var glyph = _glyphService.GetGlyph(StandardGlyphGroup.GlyphKeyword, StandardGlyphItem.GlyphItemPublic);
                    var classificationType = _classificationRegistry.GetClassificationType("HumansTxt/SectionName");
                    var format = formatMap.GetTextProperties(classificationType);

                    // construct content
                    string sectionTitle = section.NameToken.Value;

                    var content = new QuickInfoContent
                    {
                        Glyph = glyph,
                        Signature = new Run(sectionTitle) { Foreground = format.ForegroundBrush },
                        Documentation = HumansTxtDocumentation.GetDocumentation(sectionTitle),
                    };
                    
                    // add to session
                    quickInfoContent.Add(
                        new ContentPresenter
                        {
                            Content = content,
                            ContentTemplate = Template,
                        }
                    );
                    applicableToSpan = snapshot.CreateTrackingSpan(section.NameToken.Span.Span, SpanTrackingMode.EdgeInclusive);
                    return;
                }
            }

            void IDisposable.Dispose()
            { }


            static HumansTxtQuickInfoSource()
            {
                var resources = new ResourceDictionary { Source = new Uri("pack://application:,,,/HumansTxtLanguageService;component/Themes/Generic.xaml", UriKind.RelativeOrAbsolute) };

                Template = resources.Values.OfType<DataTemplate>().First();
            }
        }
    }
}
