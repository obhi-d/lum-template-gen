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
  internal sealed class CleanLumiereObject
  {
    static private List<string> templateTypes = null;

    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x0300;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid("f61c6a20-962e-4ff4-8547-480162ef662f");

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly LumiereObjectCreatorPackage package;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanLumiereObject"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    /// <param name="commandService">Command service to add command to, not null.</param>
    private CleanLumiereObject(LumiereObjectCreatorPackage package, OleMenuCommandService commandService, Options options)
    {
      this.package = package ?? throw new ArgumentNullException(nameof(package));
      commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

      var menuCommandID = new CommandID(CommandSet, CommandId);
      var menuItem = new MenuCommand(this.Execute, menuCommandID);
      commandService.AddCommand(menuItem);
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static CleanLumiereObject Instance
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
      // Switch to the main thread - the call to AddCommand in CleanLumiereObject's constructor requires
      // the UI thread.
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
      var options = (Options)package.GetDialogPage(typeof(Options));
      OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
      Instance = new CleanLumiereObject(package, commandService, options);
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
      var dte2 = (EnvDTE80.DTE2)this.package._DTE;
      var outputPane = ProjectHelpers.GetOutputWindow(dte2.ToolWindows.OutputWindow.OutputWindowPanes);
      string path = ProjectHelpers.GetSelectedFilePath(package);
      var cfg = PathHelpers.GetSourcePath(path);

      ProcessStartInfo start = new ProcessStartInfo();
      start.FileName = cfg + "\\building\\nsbuild.exe";
      start.Arguments = "--source " + cfg + "\\source";
      outputPane.OutputString("[INFO] nsbuild: " + start.Arguments + "\r\n\n");
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
            outputPane.OutputString(result);
          }
        }
      }
      catch (Exception ex)
      {
        outputPane.OutputString("[ERROR] Exception : " + ex.Message);
      }
    }

  }
}