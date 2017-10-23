using CommandLine;

namespace Etg.Yams.Client
{
    public class YamsClientOptions
    {
        [Option()]
        public string InitializationPipeName { get; set; }
        [Option()]
        public string ExitPipeName { get; set; }
        [Option()]
        public string HealthPipeName { get; set; }
    }
}