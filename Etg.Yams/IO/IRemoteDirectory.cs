using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etg.Yams.IO
{
    public interface IRemoteDirectory
    {
        string Name { get; }

        string Uri { get; }

        Task<IEnumerable<IRemoteDirectory>> ListDirectories();

        Task<IEnumerable<IRemoteFile>> ListFiles();

        Task<IRemoteDirectory> GetDirectory(string name);

        Task<IRemoteFile> GetFile(string name);

        Task<bool> Exists();

        Task Download(string destPath);
    }
}
