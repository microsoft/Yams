using System;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Etg.Yams.WorkerRole
{
    public class WorkerRoleConfig
    {
        private readonly string _storageDataConnectionString;
        private readonly string _currentRoleInstanceLocalStoreDirectory;
        private readonly int _updateFrequencyInSeconds;
        private readonly int _applicationRestartCount;

        public WorkerRoleConfig()
        {
            _updateFrequencyInSeconds = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("UpdateFrequencyInSeconds"));
            _applicationRestartCount = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("ApplicationRestartCount"));
            _storageDataConnectionString = RoleEnvironment.GetConfigurationSettingValue("StorageDataConnectionString");
            _currentRoleInstanceLocalStoreDirectory = RoleEnvironment.GetLocalResource("LocalStoreDirectory").RootPath;
        }

        public string StorageDataConnectionString
        {
            get { return _storageDataConnectionString; }
        }

        public string CurrentRoleInstanceLocalStoreDirectory
        {
            get { return _currentRoleInstanceLocalStoreDirectory; }
        }

        public int UpdateFrequencyInSeconds
        {
            get { return _updateFrequencyInSeconds; }
        }

        public int ApplicationRestartCount
        {
            get { return _applicationRestartCount; }
        }
    }
}