using System;
using System.Collections.Generic;

namespace Etg.Yams
{
    public class YamsConfigBuilder
    {
        public YamsConfigBuilder(string clusterId, string instanceUpdateDomain, 
            string instanceId, string applicationInstallDirectory)
        {
            _clusterId = clusterId;
            _instanceUpdateDomain = instanceUpdateDomain;
            _instanceId = instanceId;
            _applicationInstallDirectory = applicationInstallDirectory;
            _checkForUpdatesPeriodInSeconds = 10;
            _applicationRestartCount = 3;
            _processWaitForExitInSeconds = 5;
            _showApplicationProcessWindow = true;
            _gracefulShutdownMessageTimeout = TimeSpan.FromSeconds(30);
            _appGracefulShutdownTimeout = TimeSpan.FromSeconds(60);
            _appHeartBeatTimeout = TimeSpan.FromSeconds(60);
            _ipcConnectTimeout = TimeSpan.FromSeconds(60);
            _appInitTimeout = TimeSpan.FromSeconds(60);
        }

        public YamsConfigBuilder SetCheckForUpdatesPeriodInSeconds(int value)
        {
            _checkForUpdatesPeriodInSeconds = value;
            return this;
        }

        public YamsConfigBuilder SetApplicationRestartCount(int value)
        {
            _applicationRestartCount = value;
            return this;
        }

        public YamsConfigBuilder SetProcessWaitForExitInSeconds(int value)
        {
            _processWaitForExitInSeconds = value;
            return this;
        }

        public YamsConfigBuilder SetShowApplicationProcessWindow(bool value)
        {
            _showApplicationProcessWindow = value;
            return this;
        }

        public YamsConfigBuilder AddClusterProperty(string key, string value)
        {
            _clusterProperties[key] = value;
            return this;
        }

        public YamsConfigBuilder SetGracefulShutdownMessageTimeout(TimeSpan timeout)
        {
            _gracefulShutdownMessageTimeout = timeout;
            return this;
        }

        public YamsConfigBuilder SetAppGracefulShutdownTimeout(TimeSpan timeout)
        {
            _appGracefulShutdownTimeout = timeout;
            return this;
        }

        public YamsConfigBuilder SetAppHeartBeatTimeout(TimeSpan timeout)
        {
            _appHeartBeatTimeout = timeout;
            return this;
        }

        public YamsConfigBuilder SetIpcConnectTimeout(TimeSpan timeout)
        {
            _ipcConnectTimeout = timeout;
            return this;
        }

        public YamsConfigBuilder SetAppInitTimeout(TimeSpan timeout)
        {
            _appInitTimeout = timeout;
            return this;
        }

        public YamsConfig Build()
        {
            return new YamsConfig(
                _clusterId,
                _instanceUpdateDomain,
                _instanceId,
                _applicationInstallDirectory,
                _checkForUpdatesPeriodInSeconds,
                _applicationRestartCount,
                _processWaitForExitInSeconds,
                _showApplicationProcessWindow,
                _gracefulShutdownMessageTimeout,
                _appGracefulShutdownTimeout,
                _appHeartBeatTimeout,
                _ipcConnectTimeout,
                _appInitTimeout,
                _clusterProperties);
        }

        private readonly string _clusterId;
        private readonly string _instanceUpdateDomain;
        private readonly string _instanceId;
        private readonly string _applicationInstallDirectory;
        private int _checkForUpdatesPeriodInSeconds;
        private int _applicationRestartCount;
        private int _processWaitForExitInSeconds;
        private bool _showApplicationProcessWindow;
        private TimeSpan _gracefulShutdownMessageTimeout;
        private TimeSpan _appGracefulShutdownTimeout;
        private TimeSpan _appHeartBeatTimeout;
        private TimeSpan _ipcConnectTimeout;
        private TimeSpan _appInitTimeout;
        private readonly Dictionary<string, string> _clusterProperties = new Dictionary<string, string>();
    }
}