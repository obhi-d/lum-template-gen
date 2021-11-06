using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using Task = System.Threading.Tasks.Task;

namespace LumiereObjectCreator
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class CreateLumiereObject
	{
		static private List<string> templateTypes = null;

		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("f61c6a20-962e-4ff4-8547-480162ef662f");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly LumiereObjectCreatorPackage package;

		private Options options;
		private string kTemplateName_Source;
		private string kTemplateName_LocalHeader;
		private string kTemplateName_Header;

		static public string kScript = Path.Combine("Scripts", "build_system", "build_utils", "from_template.py");
		static public string kScriptRoot = "Scripts";

		/// <summary>
		/// Initializes a new instance of the <see cref="CreateLumiereObject"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private CreateLumiereObject(LumiereObjectCreatorPackage package, OleMenuCommandService commandService, Options options)
		{
			this.options = options;
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new MenuCommand(this.Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static CreateLumiereObject Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the service provider from the owner package.
		/// </summary>
		private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
		{
			get
			{
				return this.package;
			}
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static async Task InitializeAsync(LumiereObjectCreatorPackage package)
		{
			// Switch to the main thread - the call to AddCommand in CreateLumiereObject's constructor requires
			// the UI thread.
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
			var options = (Options)package.GetDialogPage(typeof(Options));
			OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
			Instance = new CreateLumiereObject(package, commandService, options);
		}

		private void GenerateEnum(string path)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			int index = path.IndexOf("Frameworks");
			if (index > 0)
			{
				string source = path.Substring(0, index);
				string script = Path.Combine(source, kScriptRoot);

				var dte2 = (EnvDTE80.DTE2)this.package._DTE;
				var outputPane = ProjectHelpers.GetOutputWindow(dte2.ToolWindows.OutputWindow.OutputWindowPanes);

				ProcessStartInfo start = new ProcessStartInfo();
				start.FileName = "python.exe";
				start.Arguments = "-m build_system.build_utils.enums --auto " + path;
				start.UseShellExecute = false;
				start.RedirectStandardOutput = true;
				start.WorkingDirectory = script;

				outputPane.OutputString("[INFO] Python: " + start.Arguments + "\r\n\n");
				IServiceProvider service = this.package as IServiceProvider;

				try
				{
					using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(start))
					{
						using (StreamReader reader = process.StandardOutput)
						{
							string result = reader.ReadToEnd();
							outputPane.OutputString(result);
						}
					}
				}
				catch(Exception ex)
				{
					outputPane.OutputString("[ERROR] Exception : " + ex.Message);
				}
			}
		}

		private void CreateNewFiles(string path)
		{
			string templateFolderPath = GetTemplateLocation(path, options.TemplateLocation);
			if (!string.IsNullOrEmpty(path))
			{
				if (templateTypes == null)
				{
					templateTypes = PathHelpers.GetSubDirectories(templateFolderPath);
					for (int i = 0; i < templateTypes.Count; ++i)
					{
						if (templateTypes[i].Contains("Source("))
							kTemplateName_Source = templateTypes[i];
						else if (templateTypes[i].Contains("LocalHeader("))
							kTemplateName_LocalHeader = templateTypes[i];
						else if (templateTypes[i].Contains("Header("))
							kTemplateName_Header = templateTypes[i];
					}
				}
				if (templateTypes.Count <= 0)
				{
					System.Windows.Forms.MessageBox.Show("No templates found!");
					return;
				}
				var input = new Input(templateTypes);
				input.ShowModal();
				string objectType = null;
				string objectName = null;
				if (input.GetResult(out objectType, out objectName))
				{
					ExecuteOnObject(path, templateFolderPath, objectType, objectName);
				}
				//RunOnItem(path, line);
			}
			else
			{
				System.Windows.Forms.MessageBox.Show("Couldn't resolve the folder");
			}

		}
		/// <summary>
		/// This function is the callback used to execute the command when the menu item is clicked.
		/// See the constructor to see how the menu item is associated with this function using
		/// OleMenuCommandService service and MenuCommand class.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void Execute(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Show a message box to prove we were here

			string path = ProjectHelpers.GetSelectedFilePath(this.package as IServiceProvider);
			if (path.EndsWith("Enums.json"))
			{
				GenerateEnum(path);
			}
			else
			{
				CreateNewFiles(path);
			}
		}

		private string GetTemplateLocation(string path, string templateLocation)
		{
			if (System.IO.Path.IsPathRooted(templateLocation))
				return templateLocation;

			int index = path.IndexOf("Frameworks");
			if (index > 0)
			{
				return path.Substring(0, index) + templateLocation;
			}
			return "";
		}

		private string GetScriptsLocation(string path)
		{
			int index = path.IndexOf("Frameworks");
			if (index > 0)
			{
				return path.Substring(0, index) + "Scripts";
			}
			return "";
		}

		private string GetRulesFile(string path, string namespaceRuleLoc)
		{
			if (System.IO.Path.IsPathRooted(namespaceRuleLoc))
				return namespaceRuleLoc;
			int index = path.IndexOf("Frameworks");
			if (index > 0)
			{
				return path.Substring(0, index) + "template-rules.json";
			}
			return "";
		}

		private void ExecuteOnObject(string path, string templatePath, string objectType, string objectName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var dte2 = (EnvDTE80.DTE2)this.package._DTE;
			var outputPane = ProjectHelpers.GetOutputWindow(dte2.ToolWindows.OutputWindow.OutputWindowPanes);

			string rootPath = GetPlacementLocation(path);
			objectType = DetermineType(path, objectType, objectName);
			string sanName = SantizeName(objectName);
			string frameworkName;
			string moduleName;
			PathHelpers.GetFrameworkAndModule(path, out frameworkName, out moduleName);
			if (objectType.Contains("Module"))
				moduleName = sanName;
			ProcessStartInfo start = new ProcessStartInfo();
			start.FileName = "python.exe";
			start.Arguments = string.Format("{0} " +
				"--name={1} " +
				"--type=\"{2}\" " +
				"--author=\"{3}\" " +
				"--email=\"{4}\" " +
				"--templates=\"{5}\" " +
				"--framework=\"{6}\" " +
				"--module=\"{7}\" " +
				"--file=\"{8}\" " +
				"--rules=\"{9}\" " +
				"--destroot=\"{10}\"",
				GetScriptsLocation(path) + "\\build_system\\build_utils\\from_template.py",
				sanName,
				objectType,
				options.Author,
				options.Email,
				templatePath,
				frameworkName,
				moduleName,
				objectName,
				GetRulesFile(path, options.RulesLocation),
				rootPath
				);
			outputPane.OutputString("[INFO] Python: " + start.Arguments + "\r\n\n");
			start.UseShellExecute = false;
			start.RedirectStandardOutput = true;
			IServiceProvider service = this.package as IServiceProvider;

			try
			{

				using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(start))
				{
					using (StreamReader reader = process.StandardOutput)
					{
						string result = reader.ReadToEnd();
						List<string> files = ParseFilesCreated(result);
						if (files != null && files.Count > 0)
						{
							OpenFiles(dte2, files);
						}
						outputPane.OutputString(result);
					}
				}
			}
			catch (Exception ex)
			{
				outputPane.OutputString("[ERROR] Exception : " + ex.Message);
			}
		}

		private void OpenFiles(EnvDTE80.DTE2 dte2, List<string> files)
		{
			foreach (var file in files)
				dte2.ExecuteCommand("File.OpenFile", file);
		}

		private List<string> ParseFilesCreated(string result)
		{
			List<string> list = new List<string>();
			foreach (var line in result.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
			{
				if (line.StartsWith("[FILE] "))
					list.Add(line.Substring(7));
			}
			return list;
		}

		private string GetPlacementLocation(string path)
		{
			string search = "Frameworks\\";
			int index = path.IndexOf(search);
			if (index > 0)
			{
				index = path.IndexOf('\\', index + search.Length);
				if (index > 0)
				{
					int module = path.IndexOf('\\', index + 1);
					if (module > 0)
					{
						return path.Substring(0, module);
					}
					else if (index + 1 < path.Length)
						return path;
					else
						return path.Substring(0, index);
				}
			}
			return path;
		}
				

		private string SantizeName(string objectName)
		{
			string sanitized = objectName;
			int index = sanitized.IndexOf(':');
			if (index >= 0)
			{
				sanitized = sanitized.Substring(index + 1);
			}
			index = sanitized.IndexOf('.');
			if (index >= 0)
			{
				sanitized = sanitized.Substring(0, index);
			}
			return sanitized.Trim();
		}

		private string DetermineType(string path, string objectType, string objectName)
		{
			if (objectType == "Auto")
			{
				int index = objectName.IndexOf(':');
				if (index > 0)
				{
					// guess by header
					for (int i = 0; i < templateTypes.Count; ++i)
					{
						string header = (i + 1).ToString() + ':';
						if (objectName.StartsWith(header))
							return templateTypes[i];
						int open = templateTypes[i].LastIndexOf('(');
						int close = templateTypes[i].LastIndexOf(')');
						if (open > 0 && close > 0)
						{
							header = templateTypes[i].Substring(open + 1, close - (open + 1)) + ':';
							if (objectName.StartsWith(header))
								return templateTypes[i];
						}
					}
				}
				if (path.Contains("\\src") || objectName.EndsWith(".cpp") ||
					objectName.EndsWith(".cxx"))
					return kTemplateName_Source;
				if (path.Contains("\\local_include"))
					return kTemplateName_LocalHeader;
				if (path.Contains("\\include") ||
					objectName.EndsWith(".h") ||
					objectName.EndsWith(".hpp") ||
					objectName.EndsWith(".hxx"))
					return kTemplateName_Header;
				return "Class";
			}
			else
				return objectType;
		}
	}
}