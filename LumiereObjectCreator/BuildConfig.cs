using EnvDTE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LumiereObjectCreator
{
    internal class BuildConfig
    {
        public string Target = "";
        public string Where = "";
        public string Name = "";

        public BuildConfig(string name, string target, string where)
        {
            Name = name;
            Target = target;
            Where = where;
        }

        public void Build(OutputWindowPane outputPane)
        {
			ProcessStartInfo start = new ProcessStartInfo();
			start.FileName = "cmake.exe";
			start.Arguments = "--build .\\ --config " + Name + " --target " + Target;
			start.UseShellExecute = false;
			start.RedirectStandardOutput = true;
			start.RedirectStandardError = true;
			start.WorkingDirectory = Where;
			
			outputPane.OutputString("[INFO] Running : cmake " + start.Arguments + "\r\n\n");
			outputPane.OutputString("Building from here : " + this.Where);
			try
			{
				using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(start))
				{
					using (StreamReader reader = process.StandardOutput)
					{
						string result = reader.ReadToEnd();
						outputPane.OutputString(result);
					}
					using (StreamReader reader = process.StandardError)
					{
						string result = reader.ReadToEnd();
						outputPane.OutputString("[ERROR] " + result);
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
