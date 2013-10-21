using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.ComponentModelHost;
//using Microsoft.VisualStudio.Editor;
using System.Windows.Controls;
using System.Windows.Input;
using EnvDTE;
using ninlabs.automark.VisualStudio;

namespace ninlabs.Ganji_History.Listeners
{
    public class NavigateListener: IVsTextViewEvents, IVsRunningDocTableEvents3
    {
        private Dictionary<IVsTextView, uint> _cookieList = new Dictionary<IVsTextView, uint>();
        uint m_rdtCookie;
        IVsRunningDocumentTable table;

        public bool Register(EnvDTE.DTE dte)
        {
            table = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            // Listen to show/hide events of docs to register activate/deactivate cursor listeners.
            table.AdviseRunningDocTableEvents(this, out m_rdtCookie);
            // In turn, cursor events will register a IVsTextViewEvents indexed by the IVsTextView.

            return true;
        }

        public void Shutdown()
        {
            table.UnadviseRunningDocTableEvents(m_rdtCookie);
            foreach (var activeView in new HashSet<IVsTextView>(_cookieList.Keys))
            {
                try
                {
                    DeactivateCursorLogger(activeView);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }
        }

        private void ActivateCursorLogger(IVsTextView activeView)
        {
            if (_cookieList.ContainsKey(activeView))
                return;

            IConnectionPointContainer cpContainer = activeView as IConnectionPointContainer;
            if (cpContainer != null)
            {
                IConnectionPoint cp;
                Guid textViewGuid = typeof(IVsTextViewEvents).GUID;
                //const string IID_IVsTextViewEvents = "E1965DA9-E791-49E2-9F9D-ED766D885967";
                //Guid textViewGuid = new Guid(IID_IVsTextViewEvents);
                cpContainer.FindConnectionPoint(ref textViewGuid, out cp);

                uint cookie;
                cp.Advise(this, out cookie);

                _cookieList[activeView] = cookie;
            }
        }

        private void DeactivateCursorLogger(IVsTextView activeView)
        {
            IConnectionPointContainer cpContainer = activeView as IConnectionPointContainer;
            if (cpContainer != null)
            {
                IConnectionPoint cp;
                Guid textViewGuid = typeof(IVsTextViewEvents).GUID;
                //const string IID_IVsTextViewEvents = "E1965DA9-E791-49E2-9F9D-ED766D885967";
                //Guid textViewGuid = new Guid(IID_IVsTextViewEvents);
                cpContainer.FindConnectionPoint(ref textViewGuid, out cp);

                if (_cookieList.ContainsKey(activeView))
                {
                    uint cookie = _cookieList[activeView];
                    cp.Unadvise(cookie);
                    _cookieList.Remove(activeView);
                }
            }
        }



        private static string GetFileNameFromTextView(IVsTextView vTextView)
        {
            IVsTextLines buffer;
            vTextView.GetBuffer(out buffer);
            IVsUserData userData = buffer as IVsUserData;
            Guid monikerGuid = typeof(IVsUserData).GUID;
            object pathAsObject;
            userData.GetData(ref monikerGuid, out pathAsObject);
            return (string)pathAsObject;
        }

        // IVsTextViewEvents Members

        // http://www.ngedit.com/a_intercept_keys_visual_studio_text_editor.html
        public void OnChangeCaretLine(IVsTextView pView, int iNewLine, int iOldLine)
        {
            var cDelta = Math.Abs(iNewLine - iOldLine);
            if (cDelta == 1 || cDelta == 32)
                return;
            try
            {
                var filename = GetFileNameFromTextView(pView);
                var message = string.Format("heartbeat;{0};{1};{2}", filename, iNewLine, DateTime.Now);
                automarkVisualStudioPackage.Log.WriteMessage(message);
            }
            catch (Exception ex)
            {
            }
        }

        public void OnChangeScrollInfo(IVsTextView pView, int iBar, int iMinUnit, int iMaxUnits, int iVisibleUnits, int iFirstVisibleUnit)
        {
        }

        public void OnKillFocus(IVsTextView pView) { }
        public void OnSetBuffer(IVsTextView pView, IVsTextLines pBuffer) { }
        public void OnSetFocus(IVsTextView pView) { }
        // end  IVsTextViewEvents Members


        private bool IsDirty( IVsTextView pView )
        {
            IVsTextLines lines = null;
            pView.GetBuffer(out lines);
            if (lines != null)
            {
                uint flags;
                lines.GetStateFlags(out flags);
                bool isDirty = (flags & (uint)BUFFERSTATEFLAGS.BSF_MODIFIED) != 0;
                return isDirty;
            }
            return false;
        }

        // IVsRunningDocTableEvents3 Members

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            try
            {
                IVsTextView view = VsShellUtilities.GetTextView(pFrame);
                if (view != null)
                {
                    DeactivateCursorLogger(view);//
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return VSConstants.S_OK;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            try
            {
                IVsTextView view = VsShellUtilities.GetTextView(pFrame);
                if (view != null)
                {
                    ActivateCursorLogger(view);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }
        // end IVsRunningDocTableEvents3 Members
    }
}
