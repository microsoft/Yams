# Continuous Integration with YAMS and VSTS

In this tutorial, we will demonstrate how to use YAMS in a continuous integration environments. We will use VSTS as an example, but the concepts discussed can be easily adapted to another platform. We will also pair YAMS with cloud services to get the best of both words.

We will automate the deployment of two YAMS clusters:
* A Web Api cluster.
* An Orleans cluster.

We could deploy both applications to the same cluster, but we chose to use two clusters because that will allow us to scale the two clusters independently.

Finally, we will show how you can add support for multiples configuration environments (e.g. dev, prod) to your YAMS applications.

## Deploy YAMS

To deploy services with YAMS, the YAMS host itself need to be deployed to the virtual machines where the services will be deployed. We use Azure Cloud Services for this.

The [Deploy YAMS tutorial](Deploy_YAMS.md) tutorial shows how YAMS can be deployed to a cloud service.

### Multiple configuration environments

In order to support multiple configuration environments (e.g. dev, prod), we will add the environment name to the YAMS cluster properties. In the worker role shown in [Deploy YAMS tutorial](Deploy_YAMS.md), add the `EnvironmentName` cluster property as show below:

```csharp
            string clusterId = "WebApi";
            YamsConfig yamsConfig = new YamsConfigBuilder(
                // mandatory configs
                clusterId: clusterId,
                instanceUpdateDomain: RoleEnvironment.CurrentRoleInstance.UpdateDomain.ToString(),
                instanceId: RoleEnvironment.CurrentRoleInstance.Id,
                applicationInstallDirectory: RoleEnvironment.GetLocalResource("LocalStoreDirectory").RootPath)
                // optional configs
                .SetCheckForUpdatesPeriodInSeconds(5)
                .SetApplicationRestartCount(3)
                .AddClusterProperty("EnvironmentName", RoleEnvironment.GetConfigurationSettingValue("EnvironmentName"))
                .AddClusterProperty("DeploymentId", GetDeploymentId())
                .Build();
```

For the above to work, you need to add the `EnvironmentName` to your cloud service `ServiceDefinition.csdef` and the corresponding `ServiceConfiguration.*.cscfg` files.

Notice that we have also added the `DeploymentId` as a cluster property and that we have named the cluster `WebApi` (we will take advantage of the `DeploymentId` property later in this tutorial).


### Multiple clusters

To host a second YAMS cluster in the same cloud service, simply add a second worker role to your solution. The second worker role will be used to host the cluster where Orleans is deployed, and the cluster id will be `Orleans`.

### Deploy with Visual Studio

If you followed the steps in [Deploy YAMS tutorial](Deploy_YAMS.md) and the steps above, you should be ready to deploy YAMS to a cloud service with two worker role, one hosting the Web Api and another hosting Orleans.

You have also configured the YAMS clusters so that the deployment id and the environment name are passed down to YAMS as cluster properties and available to be used as command line arguments to start YAMS applications. In fact, cluster properties can be used in the `AppConfig.json` file of a YAMS application and will be resolved to the actual value if referenced as follows: `${Cluster_Property_Name}` (e.g. ${EnvironmentName}). See [YAMS Overview](Overview.md) for more information about the `AppConfig.json` file.

Finally, deploy your YAMS clusters to the cloud service using visual studio. You will typically need to deploy the cloud service only once and then manage applications with YAMS.

## Deploy YAMS applications with VSTS

Now that the YAMS cluster is up and running, it's time to setup VSTS so that YAMS applications are automatically deployed.

Make sure that your WebApi and Orleans applications have corresponding `AppConfig.json` files and are configured so that the `EnvironmentName` and `DeploymentId` are passed as command line arguments as follows:

```json
{
  "ExeName": "HelloService.WebApi.exe",
  "ExeArgs": "${EnvironmentName} ${DeploymentId}"
}
```

```json
{
  "ExeName": "HelloService.Orleans.exe",
  "ExeArgs": "${EnvironmentName} ${DeploymentId}"
}
```

The applications can then load different configuration based on the `EnvironmentName` (this is quite easy to setup using `Microsoft.Extensions.Configuration` for example).

The `DeploymentId` is only passed down so it can be used as a silo name (don't worry about this if you are not using Orleans).

### Setup your build

Setup your VSTS build so that the build artifacts are as follows:

```
Build\drop\HelloService.WebApi\
Build\drop\HelloService.Orleans\
```

The directories above should contain all the binaries and configuration needed to start your applications.

### Setup VSTS

To deploy YAMS, we will use the YAMS powershell cmdlets.

* Add a `Download Package` task to your VSTS release definition and select the latest version of `Etg.Yams.Powershell`.
* Use `$(System.DefaultWorkingDirectory)/YamsPowershell` as a destination directory.
* Add an `Azure Powershell` task to your VSTS release definition and add the following to it:

Scripts Arguments: 
```
-WorkingDir "$(System.DefaultWorkingDirectory)" -ConnectionString "$(StorageConnectionString)" -Version "1.0.$(Release.ReleaseId)"
```

Inline Scripts:
```powershell
Param(
  [string]$ConnectionString,
  [string]$Version,
  [string]$WorkingDir
)

$BinDir = "$WorkingDir\Build\drop"

Import-Module $WorkingDir\YamsPowershell\content\Etg.Yams.Powershell.dll
Install-Applications -ConnectionString $ConnectionString -AppsIds "HelloService.WebApi","HelloService.Orleans" -ClustersIds "WebApi","Orleans" -BinariesPath "$BinDir\HelloService.WebApi","$BinDir\HelloService.Orleans\" -Versions $Version,$Version -WaitForDeploymentsToComplete $true
```

Note that this assumes that you already have a storage account configured for YAMS and that you have added `StorageConnectionString` variable to VSTS (or feel free to use any different way to propagate your connection string to the powershell script).

That's it! 

All you need to do now is to configure your deployment triggers in VSTS (e.g. deploy on every check-in to master) and you should have 1-2 minute VSTS deployments!


## Add Support for VIP Swaps

In some cases, one may not feel confident using a rolling upgrade in a production environment because there is a chance that the deployed service will experience difficulties starting up. A popular approach to avoid such issues is to deploy first to a staging environment, test and warm up the deployment, and then perform a VIP swap.

YAMS does not have native support for VIP swaps but one can easily achieve VIP swaps with YAMS when combined with Azure cloud services. The trick is to use the cloud service deployment id as a prefix in the Yams cluster id. Follow the following steps to achieve this.

* Use the deployment id of your cloud service as a prefix to your YAMS cluster id when you start your worker role:
```csharp
string clusterId = $"{RoleEnvironment.GetConfigurationSettingValue("ClusterId")}_{RoleEnvironment.DeploymentId}";
```
* Use the same deployment id above in your VSTS task

```powershell
Param(
  [string]$ConnectionString,
  [string]$Version,
  [string]$WorkingDir
)

$BinDir = "$WorkingDir\Build\drop"

Import-Module $WorkingDir\YamsPowershell\content\Etg.Yams.Powershell.dll

$DeploymentId = (Get-AzureDeployment -ServiceName $(ServiceName) -Slot Staging).DeploymentId

Install-Applications -ConnectionString $ConnectionString -AppsIds "NotificationService","NotificationService.Silo" -ClustersIds "FrontEnd_$(DeploymentId)","Orleans_$(DeploymentId)" -BinariesPath "$BinDir\NotificationService","$BinDir\NotificationService.Silo\" -Versions $Version,$Version -WaitForDeploymentsToComplete $true
```

With this setup, the YAMS applications will be deployed to the *Staging* slot instead of the *Production* slot. Once the service is deployed, warmed up and tested, simply VIP swap using the cloud service portal or a powershell VSTS task.
