using System.Threading.Tasks;

namespace Etg.Yams.IO
{
    public interface IRemoteFile
    {
        string Name { get; }

        string Uri { get; }

        Task<string> DownloadText();

        Task Download(string destPath);

        Task<bool> Exists();
    }
}