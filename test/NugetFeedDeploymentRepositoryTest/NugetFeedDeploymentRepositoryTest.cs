using System;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.NuGet.Storage;
using Xunit;

namespace Etg.Yams.Nuget.Storage.Test
{
    [Trait("Category", "Integration")]
    public class NugetFeedDeploymentRepositoryTest
    {
        [Fact]
        public async Task TestHasBinaries()
        {
            NugetFeedDeploymentRepository _deploymentRepository = new NugetFeedDeploymentRepository();
            Assert.True(await _deploymentRepository.HasApplicationBinaries(new AppIdentity("Xunit", "2.1.0")));
        }

        [Fact]
        public async Task TestHasBinaries_VersionDoesntExist()
        {
            NugetFeedDeploymentRepository _deploymentRepository = new NugetFeedDeploymentRepository();
            Assert.False(await _deploymentRepository.HasApplicationBinaries(new AppIdentity("Xunit", "0.0.0-thisversiondoesntexist")));
        }

        [Fact]
        public async Task TestHasBinaries_PackageDoesntExist()
        {
            NugetFeedDeploymentRepository _deploymentRepository = new NugetFeedDeploymentRepository();
            Assert.False(await _deploymentRepository.HasApplicationBinaries(new AppIdentity(Guid.NewGuid().ToString(), "1.0.0")));
        }
    }
}
