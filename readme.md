# RAD Projects Extension #
RAD Projects Extension is an extension for Visual Studio 2017 supporting the "operations" work with my (RAD) projects (mostly .Net Core last years).
It's implementation also demonstrates some patterns/ways used in VSIX development.

## Solution (Project) Templates ##
I use some common "operation" patterns with my projects including the extension of MSBuild process, versioning, package publishing, etc. This is done by adding some "technical" build projects (not being build into the output assemblies), property files, etc. This means that such projects, solution files and some solution settings needs to be "replicated" across the solutions with the possibility to update from template later on.
The VS templating can be used when creating the new project, but it's hard to update later on. Also the VSIX is needed to handle the solution based files (incl. Solution Items).
Using NuGet packages might be a way, how to manage the updates, but there is some overhead needed to build and maintain the packages.

### How do the templates work in RAD Project Extension ###    
I have a directory with templates, where template is actually a VS solution that can live on its own, be versioned in GIT, etc. to design the template and the functionality it provides.
The sample template here is used to manage the build process of my projects. It provides two projects - `build` with MSBuild definitions (tasks, targets, props) and `build.tasks` with implementation of custom build tasks. It also provides some common solution files (for license file) or the files to be added to the solution, but not to be updated later on from template (for example readme file).

![Template Files](doc/img/templatefiles.png)

Template is applied to the target solution using the `Apply template` command available at `RAD Solution` submenu extending the VS context menu of the solution root.

![Template Files](doc/img/extensionmenu.png)

The extension looks for templates in the directory containing the templates defined is extension options that are a part of VS settings dialog.

![Template Files](doc/img/extensionoptions.png)

The template directory setting can contain some variables that are replaces by proper values when executed
- `%Projects%` - VS default projects directory
- `%MyDocuments%` - My Documents directory of current user
- `%UserProfile%` - User profile  directory of current user

The default value is `%Projects%\Template`.

In case there is just one template available, it's applied to the target solution. Otherwise it's necessary to choose a template from the list

![Template Files](doc/img/templatechoose.png)

*Note: when re-applying the template, the last template applied is used automatically*

When the template is applied to the target solution, the files are copied to the solution directory, the projects are added to solution as well as the solution items.
- **Add projects from template** - all template subdirs containing the `.csproj` file are recognized as a project, copied to target solution and added to the solution. The subdirs starting with dot (`.`) are automatically excluded. When the project exists in target solution, the files are copied (replaced if needed), but the project is not being added to the target solution as already exists there.* Note: The Net Core projects don't need the update of project files in case that the files are added/removed to/from project*
- **Add solution items from template** - all files from the root dir of template (except `.sln`) are copied to target solution directory and added to the target solution as solution items if not excluded (see below). When the target solution contains given solution item, it's not added anymore, just the file is being replaced (if not forbidden - see below)
- **Remove the files at target solution** - it's possible that during the template evolution, some files become obsolete and are to be removed from the target solution. These files are described in template definition (`template.json`) and are removed from target solution/projects. *Note: I have some issue when removing the file from solution items, so if there is an error, the warning is raised and the file is to be removed from solution items manually (the file itself is deleted from file system without any problems).*
- **Adjust the build dependencies of target solution** - the template allows to define the build dependencies to be applied to the target solution. For example, I want to have all projects, but `build.tasks` dependent on `build` project to ensure correct behavior of my build process. *Note: Keep in mind, that build dependency is just a way how to manage the build order of solution in VS and is not the project reference.* 

The behavior is adjusted in template's `template.json` file
```json
{
  "name": "RAD .net Core solution",
  "doNotOverwriteFileNames": ["Version.props","Directory.Build.props","readme.md","changelog.md"],
  "doNotAddToSolutionFileNames": [".gitattributes",".gitignore","template.json"],
  "removeFileNames": ["template.json.bak","build\\Git - Copy.targets"],
  "buildDependencies":[{project:"*-build.tasks", dependsOn:"build"}]
}
```
- **name** is the name of the template
- **doNotOverwriteFileNames** defines the set of file names that will be added from template to target if not exist, but will not be overwritten by template in case they already exist (even if the template has been updated and/or re-applied). The exclusion is based on file name regardless the location of the file, so all the files with the same name will not be replaced even if there are more files having the same name in different project.
- **doNotAddToSolutionFileNames** defines the set of files that will be copied to the target solution dir (if presented in template, of course), but will not be added to the solution items of the target solution.
- **removeFileNames** defines the set of files to be removed from the target solution if needed. In this case the path relative to the target solution dir needs to be specified for each file. When the target file doesn't exist, the remove files entry is simply ignored.
- **buildDependencies** allows to adjust the build order of the target solution. It set's the build dependency where specified project(s) depends on another one. `dependsOn` entry is the name of required project (ignored if doesn't exist in target solution). `project` can be individual project or set of projects:
  - `project:"build.tasks"` - the dependency for `build.tasks` project will be set
  - `project:"build,build.tasks"` - the dependency for `build` and `build.tasks` projects will be set. It's the list of projects separated by comma
  - `project:"*"` - the dependency for all projects in the solution will be set
  - `project:"*-build.tasks"` - the dependency for all projects in the solution but `build.tasks` will be set
  - `project:"*-build,build.tasks"` - the dependency for all projects in the solution but `build` and `build.tasks` will be set. The exclusions are defined as the list of projects separated by comma
  - *Note: when any of listed projects doesn't exist, the entry is ignored*
  - *Note: the `dependsOn` project is always excluded to prevent the circular dependency*

The `template.json` file is kind of special. It needs to be placed in the root of the template to define the template's behavior. It's created at target solution as well and the `templateDir` field is added/updated there containing the reference (path) to the source root directory of the applied template. When the templated is to be re-applied (updated), the engine first checks for the `template.json` in the target solution dir and uses the reference to the template (template dir) from the "local" file. However the "local" `template.json` is overwritten each time the template is (re)applied to keep the up to date information. In case a new template (or when the template source has been moved) is to be applied to the target solution, it's necessary to remove the local `template.json`.

## VSIX Implementation Hints ##
Some hints for VSIX implementation can be found within the source code

### Extending VS menu ###
Extending the VS menu with new (custom) commands is quite straightforward. It's declared in `.vcst` file of extension, however it's sometimes hard to find proper IDs of parent elements, to be extended. They can be often found in `vsshlids.h` file, but it's not so easy to understand or identify the proper entries. It helped to create the dummy submenu using VS menu/toolbar customization UI and export the Menu and toolbars customizations category in VS export settings and check in `UserCustomizations` section of exported file.
 
### Extending VS settings ###
When the property grid like dialog is enough, just implement the class inheriting from `DialogPage` and register it with the package. It can be retrieved from package later on when needed to get the actual values.
```csharp
public class RadProjectsExtensionOptions : DialogPage
{
	[Category("Solution Templates")]
	[DisplayName("Templates Dir")]
	[Description("Templates location")]
	public string TemplatesDir { get; set; }
}

[ProvideOptionPage(typeof(RadProjectsExtensionOptions), "RAD Projects Extension", "General", 106, 107, true)]
[ProvideProfile(typeof(RadProjectsExtensionOptions), "RAD Projects Extension", "RAD Projects Extension Settings", 106, 108, isToolsOptionPage: true, DescriptionResourceID = 109)]
public sealed class RadProjectsExtensionPackage : AsyncPackage
{
  ...
}

var settings = package.GetDialogPage(typeof(RadProjectsExtensionOptions)) as RadProjectsExtensionOptions;
``` 

### Accessing VS settings ###
Query the DTE.Properties to get to other VS settings when needed 
```csharp
var defaultProjectPath = (string)dte.Properties["Environment", "ProjectsAndSolution"].Item("ProjectsLocation").Value;
```

### Extending VS output ###
I use the dedicated (custom) output pane, that needs to be created during the package initialization.
```csharp
public sealed class RadProjectsExtensionPackage : AsyncPackage
{
	private IVsOutputWindowPane OutputWindow { get; set; }
	
	protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
	{
		await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

		if (!(GetGlobalService(typeof(SVsOutputWindow)) is IVsOutputWindow outWindow)) throw new Exception("Can't get the output window service");

		var customOutputPaneGuid = new Guid(OutputPaneGuidString);
		const string customOutputPaneTitle = "RAD Projects Extension";
		outWindow.CreatePane(ref customOutputPaneGuid, customOutputPaneTitle, 1, 1);

		outWindow.GetPane(ref customOutputPaneGuid, out var customPane);
		OutputWindow = customPane;
		Output("RAD Projects Extension init", true);

		...
	}

	public void Output(string text, bool activate = false)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		OutputWindow.OutputString(text + Environment.NewLine);
		if (activate) OutputWindow.Activate(); // Brings output pane into view
	}
```

### Work with error list ###
The extension uses the VS error list to point out the fatal errors or important warnings. The error list provider stores the items related to the extension and is cleared at the beggining of each action (apply template) to get rid of the old items that might be obsolete. So the list contains just the current items at the end of action. 

```csharp
public sealed class RadProjectsExtensionPackage : AsyncPackage
{
	private ErrorListProvider ErrorListProvider { get; set; }

	protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
	{
		await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

		ErrorListProvider = new ErrorListProvider(this);
		...
	}

	public void HandleException(Exception exception, string title)
	{
		Output($"ERROR - {exception.Message}");
		Output(exception.StackTrace, true);
		ErrorListAddError(exception.Message);
		MessageBoxErr(exception.Message, title);
	}

	public void ErrorListAddError(string message)
	{
		ErrorListAddTask(message, TaskErrorCategory.Error)
	}

	public void ErrorListAddWarning(string message)
	{
		ErrorListAddTask(message, TaskErrorCategory.Warning);
	}

	public void ErrorListAddMessage(string message)
	{
		ErrorListAddTask(message, TaskErrorCategory.Message);
	}

	public void ErrorListClear()
	{
		ErrorListProvider.Tasks.Clear();
	}

	private void ErrorListAddTask(string message, TaskErrorCategory category)
	{
		ErrorListProvider.Tasks.Add(new ErrorTask
		{
			Category = TaskCategory.User,
			ErrorCategory = category,
			Text = message,
			Document = "RAD Projects Extension",
			CanDelete = true
		});
	}
}
```
### VSIX dialog windows ###
The dialog windows in VSIX are implemented using WPF XAML forms, so `PresentationCore`, `PresentationFramework`, `System.Xaml` and `WindowsBase` need to be added as reference. I use the `BaseDialogWindow` class inheriting from `DialogWindow` as a base for all XAML windows. So `<local:BaseDialogWindow>` is to be used as a root tag in XAML. The backing class (`ChooseTemplateDialogWindow` for example) doesn't ihnerit from `DialogWindow`!

```csharp
public class BaseDialogWindow:DialogWindow
{
	public BaseDialogWindow()
	{
		HasMaximizeButton = false;
		HasMinimizeButton = false;
	}
}

public partial class ChooseTemplateDialogWindow
{
	...
}
```

ChooseTemplateDialogWindow.xaml:
```xml
<local:BaseDialogWindow x:Class="net.adamec.dev.vs.extension.radprojects.ui.ChooseTemplateDialogWindow"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:local="clr-namespace:net.adamec.dev.vs.extension.radprojects.ui"
             mc:Ignorable="d" 
             d:DesignHeight="200" d:DesignWidth="800"
             Width="800" Height="200"
             ResizeMode="NoResize" ShowInTaskbar="False" WindowStyle="None"  Background="{DynamicResource WindowBackground}" WindowStartupLocation="CenterOwner">
    <local:BaseDialogWindow.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="StyleResourceDictionary.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </local:BaseDialogWindow.Resources>
 
  ...

</local:BaseDialogWindow>
```
Actually `App.xaml` for application (package) wide resources doesn't work in VSIX projects, so the resource dictionaries need to be references in page (window) files explicitly. Of course, it possible to use the master resource dictionary that will merge other resource dictionaries.

