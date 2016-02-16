using System;
using System.Threading.Tasks;
using Etg.Yams.Azure.Lease;
using Etg.Yams.Azure.UpdateSession;
using Etg.Yams.AzureTestUtils.Fixtures;
using Moq;
using Xunit;

namespace Etg.Yams.Azure.Test.UpdateSession
{
    public class BlobBasedUpdateSessionTest
    {
        [Fact]
        public async Task TestThatStartUpdateSessionThrowsIfBlobCannotBeLocked()
        {

            var updateBlobFactoryMock = new Mock<IUpdateBlobFactory>();
            updateBlobFactoryMock.Setup(factory => factory.TryLockUpdateBlob("app1"))
                .ThrowsAsync(new UpdateBlobUnavailableException());
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(updateBlobFactoryMock.Object, "instanceId1", "1");
            await Assert.ThrowsAsync<UpdateBlobUnavailableException>(async () => await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatEndUpdateSessionThrowsIfBlobCannotBeLocked()
        {
            var updateBlobFactoryMock = new Mock<IUpdateBlobFactory>();
            updateBlobFactoryMock.Setup(factory => factory.TryLockUpdateBlob("app1"))
                .ThrowsAsync(new UpdateBlobUnavailableException());
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(updateBlobFactoryMock.Object, "instanceId1", "1");
            await Assert.ThrowsAnyAsync<Exception>(async () => await updateSessionManager.EndUpdateSession("app1"));
        }
    }
}
