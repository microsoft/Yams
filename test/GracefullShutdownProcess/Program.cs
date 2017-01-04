using System;
using System.Threading.Tasks;
using Etg.Yams.Client;
using System.IO;

namespace GracefullShutdownProcess
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Run(args).Wait();
        }

        private static async Task Run(string[] args)
        {
            Console.WriteLine("args " + string.Join(" ", args));
            int shutdownDelay = 1000 * Convert.ToInt32(args[2]);

            var yamsClientConfig = new YamsClientConfigBuilder(args).Build();
            var yamsClientFactory = new YamsClientFactory();

            Console.WriteLine("Initializing...");
            IYamsClient yamsClient = yamsClientFactory.CreateYamsClient(yamsClientConfig);

            await yamsClient.Connect();
            Console.WriteLine("Initialization done!");

            bool exitMessageReceived = false;
            yamsClient.ExitMessageReceived += (sender, eventArgs) =>
            {
                Console.WriteLine("Exit message received!");
                exitMessageReceived = true;
            };

            File.WriteAllText($"GracefulShutdownApp.exe.out", $"GracefulShutdownApp.exe {args[0]} {args[1]}");

            while (!exitMessageReceived)
            {
                await Task.Delay(100);
                Console.WriteLine("Doing work");
            }
            await Task.Delay(shutdownDelay);
            Console.WriteLine("Exiting..");
        }
    }
}