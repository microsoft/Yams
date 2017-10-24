#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using Topshelf;

#endregion

namespace Etg.Yams.Host
{
    class Program
    {
        private static string _clusterId;
        private static string _updateDomain;
        private static int _updateFrequencyInSeconds;
        private static int _applicationRestartCount;
        private static string _deploymentRepositoryStorageConnectionString;
        private static string _updateSessionStorageConnectionString;
        private static IDictionary<string, string> _clusterProperties;

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 1000;

            HostFactory.Run(x =>
            {
                x.AddCommandLineDefinition("clusterId", cid => _clusterId = cid);
                x.AddCommandLineDefinition("updateDomain", upd => _updateDomain = upd);
                x.AddCommandLineDefinition("deploymentRepositoryStorageConnectionString", drscs => _deploymentRepositoryStorageConnectionString = drscs);
                x.AddCommandLineDefinition("updateSessionStorageConnectionString", upscs => _updateSessionStorageConnectionString = upscs);
                x.AddCommandLineDefinition("updateFrequency", uf => _updateFrequencyInSeconds = Convert.ToInt32(uf));
                x.AddCommandLineDefinition("applicationRestartCount", arc => _applicationRestartCount = Convert.ToInt32(arc));
                x.AddCommandLineDefinition("clusterProperties", cp => _clusterProperties = cp.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Split('=')).ToDictionary(s => s[0], s => s[1]));

                x.ApplyCommandLine();

                x.Service<IYamsService>(s =>
                {
                    s.ConstructUsing(name =>
                    {
                        if (string.IsNullOrWhiteSpace(_updateSessionStorageConnectionString))
                            throw new ArgumentNullException(nameof(_updateSessionStorageConnectionString));

                        if (string.IsNullOrWhiteSpace(_deploymentRepositoryStorageConnectionString))
                            throw new ArgumentNullException(nameof(_deploymentRepositoryStorageConnectionString));

                        if (string.IsNullOrWhiteSpace(_clusterId))
                            throw new ArgumentNullException(nameof(_clusterId));

                        if (string.IsNullOrWhiteSpace(_updateDomain))
                            throw new ArgumentNullException(nameof(_updateDomain));

                        YamsConfig yamsConfig = new YamsConfigBuilder(
                                // mandatory configs
                                _clusterId,
                                _updateDomain,
                                Environment.MachineName,
                                Environment.CurrentDirectory + "\\LocalStore")
                            // optional configs
                            .SetCheckForUpdatesPeriodInSeconds(_updateFrequencyInSeconds)
                            .SetApplicationRestartCount(_applicationRestartCount)
                            .AddClusterProperties(_clusterProperties)
                            .Build();

                        return YamsServiceFactory.Create(yamsConfig,
                            deploymentRepositoryStorageConnectionString: _deploymentRepositoryStorageConnectionString,
                            updateSessionStorageConnectionString: _updateSessionStorageConnectionString);
                    });
                    s.WhenStarted(yep => yep.Start().Wait());
                    s.WhenStopped(yep => yep.Stop().Wait());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Yams Service Host");
                x.SetDisplayName($"Yams Service");
                x.SetServiceName("Yams");
                x.StartAutomatically();
            });
        }
    }

    public static class YamsConfigBuilderExtensions
    {
        public static YamsConfigBuilder AddClusterProperties(this YamsConfigBuilder yamsConfigBuilder,
            IDictionary<string, string> properties)
        {
            foreach (var property in properties)
            {
                yamsConfigBuilder.AddClusterProperty(property.Key, property.Value);
            }

            return yamsConfigBuilder;
        }
    }
}