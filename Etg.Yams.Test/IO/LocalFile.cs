using System.IO;
using System.Threading.Tasks;
using Etg.Yams.IO;

namespace Etg.Yams.Test.IO
{
    public class LocalFile : IRemoteFile
    {
        private readonly string _path;

        public LocalFile(string path)
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

        public Task<string> DownloadText()
        {
            return Task.FromResult(File.ReadAllText(_path));
        }

        public Task Download(string destPath)
        {
            return Task.Run(() => File.Copy(_path, destPath));
        }

        public Task<bool> Exists()
        {
            return Task.Run(() => File.Exists(_path));
        }
    }
}