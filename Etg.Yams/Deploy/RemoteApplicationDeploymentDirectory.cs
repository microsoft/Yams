using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.IO;

namespace Etg.Yams.Deploy
{
    /// <summary>
    /// A proxy object to the blob storage where applications resources are located. This class can read the configuration file
    /// located in the blob storage to figure out what should be deployed where.
    /// </summary>
    public class RemoteApplicationDeploymentDirectory : IApplicationDeploymentDirectory
    {
        private readonly IRemoteDirectory _deploymentRootDir;
        
        public RemoteApplicationDeploymentDirectory(IRemoteDirectory deploymentRootDir)
        {
            _deploymentRootDir = deploymentRootDir;
        }

        public async Task<IEnumerable<AppIdentity>> FetchDeployments(string deploymentId)
        {
            var apps = new HashSet<AppIdentity>();

            DeploymentDirectoryConfig deploymentDirectoryConfig = await FetchDeploymentConfig(_deploymentRootDir);
            foreach (DeploymentConfig deploymentConfig in deploymentDirectoryConfig.DeploymentsConfigs.Where(deploymentConfig => deploymentConfig.DeploymentIds.Contains(deploymentId)))
            {
                try
                {
                    IRemoteDirectory deploymentDir = await _deploymentRootDir.GetDirectory(deploymentConfig.AppIdentity.Id);
                    if (await deploymentDir.Exists())
                    {
                        deploymentDir = await deploymentDir.GetDirectory(deploymentConfig.AppIdentity.Version.ToString());
                    }
                    if (!await deploymentDir.Exists())
                    {
                        Trace.TraceError("Could not find deployment {0}", deploymentConfig.AppIdentity);
                        continue;
                    }
                    if (apps.Contains(deploymentConfig.AppIdentity))
                    {
                        Trace.TraceError(
                            "Application {0} has already been deployed, the request will be ignored.", deploymentConfig.AppIdentity);
                        continue;
                    }

                    apps.Add(deploymentConfig.AppIdentity);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Exception occured while loading application {0}, Exception: {1}", deploymentConfig.AppIdentity, e);
                }
            }
            return apps;
        }

        private static async Task<DeploymentDirectoryConfig> FetchDeploymentConfig(IRemoteDirectory deploymentRootDir)
        {
            IRemoteFile configFile = await deploymentRootDir.GetFile(Constants.DeploymentConfigFileName);
            if (!await configFile.Exists())
            {
                Trace.TraceInformation("The deployments config file {0} was not found in the {1} directory; i.e. NO applications to deploy.", Constants.DeploymentConfigFileName, deploymentRootDir.Name);
                return new DeploymentDirectoryConfig(new List<DeploymentConfig>());
            }
            return await new DeploymentConfigParser().ParseData(configFile.DownloadText().Result);
        }
    }
}
