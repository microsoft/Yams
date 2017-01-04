namespace Etg.Yams.Application
{
    public class ApplicationConfig
    {
        public AppIdentity Identity { get; }
        public string ExeName { get; }
        public string ExeArgs { get; }
        public bool MonitorInitialization { get; }
        public bool MonitorHealth { get; }
        public bool GracefulShutdown { get; }

        public ApplicationConfig(
            AppIdentity identity,
            string exeName,
            string exeArgs,
            bool monitorInitialization = false,
            bool monitorHealth = false,
            bool gracefulShutdown = false)
        {
            Identity = identity;
            ExeArgs = exeArgs;
            MonitorInitialization = monitorInitialization;
            MonitorHealth = monitorHealth;
            GracefulShutdown = gracefulShutdown;
            ExeName = exeName;
        }
    }
}
