using System;
using System.Net;
using System.Security.Permissions;
using Topshelf;

namespace Etg.Yams.Host
{
    class Program
    {
        private static string _deploymentId;
        private static string _updateDomain;
        private static int _updateFrequencyInSeconds;
        private static int _applicationRestartCount;
        private static string _storageAccount;
        private static string _storageKey;

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 1000;

            HostFactory.Run(x =>
            {
                x.AddCommandLineDefinition("deploymentId", did => _deploymentId = did);
                x.AddCommandLineDefinition("updateDomain", upd => _updateDomain = upd);
                x.AddCommandLineDefinition("storageAccount", sacc => _storageAccount = sacc);
                x.AddCommandLineDefinition("storageKey", skey => _storageKey = skey);
                x.AddCommandLineDefinition("updateFrequency", uf => _updateFrequencyInSeconds = Convert.ToInt32(uf));
                x.AddCommandLineDefinition("applicationRestartCount", arc => _applicationRestartCount = Convert.ToInt32(arc));

                x.ApplyCommandLine();

                x.Service<IYamsService>(s =>
                {
                    s.ConstructUsing(name =>
                    {
                        if (string.IsNullOrWhiteSpace(_storageKey))
                            throw new ArgumentNullException(nameof(_storageKey));

                        if (string.IsNullOrWhiteSpace(_storageAccount))
                            throw new ArgumentNullException(nameof(_storageAccount));

                        if (string.IsNullOrWhiteSpace(_deploymentId))
                            throw new ArgumentNullException(nameof(_deploymentId));

                        if (string.IsNullOrWhiteSpace(_updateDomain))
                            throw new ArgumentNullException(nameof(_updateDomain));

                        string blobStorageConnectionString = $"DefaultEndpointsProtocol=https;AccountName={_storageAccount};AccountKey={_storageKey}";
                        YamsConfig yamsConfig = new YamsConfigBuilder(
                            // mandatory configs
                            _deploymentId,
                            _updateDomain,
                            Environment.MachineName,
                            Environment.CurrentDirectory + "\\LocalStore")
                            // optional configs
                            .SetCheckForUpdatesPeriodInSeconds(_updateFrequencyInSeconds)
                            .SetApplicationRestartCount(_applicationRestartCount)
                            .Build();

                        return YamsServiceFactory.Create(yamsConfig,
                            deploymentRepositoryStorageConnectionString: blobStorageConnectionString,
                            updateSessionStorageConnectionString: blobStorageConnectionString);
                    });
                    s.WhenStarted(yep => yep.Start().Wait());
                    s.WhenStopped(yep => yep.Stop().Wait());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Yams Service Host");
                x.SetDisplayName("Yams Service");
                x.SetServiceName("Yams");
                x.StartAutomatically();
            });
        }
    }
}
