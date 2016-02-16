using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            return Task.Run(() => { Directory.CreateDirectory(path); });
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

        public static IEnumerable<string> ListFilesRecursively(string dirPath)
        {
            var dirQueue = new Queue<string>();
            dirQueue.Enqueue(dirPath);

            while (dirQueue.Count > 0)
            {
                string currentFolder = dirQueue.Dequeue();
                foreach (string subFolder in Directory.GetDirectories(currentFolder))
                {
                    dirQueue.Enqueue(subFolder);
                }

                foreach (string filePath in Directory.GetFiles(currentFolder))
                {
                    yield return filePath;
                }
            }
        }

        public static string GetRelativePath(string dirPath, string filePath)
        {
            string path = filePath.Remove(0, dirPath.Length);
            if (path.StartsWith("\\"))
            {
                path = path.Remove(0, 1);
            }
            return path;
        }

        public static bool DirectoryDoesntExistOrEmpty(string path)
        {
            return !Directory.Exists(path) || !ListFilesRecursively(path).Any();
        }
    }
}