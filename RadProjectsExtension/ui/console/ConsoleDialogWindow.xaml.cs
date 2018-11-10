using System;
using System.Windows;
using net.adamec.dev.vs.extension.radprojects.utils;

namespace net.adamec.dev.vs.extension.radprojects.ui.console
{
    /// <summary>
    /// Interaction logic for ConsoleDialogWindow.xaml
    /// </summary>
    public partial class ConsoleDialogWindow 
    {
        /// <summary>
        /// Optional command to run when the console dialog is shown
        /// </summary>
        private string RunOnStart { get; set; }
        /// <summary>
        /// Optional <see cref="RunOnStart"/> CLI arguments
        /// </summary>
        private string RunOnStartArguments { get; set; }
        /// <summary>
        /// Optional <see cref="RunOnStart"/> working directory
        /// </summary>
        private string RunOnStartWorkingDir { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// CTOR - allows to register optional process (command) to be run when the console dialog is shown for the first time
        /// </summary>
        /// <param name="runOnStart">Optional command to run when the console dialog is shown</param>
        /// <param name="runOnStartArguments">Optional <paramref name="runOnStart" /> CLI arguments</param>
        /// <param name="runOnStartWorkingDir">Optional <paramref name="runOnStart" /> working directory</param>
        public ConsoleDialogWindow(string runOnStart=null,string runOnStartArguments=null,string runOnStartWorkingDir=null)
        {
            InitializeComponent();
            RunOnStart = runOnStart;
            RunOnStartArguments = runOnStartArguments;
            RunOnStartWorkingDir = runOnStartWorkingDir;

        }

        /// <summary>
        /// Clear console button handler
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event data</param>
        private void BttnClear_OnClick(object sender, RoutedEventArgs e)
        {
            Console.Clear();
        }

        /// <summary>
        /// Console process exit handler
        /// Closes the dialog with result false 
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event data</param>
        private void Console_OnOnProcessExit(object sender, ProcessEventArgs e)
        {
            if (IsVisible) RunOnUiDispatcher(() => DialogResult = false);
        }

        /// <summary>
        /// Dialog visibility change handler
        /// Used to run the optional <see cref="RunOnStart"/> command provided in CTOR when the dialog is shown for the first time
        /// It stops (kills) any running process in console when the dialog is hidden (closed). It also clears the optional <see cref="RunOnStart"/> command
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event data</param>
        private void ConsoleDialogWindow_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                if (RunOnStart != null)
                {
                    Console.StartProcess(RunOnStart,RunOnStartArguments, RunOnStartWorkingDir);
                }
            }
            else
            {
                if(Console.IsProcessRunning) 
                    Console.StopProcess();

                RunOnStart = null;
                RunOnStartArguments = null;
                RunOnStartWorkingDir = null;
            }
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
