using net.adamec.dev.vs.extension.radprojects.utils;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace net.adamec.dev.vs.extension.radprojects.ui.console
{
    //Based on code of https://github.com/dwmkerr/consolecontrol

    /// <summary>
    /// Interaction logic for ConsoleControl.xaml
    /// </summary>
    public partial class ConsoleControl
    {
        #region XAML properties
        private static readonly DependencyProperty DiagnosticsTextColorProperty =
            DependencyProperty.Register("DiagnosticsTextColor", typeof(Brush), typeof(ConsoleControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 0, 255, 0))));
        /// <summary>
        /// Brush for the diagnostics text
        /// </summary>
        public Brush DiagnosticsTextBrush
        {
            get => (Brush)GetValue(DiagnosticsTextColorProperty);
            set => SetValue(DiagnosticsTextColorProperty, value);
        }

        private static readonly DependencyProperty OutputTextColorProperty =
            DependencyProperty.Register("OutputTextColor", typeof(Brush), typeof(ConsoleControl),
                new PropertyMetadata(Brushes.LightGray));
        /// <summary>
        /// Brush for the standard output text
        /// </summary>
        public Brush OutputTextBrush
        {
            get => (Brush)GetValue(OutputTextColorProperty);
            set => SetValue(OutputTextColorProperty, value);
        }

        private static readonly DependencyProperty ErrorTextColorProperty =
            DependencyProperty.Register("ErrorTextColor", typeof(Brush), typeof(ConsoleControl),
                new PropertyMetadata(Brushes.Red));
        /// <summary>
        /// Brush for the error output text
        /// </summary>
        public Brush ErrorTextBrush
        {
            get => (Brush)GetValue(ErrorTextColorProperty);
            set => SetValue(ErrorTextColorProperty, value);
        }

        private static readonly DependencyProperty InputTextColorProperty =
            DependencyProperty.Register("InputTextColor", typeof(Brush), typeof(ConsoleControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 224, 234, 9))));
        /// <summary>
        /// Brush for the input text
        /// </summary>
        public Brush InputTextBrush
        {
            get => (Brush)GetValue(InputTextColorProperty);
            set => SetValue(InputTextColorProperty, value);
        }

        private static readonly DependencyProperty ShowDiagnosticsProperty =
            DependencyProperty.Register("ShowDiagnostics", typeof(bool), typeof(ConsoleControl),
                new PropertyMetadata(false));

        /// <summary>
        /// Flag whether to show diagnostics.
        /// </summary>
        public bool ShowDiagnostics
        {
            get => (bool)GetValue(ShowDiagnosticsProperty);
            set => SetValue(ShowDiagnosticsProperty, value);
        }

        private static readonly DependencyProperty IsInputEnabledProperty =
            DependencyProperty.Register("IsInputEnabled", typeof(bool), typeof(ConsoleControl),
                new PropertyMetadata(true));

        /// <summary>
        /// Flag whether the user input is enabled.
        /// </summary>
        public bool IsInputEnabled
        {
            get => (bool)GetValue(IsInputEnabledProperty);
            set => SetValue(IsInputEnabledProperty, value);
        }

        internal static readonly DependencyPropertyKey IsProcessRunningPropertyKey =
            DependencyProperty.RegisterReadOnly("IsProcessRunning", typeof(bool), typeof(ConsoleControl),
                new PropertyMetadata(false));

        private static readonly DependencyProperty IsProcessRunningProperty = IsProcessRunningPropertyKey.DependencyProperty;
        /// <summary>
        /// Flag whether there is a process running
        /// </summary>
        public bool IsProcessRunning
        {
            get => (bool)GetValue(IsProcessRunningProperty);
            private set => SetValue(IsProcessRunningPropertyKey, value);
        }
        #endregion

        /// <summary>
        /// The internal process wrapper used to interact with the process.
        /// </summary>
        private readonly ProcessWrapper processWrapper = new ProcessWrapper();

        /// <summary>
        /// Current position that input starts at
        /// </summary>
        private int inputStartPos;

        /// <summary>
        /// The last input string (used so that we can make sure we don't echo input twice).
        /// </summary>
        private string lastInput;

        private List<string> commandBuffer = new List<string>();
        private int commandBufferIndex = -1;

        #region Events

        /// <summary>
        /// Occurs when console output is produced.
        /// </summary>
        public event ProcessEventHandler OnProcessOutput;

        /// <summary>
        /// Raises the console output event.
        /// </summary>
        /// <param name="args">Event data</param>
        private void RaiseProcessOutputEvent(ProcessEventArgs args)
        {
            OnProcessOutput?.Invoke(this, args);
        }

        /// <summary>
        /// Occurs when console input is produced.
        /// </summary>
        public event ProcessEventHandler OnProcessInput;

        /// <summary>
        /// Raises the console input event.
        /// </summary>
        /// <param name="args">Event data</param>
        private void RaiseProcessInputEvent(ProcessEventArgs args)
        {
            OnProcessInput?.Invoke(this, args);
        }


        /// <summary>
        /// Occurs when the process ends
        /// </summary>
        public event ProcessEventHandler OnProcessExit;

        /// <summary>
        /// Raises the process exit event.
        /// </summary>
        /// <param name="args">Event data</param>
        private void RaiseProcessExitEvent(ProcessEventArgs args)
        {
            OnProcessExit?.Invoke(this, args);
        }

        #endregion


        /// <summary>
        /// CTOR
        /// </summary>
        public ConsoleControl()
        {
            InitializeComponent();

            processWrapper.OnProcessOutput += OnProcessOutputHandler;
            processWrapper.OnProcessExit += OnProcessExitHandler;

            //Setup the console rich text box
            ContentRichTextBox.PreviewKeyDown += ConsoleKeyDownHandler;
            ContentRichTextBox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, InputTextBrush);
            DataObject.AddCopyingHandler(ContentRichTextBox, (s, e) =>
            {
                if (e.IsDragDrop) e.CancelCommand();
            });
        }

        /// <summary>
        /// Runs a process the console is bind to
        /// </summary>
        /// <param name="fileName">Name of the process executable file</param>
        /// <param name="arguments">Optional Command line arguments</param>
        ///<param name="workingDir">Optional working directory</param>
        public void StartProcess(string fileName, string arguments = null, string workingDir = null)
        {
            if (ShowDiagnostics)
            {
                WriteOutput(Environment.NewLine + "Preparing to run " + fileName, DiagnosticsTextBrush);
                if (!string.IsNullOrEmpty(arguments))
                    WriteOutput(" with arguments " + arguments + "." + Environment.NewLine, DiagnosticsTextBrush);
                else
                    WriteOutput("." + Environment.NewLine, DiagnosticsTextBrush);
            }

            //Start the process.
            var result = processWrapper.StartProcess(fileName, arguments, workingDir);
            if (ShowDiagnostics)
            {
                if (result)
                {
                    WriteOutput("Started " + fileName + Environment.NewLine, DiagnosticsTextBrush);
                }
                else
                {
                    WriteOutput("Can't start " + fileName + " - another process is running" + Environment.NewLine, DiagnosticsTextBrush);
                }

            }
            RunOnUiDispatcher(() =>
            {
                //Unlock the content rich text box
                ContentRichTextBox.IsReadOnly = false;

                IsProcessRunning = true;
            });
        }

        /// <summary>
        /// Stops the process  the console is bind to
        /// </summary>
        public void StopProcess()
        {
            processWrapper.StopProcess();
        }

        /// <summary>
        /// Handles the OnProcessOutput event of the process wrapper
        /// Provides the standard output or error output from the process
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Event data.</param>
        private void OnProcessOutputHandler(object sender, ProcessEventArgs args)
        {
            var brush = args.IsError ? ErrorTextBrush : OutputTextBrush;
            WriteOutput(args.Content, brush);

            //Raise the output event.
            RaiseProcessOutputEvent(args);
        }

        /// <summary>
        /// Handles the OnProcessExit event of the process wrapper
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        private void OnProcessExitHandler(object sender, ProcessEventArgs args)
        {
            RunOnUiDispatcher(() =>
            {
                if (ShowDiagnostics)
                {
                    WriteOutput(Environment.NewLine + processWrapper.Command + " exited.", DiagnosticsTextBrush);
                }

                //Disable the console input
                ContentRichTextBox.IsReadOnly = true;

                IsProcessRunning = false;
            });

            //Raise process exit event
            RaiseProcessExitEvent(args);
        }

        /// <summary>
        /// Handles the KeyDown event of the richTextBoxConsole control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data.</param>
        private void ConsoleKeyDownHandler(object sender, KeyEventArgs e)
        {
            var caretPosition = ContentRichTextBox.GetCaretPosition();
            var delta = caretPosition - inputStartPos;
            var inReadOnlyZone = delta < 0;

            //Command buffer - Ctrl-up / Ctrl-down
            if ((e.Key == Key.Up || e.Key == Key.Down) &&
                Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
                commandBuffer.Count > 0 &&
                IsInputEnabled)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (e.Key)
                {
                    case Key.Up:
                        {
                            commandBufferIndex--;
                            if (commandBufferIndex < 0) commandBufferIndex = 0;
                            break;
                        }
                    case Key.Down:
                        {
                            commandBufferIndex++;
                            if (commandBufferIndex > commandBuffer.Count - 1) commandBufferIndex = commandBuffer.Count - 1;
                            break;
                        }
                }

                var command = commandBuffer[commandBufferIndex];
                var _ = new TextRange(ContentRichTextBox.GetPointerAt(inputStartPos), ContentRichTextBox.GetEndPointer()) { Text = command };
                ContentRichTextBox.SetCaretToEnd();
                e.Handled = true;
                return;

            }

            //ESC to clear
            if (e.Key == Key.Escape && IsInputEnabled)
            {
                var _ = new TextRange(ContentRichTextBox.GetPointerAt(inputStartPos), ContentRichTextBox.GetEndPointer()) { Text = "" };
                e.Handled = true;
                return;
            }

            //Always allow arrows and Ctrl-C.
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down ||
                e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;

            //Input allowed?
            //Backspace needs to prevent +1 character after the read only zone
            if (inReadOnlyZone || !IsInputEnabled || e.Key == Key.Back && delta <= 0)
            {
                e.Handled = true;
                return;
            }

            //If not Return key, just let WPF process it
            if (e.Key != Key.Return) return;

            //Process return key
            var input = new TextRange(ContentRichTextBox.GetPointerAt(inputStartPos), ContentRichTextBox.GetEndPointer()).Text;
            //Write the input (without echoing).
            WriteInput(input, InputTextBrush, false);
            ContentRichTextBox.SetCaretToEnd();
        }

        /// <summary>
        /// Writes the input to the console control.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="brush">The color.</param>
        /// <param name="echo">if set to <c>true</c> echo the input.</param>
        public void WriteInput(string input, Brush brush, bool echo)
        {
            input = input.TrimEnd('\r', '\n');
            RunOnUiDispatcher(() =>
            {
                if (echo)
                {
                    ContentRichTextBox.Selection.ApplyPropertyValue(TextBlock.ForegroundProperty, brush);
                    ContentRichTextBox.AppendText(input + Environment.NewLine);
                    inputStartPos = ContentRichTextBox.GetEndPosition();
                }

                lastInput = input;

                //Write the input.
                processWrapper.WriteInput(input);

                //update command buffer
                commandBuffer.Add(input);
                commandBufferIndex = commandBuffer.Count;

                // Raise the input event.
                RaiseProcessInputEvent(new ProcessEventArgs(input));
            });
        }

        /// <summary>
        /// Writes the output to the console control.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="brush">The color.</param>
        public void WriteOutput(string output, Brush brush)
        {
            if (string.IsNullOrEmpty(lastInput) == false &&
                (output == lastInput || output.Replace("\r\n", "") == lastInput))
                return;

            RunOnUiDispatcher(() =>
            {
                //Write the output.
                var range = new TextRange(ContentRichTextBox.GetEndPointer(), ContentRichTextBox.GetEndPointer())
                {
                    Text = output
                };
                range.ApplyPropertyValue(TextElement.ForegroundProperty, brush);

                //Get to the end
                ContentRichTextBox.ScrollToEnd();
                ContentRichTextBox.SetCaretToEnd();
                //Set the start of the input zone
                inputStartPos = ContentRichTextBox.GetCaretPosition();

                //Switch to input color
                ContentRichTextBox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, InputTextBrush);
                var _ = new Run("", ContentRichTextBox.CaretPosition) { Foreground = InputTextBrush };
            });
        }

        /// <summary>
        /// Clears the output.
        /// </summary>
        public void Clear()
        {
            ContentRichTextBox.Document.Blocks.Clear();
            inputStartPos = 0;
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
