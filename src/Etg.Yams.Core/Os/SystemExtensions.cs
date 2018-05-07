using System;
using System.Collections.Generic;
using System.Linq;

namespace Etg.Yams.Os
{
    public static class SystemExtensions
    {
        public static string GetPathEnvironmentVariable(this ISystem system)
        {
            var processPath = system.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            var machinePath = system.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);

            // If initialization installed something (such as nodejs) that modifies the machine path, the path wouldn't be found 
            // on the first run after imaging. Therefore we merge the current machine path into the process path. This only works 
            // with UseShellExecute = false, UseShellExecute = true won't pass the process environment and creates a new one.
            return MergePath(processPath, machinePath);
        }

        private static IEnumerable<string> SplitPath(string path)
        {
            return (path ?? string.Empty).Split(';').Where(entry => !string.IsNullOrWhiteSpace(entry));
        }

        private static string MergePath(string processPath, string machinePath)
        {
            var splitProcessPath = SplitPath(processPath);
            var splitMachinePath = SplitPath(machinePath);
            var mergedPath = splitProcessPath.Union(splitMachinePath);
            return string.Join(";", mergedPath) + ";";
        }
    }
}
