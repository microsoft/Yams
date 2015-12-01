using System.IO;
using System.Threading;
using Etg.Yams.Application;
using Etg.Yams.Utils;

namespace Etg.Yams.Test.Utils
{
    public static class TestUtils
    {
        public static string GetTestApplicationOutput(string applicationRootPath, AppIdentity appIdentity)
        {
            string processOutputPath = Path.Combine(Path.Combine(applicationRootPath,ApplicationUtils.GetApplicationRelativePath(appIdentity)), "TestProcess.exe.out");

            int maxRetry = 10;
            while (maxRetry-- > 0)
            {
                Thread.Sleep(100);
                if (File.Exists(processOutputPath))
                {
                    break;
                }
            }

            using (StreamReader reader = new StreamReader(processOutputPath))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetTestExesDirPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "Data", "Exes");
        }

        public static void CopyExe(string exeName, string destPath)
        {
            File.Copy(Path.Combine(GetTestExesDirPath(), exeName), Path.Combine(destPath, exeName), overwrite: true);
        }
    }
}
