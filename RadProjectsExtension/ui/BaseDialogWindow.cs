using Microsoft.VisualStudio.PlatformUI;

namespace net.adamec.dev.vs.extension.radprojects.ui
{
    /// <inheritdoc />
    /// <summary>
    /// Base class for the dialog windows used within the extension - to be used as a root element of  windows' XAML files
    /// </summary>
    public class BaseDialogWindow : DialogWindow
    {
        /// <summary>
        /// Flag whether the window is show as modal
        /// Use the <see cref="ShowMe"/> method when opening the window to ensure that the property is set
        /// Use IsVisible before to check whether the window is shown
        /// </summary>
        public bool IsModal { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// CTOR - disable Maximize and Minimize buttons
        /// </summary>
        public BaseDialogWindow()
        {
            HasMaximizeButton = false;
            HasMinimizeButton = false;
        }

        /// <summary>
        /// Shows the window in modal or non-modal mode
        /// </summary>
        /// <param name="modal">True to open the window as modal</param>
        /// <returns>Dialog result (true/false) for modals, null for non-modals</returns>
        public bool? ShowMe(bool modal)
        {
            if (modal)
            {
                IsModal = true;
                return ShowDialog();
            }

            IsModal = false;
            Show();
            return null;
        }

        /// <summary>
        /// Closes the window in modal or non-modal mode
        /// </summary>
        /// <param name="dialogResult">Dialog result (true/false) for modals, null for non-modals</param>
        protected void CloseMe(bool? dialogResult = null)
        {
            if (IsModal)
            {
                DialogResult = dialogResult;
            }
            else
            {
                Close();
            }
        }
    }
}
