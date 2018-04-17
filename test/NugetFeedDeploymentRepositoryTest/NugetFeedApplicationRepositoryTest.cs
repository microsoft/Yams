using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.NuGet.Storage;
using Etg.Yams.Storage;
using Etg.Yams.Utils;
using Xunit;

namespace Etg.Yams.Nuget.Storage.Test
{
    public class NugetFeedDeploymentRepositoryTest
    {
        [Fact]
        public async Task TestHasBinaries()
        {
            NugetFeedApplicationRepository _applicationRepository = new NugetFeedApplicationRepository();
            Assert.True(await _applicationRepository.HasApplicationBinaries(new AppIdentity("jQuery", "3.3.1")));
        }

        [Fact]
        public async Task TestHasBinaries_VersionDoesntExist()
        {
            NugetFeedApplicationRepository _applicationRepository = new NugetFeedApplicationRepository();
            Assert.False(await _applicationRepository.HasApplicationBinaries(new AppIdentity("jQuery", "0.0.0-thisversiondoesntexist")));
        }

        [Fact]
        public async Task TestHasBinaries_PackageDoesntExist()
        {
            NugetFeedApplicationRepository _applicationRepository = new NugetFeedApplicationRepository();
            Assert.False(await _applicationRepository.HasApplicationBinaries(new AppIdentity(Guid.NewGuid().ToString(), "1.0.0")));
        }

        [Fact]
        public async Task TestDownloadApplicationBinaries()
        {
            string localPath = await CreateTestTempDirectory("Nuget_TestDownloadApplicationBinaries");
            NugetFeedApplicationRepository _applicationRepository = new NugetFeedApplicationRepository();
            await _applicationRepository.DownloadApplicationBinaries(new AppIdentity("jQuery", "3.3.1"), localPath, ConflictResolutionMode.OverwriteExistingBinaries);

            var files = FileUtils.ListFilesRecursively(localPath);

            Assert.Contains(Path.Combine(localPath, "Content", "Scripts", "jquery-3.3.1.min.js"), files);
        }

        [Fact]
        public async Task TestDownloadApplicationBinaries_ThrowsWhenPackageNotFound()
        {
            AppIdentity appIdentity = new AppIdentity(Guid.NewGuid().ToString(), "1.0.0");
            string localPath = await CreateTestTempDirectory("Nuget_TestDownloadApplicationBinaries");
            NugetFeedApplicationRepository _applicationRepository = new NugetFeedApplicationRepository();

            BinariesNotFoundException exception = await Assert.ThrowsAsync<BinariesNotFoundException>(() => _applicationRepository.DownloadApplicationBinaries(appIdentity, localPath, ConflictResolutionMode.OverwriteExistingBinaries));
            Assert.Contains(appIdentity.ToString(), exception.Message);
            Assert.Contains(_applicationRepository.FeedUrl, exception.Message);
        }

        private static async Task<string> CreateTestTempDirectory(string testName)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), testName);
            await FileUtils.DeleteDirectoryIfAny(tempPath);
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }
    }
}
