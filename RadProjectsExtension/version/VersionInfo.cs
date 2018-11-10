using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using net.adamec.dev.vs.extension.radprojects.utils;

namespace net.adamec.dev.vs.extension.radprojects.version
{
    /// <inheritdoc />
    /// <summary>
    /// Information about version as in Version.props file
    /// </summary>
    /// <remarks>
    /// Version.props file is a MS Build property file with the version information used in my
    /// customized build process
    /// &lt;Project&gt;
    ///   &lt;PropertyGroup&gt;
    ///     &lt;RadMajor&gt;0&lt;/RadMajor&gt;
    ///     &lt;RadMinor&gt;1&lt;/RadMinor&gt;
    ///     &lt;RadPatch&gt;0&lt;/RadPatch&gt;
    ///     &lt;RadBuild&gt;279&lt;/RadBuild&gt;
    ///     &lt;PackageVersionShort&gt;0.1.0-dev.279.181101121206&lt;/PackageVersionShort&gt;
    ///     &lt;PackageVersionFull&gt;0.1.0-dev.279.181101121206+38.master.6415348-dirty&lt;/PackageVersionFull&gt;
    ///     &lt;GitCommit&gt;6415348-dirty&lt;/GitCommit&gt;
    ///     &lt;GitBranch&gt;master&lt;/GitBranch&gt;
    ///   &lt;/PropertyGroup&gt;
    /// &lt;/Project&gt;
    ///</remarks>
    public class VersionInfo : INotifyPropertyChanged
    {
        public const string StringNotAvailable = "N/A";
        public const int IntNotAvailable = -1;

        /// <summary>
        /// Version.props file path
        /// </summary>
        private string versionFile;
        public string VersionFile { get => versionFile; set { versionFile = value; NotifyPropertyChanged(nameof(VersionFile)); } }
        /// <summary>
        ///  Major part of version number
        /// </summary>
        private int major;
        public int Major { get => major; set { major = value; NotifyPropertyChanged(nameof(Major)); } }
        /// <summary>
        /// Minor  part of version number
        /// </summary>
        private int minor;
        public int Minor { get => minor; set { minor = value; NotifyPropertyChanged(nameof(Minor)); } }
        /// <summary>
        /// Patch part of version number
        /// </summary>
        private int patch;
        public int Patch { get => patch; set { patch = value; NotifyPropertyChanged(nameof(Patch)); } }
        /// <summary>
        /// Build number
        /// </summary>
        private int buildNumber;
        public int BuildNumber { get => buildNumber; set { buildNumber = value; NotifyPropertyChanged(nameof(BuildNumber)); } }

        /// <summary>
        /// Short SHA of current git commit (with suffix -dirty if applicable)
        /// </summary>
        private string gitCommit;
        public string GitCommit { get => gitCommit; set { gitCommit = value; NotifyPropertyChanged(nameof(GitCommit)); } }
        //Name of the git branch
        private string gitBranch;
        public string GitBranch { get => gitBranch; set { gitBranch = value; NotifyPropertyChanged(nameof(GitBranch)); } }
        /// <summary>
        /// Total number of git commits
        /// </summary>
        private string gitCommits;
        public string GitCommits { get => gitCommits; set { gitCommits = value; NotifyPropertyChanged(nameof(GitCommits)); } }
        /// <summary>
        /// Short package version as used in package name
        /// </summary>
        private string packageVersionShort;
        public string PackageVersionShort { get => packageVersionShort; set { packageVersionShort = value; NotifyPropertyChanged(nameof(PackageVersionShort)); } }
        /// <summary>
        /// Full package version as used in NuSpec file
        /// </summary>
        private string packageVersionFull;
        public string PackageVersionFull { get => packageVersionFull; set { packageVersionFull = value; NotifyPropertyChanged(nameof(PackageVersionFull)); } }

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
        /// Loads the <see cref="VersionInfo"/> from given file
        /// Missing fields are defaulted to <see cref="StringNotAvailable"/> or <see cref="IntNotAvailable"/>
        /// <see cref="VersionFile"/> property is set to <paramref name="fileName"/>
        /// </summary>
        /// <param name="fileName">Name of the file to load the version info from</param>
        /// <returns><see cref="VersionInfo"/> loaded from <paramref name="fileName"/></returns>
        public static VersionInfo Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (!File.Exists(fileName)) throw new Exception($"Version file {fileName} doesn't exist");

            //read version file
            var document = XDocument.Load(fileName);
            var projectNode = GetOrCreateElement(document, "Project");

            var retVal = new VersionInfo
            {
                Major = GetProjectPropertyNode(projectNode, "RadMajor", IntNotAvailable),
                Minor = GetProjectPropertyNode(projectNode, "RadMinor", IntNotAvailable),
                Patch = GetProjectPropertyNode(projectNode, "RadPatch", IntNotAvailable),
                BuildNumber = GetProjectPropertyNode(projectNode, "RadBuild", IntNotAvailable),
                GitCommit = GetProjectPropertyNode(projectNode, "GitCommit", StringNotAvailable),
                gitBranch = GetProjectPropertyNode(projectNode, "GitBranch", StringNotAvailable),
                gitCommits = GetProjectPropertyNode(projectNode, "GitCommits", StringNotAvailable),
                PackageVersionShort = GetProjectPropertyNode(projectNode, "PackageVersionShort", StringNotAvailable),
                packageVersionFull = GetProjectPropertyNode(projectNode, "PackageVersionFull", StringNotAvailable),
                VersionFile = fileName
            };

            return retVal;
        }

        /// <summary>
        /// Loads the <see cref="VersionInfo"/> from solution's Version.props file
        /// It searches for the file  with following priority:
        ///  - in solution's root dir
        ///  - in  build dir
        ///  - in all other (sub)folders - in this case the first file with the lowest "depth of dirs" is used
        /// <see cref="VersionFile"/> property is set to full file name of the file used
        /// Missing fields are defaulted to <see cref="StringNotAvailable"/> or <see cref="IntNotAvailable"/>
        /// </summary>
        /// <param name="solutionInfo"></param>
        /// <returns>Solution's <see cref="VersionInfo"/> or null if Version.props file not found</returns>
        public static VersionInfo Load(SolutionInfo solutionInfo)
        {
            if (solutionInfo == null) throw new ArgumentNullException(nameof(solutionInfo));
            //priority: root, build, all sub dirs, 2nd level subdirs, ...
            var files = new DirectoryInfo(solutionInfo.SolutionDir).GetFiles("Version.props", SearchOption.AllDirectories);
            string fileFound = null;
            var fileFoundPriority = int.MaxValue;
            foreach (var fileInfo in files)
            {
                var file = fileInfo.FullName;
                var fileRel = file.Substring(solutionInfo.SolutionDir.Length).ToLower();
                int priority;
                switch (fileRel)
                {
                    case @"\version.props": priority = 0; break;
                    case @"\build\version.props": priority = 1; break;
                    default:
                        priority = fileRel.Length - fileRel.Replace(@"\", "").Length; break; //count back slashes
                }

                if (priority >= fileFoundPriority) continue;
                fileFound = file;
                fileFoundPriority = priority;
            }

            return fileFound == null ? null : Load(fileFound);
        }

        /// <summary>
        /// Saves the updates version info into the file defined in <see cref="VersionFile"/> property
        /// When the property is not defined or the file doesn't exists anymore, it just returns without any exception raised
        /// </summary>
        public void SaveUpdatedVersion()
        {
            if (string.IsNullOrEmpty(VersionFile) | !File.Exists(VersionFile)) return;

            var document = XDocument.Load(VersionFile);
            var projectNode = GetOrCreateElement(document, "Project");

            SetProjectPropertyNode(projectNode, "RadMajor", Major.ToString());
            SetProjectPropertyNode(projectNode, "RadMinor", Minor.ToString());
            SetProjectPropertyNode(projectNode, "RadPatch", Patch.ToString());
            SetProjectPropertyNode(projectNode, "RadBuild", BuildNumber.ToString());

            File.WriteAllText(VersionFile, document.ToString());
        }

        /// <summary>
        /// Creates a new Version.prop file in solution's root directory and saves the current version info into the file
        /// </summary>
        /// <param name="solutionInfo">Solution information</param>
        public void CreateVersionFile(SolutionInfo solutionInfo)
        {
            var document = new XDocument();
            var projectNode = GetOrCreateElement(document, "Project");

            SetProjectPropertyNode(projectNode, "RadMajor", Major.ToString());
            SetProjectPropertyNode(projectNode, "RadMinor", Minor.ToString());
            SetProjectPropertyNode(projectNode, "RadPatch", Patch.ToString());
            SetProjectPropertyNode(projectNode, "RadBuild", BuildNumber.ToString());

            var file = Path.Combine(solutionInfo.SolutionDir, "Version.props");
            File.WriteAllText(file, document.ToString());
        }

        /// <summary>
        /// Gets the typed value of node identified by <paramref name="nodeName"/> within the <paramref name="projectNode"/> container,
        /// so it looks for /Project/PropertyGroup/nodeName.
        /// If the node is not found, or the value can't be converted to given type (<typeparamref name="TTypeOfValue"/>, <paramref name="defaultValue"/> is returned
        /// </summary>
        /// <typeparam name="TTypeOfValue">The type the value is to be returned in</typeparam>
        /// <param name="projectNode">Root node (Project tag)</param>
        /// <param name="nodeName">Name of the node with /Project/PropertyGroup tag to look for</param>
        /// <param name="defaultValue">Default value to be returned, if the value can't be retrieved or converted to<typeparamref name="TTypeOfValue"/></param>
        /// <returns>Value  of the property of the <paramref name="defaultValue"/> when the property can't be retrieved</returns>
        private static TTypeOfValue GetProjectPropertyNode<TTypeOfValue>(XContainer projectNode, string nodeName, TTypeOfValue defaultValue)
        {
            var propertyNode = projectNode
                .Elements("PropertyGroup")
                .SelectMany(it => it.Elements(nodeName))
                .SingleOrDefault();

            // ReSharper disable once InvertIf
            if (propertyNode?.Value != null)
            {
                var typeConverter = TypeDescriptor.GetConverter(typeof(TTypeOfValue));
                try
                {
                    var propValue = typeConverter.ConvertFromString(propertyNode.Value);
                    if (propValue != null)
                        return (TTypeOfValue)propValue;
                }
                catch (Exception)
                {
                    //do nothing, defaultValue will be returned
                }
            }
            //If no node exists or value can't be converted, return default
            return defaultValue;
        }

        /// <summary>
        /// Sets the <paramref name="nodeValue"/> into node  identified by <paramref name="nodeName"/> within the <paramref name="projectNode"/> container
        /// When the node doesn't exist, it's created and its <paramref name="nodeValue"/> is set
        /// </summary>
        /// <param name="projectNode">Root node (Project tag)</param>
        /// <param name="nodeName">Name of the node with /Project/PropertyGroup tag to set</param>
        /// <param name="nodeValue">Value to test to the node</param>
        private static void SetProjectPropertyNode(XContainer projectNode, string nodeName, string nodeValue)
        {
            var propertyNode = projectNode
                .Elements("PropertyGroup")
                .SelectMany(it => it.Elements(nodeName))
                .SingleOrDefault();
            //If no node exists, create it.
            if (propertyNode == null)
            {
                var propertyGroupNode = GetOrCreateElement(projectNode, "PropertyGroup");
                propertyNode = GetOrCreateElement(propertyGroupNode, nodeName);
            }
            propertyNode.SetValue(nodeValue);
        }

        /// <summary>
        /// Gets the child element by <paramref name="name"/> (tag) from the <paramref name="container"/>
        /// When the element doesn't exist, it's created and the new element is returned
        /// </summary>
        /// <param name="container">Node to get the child element from</param>
        /// <param name="name">Name of the child element to retrieve</param>
        /// <returns>The child element of <paramref name="container"/> having given <paramref name="name"/></returns>
        private static XElement GetOrCreateElement(XContainer container, string name)
        {
            var element = container.Element(name);
            if (element != null) return element;

            element = new XElement(name);
            container.Add(element);
            return element;
        }
    }
}
