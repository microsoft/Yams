using System.IO;
using System.Threading.Tasks;

namespace Etg.Yams.Utils
{
    public static class FileUtils
    {
        public static Task CopyDir(string srcPath, string destPath, bool overwrite)
        {
            return Task.Run(() =>
            {
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);
                }
                foreach (string dirPath in Directory.GetDirectories(srcPath, "*",
                    SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(srcPath, destPath));

                foreach (string newPath in Directory.GetFiles(srcPath, "*.*",
                    SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(srcPath, destPath), overwrite);
            });
        }

        public static Task CreateDirectory(string path)
        {
            return Task.Run(() =>
            {
                Directory.CreateDirectory(path);
            });
        }

        public static Task DeleteDirectoryIfAny(string destPath, bool recursive = true)
        {
            return Task.Run(() =>
            {
                if (Directory.Exists(destPath))
                {
                    Directory.Delete(destPath, recursive);
                }
            });
        }
    }
}
