using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.IO;
using System.Linq;
using HumansTxtLanguageService.Syntax;

namespace HumansTxtLanguageService.CodeCompletion
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("plaintext")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class VsTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        private IVsEditorAdaptersFactoryService AdaptersFactory;

        [Import]
        private ICompletionBroker CompletionBroker;

        [Import]
        private ITextDocumentFactoryService TextDocumentFactoryService;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            Debug.Assert(view != null);

            ITextDocument document;
            if (!TextDocumentFactoryService.TryGetTextDocument(view.TextDataModel.DocumentBuffer, out document))
                return;

            string fileName = Path.GetFileName(document.FilePath);
            if (!fileName.Equals("Humans.txt", StringComparison.InvariantCultureIgnoreCase))
                return;

            CommandFilter filter = new CommandFilter(view, CompletionBroker);

            IOleCommandTarget next;
            ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(filter, out next));
            filter.Next = next;
        }


        private sealed class CommandFilter : IOleCommandTarget
        {
            private ICompletionSession _currentSession;

            public CommandFilter(IWpfTextView textView, ICompletionBroker broker)
            {
                _currentSession = null;

                TextView = textView;
                Broker = broker;
            }

            public IWpfTextView TextView { get; private set; }
            public ICompletionBroker Broker { get; private set; }
            public IOleCommandTarget Next { get; set; }

            private static char GetTypeChar(IntPtr pvaIn)
            {
                return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }

            public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                bool handled = false;
                int hresult = VSConstants.S_OK;

                // pre-process
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    switch ((VSConstants.VSStd2KCmdID)nCmdID)
                    {
                        case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                        case VSConstants.VSStd2KCmdID.COMPLETEWORD: // TODO: Commit if unique
                            handled = StartSession();
                            break;

                        case VSConstants.VSStd2KCmdID.RETURN:
                        case VSConstants.VSStd2KCmdID.TAB:
                            handled = Complete();
                            break;

                        case VSConstants.VSStd2KCmdID.TYPECHAR:
                            char @char = GetTypeChar(pvaIn);
                            if (HumansTxtSyntaxFacts.SectionEnd.Contains(@char))
                                Complete();
                            break;

                        case VSConstants.VSStd2KCmdID.CANCEL:
                            handled = Cancel();
                            break;
                    }
                }

                if (!handled)
                    hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                if (ErrorHandler.Succeeded(hresult))
                {
                    if (pguidCmdGroup == VSConstants.VSStd2K)
                    {
                        switch ((VSConstants.VSStd2KCmdID)nCmdID)
                        {
                            /*case VSConstants.VSStd2KCmdID.TYPECHAR:
                                char @char = GetTypeChar(pvaIn);

                                if (!HumansTxtSyntaxFacts.SectionEnd.Contains(@char))
                                {
                                    if (_currentSession != null)
                                        Filter();
                                    else
                                        StartSession();
                                }
                                break;

                            case VSConstants.VSStd2KCmdID.BACKSPACE:
                                Filter();
                                break;*/
                        }
                    }
                }

                return hresult;
            }
            
            private void Filter()
            {
                if (_currentSession == null)
                    return;

                _currentSession.SelectedCompletionSet.SelectBestMatch();
            }

            bool Cancel()
            {
                if (_currentSession == null)
                    return false;

                _currentSession.Dismiss();

                return true;
            }

            bool Complete()
            {
                if (_currentSession == null)
                    return false;

                if (!_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected)
                {
                    _currentSession.Dismiss();
                    return false;
                }

                _currentSession.Commit();
                return true;
            }

            bool StartSession()
            {
                if (_currentSession != null)
                    return false;

                SnapshotPoint caret = TextView.Caret.Position.BufferPosition;
                ITextSnapshot snapshot = caret.Snapshot;

                if (!Broker.IsCompletionActive(TextView))
                {
                    _currentSession = Broker.CreateCompletionSession(TextView, snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive), true);
                    _currentSession.Dismissed += (sender, args) => _currentSession = null;
                    _currentSession.Start();
                }
                
                return true;
            }

            public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                    {
                        case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                        case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                            prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                            return VSConstants.S_OK;
                    }
                }

                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }
        }
    }
}

