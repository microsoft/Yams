namespace Etg.Yams.Application
{
    public class ApplicationConfig
    {
        public AppIdentity Identity { get; private set; }
        public string ExeName { get; private set; }
        public string ExeArgs { get; private set; }

        public ApplicationConfig(AppIdentity identity, string exeName, string exeArgs)
        {
            Identity = identity;
            ExeArgs = exeArgs;
            ExeName = exeName;
        }
    }
}
