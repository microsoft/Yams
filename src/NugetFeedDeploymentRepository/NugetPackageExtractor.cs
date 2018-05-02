using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.PackageExtraction;

namespace Etg.Yams.NuGet.Storage
{
    public class NugetPackageExtractor : INugetPackageExtractor
    {
        public async Task ExtractPackage(Stream fileStream, string outputDirectory)
        {
            var packagePathResolver = new FlatPackagePathResolver(outputDirectory);

            await PackageExtractor.ExtractPackageAsync(
                fileStream,
                packagePathResolver,
                new PackageExtractionContext(
                    PackageSaveMode.Files,
                    PackageExtractionBehavior.XmlDocFileSaveMode,
                    new TraceLogger(),
                    signedPackageVerifier: null),
                CancellationToken.None);
        }
    }

    /// <summary>
    /// Extracts package directly to root directory.
    /// NuGet PackagePathResolver always creates a subdirectory for the package.
    /// </summary>
    class FlatPackagePathResolver : PackagePathResolver
    {
        public FlatPackagePathResolver(string rootDirectory)
            : base(rootDirectory, useSideBySidePaths: false)
        {
        }

        public override string GetPackageDirectoryName(PackageIdentity packageIdentity)
        {
            // We don't create a directory for the package content but extract it directly to root directory.
            return string.Empty;
        }

        public override string GetPackageFileName(PackageIdentity packageIdentity)
        {
            return PackagingCoreConstants.NupkgExtension;
        }

        public new string GetPackageDownloadMarkerFileName(PackageIdentity packageIdentity)
        {
            return PackagingCoreConstants.PackageDownloadMarkerFileExtension;
        }

        public new string GetManifestFileName(PackageIdentity packageIdentity)
        {
            return PackagingCoreConstants.NuspecExtension;
        }

        public override string GetInstallPath(PackageIdentity packageIdentity)
        {
            return Path.Combine(this.Root, GetPackageDirectoryName(packageIdentity));
        }

        public override string GetInstalledPath(PackageIdentity packageIdentity)
        {
            var installedPackageFilePath = GetInstalledPackageFilePath(packageIdentity);

            return string.IsNullOrEmpty(installedPackageFilePath) ? null : Path.GetDirectoryName(installedPackageFilePath);
        }

        public override string GetInstalledPackageFilePath(PackageIdentity packageIdentity)
        {
            return PackagePathHelper.GetInstalledPackageFilePath(packageIdentity, this);
        }
    }
}
