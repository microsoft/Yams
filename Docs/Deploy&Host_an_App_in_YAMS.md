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

# Source code
The source code associated with this tutorial can be found in the [Samples/WebApp](../Samples/WebApp) directory.
