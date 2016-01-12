# YAMS Overview

The Azure **cloud services** platform allows developers to deploy and host highly available, reliable and scalable cloud applications in the cloud. The platform handles creating and managing VMs, load balancing, scaling (and auto-scaling), Virtual IP (VIP) swap and more.

Typically, an application is first deployed to a staging environment for testing. If testing is successful, the application is promoted to the production environment by performing a VIP swap. With each environment (e.g. staging and production) is associated at least one VM where the application will be running. For real world applications, several VMs are usually needed to handle traffic.

A microservices-based application is usually composed of a large number of microservices. Each microservice can be independently deployed, hosted, scaled and versioned. Microservices within the same application are loosely coupled and communicate with each other via HTTP / REST. As a result, microservices within the same application can also be developed in different programming languages.

To deploy such a Microservices-based application to Azure using the Azure cloud services platform, a cloud service is needed to deploy and host each microservice. Given that each cloud service requires at least one VM (usually more), the cost can quickly become unreasonable for a large number of microservices.

**YAMS** (Yet Another Microservices Solution) is a library that extends the Azure cloud services platform with the following capabilities:
* **Quick deployments** of Azure services (~1minute). 
* **Sharing infrastructure** (multiple microservices can be deployed to the same cloud service). 
* **Scaling microservices independently**.
* **Versioning** of microservices, quick **updates**, **reverts**, etc. 
* Support for **Azure Upgrade Domains** to minimize (and potentially eliminate) application downtime during updates.
* Microservices can be developed in **any programming language** and deployed with YAMS (as long as your service can be started with an exe).

YAMS relies on Azure **blob storage** to deploy applications. To deploy an application to a YAMS cluster, simply drop the binaries of the application in blob storage. The binaries are then picked-up by YAMS, deployed to all VMs in the cluster and then launched.

# How does it work?
Yams itself is deployed as a cloud service to Azure (we call it a Yams cluster). A Yams cluster is associated with a blob storage account where the binaries of microservices are deployed (note that *microservices* in Yams are often referred to as *applications* or simply *apps*). Yams periodically scans the blob storage for updates. The blob storage associated with a Yams cluster contains a storage container (named *applications*) where apps binaries are uploaded. 

## The storage container structure
The *applications* storage container has the following structure:

```
applications
|___ app1
|   |___ 1.0.0
|___ app2
|   |___ 1.0.0
|   |___ 1.0.1
|___ DeploymentConfig.json 
```

The `DeploymentConfig.json` file contains information about what application should be deployed and where. It has the following structure:
```
{
    "Applications":
    [
        {
            "Id": "app1",
            "Version": "1.0.0",
            "DeploymentIds": [ "DEPLOYMENT_ID" ]
        },  
        {
            "Id": "app2",
            "Version": "1.0.1",
            "DeploymentIds": [ "DEPLOYMENT_ID", "DEPLOYMENT_ID_OTHER" ]
        },
    ]
}
```

The `DeploymentIds` field identifies the Yams clusters where an application should be deployed (it is the same deployment id of the Azure cloud service where the Yams cluster is deployed). 

## Deploying Yams to a cloud service
Yams can be deployed to Azure like any typical cloud service. The [Deploy YAMS tutorial](Deploy_YAMS.md) explains the steps needed to deploy Yams to Azure.

## Scanning blob storage
When a Yams cluster is deployed to a cloud service, each instance in the cluster reads the `DeploymentConfig.json` file and deploy all apps that have the corresponding `DeploymentId` (the deployment id of the cloud service where the Yams cluster is deployed). Then, periodically, each Yams instance scans the `DeploymentConfig.json` file for changes and takes the appropriate actions. There are three types of changes that can occur:

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
* ${DeploymentId}: the cloud service deployment id.
* ${InstanceId}: the current VM instance id.

Note that Yams also support running multiple versions of the same app side-by-side. Please see the [Deploy and Host an App in YAMS tutorial](Deploy&Host_an_App_in_YAMS.md) to learn more about this feature.

### Removing an application
This occurs when an application or a version of an application is removed from the the `DeploymentConfig.json` file. Each Yams instance terminates the process associated with the application.

### Updating an application
This occurs when the version of an existing application has changed in the `DeploymentConfig.json` file (this includes upgrades and downgrades). Each Yams instance removes the old version of the application and then adds the new version.

Yams supports **Azure Upgrade Domains** to minimize (and potentially eliminate) application downtime during updates. In fact, each Yams instance (VM) in the Yams cluster is associated with an **upgrade domain** and only VMs with the same upgrade domain can be updated simultaneously. When a Yams instance attempts to update an application, it checks first if the application is being updated on another Yams instance with a different upgrade domain. If that's the case, the Yams instance discards the update and attempts again at the next blob storage scan; until it eventually performs the update.

Note that if an update fails, Yams will not try to revert back to the old version. However, Yams will keep trying to perform the update at every cycle (every time it checks for updates) and log errors if the installation fails.

## Sharing infrastructure
One of the main goals of Yams is sharing infrastructure to reduce cost. In fact, some microservices consume little resources and can be deployed alongside other microservices. In addition, sharing infrastructure reduces the cost of over-provisioning resources. To illustrate this, consider an application composed of two microservices. Each microservice requires 2 VMs at normal operation load and 4 VMs at peak time. If each microservice is deployed separately, 8 VMs are needed in total (4 VMs per microservice). However, in practice, the peak time resources are over estimated and the peak time of one microservice does not necessarily overlap with the peak time of another microservice. If the same VMs are shared by both microservices and peak times are not likely to overlap, 6 VMs can be sufficient for both microservices (which saves us 2 VMs). In fact, this strategy works better for a large number of microservices where the probability of all microservices peaking at the same time decreases with the number of microservices and as a result, sharing infrastructure can result in large savings.

Another use case where sharing infrastructure can result in large savings is in testing environments. In fact, without sharing infrastructure, at least one additional VM is needed for testing each microservice. Considering that testing (such as running integration and end to end tests) doesn't have high performance requirements and that microservices are not all tested at the same time, using a pool of shared VMs for testing can significantly reduce the number of required VMs.

To deploy multiple apps to the same Yams cluster, simply use the same deployment id as shown below:
```
{
    "Applications":
    [
        {
            "Id": "app1",
            "Version": "1.0.0",
            "DeploymentIds": [ "cluster_1_deploymentid" ]
        },  
        {
            "Id": "app2",
            "Version": "1.0.1",
            "DeploymentIds": [ "cluster_1_deploymentid" ]
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
            "DeploymentIds": [ "cluster_1_deploymentid" ]
        },  
        {
            "Id": "Microservice2",
            "Version": "1.0.1",
            "DeploymentIds": [ "cluster_2_deploymentid" ]
        },
        {
            "Id": "Microservice3",
            "Version": "2.0.0",
            "DeploymentIds": [ "cluster_2_deploymentid" ]
        },        
    ]
}
```

In the above case, CPU resources needed for `Microservice2` and `Microservice3` are scaled together (by scaling the number of VMs in cluster 2) while `Microservice1` is scaled independently (cluster 1). 

Note that it's not possible to scale microservices independently within the same cluster (via VM affinity or VM count) because the load balancer of the cluster (i.e. the corresponding Azure cloud-service) would not be aware of what microservices are deployed on what VMs. In fact, the load balancer will forward requests to any VM in the cluster which may or may not contain the appropriate microservice. As a result, using different clusters is currently the only way to scale microservices independently.

