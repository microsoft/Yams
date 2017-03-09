using CommandLine;

namespace Etg.Yams.Client
{
    public class ProcessArgsParser : IProcessArgsParser
    {
        public YamsClientOptions ParseArgs(string[] args)
        {
            var options = new YamsClientOptions();
            Parser.Default.ParseArguments(args, options);
            return options;
        }
    }
}