using System;
using System.Threading.Tasks;
using Etg.Yams.Azure.UpdateSession;
using Etg.Yams.TestUtils;
using Xunit;

namespace Etg.Yams.Azure.Test.UpdateSession
{
    public class BlobBasedUpdateSessionTest
    {
        [Fact]
        public async Task TestThatStartUpdateSessionThrowsIfBlobCannotBeLocked()
        {
            string appId = null;
            var updateBlobFactoryStub = new StubIUpdateBlobFactory
            {
                TryLockUpdateBlob_String = id =>
                {
                    appId = id;
                    return AsyncUtils.AsyncTaskThatThrows<IUpdateBlob>(new UpdateBlobUnavailableException());
                }
            };
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(updateBlobFactoryStub, "instanceId1", "1");
            await Assert.ThrowsAsync<UpdateBlobUnavailableException>(async () => await updateSessionManager.TryStartUpdateSession("app1"));
            Assert.Equal("app1", appId);
        }

        [Fact]
        public async Task TestThatEndUpdateSessionThrowsIfBlobCannotBeLocked()
        {
            string appId = null;
            var updateBlobFactoryStub = new StubIUpdateBlobFactory
            {
                TryLockUpdateBlob_String = id =>
                {
                    appId = id;
                    return AsyncUtils.AsyncTaskThatThrows<IUpdateBlob>(new UpdateBlobUnavailableException());
                }
            };
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(updateBlobFactoryStub, "instanceId1", "1");
            await Assert.ThrowsAnyAsync<Exception>(async () => await updateSessionManager.EndUpdateSession("app1"));
            Assert.Equal("app1", appId);
        }
    }
}
