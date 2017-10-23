using System;

namespace Etg.Yams.Client
{
    public class YamsClientConfig
    {
        public YamsClientConfig(TimeSpan connectTimeout, TimeSpan initDoneMessageTimeout,
            TimeSpan heartBeatMessageTimeout, string[] processArgs)
        {
            ConnectTimeout = connectTimeout;
            InitDoneMessageTimeout = initDoneMessageTimeout;
            HeartBeatMessageTimeout = heartBeatMessageTimeout;
            ProcessArgs = processArgs;
        }

        public TimeSpan ConnectTimeout { get; }
        public TimeSpan InitDoneMessageTimeout { get; }
        public TimeSpan HeartBeatMessageTimeout { get; }
        public string[] ProcessArgs { get; set; }
    }
}