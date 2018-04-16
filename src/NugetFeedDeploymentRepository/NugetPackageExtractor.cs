using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.PackageExtraction;

namespace Etg.Yams.NuGet.Storage
{
    public class NugetPackageExtractor
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
    /// </summary>
    class FlatPackagePathResolver : PackagePathResolver
    {
        public FlatPackagePathResolver(string rootDirectory)
            : base(rootDirectory, false)
        {
        }

        public override string GetPackageDirectoryName(PackageIdentity packageIdentity)
        {
            return string.Empty;
        }

        public override string GetPackageFileName(PackageIdentity packageIdentity)
        {
            return PackagingCoreConstants.NupkgExtension;
        }

        public new string GetPackageDownloadMarkerFileName(PackageIdentity packageIdentity)
        {
            var builder = new StringBuilder();

            builder.Append(GetId(packageIdentity));
            builder.Append(PackagingCoreConstants.PackageDownloadMarkerFileExtension);

            return builder.ToString();
        }

        public new string GetManifestFileName(PackageIdentity packageIdentity)
        {
            return GetId(packageIdentity) + PackagingCoreConstants.NuspecExtension;
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

        private string GetId(PackageIdentity identity)
        {
            return string.Empty;
        }
    }
}
