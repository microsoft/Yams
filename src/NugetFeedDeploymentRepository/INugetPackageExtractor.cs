using System.IO;
using System.Threading.Tasks;

namespace Etg.Yams.NuGet.Storage
{
    public interface INugetPackageExtractor
    {
        Task ExtractPackage(Stream inputStream, string outputDirectory);
    }
}
