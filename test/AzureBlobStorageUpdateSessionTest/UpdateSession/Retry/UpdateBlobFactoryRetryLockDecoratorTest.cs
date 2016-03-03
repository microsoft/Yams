using System;
using System.Threading.Tasks;
using Etg.SimpleStubs;
using Etg.Yams.Azure.UpdateSession;
using Etg.Yams.Azure.UpdateSession.Retry;
using Etg.Yams.TestUtils;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Xunit;

namespace Etg.Yams.Azure.Test.UpdateSession.Retry
{
    public class UpdateBlobFactoryRetryLockDecoratorTest
    {
        [Fact]
        public async Task TestSuccessfullRetry()
        {
            string appId = "appId";
            IUpdateBlob updateBlob = new StubIUpdateBlob();

            var sequence = StubsUtils.Sequence<StubIUpdateBlobFactory.TryLockUpdateBlob_String_Delegate>()
                .Twice(id => AsyncUtils.AsyncTaskThatThrows<IUpdateBlob>(new UpdateBlobUnavailableException()))
                .Once(id => AsyncUtils.AsyncTaskWithResult(updateBlob));
            var updateBlobFactoryStub = new StubIUpdateBlobFactory
            {
                TryLockUpdateBlob_String = id => sequence.Next(appId)
            };

            UpdateBlobFactoryRetryLockDecorator retryDecorator =
                new UpdateBlobFactoryRetryLockDecorator(updateBlobFactoryStub,
                    new FixedInterval(2, TimeSpan.Zero));
            Assert.Equal(updateBlob, await retryDecorator.TryLockUpdateBlob(appId));
        }

        [Fact]
        public async Task TestThatExceptionIsThrownIfMaxRetryCountIsReached()
        {
            string appId = "appId";
            IUpdateBlob updateBlob = new StubIUpdateBlob();

            var sequence = StubsUtils.Sequence<StubIUpdateBlobFactory.TryLockUpdateBlob_String_Delegate>()
                .Twice(id => AsyncUtils.AsyncTaskThatThrows<IUpdateBlob>(new UpdateBlobUnavailableException()))
                .Once(id => AsyncUtils.AsyncTaskWithResult(updateBlob));
            var updateBlobFactoryStub = new StubIUpdateBlobFactory
            {
                TryLockUpdateBlob_String = id => sequence.Next(appId)
            };

            UpdateBlobFactoryRetryLockDecorator retryDecorator =
                new UpdateBlobFactoryRetryLockDecorator(updateBlobFactoryStub, new FixedInterval(1, TimeSpan.Zero));
            await
                Assert.ThrowsAsync<UpdateBlobUnavailableException>(
                    async () => await retryDecorator.TryLockUpdateBlob(appId));
        }

        [Fact]
        public async Task TestThatNotAllExceptionsAreRetried()
        {
            string appId = "appId";

            var sequence = StubsUtils.Sequence<StubIUpdateBlobFactory.TryLockUpdateBlob_String_Delegate>()
                .Twice(id => AsyncUtils.AsyncTaskThatThrows<IUpdateBlob>(new Exception()));
            var updateBlobFactoryStub = new StubIUpdateBlobFactory
            {
                TryLockUpdateBlob_String = id => sequence.Next(appId)
            };

            UpdateBlobFactoryRetryLockDecorator retryDecorator =
                new UpdateBlobFactoryRetryLockDecorator(updateBlobFactoryStub, new FixedInterval(1, TimeSpan.Zero));
            await
                Assert.ThrowsAsync<Exception>(async () => await retryDecorator.TryLockUpdateBlob(appId));
        }
    }
}