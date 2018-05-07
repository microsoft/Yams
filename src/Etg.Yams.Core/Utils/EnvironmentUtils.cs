using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var missingPath = splitMachinePath.Except(splitProcessPath);
            var mergedPath = splitProcessPath.Union(missingPath);
            return string.Join(";", mergedPath) + ";";
        }
    }
}
