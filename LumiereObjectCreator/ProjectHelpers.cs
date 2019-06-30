using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

using System.Runtime.InteropServices;

namespace LumiereObjectCreator
{
    internal static class ProjectHelpers
    {
        public static string GetSelectedFilePath(IServiceProvider serviceProvider)
        {
            IVsMonitorSelection monitorSelection = serviceProvider.GetService<SVsShellMonitorSelection, IVsMonitorSelection>();

            monitorSelection.GetCurrentSelection(out IntPtr hierarchyPtr, out uint itemId, out _, out _);
            if (hierarchyPtr != IntPtr.Zero)
            {
                IVsHierarchy hierarchy = (IVsHierarchy)Marshal.GetUniqueObjectForIUnknown(hierarchyPtr);

                hierarchy.GetCanonicalName(itemId, out string filePath);
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    // The canonical name might not match the case of the file on disk
                    return PathHelpers.GetCorrectlyCasedPath(filePath);
                }
            }

            return null;
        }
    }
}