using System;

namespace Etg.Yams.Client
{
    public class YamsClientConfigBuilder
    {
        private readonly string[] _processArgs;
        private TimeSpan _connectTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan _initDoneMessageTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan _heartBeatMessageTimeout = TimeSpan.FromSeconds(30);

        public YamsClientConfigBuilder(string[] processArgs)
        {
            _processArgs = processArgs;
        }

        public YamsClientConfigBuilder SetConnectTimeout(TimeSpan timeout)
        {
            _connectTimeout = timeout;
            return this;
        }

        public YamsClientConfigBuilder SetInitDoneMessageTimeout(TimeSpan timeout)
        {
            _initDoneMessageTimeout = timeout;
            return this;
        }

        public YamsClientConfigBuilder SetHeartBeatMessageTimeout(TimeSpan timeout)
        {
            _heartBeatMessageTimeout = timeout;
            return this;
        }

        public YamsClientConfig Build()
        {
            return new YamsClientConfig(_connectTimeout, _initDoneMessageTimeout,
                _heartBeatMessageTimeout, _processArgs);
        }
    }
}