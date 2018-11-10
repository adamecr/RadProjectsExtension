using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using net.adamec.dev.vs.extension.radprojects.checklists;
using net.adamec.dev.vs.extension.radprojects.git;
using net.adamec.dev.vs.extension.radprojects.ui.version;
using net.adamec.dev.vs.extension.radprojects.utils;
using net.adamec.dev.vs.extension.radprojects.version;

namespace net.adamec.dev.vs.extension.radprojects.ui.checklists
{
    /// <summary>
    /// Interaction logic for ChecklistsDialogWindow.xaml
    /// </summary>
    public partial class ChecklistsDialogWindow
    {
        /// <summary>
        /// List of checklists available
        /// </summary>
        public Checklists Checklists { get; }
        /// <summary>
        /// Solution info
        /// </summary>
        public SolutionInfo SolutionInfo { get; }

        /// <summary>
        /// Version info from Version.props if available
        /// </summary>
        private VersionInfo VersionInfo { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// CTOR - Initialize window/dialog and loads the checklists from solution directory
        /// Sets the DataContext to <see cref="P:net.adamec.dev.vs.extension.radprojects.ui.checklists.ChecklistsDialogWindow.Checklists" />, starts the command prompt in console and
        /// retrieves the initial version/git information
        /// </summary>
        /// <param name="solutionInfo">Solution information</param>
        public ChecklistsDialogWindow(SolutionInfo solutionInfo)
        {
            SolutionInfo = solutionInfo;
            Checklists = Checklists.Load(solutionInfo.SolutionDir);

            InitializeComponent();

            DataContext = Checklists;
            Console.OnProcessExit += Console_OnProcessExit;
            StartCmd();
            RefreshInfo();
        }

        #region Event handlers
        /// <summary>
        /// Dialog/Window Close button click handler
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            CloseMe(false);
        }

        /// <summary>
        /// Run step button click handler
        /// Sets the status of checklist item to Evaluate (keeps the current item active)  for manual steps
        /// Executes the step's command and sets the status of checklist item to Evaluate (keeps the current item active)  for command steps
        /// Replaces %Major%,%Minor%,%Patch%,%Build%, %PkgShort%, %PkgFull% variables in command arguments with data from Version.props if applicable and available
        /// Checklist item is to be provided as button content via binding
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnRun_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Content is ChecklistItem item)) return;

            switch (item.ItemType)
            {
                case ChecklistItemTypeEnum.Manual:
                    item.Status = ChecklistItemStatusEnum.Evaluate;
                    break;
                case ChecklistItemTypeEnum.Command:
                    //Actually the idea was to have the status Running during the command execution,
                    //but didn't find a good/stable/reliable way how to track the command execution within the command prompt running in console.
                    //The alternative way will be to have console with the scope for individual command only (use cmd for manual steps) and
                    //to move to next step after the command if finished (process exited - with manual exit for manual steps), but this is not really 
                    //the case I'd like to have from the user point of view
                    item.Status = ChecklistItemStatusEnum.Running;
                    var args = item.CommandArgs.Trim();
                    if (!string.IsNullOrEmpty(args) && args.Contains("%"))
                    {
                        RefreshVersionInfo();
                        if (VersionInfo != null)
                        {
                            //%Major%,%Minor%,%Patch%,%Build%, %PkgShort%, %PkgFull%
                            args = args.Replace("%Major%", VersionInfo.Major.ToString());
                            args = args.Replace("%Minor%", VersionInfo.Minor.ToString());
                            args = args.Replace("%Patch%", VersionInfo.Patch.ToString());
                            args = args.Replace("%Build%", VersionInfo.BuildNumber.ToString());
                            args = args.Replace("%PkgShort%", VersionInfo.PackageVersionShort);
                            args = args.Replace("%PkgFull%", VersionInfo.PackageVersionFull);
                        }
                    }
                    var cmd = item.Command.Trim() + (!string.IsNullOrEmpty(args) ? " " + args : "");
                    Console.WriteInput(cmd, new SolidColorBrush(Colors.Aqua), true);
                    item.Status = ChecklistItemStatusEnum.Evaluate;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            RefreshInfo();
        }

        /// <summary>
        /// Skip step button click handler
        /// After the user confirmation, sets the status of checklist item to Skipped and moves to the next item    
        /// Checklist item is to be provided as button content via binding
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnSkip_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Content is ChecklistItem item)) return;
            if (MessageBox.Show($"Do you really want to skip the step {item.Name}?", "Skip step",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            item.Status = ChecklistItemStatusEnum.Skipped;
            MoveToNext(item);
        }

        /// <summary>
        /// Evaluate OK button click handler
        /// Sets the status of checklist item to Finished OK and moves to the next item    
        /// Checklist item is to be provided as button content via binding
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnEvaluateOk_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Content is ChecklistItem item)) return;

            item.Status = ChecklistItemStatusEnum.FinishedOk;
            MoveToNext(item);
        }

        /// <summary>
        /// Evaluate NOK button click handler
        /// Sets the status of checklist item to Finished NOK and moves to the next item    
        /// Checklist item is to be provided as button content via binding
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnEvaluateNok_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Content is ChecklistItem item)) return;

            item.Status = ChecklistItemStatusEnum.FinishedNok;
            MoveToNext(item);
        }

        /// <summary>
        /// Start (run) checklist  button click handler
        /// Starts the execution of the check list (activates the first item)
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnStartChecklist_OnClick(object sender, RoutedEventArgs e)
        {
            Checklists?.Current?.Start();
            RefreshInfo();
        }

        /// <summary>
        /// Resets checklist  button click handler
        /// After the user confirmation, resets the execution of the check list (set all items to pending)
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnResetChecklist_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you really want to reset the checklist?", "Reset checklist",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Checklists?.Current?.Reset();
            }
            RefreshInfo();
        }

        /// <summary>
        /// Refresh info button click handler
        /// Reloads the information provided on UI (solution, version, git)
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnRefreshInfo_OnClick(object sender, RoutedEventArgs e)
        {
            RefreshInfo();
        }

        /// <summary>
        /// Clear console button click handler
        /// Clears the console output
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnClearConsole_OnClick(object sender, RoutedEventArgs e)
        {
            Console.Clear();
        }

        /// <summary>
        /// Version detail button click handler
        /// Shows the version dialog and refreshes the info after the dialog is closed
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnVersionDetail_OnClick(object sender, RoutedEventArgs e)
        {
            var dlgVersion = new VersionDialogWindow(SolutionInfo) { Owner = this };
            dlgVersion.ShowMe(true);
            RefreshInfo();
        }

        /// <summary>
        /// Console Process Exit handler
        /// Restarts the command line (cmd) in console when the previous instance finishes (by some error or by user command)
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void Console_OnProcessExit(object sender, ProcessEventArgs e)
        {
            StartCmd();
        }

        #endregion

        /// <summary>
        /// Starts the command line in console
        /// </summary>
        private void StartCmd()
        {
            RunOnUiDispatcher(() => Console.StartProcess("cmd", workingDir: SolutionInfo?.SolutionDir ?? Environment.CurrentDirectory));
        }

        /// <summary>
        /// Moves to the next item in checklist (sets it to Active) if available
        /// Refreshes the version/git information
        /// </summary>
        /// <param name="currentItem"></param>
        private void MoveToNext(ChecklistItem currentItem)
        {
            var idx = Checklists.Current.Items.IndexOf(currentItem);
            idx++;
            if (idx < Checklists.Current.Items.Count)
                Checklists.Current.Items[idx].Status = ChecklistItemStatusEnum.Active;

            RefreshInfo();
        }

        /// <summary>
        /// Reloads the information provided on UI (solution, version, git)
        /// </summary>
        private void RefreshInfo()
        {
            RefreshSolutionInfo();
            RefreshVersionInfo();
            RefreshGitInfo();
        }

        /// <summary>
        /// Refreshes the Solution information
        /// </summary>
        private void RefreshSolutionInfo()
        {
            SolutionInfo?.Refresh();

            TbSolutionName.Text = SolutionInfo?.SolutionName;
            TbPath.Text = SolutionInfo?.SolutionDir;

        }

        /// <summary>
        /// Reloads the version information provided on UI
        /// </summary>
        private void RefreshVersionInfo()
        {
            VersionInfo = VersionInfo.Load(SolutionInfo);

            if (VersionInfo != null)
            {
                TbVersionMajor.Text = VersionInfo.Major.ToString();
                TbVersionMinor.Text = VersionInfo.Minor.ToString();
                TbVersionPatch.Text = VersionInfo.Patch.ToString();
                TbVersionBuild.Text = VersionInfo.BuildNumber.ToString();
            }
            else
            {
                TbVersionMajor.Text = VersionInfo.StringNotAvailable;
                TbVersionMinor.Text = VersionInfo.StringNotAvailable;
                TbVersionPatch.Text = VersionInfo.StringNotAvailable;
                TbVersionBuild.Text = VersionInfo.StringNotAvailable;
            }
        }

        /// <summary>
        /// Reloads the git information provided on UI
        /// Output of git status --porcelain=v2 --branch --untracked=all (parsed) and git branch --list -r -a -v (raw) is provided to user
        /// </summary>
        private void RefreshGitInfo()
        {
            var result = ProcessUtils.RunCommand(
                "git", "status --porcelain=v2 --branch --untracked=all",
                SolutionInfo?.SolutionDir ?? Environment.CurrentDirectory,
                out var output);
            if (result)
            {
                var gitInfo = GitPorcelainParser.ParseGitPorcelain(output);
                InfoGitOverview.Text = $"Git: {gitInfo.Branch} {gitInfo.CommitShort} {gitInfo.CountsString} {gitInfo.Upstream}{(string.IsNullOrEmpty(gitInfo.Upstream) ? "" : " " + gitInfo.AB)}";
                InfoGitFiles.Text = string.Join(Environment.NewLine, gitInfo.Files);
            }
            else
            {
                InfoGitOverview.Text = "Can't get git info!";
                InfoGitFiles.Text = output;
            }

            result = ProcessUtils.RunCommand(
                "git", "branch --list -r -a -v",
                SolutionInfo?.SolutionDir ?? Environment.CurrentDirectory,
                out output);
            InfoGitBranches.Text = result ? output : $"Can't get branches info!{Environment.NewLine}{output}";
        }

        /// <summary>
        /// Runs the action on UI dispatcher.
        /// </summary>
        /// <param name="action">The action to run</param>
        private void RunOnUiDispatcher(Action action)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.BeginInvoke(action, null);
            }
        }
    }
}
