using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using HumansTxtLanguageService.Syntax;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace HumansTxtLanguageService.Formatting
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("plaintext")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class HumansTxtBraceCompletion : IVsTextViewCreationListener
    {
#pragma warning disable 169

        [Import]
        private IVsEditorAdaptersFactoryService AdaptersFactory;

        [Import]
        private ITextDocumentFactoryService TextDocumentFactoryService;

        [Import]
        private ITextBufferUndoManagerProvider _textBufferUndoManagerProvider;

        [Import]
        private IEditorOperationsFactoryService _editorOperationsFactoryService;

#pragma warning restore 169


        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            Debug.Assert(view != null);

            // check whether the document is named Humans.txt
            ITextDocument document;
            if (!TextDocumentFactoryService.TryGetTextDocument(view.TextDataModel.DocumentBuffer, out document))
                return;

            string fileName = Path.GetFileName(document.FilePath);
            if (!fileName.Equals("humans.txt", StringComparison.InvariantCultureIgnoreCase))
                return;
            
            // register command filter
            CommandFilter filter = new CommandFilter(view,
                _textBufferUndoManagerProvider.GetTextBufferUndoManager(view.TextBuffer).TextBufferUndoHistory,
                _editorOperationsFactoryService.GetEditorOperations(view)
            );

            IOleCommandTarget next;
            ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(filter, out next));
            filter.Next = next;
        }


        private sealed class CommandFilter : IOleCommandTarget
        {
            public CommandFilter(ITextView view, ITextUndoHistory undoHistory, IEditorOperations editorOperations)
            {
                _textView = view;
                _undoHistory = undoHistory;
                _editorOperations = editorOperations;
            }

            private readonly ITextView _textView;
            private readonly ITextUndoHistory _undoHistory;
            private readonly IEditorOperations _editorOperations;


            public void OnCharTyped(char @char)
            {
                // auto-complete on '*'
                if (@char == HumansTxtSyntaxFacts.SectionStart.Last())
                {
                    ITextBuffer buffer = _textView.TextBuffer;
                    ITextSnapshot snapshot = buffer.CurrentSnapshot;
                    
                    SyntaxTree syntaxTree = buffer.GetSyntaxTree();
                    HumansTxtDocumentSyntax root = syntaxTree.Root as HumansTxtDocumentSyntax;

                    // find in syntax tree
                    var caret = _textView.Caret.Position.BufferPosition;
                    HumansTxtSectionSyntax section = root.Sections
                        .FirstOrDefault(p => p.OpeningBracketToken.Span.Span.End == caret);

                    if (section != null && section.TitleToken.IsMissing && section.ClosingBracketToken.IsMissing)
                    {
                        using (ITextUndoTransaction transaction = _undoHistory.CreateTransaction("Brace Completion"))
                        {
                            SnapshotPoint centerPoint = section.OpeningBracketToken.Span.Span.End;

                            using (ITextEdit edit = buffer.CreateEdit())
                            {
                                // adjust white space
                                edit.Insert(centerPoint, "  ");

                                // insert pair '*/'
                                edit.Insert(centerPoint, HumansTxtSyntaxFacts.SectionEnd);

                                snapshot = edit.Apply();
                            }

                            // move caret back to center '/* | */'
                            SnapshotPoint beforePoint = centerPoint.TranslateTo(snapshot, PointTrackingMode.Negative);
                            _textView.Caret.MoveTo(beforePoint + 1);

                            transaction.Complete();
                        }
                    }
                }
            }

            
            public IOleCommandTarget Next { get; set; }

            public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                int hresult = VSConstants.S_OK;

                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                if (ErrorHandler.Succeeded(hresult))
                {
                    if (pguidCmdGroup == VSConstants.VSStd2K)
                    {
                        switch ((VSConstants.VSStd2KCmdID)nCmdID)
                        {
                            case VSConstants.VSStd2KCmdID.TYPECHAR:
                                char @char = GetTypeChar(pvaIn);
                                OnCharTyped(@char);
                                break;
                        }
                    }
                }

                return hresult;
            }

            public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }

            private static char GetTypeChar(IntPtr pvaIn)
            {
                return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }
        }
    }
}
