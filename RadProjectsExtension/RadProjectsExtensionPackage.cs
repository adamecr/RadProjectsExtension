using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using net.adamec.dev.vs.extension.radprojects.utils;
using Task = System.Threading.Tasks.Task;

namespace net.adamec.dev.vs.extension.radprojects
{
    /// <summary>
    /// Main package class.
    /// Initialize commands, output window and error list provider and provides some common functions shared across the package.
    /// <see cref="ProvideOptionPageAttribute"/> and <see cref="ProvideProfileAttribute"/> registers the settings page and store for the package
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    [ProvideOptionPage(typeof(RadProjectsExtensionOptions), "RAD Projects Extension", "General", 106, 107, true)]
    [ProvideProfile(typeof(RadProjectsExtensionOptions), "RAD Projects Extension", "RAD Projects Extension Settings", 106, 108, isToolsOptionPage: true, DescriptionResourceID = 109)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class RadProjectsExtensionPackage : AsyncPackage
    {
        /// <summary>
        /// RadProjectsExtensionPackage GUID.
        /// </summary>
        public const string PackageGuidString = "256f6e6f-50dc-4375-b9d7-d0803e2c219c";
        /// <summary>
        /// RAD Project Extension output pane GUID
        /// </summary>
        public const string OutputPaneGuidString = "48749E8F-2227-4B75-B440-34FF4420620E";

        /// <summary>
        /// RAD Project Extension output window pane
        /// </summary>
        private IVsOutputWindowPane OutputWindow { get; set; }
        /// <summary>
        /// RAD Project Extension error list provider
        /// </summary>
        private ErrorListProvider ErrorListProvider { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            //Create error list provider for the package
            ErrorListProvider = new ErrorListProvider(this);

            //Initialize output window
            if (!(GetGlobalService(typeof(SVsOutputWindow)) is IVsOutputWindow outWindow)) throw new Exception("Can't get the output window service");
            var customOutputPaneGuid = new Guid(OutputPaneGuidString);
            const string customOutputPaneTitle = "RAD Projects Extension";
            outWindow.CreatePane(ref customOutputPaneGuid, customOutputPaneTitle, 1, 1);

            outWindow.GetPane(ref customOutputPaneGuid, out var customPane);
            OutputWindow = customPane;
            Output("RAD Projects Extension init", true);

            //Initialize commands
            await RadProjectsExtensionCommands.InitializeAsync(this);
        }

        /// <summary>
        /// Adds a line of <paramref name="text"/> to RAD Projects Extension output pane
        /// </summary>
        /// <param name="text">Text to be added to the output</param>
        /// <param name="activate">If true, the pane is activated (brought to front)</param>
        public void Output(string text, bool activate = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OutputWindow.OutputString(text + Environment.NewLine);
            if (activate)
            {
                OutputWindow.Activate(); // Brings output pane into view
            }
        }

        /// <summary>
        /// Package "top-level" exception handler to be called from catch blocks
        /// Shows the message box with the exception message    
        /// Outputs the exception message and stack to output pane
        /// Adds the exception message to the error list
        /// </summary>
        /// <param name="exception">Exception thrown</param>
        /// <param name="title">Title of the error message box (can be set individually for each action for example)</param>
        public void HandleException(Exception exception, string title)
        {
            Output($"ERROR - {exception.Message}");
            Output(exception.StackTrace, true);
            ErrorListAddError(exception.Message);
            MessageBoxErr(exception.Message, title);
        }

        /// <summary>
        /// Adds and error item to VS error list
        /// </summary>
        /// <param name="message">Message describing the item and presented within the list</param>
        public void ErrorListAddError(string message)
        {
            ErrorListAddTask(message, TaskErrorCategory.Error);
        }

        /// <summary>
        /// Adds and warning item to VS error list
        /// </summary>
        /// <param name="message">Message describing the item and presented within the list</param>
        public void ErrorListAddWarning(string message)
        {
            ErrorListAddTask(message, TaskErrorCategory.Warning);
        }

        /// <summary>
        /// Adds and message (information) item to VS error list
        /// </summary>
        /// <param name="message">Message describing the item and presented within the list</param>
        public void ErrorListAddMessage(string message)
        {
            ErrorListAddTask(message, TaskErrorCategory.Message);
        }

        /// <summary>
        /// Clears package's items from the error list 
        /// </summary>
        public void ErrorListClear()
        {
            ErrorListProvider.Tasks.Clear();
        }

        /// <summary>
        /// Adds an item to VS error list
        /// </summary>
        /// <param name="message">Message describing the item and presented within the list</param>
        /// <param name="category">Type of the item (Error, Warning, Message)</param>
        private void ErrorListAddTask(string message, TaskErrorCategory category)
        {
            ErrorListProvider.Tasks.Add(new ErrorTask
            {
                Category = TaskCategory.User,
                ErrorCategory = category,
                Text = message,
                Document = "RAD Projects Extension",
                CanDelete = true
            });
        }

        /// <summary>
        /// Used at the beginning of an action (for example command processing)
        /// Clears the error list and adds the "opening" to the output
        /// </summary>
        /// <param name="title">Action title</param>
        public void StartAction(string title)
        {
            ErrorListClear();
            Output("==============================");
            Output(title);
            Output("==============================", true);

        }

        /// <summary>
        /// Used at the end of an action (for example command processing)
        /// Adds the "finalizer" to the output
        /// </summary>
        public void FinishAction()
        {
            Output("========== Finished ==========", true);
        }

        public enum MessageBoxResult
        {
            Ok = 1, Cancel = 2, Abort = 3, Retry = 4, Ignore = 5, Yes = 6, No = 7
        }
        public MessageBoxResult MessageBox(string message, string title)
        {
            return MessageBox(message, title, OLEMSGICON.OLEMSGICON_INFO);
        }
        public MessageBoxResult MessageBoxWarn(string message, string title)
        {
            return MessageBox(message, title, OLEMSGICON.OLEMSGICON_WARNING);
        }
        public MessageBoxResult MessageBoxErr(string message, string title)
        {
            return MessageBox(message, title, OLEMSGICON.OLEMSGICON_CRITICAL);
        }
        public MessageBoxResult MessageBoxYesNo(string message, string title)
        {
            return MessageBox(message, title, OLEMSGICON.OLEMSGICON_QUERY, OLEMSGBUTTON.OLEMSGBUTTON_YESNO);
        }
        public MessageBoxResult MessageBoxOkCancel(string message, string title)
        {
            return MessageBox(message, title, OLEMSGICON.OLEMSGICON_QUERY, OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL);
        }

        private MessageBoxResult MessageBox(string message, string title, OLEMSGICON icon, OLEMSGBUTTON buttons = OLEMSGBUTTON.OLEMSGBUTTON_OK)
        {
            var result = VsShellUtilities.ShowMessageBox(
                this,
                message,
                title,
                icon,
                buttons,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return (MessageBoxResult)result;
        }

    }
}
