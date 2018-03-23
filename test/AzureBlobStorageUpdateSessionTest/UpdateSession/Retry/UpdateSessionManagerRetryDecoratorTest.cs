using System;
using System.Threading.Tasks;
using Etg.SimpleStubs;
using Etg.Yams.Azure.UpdateSession.Retry;
using Etg.Yams.TestUtils;
using Etg.Yams.Update;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.WindowsAzure.Storage;
using Xunit;

namespace Etg.Yams.Azure.Test.UpdateSession.Retry
{
    public class UpdateSessionManagerRetryDecoratorTest
    {
        [Fact]
        public async Task TestThatStartUpdateSessionIsRetried()
        {
            var sequence = StubsUtils.Sequence<StubIUpdateSessionManager.TryStartUpdateSession_Delegate>()
                .Once(() => AsyncUtils.AsyncTaskThatThrows<bool>(new StorageException()))
                .Once(() => AsyncUtils.AsyncTaskWithResult(true));

	        var updateSessionStub = new StubIUpdateSessionManager()
		        .TryStartUpdateSession(() => sequence.Next());

            IUpdateSessionManager retryDecorator = new StorageExceptionUpdateSessionRetryDecorator(
                updateSessionStub,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            Assert.True(await retryDecorator.TryStartUpdateSession());
        }

        [Fact]
        public async Task TestThatEndUpdateSessionIsRetried()
        {
            var sequence = StubsUtils.Sequence<StubIUpdateSessionManager.EndUpdateSession_Delegate>()
                .Once(() => AsyncUtils.AsyncTaskThatThrows(new StorageException()))
                .Once(() => Task.CompletedTask);

	        var updateSessionStub = new StubIUpdateSessionManager()
		        .EndUpdateSession(() => sequence.Next());

            IUpdateSessionManager retryDecorator = new StorageExceptionUpdateSessionRetryDecorator(
                updateSessionStub,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            await retryDecorator.EndUpdateSession();
        }

        [Fact]
        public async Task TestThatExceptionIsThrownIfMaxRetryCountIsReached()
        {
            var sequence = StubsUtils.Sequence<StubIUpdateSessionManager.TryStartUpdateSession_Delegate>()
                .Twice(() => AsyncUtils.AsyncTaskThatThrows<bool>(new StorageException()))
                .Once(() => AsyncUtils.AsyncTaskWithResult(true));

	        var updateSessionStub = new StubIUpdateSessionManager()
		        .TryStartUpdateSession(() => sequence.Next());

            IUpdateSessionManager retryDecorator = new StorageExceptionUpdateSessionRetryDecorator(
                updateSessionStub,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            await Assert.ThrowsAsync<StorageException>(async () => await retryDecorator.TryStartUpdateSession());
        }

        [Fact]
        public async Task TestThatNotAllExceptionsAreRetried()
        {
	        var updateSessionStub = new StubIUpdateSessionManager()
		        .TryStartUpdateSession(() => AsyncUtils.AsyncTaskThatThrows<bool>(new Exception()));

            IUpdateSessionManager retryDecorator = new StorageExceptionUpdateSessionRetryDecorator(
                updateSessionStub,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            await Assert.ThrowsAsync<Exception>(async () => await retryDecorator.TryStartUpdateSession());
        }
    }
}