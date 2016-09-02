# Overview
In this tutorial, we will explain how to create and deploy an Orleans application in YAMS. We will also explain how to connect to the Orleans application from a Web Api application.

# Prerequisites
You must deploy YAMS using the multiple cluster sample in order to deploy Orleans applications. One cluster will be used for the client and one cluster will be used for the silos. It's recommended that you read the [Deploy and Host an App in YAMS tutorial](Deploy&Host_an_App_in_YAMS.md) before reading this tutorial. 

# Create the Orleans application

Follow the *Hello World* tutorial described in Orleans [My First Orleans Application](http://dotnet.github.io/orleans/Step-by-step-Tutorials/My-First-Orleans-Application) to create a simple Orleans application that will be used in the rest of this tutorial.

Once you have the app ready, follow the steps below to deploy the app in YAMS:

* Go to the **SiloHost** console application project and remove any client related code from the `Program.cs` file.
* Open the `Program.cs` file and add the following code to configure the silo:

```csharp
        private static int StartSilo(string[] args)
        {
            // define the cluster configuration
            var config = new ClusterConfiguration();
            config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.AzureTable;
            config.Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.AzureTable;
            config.Globals.DataConnectionString = "MY_DATA_CONNECTION_STRING";
            config.AddMemoryStorageProvider();
            config.AddAzureTableStorageProvider("AzureStore");
            config.Defaults.DefaultTraceLevel = Severity.Error;
            config.Defaults.Port = 100;
            config.Defaults.ProxyGatewayEndpoint = new IPEndPoint(config.Defaults.Endpoint.Address, 101);

            hostWrapper = new OrleansHostWrapper(config, args);
            return hostWrapper.Run();
        }
```

* Add the YAMS `AppConfig.json` file to the **SiloHost** project. The content of the `AppConfig.json` file is as follows:

```json
{
    "ExeName": "SiloHost.exe",
    "ExeArgs": "deploymentid=${Id}_${Version.Major}.${Version.Minor}_${DeploymentId}"
}
```

The argument for the `SiloHost.exe` executable is the Orleans deployment id for this application which must be the same across all role instances in the cloud service. In our case, the value of this argument will be resolved to `helloworld.orleans_1_0_MY_YAMS_BACKEND_CLUSTER_ID` and `MY_YAMS_BACKEND_CLUSTER_ID` will be a combination of the Cloud Service's deployment id and the Worker Role name. This deploymentId is used by Orleans to communicate between silos running on different role instances. It is also used by the client app (that we will describe later in this tutorial) to connect to the Orleans cluster.

* In the `AppConfig.json` file properties, under "Copy to Output Directory", select either "Copy Always" or "Copy if Newer".

Notice that we only used the major and minor versions (not the build version) in the silo name and the deploymentId. This will allow us to perform bug fixes to the silo and re-deploy it quickly using YAMS without affecting the clients of our app.

* Finally, build the project and make sure that `SiloHost.exe` and `AppConfig.json` are copied to the build output directory and have the correct content.

# Create the Orleans client application
Let's now create a Web app that connects to the Orleans silo, calls the `HelloGrain.SayHello()` and prints the returned message to the output.

Follow the [Deploy and Host an App in YAMS tutorial](Deploy&Host_an_App_in_YAMS.md) to create a Web app. To connect to the Orleans app from the web app, follow the steps below:

* Install `Microsoft.Orleans.Client` NuGet package to the Web app project.
* Install `Microsoft.Orleans.OrleansAzureUtils` NuGet package to the `SiloHost` console application project.
* Add the following at the end of the `Main()` function (right before `Console.ReadLine()`):
```csharp
            var config = new ClientConfiguration();
            config.GatewayProvider = ClientConfiguration.GatewayProviderType.AzureTable;
            config.DeploymentId = "hello.orleans_1.0_MY_YAMS_BACKEND_CLUSTER_ID";
            config.DataConnectionString = "MY_DATA_CONNECTION_STRING";
            config.DefaultTraceLevel = Severity.Error;

            // Attempt to connect a few times to overcome transient failures and to give the silo enough 
            // time to start up when starting at the same time as the client (useful when deploying or during development).

            const int initializeAttemptsBeforeFailing = 5;

            int attempt = 0;
            while (true)
            {
                try
                {
                    GrainClient.Initialize(config);
                    Console.WriteLine("Client initialized");
                    break;
                }
                catch (SiloUnavailableException e)
                {
                    attempt++;
                    if (attempt >= initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            }
```

It's **very important** that the `DeploymentId` and the `DataConnectionString` used above matches the ones that were used in the `helloworld.orleans` app.

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
        public async Task<string> SayHello()
        {
            var helloGrain = GrainClient.GrainFactory.GetGrain<IHelloGrain>(0);
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
* Finally, build the project and make sure that `WebApp.exe` and `AppConfig.json` are copied to the build output directory and have the correct content.

# Deploy the Orleans app and the web app to YAMS
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
			"DeploymentIds": [ "MY_YAMS_BACKEND_CLUSTER_ID" ]
		},	
		{
			"Id": "hello.webapp",
			"Version": "1.0.0",
			"DeploymentIds": [ "MY_YAMS_FRONTEND_CLUSTER_ID" ]
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
