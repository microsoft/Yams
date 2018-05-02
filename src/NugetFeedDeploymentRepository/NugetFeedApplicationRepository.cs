using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage;
using Etg.Yams.Utils;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Etg.Yams.NuGet.Storage
{
    public class NugetFeedApplicationRepository : IApplicationRepository
    {
        private const string NugetOrgFeedUrl = "https://api.nuget.org/v3/index.json";
        private readonly INugetPackageExtractor _packageExtractor;
        private readonly SourceRepository _sourceRepository;
        private readonly ILogger _logger = new TraceLogger();

        public NugetFeedApplicationRepository(INugetPackageExtractor packageExtractor, string feedUrl = NugetOrgFeedUrl, NugetFeedCredentials credentials = null)
        {
            this.FeedUrl = feedUrl;
            _packageExtractor = packageExtractor;

            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            PackageSource packageSource = new PackageSource(feedUrl);

            if (credentials != null)
            {
                packageSource.Credentials = new PackageSourceCredential("Yams Host", credentials.Username, credentials.Password, true);
            }

            _sourceRepository = new SourceRepository(packageSource, providers);
        }

        public string FeedUrl { get; }

        public Task DeleteApplicationBinaries(AppIdentity appIdentity)
        {
            throw new NotSupportedException("NuGet feeds are immutable.");
        }

        public async Task DownloadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode)
        {
            bool exists = !FileUtils.DirectoryDoesntExistOrEmpty(localPath);
            if (exists)
            {
                if (conflictResolutionMode == ConflictResolutionMode.DoNothingIfBinariesExist)
                {
                    return;
                }
                if (conflictResolutionMode == ConflictResolutionMode.FailIfBinariesExist)
                {
                    throw new DuplicateBinariesException(
                        $"Cannot download the binaries because the destination directory {localPath} contains files");
                }
            }

            string tempPath = Path.GetTempPath();
            DownloadResource downloadResource = await _sourceRepository.GetResourceAsync<DownloadResource>();
            PackageIdentity packageIdentity = new PackageIdentity(appIdentity.Id, new NuGetVersion(appIdentity.Version.ToString()));
            PackageDownloadContext downloadContext = new PackageDownloadContext(new SourceCacheContext(), tempPath, directDownload: true);

            DownloadResourceResult result = await downloadResource.GetDownloadResourceResultAsync(packageIdentity, downloadContext, tempPath, _logger, CancellationToken.None);

            if (result.Status != DownloadResourceResultStatus.Available)
            {
                throw new BinariesNotFoundException($"NuGet package for application {appIdentity} is not available from feed {this.FeedUrl}");
            }

            await _packageExtractor.ExtractPackage(result.PackageStream, localPath);
        }

        public async Task<bool> HasApplicationBinaries(AppIdentity appIdentity)
        {
            PackageMetadataResource packageMetadataResource = await _sourceRepository.GetResourceAsync<PackageMetadataResource>();
            PackageIdentity packageIdentity = new PackageIdentity(appIdentity.Id, new NuGetVersion(appIdentity.Version.ToString()));
            IPackageSearchMetadata searchMetadata = await packageMetadataResource.GetMetadataAsync(packageIdentity, new SourceCacheContext(), _logger, CancellationToken.None);

            return searchMetadata?.IsListed ?? false;
        }

        public Task UploadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode)
        {
            throw new NotSupportedException("Yams doesn't support publishing NuGet packages. Publish your packages from your build server.");
        }
    }
}
