using System.ComponentModel;
using System.Windows;
using net.adamec.dev.vs.extension.radprojects.utils;
using net.adamec.dev.vs.extension.radprojects.version;

namespace net.adamec.dev.vs.extension.radprojects.ui.version
{
    /// <summary>
    /// Interaction logic for VersionDialogWindow.xaml
    /// </summary>
    public partial class VersionDialogWindow : INotifyPropertyChanged
    {
        private VersionInfo versionInfo;
        /// <summary>
        /// Version information managed by the dialog
        /// </summary>
        public VersionInfo VersionInfo { get => versionInfo; private set { versionInfo = value; NotifyPropertyChanged(nameof(VersionInfo)); } }

        /// <summary>
        /// Solution info
        /// </summary>
        public SolutionInfo SolutionInfo { get; }

        /// <inheritdoc />
        /// <summary>
        /// Property Changed event - to be raised in property setters to notify the UI about the changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName">Name of the property changed</param>
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// CTOR - loads the <see cref="VersionInfo"/> when constructed
        /// </summary>
        /// <param name="solutionInfo"></param>
        public VersionDialogWindow(SolutionInfo solutionInfo)
        {
            SolutionInfo = solutionInfo;
            InitializeComponent();

            Refresh();
        }
        #region Event handlers
        /// <summary>
        /// Dialog/Window OK button click handler
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnOk_OnClick(object sender, RoutedEventArgs e)
        {
            if (VersionInfo.VersionFile != null)
            {
                VersionInfo.SaveUpdatedVersion();
            }
            else
            {
                VersionInfo.CreateVersionFile(SolutionInfo);
            }

            CloseMe(true);
        }
        /// <summary>
        /// Dialog/Window Cancel button click handler
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            CloseMe(false);
        }

        /// <summary>
        /// Reload button click handler
        /// Reloads the solution and version information and updated UI accordingly
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnReload_OnClick(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        /// <summary>
        /// Patch+ button click handler
        /// Increases the Patch value
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnPatchPlus_OnClick(object sender, RoutedEventArgs e)
        {
            if (VersionInfo == null) return;
            VersionInfo.Patch++;
        }

        /// <summary>
        /// Minor+ button click handler
        /// Increases the Minor value, resets the Patch value
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnMinorPlus_OnClick(object sender, RoutedEventArgs e)
        {
            if (VersionInfo == null) return;
            VersionInfo.Minor++;
            VersionInfo.Patch = 0;
        }
        /// <summary>
        /// Major+ button click handler
        /// Increases the Major value, resets the Minor and Patch values
        /// </summary>
        /// <param name="sender">Sender raising the event</param>
        /// <param name="e">Event arguments</param>
        private void BttnMajorPlus_OnClick(object sender, RoutedEventArgs e)
        {
            if (VersionInfo == null) return;
            VersionInfo.Major++;
            VersionInfo.Minor = 0;
            VersionInfo.Patch = 0;
        }

        #endregion

        /// <summary>
        /// Refreshes the <see cref="SolutionInfo"/> and (re)loads the <see cref="VersionInfo"/>
        /// Updates the UI elements accordingly
        /// </summary>
        private void Refresh()
        {
            SolutionInfo.Refresh();
            Title = $"Version - {SolutionInfo.SolutionName}";
            VersionInfo = VersionInfo.Load(SolutionInfo);

            if (VersionInfo != null)
            {
                TbCurrentVersion.Text =
                    $"{VersionInfo.Major}.{VersionInfo.Minor}.{VersionInfo.Patch}.{VersionInfo.BuildNumber}";
                TbCurrentPackageVersionShort.Text = VersionInfo.PackageVersionShort;
                TbCurrentPackageVersionFull.Text = VersionInfo.PackageVersionFull;
                TbCurrentVersionErr.Visibility = Visibility.Collapsed;
            }
            else
            {
                VersionInfo = new VersionInfo()
                {
                    Major = 0,
                    Minor = 1,
                    Patch = 0,
                    BuildNumber = 0,
                };
                TbCurrentVersionErr.Visibility = Visibility.Visible;
                TbCurrentVersion.Text ="";
                TbCurrentPackageVersionShort.Text = "";
                TbCurrentPackageVersionFull.Text = "";
            }

            DataContext = VersionInfo;
        }
    }
}
