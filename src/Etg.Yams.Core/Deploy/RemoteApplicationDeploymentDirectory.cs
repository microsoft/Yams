using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Deploy
{
    /// <summary>
    /// A proxy object to the blob storage where applications resources are located. This class can read the configuration file
    /// located in the blob storage to figure out what should be deployed where.
    /// </summary>
    public class RemoteApplicationDeploymentDirectory : IApplicationDeploymentDirectory
    {
        private readonly IDeploymentConfigRepository _deploymentConfigRepository;
        private readonly IDeploymentRepository _deploymentRepository;
        private readonly IAppDeploymentMatcher _appDeploymentMatcher;

        public RemoteApplicationDeploymentDirectory(IDeploymentConfigRepository deploymentConfigRepository, IDeploymentRepository deploymentRepository, IAppDeploymentMatcher appDeploymentMatcher)
        {
            _deploymentConfigRepository = deploymentConfigRepository;
            _deploymentRepository = deploymentRepository;
            _appDeploymentMatcher = appDeploymentMatcher;
        }

        public async Task<IEnumerable<AppDeploymentConfig>> FetchDeployments()
        {
            var apps = new HashSet<AppDeploymentConfig>();

            DeploymentConfig deploymentConfig = await _deploymentConfigRepository.FetchDeploymentConfig();

            foreach (AppDeploymentConfig appDeploymentConfig in deploymentConfig.Where(_appDeploymentMatcher.IsMatch))
            {
                AppIdentity appIdentity = appDeploymentConfig.AppIdentity;
                try
                {
                    if (!await _deploymentRepository.HasApplicationBinaries(appIdentity))
                    {
                        Trace.TraceError($"Could not find binaries for application {appIdentity} in the yams repository");
                        continue;
                    }

                    apps.Add(appDeploymentConfig);
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Exception occured while loading application {appIdentity}, Exception: {e}");
                }
            }
            return apps;
        }
    }
}
