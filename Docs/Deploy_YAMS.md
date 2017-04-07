# Overview

This tutorial will show you how to configure YAMS and deploy it to a cloud service. If you already have a YAMS cluster deployed, skip to [Deploy and Host an App in YAMS](Deploy&Host_an_App_in_YAMS.md) tutorial.

## Deploy YAMS
1. Create a cloud service and add a Worker Role to it.
2. Install the latest version of `Etg.Yams` from the NuGet gallery to the worker role.
3. Add the following private members to your `WorkerRoles.cs` class:

```csharp
private IYamsService _yamsService;

private static string GetDeploymentId()
{
    if (!RoleEnvironment.IsAvailable || RoleEnvironment.IsEmulated)
    {
        return "testdeploymentid";
    }

    return RoleEnvironment.DeploymentId;
}
```
4. Replace the content of the `RunAsync` method with the following:

```csharp
private async Task RunAsync(CancellationToken cancellationToken)
{
    YamsConfig yamsConfig = new YamsConfigBuilder(
        // mandatory configs
        clusterId: GetYamsClusterId(),
        instanceUpdateDomain: RoleEnvironment.CurrentRoleInstance.UpdateDomain.ToString(),
        instanceId: RoleEnvironment.CurrentRoleInstance.Id,
        applicationInstallDirectory: RoleEnvironment.GetLocalResource("LocalStoreDirectory").RootPath)
        // optional configs
        .SetCheckForUpdatesPeriodInSeconds(5)
        .SetApplicationRestartCount(3)
        .Build();
    _yamsService = YamsServiceFactory.Create(yamsConfig,
        deploymentRepositoryStorageConnectionString: RoleEnvironment.GetConfigurationSettingValue("StorageDataConnectionString"),
        updateSessionStorageConnectionString: RoleEnvironment.GetConfigurationSettingValue("StorageDataConnectionString"));

    try
    {
        Trace.TraceInformation("Yams is starting");
        await _yamsService.Start();
        Trace.TraceInformation("Yams has started. Looking for apps with clusterId:" + GetYamsClusterId());
    }
    catch (Exception e)
    {
        Trace.TraceError($"Failed to start the Yams cluster {GetYamsClusterId()}", e);
    }

    while (!cancellationToken.IsCancellationRequested)
    {
        Trace.TraceInformation("Working");
        await Task.Delay(1000);
    }
}
```

5. Add the following configuration to your `ServiceDefinition.csdef`

```xml
  <WorkerRole name="YamsWorkerRole" vmsize="Small">

    <!-- Make sure that YAMS has elevated permissions -->
    <Runtime executionContext="elevated" />
    
    <ConfigurationSettings>
      <!--blob storage connection string needed so that Yams can access the deployment storage -->
      <Setting name="StorageDataConnectionString" />
    </ConfigurationSettings>

    <LocalResources>
      <!--Needed to tell Yams where to install apps -->
      <LocalStorage name="LocalStoreDirectory" cleanOnRoleRecycle="false" />
    </LocalResources>
    
  </WorkerRole>
```

Don't forget to add the corresponding values to your `ServiceConfiguration.Cloud.cscfg` and `ServiceConfiguration.Local.cscfg`

6. Configure EndPoints to be used by YAMS apps

To allow applications to access endpoints, YAMS must register those endpoints in Azure when the cloud service containing YAMS is deployed. Fortunately, it is possible in Azure to register a range of endpoints. To do so, add the following to the **ServiceConfiguration.csdef** file:

```xml
    <Endpoints>
      <InputEndpoint name="HttpsIn" protocol="https" port="443" certificate="your-certificate.net"/>
      <InputEndpoint name="HttpIn" protocol="http" port="80"/>
      <InternalEndpoint name="TcpEndpoints" protocol="tcp">
        <FixedPortRange min="81" max="400"/>
      </InternalEndpoint>
    </Endpoints>
```

In this case, port 443 will be available for **https** connections, port 80 will be available for **http** connections and all ports from 81 to 400 will be open for **tcp** connections.

7. Publish the cloud service to Azure and start using YAMS. You should only need to publish the cloud service hosting YAMS once.

## Deployment Storage

YAMS relies on Azure blob storage to deploy applications. It uses the `dataConnectionString` provided in the `YamsConfig` to connect to the appropriate blob storage.

YAMS expects to find a Storage Container called `applications` at the root of the blob storage. The `applications` container must contain a **DeploymentConfig.json** file that describes the applications to be deployed. The **DeploymentConfig.json** file looks like the following:

```json
{
	"Applications":
	[
        {
            "Id": "hello.webapi",
            "Version": "1.0.1",
            "TargetClusters": [ "YAMS_CLUSTER_ID" ]
        },	
		{
            "Id": "hello.backend",
			"Version": "1.0.0",
            "TargetClusters": [ "YAMS_CLUSTER_ID" ]
		},
	]
}
```

In this case, the application `hello.webapi`, version `1.0.1` will be deployed to the cloud service with deployment Id *"YAMS_CLUSTER_ID"*. The binaries of the application should be available in the blob storage at the path `applications/hello.webapi/1.0.1`.

The blob storage content in the example above will look as follows:

```
applications
|___ hello.backend
|   |___ 1.0.0
|___ hello.webapi
|   |___ 1.0.1
|___ DeploymentConfig.json            
```

YAMS will download all the applications in the blob storage (that are listed in the **DeploymentConfig.json** file) to each role instance of the cloud service and start the applications on all instances. To start an application, YAMS reads the **AppConfig.json** (that must be available at the root of each application) to figure out the **exe** name and arguments for that specific application.

The **AppConfig.json** file looks as follows:

```json
{
    "ExeName": "WebApiHost.exe",
    "ExeArgs": "-p Https --appName=${Id}"
}
```

In this case, YAMS will use the following command to start the application:

```
WebApiHost.exe -p Https --appName=hello.webapi
```

Notice the `${Id}` symbol in the `ExeArgs` that was substituted with the actual **id** of the application. Other available symbols are (symbols are resolved at runtime by YAMS):
* ${Version}
* ${Version.Major}
* ${Version.Minor}
* ${Version.Build}
* ${DeploymentId}: the cloud service deployment id.
* ${InstanceId}: the current role instance id.
