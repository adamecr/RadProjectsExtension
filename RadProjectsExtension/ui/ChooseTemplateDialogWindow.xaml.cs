using System.Collections.Generic;
using System.Windows;
using net.adamec.dev.vs.extension.radprojects.template;

namespace net.adamec.dev.vs.extension.radprojects.ui
{
    /// <summary>
    /// Interaction logic for ChooseTemplateDialogWindow.xaml
    /// </summary>
    public partial class ChooseTemplateDialogWindow
    {
        private RadProjectsExtensionPackage Package { get; }
        /// <summary>
        /// Selected template "returned" when the dialog is  closed by OK button
        /// </summary>
        public TemplateInfo SelectedTemplateInfo { get; private set; }

        /// <summary>
        /// CTOR - initialize the dialog window
        /// </summary>
        /// <param name="package">Reference to package the dialog window belongs to</param>
        /// <param name="templates">List of templates to choose from</param>
        public ChooseTemplateDialogWindow(RadProjectsExtensionPackage package,IEnumerable<TemplateInfo> templates)
        {
            Package = package;
            InitializeComponent();
            ListBoxTemplates.ItemsSource = templates;
        }

        /// <summary>
        /// OK button click handler
        /// Validates whether any template is selected and if yes, sets <see cref="SelectedTemplateInfo"/> property and closes the dialog with positive result
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnOk_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedTemplateInfo = (TemplateInfo) ListBoxTemplates.SelectedItem;
            if (SelectedTemplateInfo == null)
            {
                Package.MessageBoxErr("Please select the template", "Apply template");
                return;
            }
            DialogResult = true;
        }
    }
}
