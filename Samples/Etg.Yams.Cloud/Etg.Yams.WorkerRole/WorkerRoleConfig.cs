using System;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Etg.Yams.WorkerRole
{
    public class WorkerRoleConfig
    {
        public WorkerRoleConfig()
        {
            UpdateFrequencyInSeconds = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("UpdateFrequencyInSeconds"));
            ApplicationRestartCount = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("ApplicationRestartCount"));
            StorageDataConnectionString = RoleEnvironment.GetConfigurationSettingValue("StorageDataConnectionString");
            CurrentRoleInstanceLocalStoreDirectory = RoleEnvironment.GetLocalResource("LocalStoreDirectory").RootPath;
        }

        public string StorageDataConnectionString { get; }

        public string CurrentRoleInstanceLocalStoreDirectory { get; }

        public int UpdateFrequencyInSeconds { get; }

        public int ApplicationRestartCount { get; }
    }
}