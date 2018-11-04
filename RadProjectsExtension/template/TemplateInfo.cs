using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace net.adamec.dev.vs.extension.radprojects.template
{
    /// <summary>
    /// Template information data object
    /// </summary>
    public class TemplateInfo
    {
        /// <summary>
        /// Template directory (used in "local" template information files, ignored in template definition files)
        /// </summary>
        [JsonProperty ("templateDir")]
        public string TemplateDir { get; set; }
        /// <summary>
        /// Name of the template
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
        /// <summary>
        /// List of the file names that are not to be overwritten when the template is copied to the target
        /// </summary>
        [JsonProperty("doNotOverwriteFileNames")]
        public string[] DoNotOverwriteFileNames { get; set; }
        /// <summary>
        /// List of the files that are not to be added to the target solution as the solution items (the files are just copies to the solution directory)
        /// </summary>
        [JsonProperty("doNotAddToSolutionFileNames")]
        public string[] DoNotAddToSolutionFileNames { get; set; }
        /// <summary>
        /// List of file paths relative to the target solution directory of the files to be removed when the template is applied
        /// </summary>
        [JsonProperty("removeFileNames")]
        public string[] RemoveFileNames { get; set; }
        /// <summary>
        /// Definitions of the build dependencies to be set (if provided)
        /// </summary>
        [JsonProperty("buildDependencies")]
        public BuildDependency[] BuildDependencies { get; set; }

        /// <summary>
        /// Checks whether the <paramref name="sourceFileName"/> is in the add to solution exclusion list (<see cref="DoNotOverwriteFileNames"/>)
        /// </summary>
        /// <param name="sourceFileName">True if the <paramref name="sourceFileName"/> is not to be added as the solution item</param>
        /// <returns></returns>
        public bool DoNotAddToSolution(string sourceFileName)
        {
            return DoNotAddToSolutionFileNames.ToList().Exists(sourceFileName.EndsWith);
        }

        /// <summary>
        /// Loads the <see cref="TemplateInfo"/> from given <paramref name="fileName">file</paramref>
        /// </summary>
        /// <param name="fileName">Full path to the file to load the template information from</param>
        /// <returns>Instance of <see cref="TemplateInfo"/> loaded from <paramref name="fileName">file</paramref></returns>
        public static TemplateInfo Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (!File.Exists(fileName)) throw new Exception($"Template file {fileName} doesn't exist");

            var templateInfo= JsonConvert.DeserializeObject<TemplateInfo>(File.ReadAllText(fileName));
            return templateInfo;
        }

        /// <summary>
        /// Saves the current <see cref="TemplateInfo"/> to given <paramref name="fileName">file</paramref>
        /// </summary>
        /// <param name="fileName">Full path to the file to save the template information to</param>
        public void Save(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            var templateInfoContent = JsonConvert.SerializeObject(this);
            File.WriteAllText(fileName, templateInfoContent);
        }
    }

    /// <summary>
    /// Build dependency definition (part of the template information)
    /// </summary>
    public class BuildDependency
    {
        /// <summary>
        /// Name of the project(s) to set the dependency for
        ///  * means all projects
        ///  *-project1,project2 means all projects but the ones defines in list split by ","
        ///  other strings represent the project names split by ","
        /// </summary>
        [JsonProperty("project")]
        public string ProjectName { get; set; }

        /// <summary>
        /// Name of the required project, the <see cref="ProjectName"/> depends on
        /// </summary>
        [JsonProperty("dependsOn")]
        public string DependsOnProjectName { get; set; }

        /// <summary>
        ///  Returns the string representation of the build dependency
        /// </summary>
        /// <returns>String representation of the build dependency</returns>
        public override string ToString()
        {
            return $"project '{ProjectName}' depends on '{DependsOnProjectName}'";
        }
    }
}
