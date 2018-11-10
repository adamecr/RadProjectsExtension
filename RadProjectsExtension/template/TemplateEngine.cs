using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using net.adamec.dev.vs.extension.radprojects.ui;
using net.adamec.dev.vs.extension.radprojects.utils;

namespace net.adamec.dev.vs.extension.radprojects.template
{
    /// <summary>
    /// Implements the Solution (Projects) template functionality
    /// </summary>
    public class TemplateEngine
    {
        /// <summary>
        /// Name of the template info file ("template.json")
        /// </summary>
        public const string TemplateInfoFileName = "template.json";

        /// <summary>
        /// Reference to the package
        /// </summary>
        private RadProjectsExtensionPackage Package { get; }
        /// <summary>
        /// Package settings
        /// </summary>
        private RadProjectsExtensionOptions Options { get; }
        /// <summary>
        /// Visual Studio DTE object
        /// </summary>
        private DTE2 Dte { get; }

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="package">Reference to the package</param>
        /// <param name="options">Package settings</param>
        /// <param name="dte">Visual Studio DTE object</param>
        public TemplateEngine(RadProjectsExtensionPackage package, RadProjectsExtensionOptions options, DTE2 dte)
        {
            Package = package;
            Options = options;
            Dte = dte;
        }

        /// <summary>
        /// Applies the template to current solution (called by Apply template button handler)
        /// </summary>
        public void ApplyTemplate()
        {
            try
            {
                Package.StartAction("Apply Template command");

                ThreadHelper.ThrowIfNotOnUIThread();

                var solution = Dte.Solution;
                if (solution == null || solution.IsOpen == false) throw new Exception("A solution needs to be open");
                var solutionInfo = new SolutionInfo(solution);

                //get template
                var templateInfo = GetTemplateInfo(solutionInfo);
                if (templateInfo == null) return; //no template or user cancelled 
                Package.Output($"Using template {templateInfo.Name} at {templateInfo.TemplateDir}");
                var templateDir = templateInfo.TemplateDir;

                //add projects
                Package.Output("Adding projects ...");
                var templateDirInfo = AddProjects(templateDir, solutionInfo, templateInfo);

                //add solution items
                Package.Output("Adding solutions items ...");
                AddSolutionItems(templateDirInfo, solutionInfo, templateInfo);

                //remove items
                if (templateInfo.RemoveFileNames != null)
                {
                    RemoveFiles(templateInfo, solutionInfo);
                }

                //store updated template info (with template dir reference) into solution folder
                Package.Output($"Updating {TemplateInfoFileName} file in solution folder...");
                templateInfo.Save(solutionInfo.SolutionDir.AddPath(TemplateInfoFileName));
                Package.Output($"Updated {TemplateInfoFileName} file in solution folder");

                //build dependencies
                if (templateInfo.BuildDependencies != null && templateInfo.BuildDependencies.Length > 0)
                {
                    Package.Output($"Processing build dependencies...");
                    ProcessBuildDependencies(templateInfo, solutionInfo);
                }
                Package.FinishAction();
            }
            catch (Exception exception)
            {
                Package.HandleException(exception, "Apply template - ERROR");
            }
        }

        /// <summary>
        /// Gets the template info to be used when applying the template
        /// First checks for the existing solution-local template info file. If exists, the reference to the template (template dir) is retrieved from the file  and such template is used
        /// When there is no local template file, it checks the template directory (defined in package settings).
        /// When a single template is available, the template is used
        /// When there are multiple templates available, a dialog with the list of templates is presented to user to choose the template
        /// </summary>
        /// <param name="solutionInfo">Current information about the opened solution</param>
        /// <returns>Template info (template) to be used or null when the action has been canceled by user or no templates are available</returns>
        private TemplateInfo GetTemplateInfo(SolutionInfo solutionInfo)
        {
            TemplateInfo templateInfo;
            //check for existing solution template first
            var existingLocalTemplateFile = solutionInfo.SolutionDir.AddPath(TemplateInfoFileName);
            if (File.Exists(existingLocalTemplateFile))
            {
                //if the local (solution) template exists, use it to get the reference to the template used by solution
                templateInfo = TemplateInfo.Load(existingLocalTemplateFile);
                Package.Output($"Using existing solution template file to identify template");
                if (!Directory.Exists(templateInfo.TemplateDir))
                {
                    throw new Exception($"Template directory {templateInfo.TemplateDir} doesn't exist");
                }

                //but use the template file (at template) for the processing to ensure the changes in template definition (the template file may be updated)
                var originalTemplateFile = templateInfo.TemplateDir.AddPath(TemplateInfoFileName);
                if (!File.Exists(originalTemplateFile))
                {
                    throw new Exception($"Original template file {originalTemplateFile} doesn't exist");
                }

                templateInfo = TemplateInfo.Load(originalTemplateFile);
                templateInfo.TemplateDir = new FileInfo(originalTemplateFile).DirectoryName;
            }
            else
            {
                var templates = GetTemplates();
                if (templates.Count == 0)
                {
                    Package.Output($"No template found");
                    Package.MessageBoxWarn("No template found", "Apply template");
                    return null;
                }

                templateInfo = templates.First();
                if (templates.Count <= 1) return templateInfo;

                //select template
                var dlg = new ChooseTemplateDialogWindow(Package, templates) { Owner = Application.Current.MainWindow };
                if (dlg.ShowMe(true) != true) return null;

                templateInfo = dlg.SelectedTemplateInfo;
            }

            return templateInfo;
        }

        /// <summary>
        /// Adds projects from template to target solution - copy the project and add to the solution (if needed)
        /// </summary>
        /// <param name="templateDir">Template directory</param>
        /// <param name="solutionInfo">Information about current solution</param>
        /// <param name="templateInfo">Information about the template used</param>
        /// <returns>Returns template directory <see cref="DirectoryInfo"/></returns>
        private DirectoryInfo AddProjects(string templateDir, SolutionInfo solutionInfo, TemplateInfo templateInfo)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var templateDirInfo = new DirectoryInfo(templateDir);
            foreach (var sourceProjectDirectoryInfo in templateDirInfo.GetDirectories()
                .Where(di => !di.Name.StartsWith(".") && di.GetFiles("*.csproj").Length > 0))
            {
                //copy project dir to solution dir
                var destinationDir = solutionInfo.SolutionDir.AddPath(sourceProjectDirectoryInfo.Name);
                Package.Output($"  Copying project from {sourceProjectDirectoryInfo.FullName} to {destinationDir}...");
                FileUtils.DirectoryCopy(sourceProjectDirectoryInfo.FullName, destinationDir, true, true,
                    templateInfo.DoNotOverwriteFileNames.ToList());

                //add project to solution
                var sourceProjectFiles = sourceProjectDirectoryInfo.GetFiles("*.csproj");
                if (sourceProjectFiles.Length != 1)
                    throw new Exception("Can't find unique project file within " + sourceProjectDirectoryInfo.FullName);
                var projectFileName = sourceProjectFiles[0].Name;

                var destinationProjectFileFullName = destinationDir.AddPath(projectFileName);
                if (!solutionInfo.ProjectExists(projectFileName))
                {
                    Package.Output($"  Adding project {destinationProjectFileFullName}...");
                    // ReSharper disable once RedundantArgumentDefaultValue
                    solutionInfo.Solution.AddFromFile(destinationProjectFileFullName, false);
                    Package.Output($"  Added project {destinationProjectFileFullName}");
                }
                else
                {
                    Package.Output($"  Project {destinationProjectFileFullName} already exists - skip add");
                }
            }

            return templateDirInfo;
        }

        /// <summary>
        /// Adds solution items from template to target solution - copy the files and add them to solution as Solution Items (when not excluded)
        /// </summary>
        /// <param name="templateDirInfo">Template directory's <see cref="DirectoryInfo"/></param>
        /// <param name="solutionInfo">Information about current solution</param>
        /// <param name="templateInfo">Information about the template used</param>
        private void AddSolutionItems(DirectoryInfo templateDirInfo, SolutionInfo solutionInfo, TemplateInfo templateInfo)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (var sourceFileInfo in templateDirInfo.GetFiles().Where(fi => !fi.Name.ToLower().EndsWith(".sln")))
            {
                var destFile = solutionInfo.SolutionDir.AddPath(sourceFileInfo.Name);
                Package.Output($"  Copying solution item from {sourceFileInfo.FullName} to {destFile}...");
                FileUtils.FileCopy(sourceFileInfo.FullName, destFile, true, templateInfo.DoNotOverwriteFileNames.ToList());

                if (solutionInfo.HasSolutionItem(sourceFileInfo.Name) ||
                    templateInfo.DoNotAddToSolution(sourceFileInfo.Name))
                {
                    Package.Output(
                        $"  Solution item {sourceFileInfo.Name} not added to solution - already exists or blacklisted");
                    continue;
                }

                Package.Output($"  Adding solution item {sourceFileInfo.Name} to solution...");
                Dte.ToolWindows.SolutionExplorer.UIHierarchyItems.Item(1).Select(vsUISelectionType.vsUISelectionTypeSelect);
                solutionInfo.Solution.DTE.ItemOperations.AddExistingItem(destFile);
                Package.Output($"  Added solution item {sourceFileInfo.Name} to solution");
            }
        }

        /// <summary>
        /// Removes the files defined in <paramref name="templateInfo"/> from the target solution
        /// </summary>
        /// <param name="templateInfo">Information about the template used</param>
        /// <param name="solutionInfo">Information about current solution</param>
        private void RemoveFiles(TemplateInfo templateInfo, SolutionInfo solutionInfo)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (var removeFileName in templateInfo.RemoveFileNames)
            {
                var fileToRemove = solutionInfo.SolutionDir.AddPath(removeFileName);
                if (!File.Exists(fileToRemove)) continue;

                Package.Output($"  Removing file {fileToRemove}...");

                var itemToRemove = solutionInfo.SolutionItems.FirstOrDefault(i =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return i.Name == removeFileName;
                });
                if (itemToRemove != null)
                {
                    var itmName = itemToRemove.Name;
                    try
                    {
                        itemToRemove.Remove();
                        Package.Output($"  Removed {itmName} from solution items");
                    }
                    catch (Exception)
                    {
                        //Can't remove solution item - ? just a problem of debug mode?
                        Package.Output($"  WARN: Can't remove {itmName} from solution items, please do it manually");
                        Package.ErrorListAddWarning($"Can't remove {itmName} from solution items, please do it manually");
                    }
                }

                File.Delete(fileToRemove);
                Package.Output($"  Removed file {fileToRemove}");
            }
        }

        /// <summary>
        /// Processes the build dependencies defined in template
        /// </summary>
        /// <param name="templateInfo">Information about the template used</param>
        /// <param name="solutionInfo">Information about current solution</param>
        private void ProcessBuildDependencies(TemplateInfo templateInfo, SolutionInfo solutionInfo)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var solutionBuild = solutionInfo.Solution.SolutionBuild;

            foreach (var buildDependency in templateInfo.BuildDependencies)
            {
                if (!string.IsNullOrEmpty(buildDependency.ProjectName) && !string.IsNullOrEmpty(buildDependency.DependsOnProjectName))
                {
                    ProcessBuildDependency(solutionInfo, buildDependency, solutionBuild);
                }
                else
                {
                    Package.Output($"  WARN: Dependency {buildDependency} is missing data ");
                    Package.ErrorListAddWarning($"Dependency {buildDependency} is missing data");
                }
            }

            Package.Output($"Processed build dependencies");
        }

        /// <summary>
        ///  Processes single build dependency defined in template
        /// </summary>
        /// <param name="solutionInfo">Information about current solution</param>
        /// <param name="buildDependency">Build dependency definition</param>
        /// <param name="solutionBuild">Visual Studio build wrapper for current solution</param>
        private void ProcessBuildDependency(SolutionInfo solutionInfo, BuildDependency buildDependency, SolutionBuild solutionBuild)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Package.Output($"  Processing build dependency {buildDependency}...");

            //get applicable projects (the projects the dependency is to be se on)
            var applicableProjects = GetBuildDependencyApplicableProjects(solutionInfo, buildDependency);

            if (applicableProjects.Count > 0)
            {
                var applicableProjectsStr = string.Join(", ", applicableProjects.Select(p =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return p.Name;
                }));
                Package.Output($"    Applicable projects: {applicableProjectsStr}");

                //find required project (the project that the applicable project depends on)
                var dependsOn = buildDependency.DependsOnProjectName.Trim();
                var dependsOnProject = solutionInfo.ExistingProjects.FirstOrDefault(p =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return p.Name == dependsOn;
                });
                if (dependsOnProject != null)
                {
                    SetBuildDependency(solutionBuild, applicableProjects, dependsOnProject);
                }
                else
                {
                    Package.Output($"    Required project (depends on) not found");
                }
            }
            else
            {
                Package.Output($"    No applicable projects found");
            }

            Package.Output($"  Processed build dependency {buildDependency}");
        }

        /// <summary>
        /// Gets the lists of projects the <paramref name="buildDependency"/> should be applied to
        /// </summary>
        /// <param name="solutionInfo">Information about current solution</param>
        /// <param name="buildDependency">Build dependency definition</param>
        /// <returns></returns>
        private static List<Project> GetBuildDependencyApplicableProjects(SolutionInfo solutionInfo, BuildDependency buildDependency)
        {
            //  * means all projects
            //  *-project1,project2 means all projects but the ones defines in list split by ","
            //  other strings represent the project names split by ","
            var projectsRaw = buildDependency.ProjectName.Trim();
            var allProjects = projectsRaw == "*" || projectsRaw.StartsWith("*-");
            if (projectsRaw == "*") projectsRaw = "";
            if (projectsRaw.StartsWith("*-")) projectsRaw = projectsRaw.Substring(2);
            var projects = projectsRaw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var applicableProjects = new List<Project>();

            foreach (var project in solutionInfo.ExistingProjects.Where(p =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return !SolutionInfo.IsProjectSolutionItems(p) &&
                       p.Name != "Miscellaneous Files" && p.Kind != Constants.vsProjectItemsKindMisc;
            }))
            {
                var isInList = projects.Any(p =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return p == project.Name;
                });
                if (allProjects && !isInList || !allProjects && isInList)
                {
                    applicableProjects.Add(project);
                }
            }

            return applicableProjects;
        }

        /// <summary>
        /// Sets the build dependencies in the Visual Studio solution
        /// The <paramref name="dependsOnProject"/> is excluded from the <paramref name="applicableProjects"/> automatically
        /// </summary>
        /// <param name="solutionBuild">Visual Studio build wrapper for current solution</param>
        /// <param name="applicableProjects">Projects to set the dependency </param>
        /// <param name="dependsOnProject">Required project the <paramref name="applicableProjects"/> depends on</param>
        private void SetBuildDependency(SolutionBuild solutionBuild, IEnumerable<Project> applicableProjects, Project dependsOnProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (var applicableProject in applicableProjects)
            {
                if (applicableProject.UniqueName == dependsOnProject.UniqueName) continue;

                solutionBuild.BuildDependencies.Item(applicableProject.UniqueName).AddProject(dependsOnProject.UniqueName);
                Package.Output($"    Added dependency {applicableProject.UniqueName} depends on {dependsOnProject.UniqueName}");
            }
        }

        /// <summary>
        /// Gets the list of templates available in the templates directory defined in package settings
        /// </summary>
        /// <returns>List of templates available in the templates directory defined in package settings (empty when no templates are available)</returns>
        public List<TemplateInfo> GetTemplates()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var retVal = new List<TemplateInfo>();

            var templatesPath = RadProjectsExtensionOptions.ApplyVariables(Options?.TemplatesDir ?? RadProjectsExtensionOptions.DefaultTemplatesDir, Dte);
            Package.Output($"Checking templates in {templatesPath}...");
            var templatesDir = new DirectoryInfo(templatesPath);
            var templateDirs = templatesDir.GetDirectories().Where(di => File.Exists(di.FullName.AddPath(TemplateInfoFileName)));
            foreach (var templateDir in templateDirs)
            {
                var templateFile = templateDir.FullName.AddPath(TemplateInfoFileName);
                var templateInfo = TemplateInfo.Load(templateFile);
                templateInfo.TemplateDir = templateDir.FullName;
                retVal.Add(templateInfo);
            }
            Package.Output($"Found {retVal.Count} templates in {templatesPath}");
            return retVal;
        }
    }
}
