using System;
using System.Collections.Generic;
using System.IO;

namespace LumiereObjectCreator
{
    public static class PathHelpers
    {
        public static int CompareNumberedPath(string s1, string s2)
        {
            int s1start = s1.LastIndexOf('\\');
            if (s1start < 0)
                s1start = 0;
            else
                s1start++;

            int s2start = s2.LastIndexOf('\\');
            if (s2start < 0)
                s2start = 0;
            else
                s2start++;

            return int.Parse(s1.Substring(s1start, s1.IndexOf('.', s1start) - s1start)) - int.Parse(s2.Substring(s2start, s2.IndexOf('.', s2start) - s2start));
        }

        public static List<String> GetSubDirectories(string scanPath)
        {
            string[] source = Directory.GetDirectories(scanPath);
            Array.Sort(source, CompareNumberedPath);
            List<string> sourceList = new List<string>();
            sourceList.Add("Auto");
            foreach (var dir in source)
            {
                sourceList.Add(dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1));
            }
            return sourceList;
        }

        public static string GetRelativePath(string basePath, string filePath)
        {
            if (basePath.Length > 0 && basePath[basePath.Length - 1] != Path.DirectorySeparatorChar)
            {
                basePath += Path.DirectorySeparatorChar;
            }

            var fileUri = new Uri(filePath);
            var baseUri = new Uri(basePath, UriKind.Absolute);
            var relativeUri = baseUri.MakeRelativeUri(fileUri);
            return relativeUri.ToString().Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        public static string GetCorrectlyCasedPath(string path)
        {
            if (!(File.Exists(path) || Directory.Exists(path)))
            {
                return path;
            }

            var di = new DirectoryInfo(path);

            if (di.Parent != null)
            {
                return Path.Combine(
                    GetCorrectlyCasedPath(di.Parent.FullName),
                    di.Parent.GetFileSystemInfos(di.Name)[0].Name);
            }
            else
            {
                return di.Name.ToUpper();
            }
        }

        public static string ToUnixPath(string windowsPath)
        {
            return windowsPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static string ToWindowsPath(string unixPath)
        {
            return unixPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
    }
}