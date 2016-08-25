using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly IDeploymentRepository _deploymentRepository;

        public RemoteApplicationDeploymentDirectory(IDeploymentRepository deploymentRepository)
        {
            _deploymentRepository = deploymentRepository;
        }

        public async Task<IEnumerable<AppIdentity>> FetchDeployments(string deploymentId)
        {
            var apps = new HashSet<AppIdentity>();

            DeploymentConfig deploymentConfig = await _deploymentRepository.FetchDeploymentConfig();
            foreach (string appId in deploymentConfig.ListApplications(deploymentId))
            {
                foreach (string version in deploymentConfig.ListVersions(appId, deploymentId))
                {
                    AppIdentity appIdentity = new AppIdentity(appId, version);
                    try
                    {
                        if (!await _deploymentRepository.HasApplicationBinaries(appIdentity))
                        {
                            Trace.TraceError($"Could not find binaries for application {appIdentity} of {version} in the yams repository");
                            continue;
                        }

                        apps.Add(appIdentity);
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError($"Exception occured while loading application {appIdentity}, Exception: {e}");
                    }
                }
            }
            return apps;
        }
    }
}
