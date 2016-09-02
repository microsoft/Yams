# Overview

This tutorial will show you how to configure YAMS and deploy it to a cloud service. If you already have a YAMS cluster deployed, skip to [Deploy and Host an App in YAMS](Deploy&Host_an_App_in_YAMS.md) tutorial.

## Deploy YAMS
1. Create a cloud service and a Worker Role.
2. Install the latest version of Etg.Yams from the NuGet gallery to the worker role.
3. Add a `WorkerRoleConfig` class as follows:

  ```csharp
      public class WorkerRoleConfig
      {
          public WorkerRoleConfig()
          {
              UpdateFrequencyInSeconds =
                  Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("UpdateFrequencyInSeconds"));
              ApplicationRestartCount =
                  Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("ApplicationRestartCount"));
              StorageDataConnectionString = RoleEnvironment.GetConfigurationSettingValue("StorageDataConnectionString");
              CurrentRoleInstanceLocalStoreDirectory = RoleEnvironment.GetLocalResource("LocalStoreDirectory").RootPath;
          }

          public string StorageDataConnectionString { get; }

          public string CurrentRoleInstanceLocalStoreDirectory { get; }

          public int UpdateFrequencyInSeconds { get; }

          public int ApplicationRestartCount { get; }
      }
  ```

4. Add a `Utils` folder to the Worker Role. Add an `AzureUtils` class and a `DeploymentIdUtils` class to the folder with the following content:

  ```csharp
      public static class AzureUtils
      {
          public static bool IsEmulator()
          {
              return RoleEnvironment.IsAvailable && RoleEnvironment.IsEmulated;
          }
      }
  ```

  ```csharp
      public static class DeploymentIdUtils
      {
          public static string GetYamsClusterId(bool isSingleClusterDeployment)
          {
              if (!RoleEnvironment.IsAvailable)
              {
                  return Constants.TestDeploymentId;
              }

              string deploymentId = RoleEnvironment.IsEmulated
                  ? Constants.TestDeploymentId
                  : RoleEnvironment.DeploymentId;

              if (isSingleClusterDeployment)
              {
                  return deploymentId;
              }

              // This concatenates the Cloud Service deployment id and the role name so that there is a unique name for each role in the Cloud Service
              return $"{deploymentId}_{RoleEnvironment.CurrentRoleInstance.Role.Name}";
          }
      }
  ```

5. Configure and start YAMS in your Worker Role as follows:

  ```csharp
      public class YamsWorkerRole : RoleEntryPoint
      {
          private IYamsService _yamsService;

          protected virtual bool IsSingleClusterDeployment
          {
              get { return true; }
          }

          [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
          public override void Run()
          {
              RunAsync().Wait();
          }

          public async Task RunAsync()
          {
              WorkerRoleConfig config = new WorkerRoleConfig();
              YamsConfig yamsConfig = new YamsConfigBuilder(
                  // mandatory configs
                  DeploymentIdUtils.GetYamsClusterId(this.IsSingleClusterDeployment),
                  RoleEnvironment.CurrentRoleInstance.UpdateDomain.ToString(),
                  RoleEnvironment.CurrentRoleInstance.Id,
                  config.CurrentRoleInstanceLocalStoreDirectory)
                  // optional configs
                  .SetCheckForUpdatesPeriodInSeconds(config.UpdateFrequencyInSeconds)
                  .SetApplicationRestartCount(config.ApplicationRestartCount)
                  .Build();
              _yamsService = YamsServiceFactory.Create(yamsConfig,
                  deploymentRepositoryStorageConnectionString: config.StorageDataConnectionString,
                  updateSessionStorageConnectionString: config.StorageDataConnectionString);

              try
              {
                  Trace.TraceInformation("Yams is starting");
                  await _yamsService.Start();
                  Trace.TraceInformation("Yams has started. Looking for apps with deploymentId:" + yamsConfig.ClusterDeploymentId);
                  while (true)
                  {
                      await Task.Delay(1000);
                  }
              }
              catch (Exception ex)
              {
                  Trace.TraceError(ex.ToString());
              }
          }

          public override bool OnStart()
          {
              Trace.TraceInformation("Yams WorkerRole is starting");
              ServicePointManager.DefaultConnectionLimit = 1000;
              RoleEnvironment.Changing += RoleEnvironmentChanging;
              var result = base.OnStart();
              Trace.TraceInformation("Yams WorkerRole has started");
              return result;
          }

          public override void OnStop()
          {
              StopAsync().Wait();
          }

          public async Task StopAsync()
          {
              Trace.TraceInformation("Yams WorkerRole is stopping");
              RoleEnvironment.Changing -= RoleEnvironmentChanging;
              if (_yamsService != null)
              {
                  await _yamsService.Stop();
              }
              base.OnStop();
              Trace.TraceInformation("Yams has stopped");
          }

          private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
          {
              // If a configuration setting is changing);
              if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
              {
                  e.Cancel = true;
              }
          }
      }
  ```

You can create additional Worker Roles that inherit from this Worker Role. If you create multiple Worker Roles, you must override `IsSingleClusterDeployment` and set it to be `false` in each Worker Role. This will make the YAMS cluster id a concatenation of the Azure cloud service deployment id and the Worker Role name.

YAMS relies on Azure blob storage to deploy applications. It uses the `dataConnectionString` provided in the `YamsConfig` to connect to the appropriate blob storage.

YAMS expects to find a Storage Container called `applications` at the root of the blob storage. The `applications` container must contain a **DeploymentConfig.json** file that describes the applications to be deployed. The **DeploymentConfig.json** file looks like the following:

```json
{
	"Applications":
	[
        {
            "Id": "hello.webapi",
            "Version": "1.0.1",
            "DeploymentIds": [ "YAMS_CLUSTER_ID" ]
        },	
		{
            "Id": "hello.backend",
			"Version": "1.0.0",
            "DeploymentIds": [ "YAMS_CLUSTER_ID" ]
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


# EndPoints configuration

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

The **ServiceConfiguration.csdef** file should also contain the following configuration settings:

```xml
    <ConfigurationSettings>
      <Setting name="StorageDataConnectionString" />
      <Setting name="UpdateFrequencyInSeconds" />
      <Setting name="ApplicationRestartCount" />
      <Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString"/>
    </ConfigurationSettings>
```

### Execution context

Execution context should be set to elevated in order to host apps reserving incoming ports (Web API, frontend etc.). Following line should be added to worker role section in ServiceDefinition file.

``` xml
    <Runtime executionContext="elevated" />
```

# Sample project
A sample cloud service project that can be used to deploy YAMS is available in the [Samples/Etg.Yams.Cloud](../Samples/Etg.Yams.Cloud) directory. The sample project uses an existing storage account and cloud services which you should replace with you own! 

# Publish the cloud service

Once you're done configuring the cloud service, simply publish it to Azure and start using YAMS. You should only need to publish the cloud service hosting YAMS once.
