using System;
using System.Threading.Tasks;
using System.Timers;
using Etg.Yams.Update;

namespace Etg.Yams.Watcher
{
    public class DeploymentWatcher : IDeploymentWatcher, IDisposable
    {
        private readonly IApplicationUpdateManager _deploymentUpdateManager;
        protected Timer UpdateTimer;
        protected volatile bool IsUpdating;

        public DeploymentWatcher(IApplicationUpdateManager deploymentUpdateManager, int updateFrequencyInSeconds)
        {
            _deploymentUpdateManager = deploymentUpdateManager;
            IsUpdating = false;
            UpdateTimer = new Timer(updateFrequencyInSeconds*1000);
            UpdateTimer.Elapsed += OnTimer;
        }

        public Task Start()
        {
            UpdateTimer.Start();
            return Task.FromResult(true);
        }

        public Task Stop()
        {
            UpdateTimer.Stop();
            return Task.FromResult(true);
        }

        private async void OnTimer(object sender, ElapsedEventArgs args)
        {
            if (IsUpdating) return;
            IsUpdating = true;

            try
            {
                await CheckForUpdates();
            }
            finally
            {
                IsUpdating = false;
            }
        }

        public Task CheckForUpdates()
        {
            return _deploymentUpdateManager.CheckForUpdates();
        }

        public void Dispose()
        {
            Stop().Wait();
            UpdateTimer.Dispose();
        }
    }
}
