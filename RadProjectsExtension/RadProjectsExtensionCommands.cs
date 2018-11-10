using System;
using System.ComponentModel.Design;
using System.IO;
using System.Windows;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using net.adamec.dev.vs.extension.radprojects.template;
using net.adamec.dev.vs.extension.radprojects.ui.checklists;
using net.adamec.dev.vs.extension.radprojects.ui.console;
using net.adamec.dev.vs.extension.radprojects.ui.version;
using net.adamec.dev.vs.extension.radprojects.utils;
using Task = System.Threading.Tasks.Task;

namespace net.adamec.dev.vs.extension.radprojects
{
    /// <summary>
    /// Commands handler class
    /// </summary>
    internal sealed class RadProjectsExtensionCommands
    {
        public static RadProjectsExtensionCommands Instance { get; private set; }

        //IDs
        public static readonly Guid CommandSet = new Guid("3994f851-c0ed-4950-8bc5-46fd1a52c1eb");
        public const int RadCmdApplyTemplateId = 0x0100;
        public const int RadCmdSolutionConsoleId = 0x0101;
        public const int RadCmdChecklistsId = 0x0102;
        public const int RadCmdVersionId = 0x0103;

        //Internals
        private readonly RadProjectsExtensionPackage package;
        private readonly DTE2 dte;

        /// <summary>
        /// Initialize commands (called form <see cref="RadProjectsExtensionPackage.InitializeAsync"/>)
        /// Get the MemuCommand and DTE services and creates the command handler <see cref="Instance"/>
        /// </summary>
        /// <param name="package">Package the command handler belongs to</param>
        /// <returns>Async task</returns>
        public static async Task InitializeAsync(RadProjectsExtensionPackage package)
        {
            // Switch to the main thread - the call to AddCommand in RadSolutionCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dteService = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as DTE2;
            Instance = new RadProjectsExtensionCommands(package, commandService, dteService);
        }

        /// <summary>
        /// Private CTOR
        /// Add command(s) as menu item(s) to VS
        /// </summary>
        /// <param name="package">Package the command handler belongs to</param>
        /// <param name="commandService">Menu Command service used to extend the VS menu</param>
        /// <param name="dteService">DTE service used to manipulate the VS</param>
        private RadProjectsExtensionCommands(RadProjectsExtensionPackage package, OleMenuCommandService commandService, DTE2 dteService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            dte = dteService ?? throw new ArgumentNullException(nameof(dteService));

            //Extend the VS menu
            package.Output("Initializing commands...");
            var menuItem1 = new MenuCommand(ExecuteRadCmdApplyTemplate, new CommandID(CommandSet, RadCmdApplyTemplateId));
            commandService.AddCommand(menuItem1);

            var menuItem2 = new MenuCommand(ExecuteRadCmdSolutionConsole, new CommandID(CommandSet, RadCmdSolutionConsoleId));
            commandService.AddCommand(menuItem2);

            var menuItem3 = new MenuCommand(ExecuteRadCmdChecklists, new CommandID(CommandSet, RadCmdChecklistsId));
            commandService.AddCommand(menuItem3);

            var menuItem4 = new MenuCommand(ExecuteRadCmdVersion, new CommandID(CommandSet, RadCmdVersionId));
            commandService.AddCommand(menuItem4);

            package.Output("Initialized commands");
        }

        /// <summary>
        /// Apply Template command (menu item) event handler
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void ExecuteRadCmdApplyTemplate(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //Get current settings
            var settings = package.GetDialogPage(typeof(RadProjectsExtensionOptions)) as RadProjectsExtensionOptions;

            //Initialize TemplateEngine and call it's ApplyTemplate method
            var templateEngine = new TemplateEngine(package, settings, dte);
            templateEngine.ApplyTemplate();

        }
        /// <summary>
        /// Solution console command (menu item) event handler
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void ExecuteRadCmdSolutionConsole(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
            var dlg = new ConsoleDialogWindow("cmd", runOnStartWorkingDir: solutionDir)
            {
                Owner = Application.Current.MainWindow
            };
            dlg.ShowMe(true);

        }

        /// <summary>
        /// Checklists command (menu item) event handler
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void ExecuteRadCmdChecklists(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dlg = new ChecklistsDialogWindow(new SolutionInfo(dte.Solution))
            {
                Owner = Application.Current.MainWindow
            };
            dlg.ShowMe(true);

        }

        /// <summary>
        /// Version info command (menu item) event handler
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void ExecuteRadCmdVersion(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dlg = new VersionDialogWindow(new SolutionInfo(dte.Solution)) { Owner = Application.Current.MainWindow };
            dlg.ShowMe(true);

        }
    }
}
