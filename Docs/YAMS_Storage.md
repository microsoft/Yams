# YAMS Storage Tutorial

This tutorial demonstrates the use of the `IDeploymentRepository` Api which can be used to fully control the Yams Storage. It provides capabilities such as downloading and uploading of application binaries, and the manipulation of the `DeploymentConfig.json` file. In fact, the Api completely decouples users from Blob storage and from the `DeploymentConfig.json` content and format.

* Create a proxy to the Yams storage
```csharp
    IDeploymentRepository deploymentRepository = BlobStorageDeploymentRepository.Create("my_data_connection_string");
```

* Fetch the DeploymentConfig
```csharp
	DeploymentConfig deploymentConfig = await deploymentRepository.FetchDeploymentConfig();
	
	// DeploymentConfig implements IEnumerable.
	foreach(AppDeploymentConfig appDeploymentConfig in deploymentConfig)
	{
		AppIdentity appIdentity = appDeploymentConfig.AppIdentity;
		IEnumerable<string> targetClusters = appDeploymentConfig.TargetClusters;
	}	
```

* Deploy a new application
```csharp
	AppIdentity appIdentity = new AppIdentity("AppId", "1.0.0");

	// Upload the application binaries
	await deploymentRepository.UploadApplicationBinaries(appIdentity, localBinariesDirPath, ConflictResolutionMode.FailIfBinariesExist);

	// Update the DeploymentConfig. Note that the DeploymentConfig class is immutable
	DeploymentConfig deploymentConfig = await deploymentRepository.FetchDeploymentConfig();
	deploymentConfig = deploymentConfig.AddApplication(appIdentity, "yams_cluster_id");

	// The application will only be deployed to the cluster when the DeploymentConfig is published
	await deploymentRepository.PublishDeploymentConfig(deploymentConfig);
```

* Update an application

```csharp
	// Upload the new binaries
	await deploymentRepository.UploadApplicationBinaries(newAppIdentity, localBinariesDirPath, ConflictResolutionMode.FailIfBinariesExist);

	// Fetch and update the DeploymentConfig
	DeploymentConfig deploymentConfig = await deploymentRepository.FetchDeploymentConfig();
	deploymentConfig = deploymentConfig.RemoveApplication(oldAppIdentity, "yams_cluster_id");
	deploymentConfig = deploymentConfig.AddApplication(newAppIdentity, "yams_cluster_id");

	// The update will be performed when the new DeploymentConfig is published
	await deploymentRepository.PublishDeploymentConfig(deploymentConfig);

	// You can also cleanup the old binaries if you're not planing to revert back to it in the future.
	await deploymentRepository.DeleteApplicationBinaries(oldAppIdentity);
```

* Remove an application
```csharp
    // Update the DeploymentConfig
    DeploymentConfig deploymentConfig = await deploymentRepository.FetchDeploymentConfig();

    // Remove the app from the DeploymentConfig
    deploymentConfig = deploymentConfig.RemoveApplication(appIdentity, "yams_cluster_id");

    // The app will be shutdown when the DeploymentConfig is published
    await deploymentRepository.PublishDeploymentConfig(deploymentConfig);

    // You can also cleanup the old binaries if you're not planing to revert back to it in the future.
    await deploymentRepository.DeleteApplicationBinaries(appIdentity);
```

* Other DeploymentConfig Apis
```csharp
    // Get the list of apps Ids
    IEnumerable<string> appIds = deploymentConfig.ListApplications();

    // Get the list of applications deployed to a given yams cluster
    appIds = deploymentConfig.ListApplications("yams_cluster_id");

    // Get the list of versions of a given app
    IEnumerable<string> versions = deploymentConfig.ListVersions("MyAppId");

    // Get the list of versions of a given app that are deployed on a given Yams cluster
    versions = deploymentConfig.ListVersions("myAppId", "yams_cluster_id");

    // List the yams clusters where an app is deployed
    IEnumerable<string> clusterIds = deploymentConfig.ListClusters("MyAppId");

    // List the yams clusters where a version of an app is deployed
    clusterIds = deploymentConfig.ListClusters(new AppIdentity("MyAppId", "1.0.0"));

    // Add a application to the DeploymentConfig.json
    deploymentConfig.AddApplication(new AppIdentity("AppId", "2.0.0"), "yams_cluster_id");

    // Remove an application, a specific version or a specific cluster:
    deploymentConfig.RemoveApplication("MyAppId");
    deploymentConfig.RemoveApplication(new AppIdentity("MyAppId", "1.0.0"));
    deploymentConfig.RemoveApplication(new AppIdentity("MyAppId", "1.0.0"), "yams_cluster_id");
```
