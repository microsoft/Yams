using System;
using System.Collections.Generic;
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
    public class NugetFeedDeploymentRepository : IDeploymentRepository
    {
        private SourceRepository _sourceRepository;

        public NugetFeedDeploymentRepository(string feedUrl = "https://api.nuget.org/v3/index.json")
        {
            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            PackageSource packageSource = new PackageSource(feedUrl);
            _sourceRepository = new SourceRepository(packageSource, providers);
        }

        public Task DeleteApplicationBinaries(AppIdentity appIdentity)
        {
            throw new NotSupportedException("NuGet feeds are immutable.");
        }

        public async Task DownloadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode)
        {
            DownloadResource downloadResource = await _sourceRepository.GetResourceAsync<DownloadResource>();
            PackageIdentity packageIdentity = new PackageIdentity(appIdentity.Id, new NuGetVersion(appIdentity.Version.ToString()));
            var result = await downloadResource.GetDownloadResourceResultAsync(packageIdentity, new PackageDownloadContext(new SourceCacheContext()), null, new Logger(), CancellationToken.None);
        }

        public async Task<bool> HasApplicationBinaries(AppIdentity appIdentity)
        {
            PackageMetadataResource packageMetadataResource = await _sourceRepository.GetResourceAsync<PackageMetadataResource>();
            PackageIdentity packageIdentity = new PackageIdentity(appIdentity.Id, new NuGetVersion(appIdentity.Version.ToString()));
            IPackageSearchMetadata searchMetadata = await packageMetadataResource.GetMetadataAsync(packageIdentity, new SourceCacheContext(), new Logger(), CancellationToken.None);

            return searchMetadata?.IsListed ?? false;
        }

        public Task UploadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode)
        {
            throw new NotSupportedException("Yams doesn't support publishing NuGet packages. Publish your packages from your build server.");
        }
    }
}
