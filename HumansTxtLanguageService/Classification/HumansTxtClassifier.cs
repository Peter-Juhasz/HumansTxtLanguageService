using HumansTxtLanguageService.Syntax;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace HumansTxtLanguageService
{
    /// <summary>
    /// Classifier provider. It adds the classifier to the set of classifiers.
    /// </summary>
    [Export(typeof(IClassifierProvider))]
    [ContentType(HumansTxtContentTypeNames.HumansTxt)] // This classifier applies to all text files.
    [Order(Before = Priority.High)]
    internal sealed class RobotsTxtClassifierProvider : IClassifierProvider
    {
#pragma warning disable 649

        /// <summary>
        /// Classification registry to be used for getting a reference
        /// to the custom classification type later.
        /// </summary>
        [Import]
        private IClassificationTypeRegistryService classificationRegistry;

        [Import("HumansTxt")]
        private ISyntacticParser syntacticParser;

#pragma warning restore 649


        /// <summary>
        /// Gets a classifier for the given text buffer.
        /// </summary>
        /// <param name="buffer">The <see cref="ITextBuffer"/> to classify.</param>
        /// <returns>A classifier for the text buffer, or null if the provider cannot do so in its current state.</returns>
        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new HumansTxtClassifier(buffer, syntacticParser, this.classificationRegistry)
            );
        }


        /// <summary>
        /// Classifier that classifies all text as an instance of the "RobotsTxtClassifier" classification type.
        /// </summary>
        internal sealed class HumansTxtClassifier : IClassifier
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="HumansTxtClassifier"/> class.
            /// </summary>
            /// <param name="registry">Classification registry.</param>
            public HumansTxtClassifier(ITextBuffer buffer, ISyntacticParser syntacticParser, IClassificationTypeRegistryService registry)
            {
                buffer.Properties.AddProperty(typeof(ISyntacticParser), syntacticParser);

                buffer.Changed += OnBufferChanged;
            }

            private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
            {
                // change affect multiple lines
                if (e.Changes.Any(c =>
                    c.OldText.Intersect(HumansTxtSyntaxFacts.SectionStart).Any() ||
                    c.NewText.Intersect(HumansTxtSyntaxFacts.SectionStart).Any()
                ))
                {
                    this.ClassificationChanged?.Invoke(this,
                        new ClassificationChangedEventArgs(
                            new SnapshotSpan(
                                new SnapshotPoint(e.After, e.Changes.OrderBy(c => c.NewPosition).First().NewPosition),
                                new SnapshotPoint(e.After, e.After.Length)
                            )
                        )
                    );
                }
            }

#pragma warning disable 67

            /// <summary>
            /// An event that occurs when the classification of a span of text has changed.
            /// </summary>
            /// <remarks>
            /// This event gets raised if a non-text change would affect the classification in some way,
            /// for example typing /* would cause the classification to change in C# without directly
            /// affecting the span.
            /// </remarks>
            public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

            /// <summary>
            /// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
            /// </summary>
            /// <remarks>
            /// This method scans the given SnapshotSpan for potential matches for this classification.
            /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
            /// </remarks>
            /// <param name="span">The span currently being classified.</param>
            /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
            public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
            {
                SyntaxTree syntaxTree = span.Snapshot.GetSyntaxTree();

                return (
                    from token in syntaxTree.Root.GetTokens()
                    where !token.IsMissing
                    where token.Span.Span.IntersectsWith(span)
                    select token.Span
                ).ToList();
            }
        }
    }

    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("plaintext")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class HumansTxtVCL : IVsTextViewCreationListener
    {
        [Import]
        private ITextDocumentFactoryService textDocumentFactoryService;

        [Import]
        private IVsEditorAdaptersFactoryService editorAdaptersFactoryService;

        [Import]
        private IContentTypeRegistryService contentTypeRegistry;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var view = editorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            var buffer = view.TextBuffer;

            ITextDocument document = null;
            if (textDocumentFactoryService.TryGetTextDocument(buffer, out document))
            {
                string fileName = Path.GetFileName(document.FilePath);

                if (fileName.Equals("humans.txt", StringComparison.InvariantCultureIgnoreCase))
                {
                    var contentType = contentTypeRegistry.GetContentType(HumansTxtContentTypeNames.HumansTxt);
                    buffer.ChangeContentType(contentType, null);
                }
            }
        }
    }
}
