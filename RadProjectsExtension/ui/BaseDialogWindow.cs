using Microsoft.VisualStudio.PlatformUI;

namespace net.adamec.dev.vs.extension.radprojects.ui
{
    /// <inheritdoc />
    /// <summary>
    /// Base class for the dialog windows used within the extension - to be used as a root element of  windows' XAML files
    /// </summary>
    public class BaseDialogWindow:DialogWindow
    {
        /// <inheritdoc />
        /// <summary>
        /// CTOR - disable Maximize and Minimize buttons
        /// </summary>
        public BaseDialogWindow()
        {
            HasMaximizeButton = false;
            HasMinimizeButton = false;
        }
    }
}
