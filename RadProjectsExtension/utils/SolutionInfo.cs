using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace net.adamec.dev.vs.extension.radprojects.utils
{
    /// <summary>
    /// Information about the target solution
    /// </summary>
    public class SolutionInfo
    {
        /// <summary>
        /// <see cref="Solution"/> object
        /// </summary>
        public Solution Solution { get; }
        /// <summary>
        /// Solution directory
        /// </summary>
        public string SolutionDir { get; private set; }
        /// <summary>
        /// List of <see cref="Project">project</see> existing within the solution
        /// </summary>
        public List<Project> ExistingProjects { get; private set; }
        /// <summary>
        /// Solution Items (virtual) <see cref="Project"/>
        /// </summary>
        public Project SolutionItemsProject { get; private set; }
        /// <summary>
        /// List of existing Solution Items
        /// </summary>
        public List<ProjectItem> SolutionItems { get; private set; }
        /// <summary>
        /// Returns true when there is any Solution Item (Solution Items project exists)
        /// </summary>
        public bool HasSolutionItemsProject => SolutionItemsProject != null;

        /// <summary>
        /// CTOR - creates <see cref="SolutionInfo"/> from given DTE <paramref name="solution"/> object
        /// </summary>
        /// <param name="solution">DTE <see cref="Solution"/> object</param>
        public SolutionInfo(Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (solution == null || solution.IsOpen == false) throw new Exception("A solution needs to be open");

            Solution = solution;
            Refresh();
        }

        /// <summary>
        /// Gets the current information from <see cref="Solution"/>
        /// </summary>
        public void Refresh()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SolutionDir = Path.GetDirectoryName(Solution.FullName);
            if (string.IsNullOrEmpty(SolutionDir)) throw new Exception("Can't get the solution directory");

            ExistingProjects = GetSolutionProjects(Solution);
            SolutionItemsProject = GetSolutionItemsProject(ExistingProjects);
            SolutionItems = SolutionItemsProject?.ProjectItems.GetEnumerator().ToList<ProjectItem>() ?? new List<ProjectItem>();
        }

        /// <summary>
        /// Checks whether the project with given <paramref name="projectFileName"/> exists
        /// </summary>
        /// <param name="projectFileName">Project file (.csproj) to check</param>
        /// <returns>True when the project exists within the solution</returns>
        public bool ProjectExists(string projectFileName)
        {
            return ExistingProjects.FirstOrDefault(p =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return new FileInfo(p.FileName).Name == projectFileName;
            }) != null;
        }

        /// <summary>
        /// Checks whether there is a solution item with given <paramref name="fileName"/>
        /// </summary>
        /// <param name="fileName">File to check</param>
        /// <returns>True when the <paramref name="fileName"/> exists within the Solution Items</returns>
        public bool HasSolutionItem(string fileName)
        {
            return SolutionItems.Exists(i =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return i.Name == fileName;
                });
        }

        /// <summary>
        /// Get the list of projects within the <paramref name="solution"/>
        /// </summary>
        /// <param name="solution">Solution to evaluate</param>
        /// <returns>List of projects within the solution</returns>
        public static List<Project> GetSolutionProjects(Solution solution)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            ThreadHelper.ThrowIfNotOnUIThread();

            return solution.Projects.GetEnumerator().ToList<Project>();
        }

        /// <summary>
        /// Checks whether the solution has any solution items (has Solution Items virtual project)
        /// </summary>
        /// <param name="solution">Solution to evaluate</param>
        /// <returns>True when the solution has solution items</returns>
        public static bool HasSolutionItems(Solution solution)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            return HasSolutionItems(GetSolutionProjects(solution));
        }

        /// <summary>
        /// Checks whether there is a virtual Solution Items project within the given list of <paramref name="projects"/>
        /// </summary>
        /// <param name="projects">List of projects to check</param>
        /// <returns>True when there is a Solution Items virtual project within the given list of projects</returns>
        public static bool HasSolutionItems(List<Project> projects)
        {
            if (projects == null) throw new ArgumentNullException(nameof(projects));
            return GetSolutionItemsProject(projects) != null;
        }

        /// <summary>
        /// Gets the virtual Solution Items project of given <paramref name="solution"/> 
        /// </summary>
        /// <param name="solution">Solution to evaluate</param>
        /// <returns>Solution Items project of given <paramref name="solution"/> or null when doesn't exist</returns>
        public static Project GetSolutionItemsProject(Solution solution)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            return GetSolutionItemsProject(GetSolutionProjects(solution));
        }

        /// <summary>
        /// Gets the virtual Solution Items project of from given list of <paramref name="projects"/>
        /// </summary>
        /// <param name="projects">List of projects to check</param>
        /// <returns>Solution Items project or null when doesn't exist</returns>
        public static Project GetSolutionItemsProject(List<Project> projects)
        {
            if (projects == null) throw new ArgumentNullException(nameof(projects));

            return projects.FirstOrDefault(IsProjectSolutionItems);
        }

        /// <summary>
        /// Checks whether the given <paramref name="project"/> is virtual Solution Items project
        /// </summary>
        /// <param name="project">Project to check</param>
        /// <returns>True when the <paramref name="project"/> is Solution Items project</returns>
        public static bool IsProjectSolutionItems(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return project.Name == "Solution Items" || project.Kind == Constants.vsProjectItemKindSolutionItems;
        }
    }
}
