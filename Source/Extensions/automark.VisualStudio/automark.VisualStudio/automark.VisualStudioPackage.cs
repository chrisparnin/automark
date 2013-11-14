using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Reflection;
using System.Text;
using ninlabs.Ganji_History.Listeners;
using ninlabs.automark.VisualStudio.Util;

namespace ninlabs.automark.VisualStudio
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidautomarkVisualStudioPkgString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    public sealed class automarkVisualStudioPackage : Package, IVsSolutionEvents
    {
        private uint m_solutionCookie = 0;
        private EnvDTE.DTE m_dte;
        private string m_localHistoryPath = "";
        private string m_basePath = "";
        public static Log Log { get; set; }
        public NavigateListener m_navigateListener = new NavigateListener();
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public automarkVisualStudioPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            Log = new Log();
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(MyToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }


        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidautomarkVisualStudioCmdSet, (int)PkgCmdIDList.cmdidAutomark);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID );
                mcs.AddCommand( menuItem );

                // Create the command for the menu item.
                menuCommandID = new CommandID(GuidList.guidautomarkVisualStudioCmdSet, (int)PkgCmdIDList.cmdidAutomarkReverse);
                menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);

                // Create the command for the menu item.
                menuCommandID = new CommandID(GuidList.guidautomarkVisualStudioCmdSet, (int)PkgCmdIDList.cmdidAutomarkHtml);
                menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);

                // Create the command for the menu item.
                menuCommandID = new CommandID(GuidList.guidautomarkVisualStudioCmdSet, (int)PkgCmdIDList.cmdidAutomarkHtmlReverse);
                menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);

                // Create the command for the menu item.
                menuCommandID = new CommandID(GuidList.guidautomarkVisualStudioCmdSet, (int)PkgCmdIDList.cmdidExport);
                menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);

                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.guidautomarkVisualStudioCmdSet, (int)PkgCmdIDList.cmdidAutomarkWindow);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand( menuToolWin );
            }

            IVsSolution solution = (IVsSolution)GetService(typeof(SVsSolution));
            ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out m_solutionCookie));
        }
        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                string flags = "";
                var command = sender as MenuCommand;
                if (command != null)
                {
                    if (command.CommandID.ID == PkgCmdIDList.cmdidAutomarkReverse)
                    {
                        flags = " -r -fuzz";
                    }
                    if (command.CommandID.ID == PkgCmdIDList.cmdidAutomarkHtml)
                    {
                        flags = " -html -fuzz";
                    }
                    if (command.CommandID.ID == PkgCmdIDList.cmdidAutomarkHtmlReverse)
                    {
                        flags = " -r -html -fuzz";
                    }
                    if (command.CommandID.ID == PkgCmdIDList.cmdidExport)
                    {
                        flags = " -export";
                        Log.WriteMessage(string.Format("automarkexport;{0}", DateTime.Now));
                        Log.Flush();
                    }
                }
                RunAutomark(flags);
                Log.WriteMessage(string.Format("automarkcmd;{0};{1}", DateTime.Now, flags));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("ERROR: Running automark {0}", ex.Message));
                Log.WriteMessage(string.Format("ERROR: Running automark {0}", ex.Message));
            }
        }

        private void RunAutomark(string flags)
        {
            string path = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
            string directory = System.IO.Path.GetDirectoryName(path);
            string executable = System.IO.Path.Combine(directory, "automark.exe");

            if (!System.IO.File.Exists(executable))
            {
                ShowMessage("automark - install error", "Could not find automark.exe");
                return;
            }
            if (!System.IO.Directory.Exists(m_localHistoryPath))
            {
                ShowMessage("automark - package dependency error", "autogit package is required, but has not been loaded for solution.");
                return;
            }

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            startInfo.UseShellExecute = false;
            startInfo.FileName = executable;
            startInfo.Arguments = '"' + m_localHistoryPath + '"' + flags;
            process.StartInfo = startInfo;
            process.Start();

            StringBuilder builder = new StringBuilder();
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                builder.AppendLine(line);
            }

            StringBuilder buildForError = new StringBuilder();
            while (!process.StandardError.EndOfStream)
            {
                string line = process.StandardError.ReadLine();
                buildForError.AppendLine(line);
            }
            var error = buildForError.ToString();

            if (error.Trim().Length > 0)
            {
                ShowMessage("automark", error);
                //return;
            }

             
            if (flags.Contains("html"))
            {
                string tempHtml = System.IO.Path.Combine(m_basePath,"html",string.Format("automark-{0:yyyy-MM-dd-hh-mm-tt}.html", DateTime.Now));
                var parent = System.IO.Path.GetDirectoryName(tempHtml);
                if (!System.IO.Directory.Exists(parent))
                {
                    System.IO.Directory.CreateDirectory(parent);
                }

                System.IO.File.WriteAllText(tempHtml, builder.ToString());
                Log.WriteMessage(string.Format("automarkresult;{0};{1}", tempHtml, DateTime.Now));
                System.Diagnostics.Process.Start(tempHtml);
            }
            else if (flags.Contains("-export"))
            {
                var time = DateTime.Now;
                string tempExport = System.IO.Path.Combine(m_basePath, "exports", string.Format("export-{0:yyyy-MM-dd-hh-mm-tt}.export", time));
                string tempExportZip = System.IO.Path.Combine(m_basePath, "exports", string.Format("export-{0:yyyy-MM-dd-hh-mm-tt}.zip", time));

                var parent = System.IO.Path.GetDirectoryName(tempExport);
                if (!System.IO.Directory.Exists(parent))
                {
                    System.IO.Directory.CreateDirectory(parent);
                }

                System.IO.File.WriteAllText(tempExport, builder.ToString());
                Zip.ZipFile(tempExportZip, tempExport);

                bool triedBackup = false;
                try
                {
                    MapiMailMessage message = new MapiMailMessage("Automark export", "This information includes the automark usage log, timestamps of saves and diffs, and generated html and markdown files.  This usage info will help in testing and improving the tool. You can review the exported info in " + tempExport + "  \nThanks!");
                    message.Recipients.Add("chris.parnin@gatech.edu");
                    message.Files.Add(tempExportZip);
                    message.OnDone += (success) =>
                    {
                        if (!success)
                        {
                            triedBackup = true;
                            string msg = @"mailto:chris.parnin@gatech.edu&subject=Automark export&body=Please attach {0} and send.";
                            System.Diagnostics.Process.Start(string.Format(msg, tempExportZip));
                        }
                    };
                    message.ShowDialog();
                }
                catch (Exception ex)
                {
                    if (!triedBackup)
                    {
                        string msg = @"mailto:chris.parnin@gatech.edu&subject=Automark export&body=Please attach {0} and send.";
                        System.Diagnostics.Process.Start(string.Format(msg, tempExportZip));
                    }
                }
            }
            else
            {
                string tempMD = System.IO.Path.Combine(m_basePath, "md", string.Format("automark-{0:yyyy-MM-dd-hh-mm-tt}.md", DateTime.Now));
                var parent = System.IO.Path.GetDirectoryName(tempMD);
                if (!System.IO.Directory.Exists(parent))
                {
                    System.IO.Directory.CreateDirectory(parent);
                }

                System.IO.File.WriteAllText(tempMD, builder.ToString());
                Log.WriteMessage(string.Format("automarkresult;{0};{1}", tempMD, DateTime.Now));
                System.Diagnostics.Process.Start(tempMD);
            }
        }

        private void ShowMessage(string title, string message)
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       title,
                       message,
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }


        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            InitializeWithDTEAndSolutionReady();
            return VSConstants.S_OK;
        }


        private void InitializeWithDTEAndSolutionReady()
        {
            m_dte = (EnvDTE.DTE)this.GetService(typeof(EnvDTE.DTE));

            if (m_dte == null)
                ErrorHandler.ThrowOnFailure(1);


            m_navigateListener.Register(m_dte);

            var solutionBase = "";
            var solutionName = "";
            if (m_dte.Solution != null)
            {
                solutionBase = System.IO.Path.GetDirectoryName(m_dte.Solution.FullName);
                solutionName = System.IO.Path.GetFileNameWithoutExtension(m_dte.Solution.FullName);
            }
            m_localHistoryPath = FindLocalHistoryPath();


        }

        private string FindLocalHistoryPath()
        {
            var basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            if (m_dte.Solution != null)
            {
                basePath = System.IO.Path.GetDirectoryName(m_dte.Solution.FullName);
            }
            basePath = System.IO.Path.Combine(basePath, ".HistoryData");

            m_basePath = basePath;
            Log.LogPath = System.IO.Path.Combine(basePath, "usage.log");

            var contextPath = System.IO.Path.Combine(basePath, "LocalHistory");

            return contextPath;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            Log.Flush();
            m_navigateListener.Shutdown();
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            throw new NotImplementedException();
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }
    }
}
