# YAMS Storage Tutorial

This tutorial demonstrates the use of the `IYamsStorageRepository` Api which can be used to fully control the Yams Storage. It provides capabilities such as downloading and uploading of application binaries, and the manipulation of the `DeploymentConfig.json` file. In fact, the Api completely decouples users from Blob storage and from the `DeploymentConfig.json` content and format.

* Create a proxy to the Yams storage
```csharp
    IYamsRepositoryFactory factory = new YamsRepositoryFactory();
    IYamsRepository yamsRepository = factory.CreateRepository("my_data_connection_string");
```

* Deploy a new application
```csharp
    // Upload the application binaries
	AppIdentity appIdentity = new AppIdentity("AppId", "1.0.0");
    await yamsRepository.UploadApplicationBinaries(appIdentity, localBinariesDirPath, FileMode.FailIfBinariesExist);

	// Update the DeploymentConfig. Note that the DeploymentConfig class is immutable
	DeploymentConfig deploymentConfig = await yamsRepository.FetchDeploymentConfig();
    deploymentConfig = deploymentConfig.AddApplication(appIdentity, "cloudservice_deployment_id");
	await yamsRepository.PublishDeploymentConfig(deploymentConfig);
```

* Update an application

```csharp
	AppIdentity oldAppIdentity = new AppIdentity("AppId", "1.0.0");
	AppIdentity newAppIdentity = new AppIdentity("AppId", "1.0.1");
	await yamsRepository.UploadApplicationBinaries(oldAppIdentity, localBinariesDirPath, FileMode.FailIfBinariesExist);

	// update the DeploymentConfig
	DeploymentConfig deploymentConfig = await yamsRepository.FetchDeploymentConfig();
    deploymentConfig = deploymentConfig.RemoveApplication(oldAppIdentity, "cloudservice_deployment_id");
	deploymentConfig = deploymentConfig.AddApplication(newAppIdentity, "cloudservice_deployment_id");
	await yamsRepository.PublishDeploymentConfig(deploymentConfig);

	// You can also cleanup the old binaries if you're not planing to revert back to it in the future.
    await yamsRepository.DeleteApplicationBinaries(oldAppIdentity);
```

* Remove an application
```csharp
	AppIdentity appIdentity = new AppIdentity("AppId", "1.0.0");
	await yamsRepository.UploadApplicationBinaries(oldAppIdentity, localBinariesDirPath, FileMode.FailIfBinariesExist);

	// update the DeploymentConfig
	DeploymentConfig deploymentConfig = await yamsRepository.FetchDeploymentConfig();
    deploymentConfig = deploymentConfig.RemoveApplication(oldAppIdentity, "cloudservice_deployment_id");
	deploymentConfig = deploymentConfig.AddApplication(newAppIdentity, "cloudservice_deployment_id");
	await yamsRepository.PublishDeploymentConfig(deploymentConfig);

	// You can also cleanup the old binaries if you're not planing to revert back to it in the future.
    await yamsRepository.DeleteApplicationBinaries(oldAppIdentity);
```

* Other DeploymentConfig Apis
```csharp
	// Get the list of apps Ids
	IEnumerable<string> appIds = deploymentConfig.ListApplications();

	// Get the list of applications deployed to a given yams cluster
	appIds = deploymentConfig.ListApplications("MyDeploymentId");

	// Get the list of versions of a given app
	IEnumerable<string> versions = deploymentConfig.ListVersions("MyAppId");

	// Get the list of versions of a given app that are deployed on a given Yams cluster
	versions = deploymentConfig.ListVersions("myAppId", "MyDeploymentId");

	// List the yams clusters where an app is deployed
	IEnumerable<string> deploymentIds = deploymentConfig.ListDeploymentIds("MyAppId");

	// List the yams clusters where a version of an app is deployed
	deploymentIds = deploymentConfig.ListDeploymentIds(new AppIdentity("MyAppId", "1.0.0"));

	// Add a application to the DeploymentConfig.json
	deploymentConfig.AddApplication(new AppIdentity("AppId", "2.0.0"), "DeploymentId");

	// Remove an application, a specific version or a specific deployment:
	deploymentConfig.RemoveApplication("MyAppId");
	deploymentConfig.RemoveApplication(new AppIdentity("MyAppId", "1.0.0"));
	deploymentConfig.RemoveApplication(new AppIdentity("MyAppId", "1.0.0"), "MyDeploymentId");
```