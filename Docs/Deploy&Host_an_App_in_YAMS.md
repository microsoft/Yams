# Overview

In this tutorial, we will create a simple application and deploy it to YAMS. We will learn the following:
* How to deploy an app for the first time.
* How to deploy multiple apps.
* How to update an app (to perform bug fixes or upgrades).
* How to deploy two versions of the same app side-by-side.
* How to remove or revert a deployment.

# Create an App
Let's create a simple web Api that we will later deploy to YAMS (**note** that any **exe** can be deployed to YAMS but we chose a Web Api in this example because we can interact with it). To deploy an app to YAMS, the app must contain an **exe** that can be used to start the app. Follow the steps below to create a web Api that can be started with an **exe**.

* Create a new *Console Application* project in visual studio and name it *WebApp*.
* Install the NuGet package `Microsoft.AspNet.WebApi.OwinSelfHost` to the *WebApp* project.
* Rename the `Program.cs` file to `App.cs` and add the following content to it:

```csharp
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace WebApp
{
    public class App
    {
        public static string Id;
        public static string Version;
        public static string ClusterId;

        static void Main(string[] args)
        {
            Id = args[0];
            Version = args[1];
            ClusterId = args[2];

            string baseUrl = string.Format("http://{0}/{1}/", GetIpAddress(), Id);
            Console.WriteLine("Url is: " + baseUrl);

            // Start OWIN host 
            Microsoft.Owin.Hosting.WebApp.Start<Startup>(url: baseUrl);
            Console.WriteLine("WebApp has been started successfully");
            Console.ReadLine(); 

        }

        private static string GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString();
        }
    }
}
```

* Create a class called `Startup` and add the following to it:

```csharp
using System;
using System.Web.Http;
using Owin;

namespace WebApp
{
    class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.EnsureInitialized();
            appBuilder.UseWebApi(config);
        } 
    }
}
```

* Create an `ApplicationController` class and add the following to it:

```csharp
using System.Web.Http;
using Newtonsoft.Json.Linq;

namespace WebApp
{
    [RoutePrefix("application")]
    public class ApplicationController : ApiController
    {
        [Route("info")]
        public JObject GetInfo()
        {
            string json = string.Format(@"
                {{
                    'Id': '{0}',
                    'Version': '{1}',
                    'Yams Cluster Id': '{2}'
                }}
                ", App.Id, App.Version, App.ClusterId);

            return JObject.Parse(json);
        }
    }
}
```

So far, we have created a simple web Api application. The app exposes an Api that allows one to obtain basic information such as the *id* and the *version* of the app.

Before deploying an application in YAMS, we need to add a YAMS specific configuration file to the root of the application.
* Add an `AppConfig.json` file to the root of the WebApp project.
* In the `AppConfig.json` file properties, under "Copy to Output Directory", select either "Copy Always" or "Copy if Newer".
* Add the following to the `AppConfig.json` file:

```json
{
    "ExeName": "WebApp.exe",
    "ExeArgs": "${Id} ${Version} ${ClusterId}"
}
```

Notice the use of `${symbol}` in the config file. The values of these symbols will be resolved by YAMS at runtime. The set of all supported symbols is available [here](Deploy_YAMS.md). In this case, we are passing the *id*, *version* and *cloud service deployment id* to the *WebApp.exe* which is expecting these arguments (see the `Main` function of the app).

Finally, build the app and make sure that the exe has been created and that the `AppConfig.json` file has been copied to the output.

# Deploy the App
YAMS relies on blob storage to download and deploy the binaries of an application. In fact, a YAMS cluster is associated with a storage account where all applications binaries are uploaded. See the [Deploy YAMS](Deploy_YAMS.md) tutorial to learn more on how to deploy a YAMS cluster.

To deploy **WebApp** to YAMS, follow the steps below:
* In the `applications` container (at the root of the YAMS blob storage), create `WebApp/1.0.0` directory.
* Upload the output of the *WebApp* project to the `applications/WebApp/1.0.0` blob directory.
* Tell YAMS to download and deploy the app to the cloud service by adding a corresponding entry in the `DeploymentConfig.json` file located at the root of the `applications` folder:

```json
{
	"Applications":
	[
		{
			"Id": "WebApp",
			"Version": "1.0.0",
			"TargetClusters": [ "MY_CLUSTER_ID" ]
		}
	]
}
```

The application should be up and running in a matter of seconds. To test it, use a web Api client (such as Postman) and run the following http request:

```
GET http://cloudservicename.cloudapp.net/WebApp/application/info
```

The result should look like the following:

```json
{
  "Id": "WebApp",
  "Version": "1.0.0",
  "Yams Cluster Id": "MY_CLUSTER_ID"
}
```

Similarly, you can deploy as many apps as you want by adding the corresponding binaries in the blob storage and the associated entries in the `DeploymentConfig.json` file.

# Update a running app
Let's update the web app and add a new coin flip Api to it. To do so:
* Create a new `CoinFlipController`:

```csharp
using System;
using System.Web.Http;

namespace WebApp
{
    [RoutePrefix("coinflip")]
    public class CoinFlipController : ApiController
    {
        private static readonly Random Random = new Random();

        [HttpGet]
        [Route("next")]
        public string Run()
        {
            if (Random.Next(2) == 0)
            {
                return "Heads";
            }
            return "Tails";
        }
    }
}
```

* Build the project.
* Upload the output of the build to the `applications/WebApp/1.1.0` blob directory.
* Update the version number in the `DeploymentConfig.json` file to `1.1.0` (this step will trigger the update).

The new version of **WebApp** should be running in YAMS in less than a minute. To test it, run the following http requests:

```
GET http://cloudservicename.net/WebApp/application/info
GET http://cloudservicename.net/WebApp/coinflip/next
```

# Running multiple versions of the same app side-by-side

YAMS supports running multiple versions of the same app side-by-side. However, the app must be designed in a way that supports running multiple versions side-by-side. For instance, our current **WebApp** has not been designed with that in mind. In fact, if we try to run two versions of **WebApp** side-by-side, they will both try to use the same **url** and one of them will fail.

To fix this problem, let's add the **version** of the app to the url. To do so, go to the `Main` function and replace

```csharp
string baseUrl = string.Format("http://{0}/{1}/", GetIpAddress(), Id);
```

with
```csharp
            Version version = new Version(Version);
            string apiVersion = string.Format("{0}.{1}", version.Major, version.Minor);
            string baseUrl = string.Format("http://{0}/{1}/{2}", GetIpAddress(), Id, apiVersion);
```

To deploy the new version (let's say `2.0.0`) side-by-side with the currently running version (`1.1.0`), create and upload the `2.0.0` version as described in the previous section and add it to the `DeploymentConfig.json` file as follows:

```json
{
	"Applications":
	[
		{
			"Id": "WebApp",
			"Version": "1.1.0",
			"TargetClusters": [ "MY_CLUSTER_ID" ]
		},
		{
			"Id": "WebApp",
			"Version": "2.0.0",
			"TargetClusters": [ "MY_CLUSTER_ID" ]
		}		
	]
}
```

That's it! The two versions should be now happily running side-by-side.

# Removing or reverting a deployment
To remove an app from YAMS, simply remove the corresponding entry from the `DeploymentConfig.json` file. YAMS will terminate the corresponding process and remove the app. You can also delete the associated files from the blob storage if you are never going to use this app again or keep it there to preserve history of deployments and to allow reverts. In fact, to revert a deployment in YAMS, simply edit the `DeploymentConfig.json` file and replace the current version of the app (the version to be reverted) with the old version (the version to revert to).

# Advanced features

Yams has support for health monitoring and graceful shutdown of apps as described [here](../Docs/Overview.md#health-monitoring-and-graceful-shutdown). Note that you can choose to enable one or multiple features and apps within the same cluster can use different features. 

## Monitored initialization
By default, Yams does not monitor the initialization of apps. In other words, when an app is deployed, the associated process is launched and then Yams assumes that the app is running and ready to receive requests. With the monitored initialization feature enabled, Yams would wait for the app to finish initialization before moving on to the next app (the app would notify Yams that it's done initializing through an IPC message).

To enable *monitored initialization* for a given app, the corresponding flag must be added to the `AppConfig.json` file as shown below:
```json
{
  "ExeName": "MyProcess.exe",
  "ExeArgs": "Foo Bar",
  "MonitorInitialization": true
}
```

The app source code will also need to be updated so that the app can communicate with Yams (using IPC). Install the `Etg.Yams` NuGet package and modify the app source code so that Yams is notified when initialization is done, as shown in the code below:

```csharp
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            var yamsClientConfig = new YamsClientConfigBuilder(args).Build();
            var yamsClientFactory = new YamsClientFactory();
            IYamsClient yamsClient = yamsClientFactory.CreateYamsClient(yamsClientConfig);

            await Task.WhenAll(yamsClient.Connect(), Initialize());

            await yamsClient.SendInitializationDoneMessage();
	    
	    // ...
```

## Heart beats
With this feature enabled, the app is expected to send heart beat messages to Yams at steady intervals. If heart beats are not received in time, errors will be logged (more complex handling will be added in the future). To enable this feature, update the `AppConfig.json` and your app source code as shown below:

```json
{
  "ExeName": "MyProcess.exe",
  "ExeArgs": "Foo Bar",
  "MonitorHealth":  true
}
```

```csharp
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            var yamsClientConfig = new YamsClientConfigBuilder(args).Build();
            var yamsClientFactory = new YamsClientFactory();
            IYamsClient yamsClient = yamsClientFactory.CreateYamsClient(yamsClientConfig);

            await Task.WhenAll(yamsClient.Connect(), Initialize());

            while (true)
            {
                await Task.Delay(heartBeatPeriod);
                await yamsClient.SendHeartBeat();
            }
```

## Graceful Shutdown

When graceful shutdown is enabled for a given app, Yams will deliver an event to the app and allow it a configurable amount of time to exit gracefully before closing/killing it. The graceful shutdown event will be delivered through the `YamsClient` as a normal C# event. To enable this feature, update the `AppConfig.json` and your app source code as shown below:

```json
{
  "ExeName": "MyProcess.exe",
  "ExeArgs": "Foo Bar",
  "GracefulShutdown":  true
}
```

```csharp
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            var yamsClientConfig = new YamsClientConfigBuilder(args).Build();
            var yamsClientFactory = new YamsClientFactory();
            IYamsClient yamsClient = yamsClientFactory.CreateYamsClient(yamsClientConfig);

            await Task.WhenAll(yamsClient.Connect(), Initialize());

            bool exitMessageReceived = false;
            yamsClient.ExitMessageReceived += (sender, eventArgs) =>
            {
                exitMessageReceived = true;
            };
	    
            while (!exitMessageReceived)
            {
                await DoWork();                
            }	    
```

# Source code
The source code associated with this tutorial can be found in the [Samples/WebApp](../Samples/WebApp) directory.
