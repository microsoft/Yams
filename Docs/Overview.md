# YAMS Overview

The [Microsoft Azure cloud services](https://azure.microsoft.com/en-us/services/cloud-services/) platform allows developers to deploy and host highly available, reliable and scalable microservices and applications in the cloud. The platform handles creating and managing VMs, load balancing, scaling (and auto-scaling), Virtual IP (VIP) swap and more.

Typically, an application is first deployed to a staging environment for testing. If testing is successful, the application is promoted to the production environment by performing a VIP swap. With each environment (e.g. staging and production) is associated at least one VM where the application will be running. For real world applications, several VMs are usually needed to handle traffic.

A microservices-based application is usually composed of a large number of microservices. Each microservice can be independently deployed, hosted, scaled and versioned. Microservices within the same application are loosely coupled and communicate with each other via HTTP / REST. As a result, microservices within the same application can also be developed in different programming languages.

To deploy such a Microservices-based application to Azure using the Azure cloud services platform, a cloud service is needed to deploy and host each microservice. Given that each cloud service requires at least one VM (usually more), the cost can quickly become unreasonable for a large number of microservices.

**YAMS** (Yet Another Microservices Solution) is a library that can be used to deploy and host microservices on premises, in Azure, or on other cloud service platforms. It offers the following features:
* **Quick deployments** of microservices to any target environment (~1 minute deployments to Azure). 
* **Sharing infrastructure** (multiple microservices can be deployed to the same on premises or cloud service). 
* **Scaling microservices independently**.
* **Versioning** of microservices, quick **updates**, **reverts**, etc. 
* Support for **Upgrade Domains** to minimize (and potentially eliminate) application downtime during updates, including first-class support for **Azure Upgrade Domains**.
* Microservices can be developed in **any programming language** and deployed with YAMS (as long as your service can be started with an exe).
* **Health monitoring** and **graceful shutdown** of microservices.

YAMS has first-class support for deploying applications from Azure **blob storage**, but with its pluggable storage architecture, other providers such as SQL Server or file storage can be created and plugged in as well. 

To deploy an application to a YAMS cluster, simply drop the binaries of the application into YAMS storage. The binaries are then picked-up by YAMS, deployed to all VMs in the cluster, and then launched.

# How does it work?
Yams itself is deployed either as a cloud service to Azure, on another cloud service platform, or on premises (we call it a Yams cluster). A Yams cluster is associated with a deployment storage (e.g. blob storage) where the binaries of microservices are deployed (note that *microservices* in Yams are often referred to as *applications* or simply *apps*). Yams periodically scans its deployment storage for updates. 

## The deployment storage structure
The *applications* in deployment storage can be organized with the following structure:

```
applications
|___ app1
|   |___ 1.0.0
|___ app2
|   |___ 1.0.0
|   |___ 1.0.1
|___ DeploymentConfig.json 
```

Depending on what kind of storage provider is being used, these could be folders in a blob- or file-system-based deployment storage provider, columns in a relational database table, etc.

The `DeploymentConfig.json` file contains information about what application should be deployed and where. It has the following structure:
```
{
    "Applications":
    [
        {
            "Id": "app1",
            "Version": "1.0.0",
            "TargetClusters": [ "YAMS_CLUSTER_ID" ]
        },  
        {
            "Id": "app2",
            "Version": "1.0.1",
            "TargetClusters": [ "YAMS_CLUSTER_ID", "YAMS_CLUSTER_ID_OTHER" ]
        },
    ]
}
```

The `TargetClusters` field identifies the Yams clusters where an application should be deployed.

## Deploying Yams to a cloud service
Yams can be deployed to Azure like any typical cloud service. The [Deploy YAMS tutorial](Deploy_YAMS.md) explains the steps needed to deploy Yams to Azure.

## Scanning blob storage
When a Yams cluster is deployed to a cloud service, each instance in the cluster reads the `DeploymentConfig.json` file and deploy all apps that have the corresponding `ClusterId` (typically the deployment id of the cloud service where the Yams cluster is deployed). Then, periodically, each Yams instance scans the `DeploymentConfig.json` file for changes and takes the appropriate actions. There are three types of changes that can occur:

1. **An application is added** 
2. **An application is removed**
3. **An application is updated**

### Adding an application
This occurs when a new application or a new version of an application is added to the `DeploymentConfig.json` file. Each Yams instance downloads the app's binaries to the VM where it's running and starts the application using the exe available with the binaries. In fact, each Yams application (i.e. microservice) contains an `AppConfig.json` file that describes how the application can be started. The `AppConfig.json` file has the following structure:
```
{
    "ExeName": "Foo.exe",
    "ExeArgs": "--appName=${Id} --arg2 10"
}
```

Notice the ${Id} symbol in the ExeArgs which will be substituted with the actual id of the application at runtime. Other available symbols are:

* ${Version}
* ${Version.Major}
* ${Version.Minor}
* ${Version.Build}
* ${ClusterId}: the Yams cluster id.
* ${InstanceId}: the current VM instance id.

Note that Yams also support running multiple versions of the same app side-by-side. Please see the [Deploy and Host an App in YAMS tutorial](Deploy&Host_an_App_in_YAMS.md) to learn more about this feature.

### Removing an application
This occurs when an application or a version of an application is removed from the the `DeploymentConfig.json` file. Each Yams instance terminates the process associated with the application.

### Updating an application
This occurs when the version of an existing application has changed in the `DeploymentConfig.json` file (this includes upgrades and downgrades). Each Yams instance removes the old version of the application and then adds the new version.

Yams supports **Azure Upgrade Domains** to minimize (and potentially eliminate) application downtime during updates. In fact, each Yams instance (VM) in the Yams cluster is associated with an **upgrade domain** and only VMs with the same upgrade domain can be updated simultaneously. When a Yams instance attempts to update an application, it checks first if the application is being updated on another Yams instance with a different upgrade domain. If that's the case, the Yams instance discards the update and attempts again at the next blob storage scan; until it eventually performs the update.

Note that if an update fails, Yams will not try to revert back to the old version. However, Yams will keep trying to perform the update at every cycle (every time it checks for updates) and will log errors if the installation fails. Yams uses `System.Diagnostics.Trace` to log errors which can be re-routed to blob storage or other locations by configuring the appropriate trace listener.

To revert a deployment, simply edit the `DeploymentConfig.json` file and replace the current version of the app (the version to be reverted) with the old version (the version to revert to).

## Health monitoring and graceful shutdown

Apps deployed with Yams can optionally enable health monitoring and/or graceful shutdown. Yams uses inter-process-communication (currently [named pipes](https://msdn.microsoft.com/en-us/library/bb546085(v=vs.110).aspx)) to communicate with apps. 

There are three features available:
* *Monitored initialization*: Yams waits for the app to finish initialization before considering it ready to receive requests. If an app takes longer than expected to finish initialization (the timeout is configurable), it's considered unhealthy and is killed.
* *Monitored heart beats*: With this feature enabled, Yams expects to receive heart beats from apps at steady intervals. If a heart beat is not received in time, an error is logged (more complex handling will be added in the future).
* *Graceful shutdown*: A event is sent to the app to signal shutdown. If the app does not exit gracefully in time (the timeout is configurable), the app will be closed or killed.

Note that each of these features can be enabled/disabled separately. In addition, apps running within the same cluster can choose to enable/disable different features.

The [Yams Client API](../src/Etg.Yams.Core/Client/IYamsClient.cs) can be used by apps to communicate with Yams. See [Deploy and host applications in YAMS tutorial](../Docs/Deploy&Host_an_App_in_YAMS.md) to learn how you can enable these features (one ore more features can be enabled at a time).

## Sharing infrastructure
One of the main goals of Yams is sharing infrastructure to reduce cost. In fact, some microservices consume little resources and can be deployed alongside other microservices. In addition, sharing infrastructure reduces the cost of over-provisioning resources. To illustrate this, consider an application composed of two microservices. Each microservice requires 2 VMs at normal operation load and 4 VMs at peak time. If each microservice is deployed separately, 8 VMs are needed in total (4 VMs per microservice). However, in practice, the peak time resources are over estimated and the peak time of one microservice does not necessarily overlap with the peak time of another microservice. If the same VMs are shared by both microservices and peak times are not likely to overlap, 6 VMs can be sufficient for both microservices (which saves us 2 VMs). In fact, this strategy works better for a large number of microservices where the probability of all microservices peaking at the same time decreases with the number of microservices and as a result, sharing infrastructure can result in large savings.

Another use case where sharing infrastructure can result in large savings is in testing environments. In fact, without sharing infrastructure, at least one additional VM is needed for testing each microservice. Considering that testing (such as running integration and end to end tests) doesn't have high performance requirements and that microservices are not all tested at the same time, using a pool of shared VMs for testing can significantly reduce the number of required VMs.

To deploy multiple apps to the same Yams cluster, simply use the same cluster id as shown below:
```
{
    "Applications":
    [
        {
            "Id": "app1",
            "Version": "1.0.0",
            "TargetClusters": [ "cluster_1_id" ]
        },  
        {
            "Id": "app2",
            "Version": "1.0.1",
            "TargetClusters": [ "cluster_1_id" ]
        },
    ]
}
```

### Scaling microservices independently
Even though sharing VMs can result in large cost savings, the number of microservices that can coexist on the same VM is limited due to memory constraints. In fact, if a pool of VMs is shared by several microservices, each VM must have a total amount of memory that is the sum of the amounts of memory required for each microservice. Obviously, this doesn't scale very well.

To address this issue, microservices can be deployed to different Yams clusters depending on the requirements of each microservice. In the following example, `Microservice1` requires a dedicated VM while `Microservice2` and `Microservice3` can be deployed to the same VM. 
```
{
    "Applications":
    [
        {
            "Id": "Microservice1",
            "Version": "1.0.0",
            "TargetClusters": [ "cluster_1_id" ]
        },  
        {
            "Id": "Microservice2",
            "Version": "1.0.1",
            "TargetClusters": [ "cluster_2_id" ]
        },
        {
            "Id": "Microservice3",
            "Version": "2.0.0",
            "TargetClusters": [ "cluster_2_id" ]
        },        
    ]
}
```

In the above case, CPU resources needed for `Microservice2` and `Microservice3` are scaled together (by scaling the number of VMs in cluster 2) while `Microservice1` is scaled independently (cluster 1). 

