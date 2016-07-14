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
            string appId = "appId";

            var sequence = StubsUtils.Sequence<StubIUpdateSessionManager.TryStartUpdateSession_String_Delegate>()
                .Once(id => AsyncUtils.AsyncTaskThatThrows<bool>(new StorageException()))
                .Once(id => AsyncUtils.AsyncTaskWithResult(true));

	        var updateSessionStub = new StubIUpdateSessionManager()
		        .TryStartUpdateSession(id => sequence.Next(id));

            IUpdateSessionManager retryDecorator = new UpdateSessionManagerRetryDecorator(
                updateSessionStub,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            Assert.True(await retryDecorator.TryStartUpdateSession(appId));
        }

        [Fact]
        public async Task TestThatEndUpdateSessionIsRetried()
        {
            string appId = "appId";
            var sequence = StubsUtils.Sequence<StubIUpdateSessionManager.EndUpdateSession_String_Delegate>()
                .Once(id => AsyncUtils.AsyncTaskThatThrows(new StorageException()))
                .Once(id => Task.CompletedTask);

	        var updateSessionStub = new StubIUpdateSessionManager()
		        .EndUpdateSession(id => sequence.Next(id));

            IUpdateSessionManager retryDecorator = new UpdateSessionManagerRetryDecorator(
                updateSessionStub,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            await retryDecorator.EndUpdateSession(appId);
        }

        [Fact]
        public async Task TestThatExceptionIsThrownIfMaxRetryCountIsReached()
        {
            string appId = "appId";

            var sequence = StubsUtils.Sequence<StubIUpdateSessionManager.TryStartUpdateSession_String_Delegate>()
                .Twice(id => AsyncUtils.AsyncTaskThatThrows<bool>(new StorageException()))
                .Once(id => AsyncUtils.AsyncTaskWithResult(true));

	        var updateSessionStub = new StubIUpdateSessionManager()
		        .TryStartUpdateSession(id => sequence.Next(id));

            IUpdateSessionManager retryDecorator = new UpdateSessionManagerRetryDecorator(
                updateSessionStub,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            await Assert.ThrowsAsync<StorageException>(async () => await retryDecorator.TryStartUpdateSession(appId));
        }

        [Fact]
        public async Task TestThatNotAllExceptionsAreRetried()
        {
            string appId = "appId";
	        var updateSessionStub = new StubIUpdateSessionManager()
		        .TryStartUpdateSession(id => AsyncUtils.AsyncTaskThatThrows<bool>(new Exception()));

            IUpdateSessionManager retryDecorator = new UpdateSessionManagerRetryDecorator(
                updateSessionStub,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            await Assert.ThrowsAsync<Exception>(async () => await retryDecorator.TryStartUpdateSession(appId));
        }
    }
}