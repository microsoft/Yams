using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Etg.Yams.NuGet.Storage
{
    public class NugetFeedApplicationRepository : IApplicationRepository
    {
        private readonly SourceRepository _sourceRepository;

        public NugetFeedApplicationRepository(string feedUrl = "https://api.nuget.org/v3/index.json", NugetFeedCredentials credentials = null)
        {
            this.FeedUrl = feedUrl;

            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            PackageSource packageSource = new PackageSource(feedUrl);

            if(credentials != null)
                packageSource.Credentials = new PackageSourceCredential("Yams Host", credentials.Username, credentials.Password, true);

            _sourceRepository = new SourceRepository(packageSource, providers);
        }

        public string FeedUrl { get; private set; }

        public Task DeleteApplicationBinaries(AppIdentity appIdentity)
        {
            throw new NotSupportedException("NuGet feeds are immutable.");
        }

        public async Task DownloadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode)
        {
            string tempPath = Path.GetTempPath();
            DownloadResource downloadResource = await _sourceRepository.GetResourceAsync<DownloadResource>();
            PackageIdentity packageIdentity = new PackageIdentity(appIdentity.Id, new NuGetVersion(appIdentity.Version.ToString()));
            PackageDownloadContext downloadContext = new PackageDownloadContext(new SourceCacheContext(), tempPath, true);

            var result = await downloadResource.GetDownloadResourceResultAsync(packageIdentity, downloadContext, tempPath, new TraceLogger(), CancellationToken.None);

            if (result.Status != DownloadResourceResultStatus.Available)
                throw new BinariesNotFoundException($"NuGet package for application {appIdentity} is not available from feed {this.FeedUrl}");

            NugetPackageExtractor extractor = new NugetPackageExtractor();
            await extractor.ExtractPackage(result.PackageStream, localPath);
        }

        public async Task<bool> HasApplicationBinaries(AppIdentity appIdentity)
        {
            PackageMetadataResource packageMetadataResource = await _sourceRepository.GetResourceAsync<PackageMetadataResource>();
            PackageIdentity packageIdentity = new PackageIdentity(appIdentity.Id, new NuGetVersion(appIdentity.Version.ToString()));
            IPackageSearchMetadata searchMetadata = await packageMetadataResource.GetMetadataAsync(packageIdentity, new SourceCacheContext(), new TraceLogger(), CancellationToken.None);

            return searchMetadata?.IsListed ?? false;
        }

        public Task UploadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode)
        {
            throw new NotSupportedException("Yams doesn't support publishing NuGet packages. Publish your packages from your build server.");
        }
    }
}
