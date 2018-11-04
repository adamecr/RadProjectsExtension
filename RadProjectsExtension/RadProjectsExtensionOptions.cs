using System;
using System.ComponentModel;
using System.IO;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace net.adamec.dev.vs.extension.radprojects
{
    /// <inheritdoc />
    /// <summary>
    /// Rad Project Extensions settings definition and data object
    /// </summary>
    public class RadProjectsExtensionOptions : DialogPage
    {
        /// <summary>
        /// Default templates directory - %Projects%\Template
        /// </summary>
        public static string DefaultTemplatesDir= Path.Combine("%Projects%", "Template");

        /// <summary>
        /// Template directory path
        /// </summary>
        [Category("Solution Templates")]
        [DisplayName("Templates Dir")]
        [Description("Templates location")]
        public string TemplatesDir { get; set; }

        /// <summary>
        /// Replaces the %UserProfile%, %MyDocuments%, %Projects% variables in given value with actual values
        /// `%Projects%` - VS default projects directory
        /// `%MyDocuments%` - My Documents directory of current user
        /// `%UserProfile%` - User profile  directory of current user
        /// </summary>
        /// <param name="value">Value to be updated</param>
        /// <param name="dte">DTE object providing the access to the VS settings</param>
        /// <returns></returns>
        public static string ApplyVariables(string value, DTE2 dte)
        {
            if (string.IsNullOrEmpty(value)) return value;

            ThreadHelper.ThrowIfNotOnUIThread();
            var defaultProjectPath = (string)dte.Properties["Environment", "ProjectsAndSolution"].Item("ProjectsLocation").Value;

            var retVal = value;
            retVal = retVal.Replace("%Projects%", defaultProjectPath); 
            retVal = retVal.Replace("%UserProfile%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            retVal = retVal.Replace("%MyDocuments%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            return retVal;
        }

    }
}
