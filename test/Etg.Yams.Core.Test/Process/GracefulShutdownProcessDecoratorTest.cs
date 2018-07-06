using System.Threading.Tasks;
using Etg.Yams.Process;
using Xunit;
using Etg.Yams.Ipc;
using Etg.Yams.Application;

namespace Etg.Yams.Test.Process
{
    public class GracefulShutdownProcessDecoratorTest
    {
        [Fact]
        public async Task TestThatExitedEventIsNotFiredOnGracefulShutdown()
        {
            var yamsConfig = new YamsConfigBuilder("clusterId", "1", "instanceId", "C:\\Foo").Build();
            var process = new StubIProcess().HasExited_Get(() => true);
            IIpcConnection connection = new StubIIpcConnection()
                .SendMessage(message =>
                {
                    process.Exited_Raise(process, new ProcessExitedArgs(""));
                    return Task.CompletedTask;
                })
                .Disconnect(() => Task.CompletedTask);
            GracefulShutdownProcessDecorator decorator = new GracefulShutdownProcessDecorator(new AppIdentity("app", "1.0.0"), yamsConfig, process, connection);
            bool hasExitedFired = false;
            decorator.Exited += (sender, args) =>
            {
                hasExitedFired = true;
            };

            await decorator.Close();
            Assert.False(hasExitedFired);
        }
    }
}
