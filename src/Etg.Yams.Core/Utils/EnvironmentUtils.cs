using System;
using System.Collections.Generic;
using System.Linq;

namespace Etg.Yams.Utils
{
    public static class EnvironmentUtils
    {
        public static string GetPath(EnvironmentVariableTarget target)
        {
            return Environment.ExpandEnvironmentVariables(Environment.GetEnvironmentVariable("PATH", target));
        }

        public static IEnumerable<string> SplitPath(string path)
        {
            return (path ?? string.Empty).Split(';').Where(entry => !string.IsNullOrEmpty(entry));
        }

        public static string MergePath(string processPath, string machinePath)
        {
            var splitProcessPath = SplitPath(processPath);
            var splitMachinePath = SplitPath(machinePath);
            var mergedPath = splitProcessPath.Union(splitMachinePath);
            return string.Join(";", mergedPath) + ";";
        }
    }
}
