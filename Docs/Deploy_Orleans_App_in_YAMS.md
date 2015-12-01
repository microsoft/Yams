# Overview
In this tutorial, we will explain how to create and deploy an Orleans application in YAMS. We will also explain how to connect to the Orleans application from a Web Api application.

# Prerequisites
It's recommended that you read the [Deploy and Host an App in YAMS tutorial](Deploy&Host_an_App_in_YAMS.md) before reading this tutorial.

# Create the Orleans application

Follow the *Hello World* tutorial described in Orleans [My First Orleans Application](http://dotnet.github.io/orleans/Step-by-step-Tutorials/My-First-Orleans-Application) to create a simple Orleans application that will be used in the rest of this tutorial.

Once you have the app ready, follow the steps below to deploy the app in YAMS:

* Add a class library project (call it `OrleansHost`) to the visual studio solution.
* Install `Microsoft.Orleans.SiloHost` NuGet package to the `OrleansHost` class library project (this will add an `OrleansHost.exe` to the build output).
* Install `Microsoft.Orleans.OrleansAzureUtils` NuGet package to the `OrleansHost` class library project.
* From the `OrleansHost` project, add references to both grains and grains interfaces projects.
* Add `OrleansConfiguration.xml` to the `OrleansHost` project (it is needed to start Orleans using `OrleansHost.exe`). The content of the `OrleansConfiguration.xml` file is as follows (for more information about this file please consult Orleans documentation):

```xml
<?xml version="1.0" encoding="utf-8"?>
<OrleansConfiguration xmlns="urn:orleans">
  <Globals>
    <SystemStore SystemStoreType="AzureTable" DeploymentId="" DataConnectionString="MY_DATA_CONNECTION_STRING"/>
    <Liveness LivenessType="AzureTable" ProbeTimeout="3s" TableRefreshTimeout="3s" DeathVoteExpirationTimeout="120s" NumMissedProbesLimit="3" NumProbedSilos="3" NumVotesForDeathDeclaration="2"/>
    <StorageProviders>
      <Provider Type="Orleans.Storage.MemoryStorage" Name="MemoryStore" />
      <Provider Type="Orleans.Storage.AzureTableStorage"
                      Name="AzureStore"
                      DataConnectionString="MY_DATA_CONNECTION_STRING" />
    </StorageProviders>
  </Globals>
  <Defaults>
    <Networking Address="" Port="100"/>
    <ProxyingGateway Address="" Port="101"/>
    <Statistics MetricsTableWriteInterval="30s" PerfCounterWriteInterval="30s" LogWriteInterval="300s" WriteLogStatisticsToTable="true"/>
    <Tracing DefaultTraceLevel="Error" TraceToConsole="true" TraceToFile="trace.log">
    </Tracing>
  </Defaults>
</OrleansConfiguration>
```

* In the `OrleansConfiguration.xml` file properties, under "Copy to Output Directory", select either "Copy Always" or "Copy if Newer".
* Add the YAMS `AppConfig.json` file to the **OrleansHost** project. The content of the `AppConfig.json` is as follows:

```json
{
    "ExeName": "OrleansHost.exe",
    "ExeArgs": "${Id}_${Version.Major}.${Version.Minor}_${InstanceId} OrleansConfiguration.xml deploymentId=${Id}_${Version.Major}.${Version.Minor}_${DeploymentId}"
}
```

The `OrleansHost.exe` executable expects three arguments:
1. **The silo host name**: The name of the silo to run on the current role instance. In our case, the value of this argument will be resolved to `helloworld.orleans_1_0_currentRoleInstanceId`.
2. **Orleans configuration file name** (`OrleansConfiguration.xml` in this case).
3. **DeploymentId**: The Orleans deployment id for this application which must be the same across all role instances in the cloud service. In our case, the value of this argument will be resolved to `helloworld.orleans_1_0_MY_DEPLOYMENT_ID`. This deploymentId is used by Orleans to communicate between silos running on different role instances. It is also used by the client app (that we will describe later in this tutorial) to connect to the Orleans cluster.
* In the `AppConfig.json` file properties, under "Copy to Output Directory", select either "Copy Always" or "Copy if Newer".

Notice that we only used the major and minor versions (not the build version) in the silo name and the deploymentId. This will allow us to perform bug fixes to the silo and re-deploy it quickly using YAMS without affecting the clients of our app.

* Finally, build the project and make sure that `OrleansHost.exe`, `OrleansConfiguration.xml` and `AppConfig.json` are copied to the build output directory and have the correct content.

# Create the Orleans client application
Let's now create a Web app that connects to the Orleans silo, calls the `HelloGrain.SayHello()` and prints the returned message to the output.

Follow the [Deploy and Host an App in YAMS tutorial](Deploy&Host_an_App_in_YAMS.md) to create a Web app. To connect to the Orleans app from the web app, follow the steps below:

* Install `Microsoft.Orleans.Client` NuGet package to the Web app project.
* Install `Microsoft.Orleans.OrleansAzureUtils` NuGet package to the OrleansHost class library project.
* Add `OrleansClientConfiguration.xml` to the Web app project. The content of the `OrleansConfiguration.xml` file is as follows:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ClientConfiguration xmlns="urn:orleans">
  <SystemStore SystemStoreType="AzureTable" DeploymentId="hello.orleans_1.0_MY_DEPLOYMENT_ID" DataConnectionString="MY_DATA_CONNECTION_STRING"/>
  <Tracing DefaultTraceLevel="Error" TraceToConsole="true" TraceToFile="trace.log" WriteTraces="false">
    <TraceLevelOverride LogPrefix="Application" TraceLevel="Error" />
  </Tracing>
</ClientConfiguration>
```

It's **very important** that the `DeploymentId` and the `dataConnectionString` used above matches the ones that were used in the `helloworld.orleans` app.

* In the `OrleansClientConfiguration.xml` file properties, under "Copy to Output Directory", select either "Copy Always" or "Copy if Newer".
* Add the following to at the end of the `Main()` function (right before `Console.ReadLine()`):
```csharp
GrainClient.Initialize("OrleansClientConfiguration.xml");
```
* Reference the Orleans interfaces project from the **WebApp** project.
* Create an `OrleansHelloController` (see below) and add it to the **WebApp** project.

```csharp
namespace WebApp
{
    [RoutePrefix("orleans")]
    public class OrleansHelloController : ApiController
    {
        [HttpGet]
        [Route("hello")]
        public async Task<String> SayHello()
        {
            IHelloGrain helloGrain = GrainClient.GrainFactory.GetGrain<IHelloGrain>(0);
            return await helloGrain.SayHello();
        }
    }
}
```

* Add a `AppConfig.json` file to the **WebApp** project. The content of the `AppConfig.json` is as follows:

```json
{
    "ExeName": "WebApp.exe",
    "ExeArgs": "${Id} ${Version} ${DeploymentId}"
}
```

* In the `AppConfig.json` file properties, under "Copy to Output Directory", select either "Copy Always" or "Copy if Newer".
* Finally, build the project and make sure that `WebApp.exe`, `OrleansClientConfiguration.xml` and `AppConfig.json` are copied to the build output directory and have the correct content.

# Deploy the orleans app and the web app to YAMS
To deploy both apps to YAMS follow the steps below:
* Upload the build output of **OrleansHost** build output to the `applications/hello.orleans/1.0.0` blob directory.
* Upload the build output of **WebApp** build output to the `applications/hello.webapp/1.0.0` blob directory.
* Add the following content to the **DeploymentConfig.json** file:

```json
{
	"Applications":
	[
		{
			"Id": "hello.orleans",
			"Version": "1.0.0",
			"DeploymentIds": [ "MY_DEPLOYMENT_ID" ]
		},	
		{
			"Id": "hello.webapp",
			"Version": "1.0.0",
			"DeploymentIds": [ "MY_DEPLOYMENT_ID" ]
		},			
	]
}
```

Give YAMS some time to pick up the apps (typically less than a minute but in this case it'll probably take a bit longer because the Web app needs to connect to the Orleans silo) and run the following requests:

```
GET http://cloudservicename.cloudapp.net/hello.webapp/1.0/orleans/hello
```

You should get the output below which indicates that both the Web App and the Orleans App are running in YAMS.
```
"Hello World!"
```

# Source code
The source code associated with this tutorial can be found in the [Samples/OrleansApp](../Samples/OrleansApp) directory.