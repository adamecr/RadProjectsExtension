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
        private string RunOnStart { get; set; }
        private string RunOnStartArguments { get; set; }
        private string RunOnStartWorkingDir { get; set; }
        public ConsoleDialogWindow(string runOnStart=null,string runOnStartArguments=null,string runOnStartWorkingDir=null)
        {
            InitializeComponent();
            RunOnStart = runOnStart;
            RunOnStartArguments = runOnStartArguments;
            RunOnStartWorkingDir = runOnStartWorkingDir;

        }

        private void BttnClear_OnClick(object sender, RoutedEventArgs e)
        {
            Console.Clear();
        }

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

        private void Console_OnOnProcessExit(object sender, ProcessEventArgs args)
        {
            if(IsVisible) RunOnUiDispatcher(() => DialogResult = false);
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
