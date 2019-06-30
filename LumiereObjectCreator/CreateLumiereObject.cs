using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
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
        static private string[] KnownObjectTypeFolders =
        {
            "Unknown",
            "Module-Bin",
            "Module-Data",
            "Module-Extern",
            "Module-Lib",
            "Module-Plugin",
            "Module-Ref",
            "Module-Test",
            "LocalHeader",
            "Header",
            "Source",
            "LocalClass",
            "Class"
        };

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
        public static CreateLumiereObject Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider {
            get {
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
            string templateFolderPath = GetTemplateLocation(path, options.TemplateLocation);
            if (!string.IsNullOrEmpty(path))
            {
                var input = new Input(templateFolderPath);
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
            var dte2 = (EnvDTE80.DTE2)this.package._DTE;
            var outputPane = GetOutputWindow(dte2.ToolWindows.OutputWindow.OutputWindowPanes);

            string rootPath = GetPlacementLocation(path);
            objectType = DetermineType(path, objectType, objectName);
            string sanName = SantizeName(objectName);
            string frameworkName;
            string moduleName;
            GetFrameworkAndModule(path, out frameworkName, out moduleName);
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "python.exe";
            start.Arguments = string.Format("{0} " +
                "--name={1} " +
                "--type={2} " +
                "--author=\"{3}\" " +
                "--email=\"{4}\" " +
                "--templates={5} " +
                "--framework=\"{6}\" " +
                "--module=\"{7}\" " +
                "--file={8} " +
                "--rules={9} " +
                "--destroot={10}",
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
            outputPane.OutputString(start.Arguments);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            IServiceProvider service = this.package as IServiceProvider;

            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    outputPane.OutputString(result);
                }
            }
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

        private OutputWindowPane GetOutputWindow(OutputWindowPanes outputWindowPanes)
        {
            for (uint i = 1; i <= outputWindowPanes.Count; i++)
            {
                if (outputWindowPanes.Item(i).Name.Equals("Lumiere", StringComparison.CurrentCultureIgnoreCase))
                {
                    return outputWindowPanes.Item(i);
                }
            }
            return outputWindowPanes.Add("Lumiere");
        }

        private void GetFrameworkAndModule(string path, out string frameworkName, out string moduleName)
        {
            frameworkName = "";
            moduleName = "";
            string search = "Frameworks\\";
            int index = path.IndexOf(search);
            if (index > 0)
            {
                string code = path.Substring(index + search.Length);
                index = code.IndexOf('\\');
                if (index > 0)
                {
                    frameworkName = code.Substring(0, index);
                    code = code.Substring(index + 1);
                    index = code.IndexOf('\\');
                    if (index > 0)
                    {
                        moduleName = code.Substring(0, index);
                    }
                    else
                        moduleName = code;
                }
            }
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
            return sanitized;
        }

        private string DetermineType(string path, string objectType, string objectName)
        {
            if (objectType == "Auto")
            {
                if (path.Contains("\\src"))
                    return "Source";
                if (path.Contains("\\local_include"))
                    return "LocalHeader";
                if (objectName.EndsWith(".cpp"))
                    return "Source";
                else if (objectName.EndsWith(".cxx"))
                    return "Source";
                else if (objectName.EndsWith(".h"))
                    return "Header";
                else if (objectName.EndsWith(".hpp"))
                    return "Header";
                else if (objectName.EndsWith(".hxx"))
                    return "Header";
                else if (objectName.StartsWith("l:"))
                    return "LocalClass";
                else if (objectName.StartsWith("mb:"))
                    return "Module-Bin";
                else if (objectName.StartsWith("ml:"))
                    return "Module-Lib";
                else if (objectName.StartsWith("md:"))
                    return "Module-Data";
                else if (objectName.StartsWith("me:"))
                    return "Module-Extern";
                else if (objectName.StartsWith("mp:"))
                    return "Module-Plugin";
                else if (objectName.StartsWith("mr:"))
                    return "Module-Ref";
                else if (objectName.StartsWith("mt:"))
                    return "Module-Test";
                else
                    return "Class";
            }
            else
                return objectType;
        }
    }
}