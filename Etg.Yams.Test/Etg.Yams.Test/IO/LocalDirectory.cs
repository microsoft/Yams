using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.IO;
using Etg.Yams.Utils;

namespace Etg.Yams.Test.IO
{
    public class LocalDirectory : IRemoteDirectory
    {
        private readonly string _path;

        public LocalDirectory(string path)
        {
            _path = path;
        }

        public string Name
        {
            get { return Path.GetFileName(_path); }
        }

        public string Uri
        {
            get { return new System.Uri(_path).AbsoluteUri; }
        }

        public Task<IEnumerable<IRemoteDirectory>> ListDirectories()
        {
            return Task.Run(() =>
            {
                IEnumerable<IRemoteDirectory> directories =
                    Directory.GetDirectories(_path).Select(dirPath => new LocalDirectory(dirPath));
                return directories;
            });
        }

        public Task<IEnumerable<IRemoteFile>> ListFiles()
        {
            return Task.Run(() =>
            {
                IEnumerable<IRemoteFile> files = Directory.GetFiles(_path).Select(filePath => new LocalFile(filePath));
                return files;
            });
        }

        public Task<IRemoteDirectory> GetDirectory(string name)
        {
            return Task.Run(() =>
            {
                IRemoteDirectory directory = new LocalDirectory(Path.Combine(_path, name));
                return directory;
            });
        }

        public Task<IRemoteFile> GetFile(string name)
        {
            return Task.Run(() =>
            {
                IRemoteFile file = new LocalFile(Path.Combine(_path, name));
                return file;
            });
        }

        public Task<bool> Exists()
        {
            return Task.Run(() => Directory.Exists(_path));
        }

        public Task Download(string destPath)
        {
            return FileUtils.CopyDir(_path, destPath, overwrite: true);
        }
    }
}