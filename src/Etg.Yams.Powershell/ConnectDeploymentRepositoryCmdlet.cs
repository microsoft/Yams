using Etg.Yams.Azure.Storage;
using Etg.Yams.Storage;
using System;
using System.Management.Automation;

namespace Etg.Yams.Powershell
{
    [Cmdlet(VerbsCommunications.Connect, "DeploymentRepository")]
    [OutputType(typeof(IDeploymentRepository))]

    public class ConnectDeploymentRepositoryCmdlet : Cmdlet
    {
        [Parameter]
        public string ConnectionString { get; set; }

        protected override void ProcessRecord()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new ArgumentException(nameof(ConnectionString));
            }
            var deploymentRepository = BlobStorageDeploymentRepository.Create(ConnectionString);
            WriteObject(deploymentRepository);
        }
    }
}
