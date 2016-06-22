YAMS
=======

[![Join the chat at https://gitter.im/Microsoft/Yams](https://badges.gitter.im/Microsoft/Yams.svg)](https://gitter.im/Microsoft/Yams?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

**YAMS** (Yet Another Microservices Solution) is a library that can be used to deploy and host microservices in Azure. It offers the following features:
* **Quick deployments** of Azure services (~1minute). 
* **Sharing infrastructure** (multiple microservices can be deployed to the same cloud service). 
* **Versioning** of microservices, quick **updates**, **reverts**, etc. 
* Support for **Azure Upgrade Domains** to minimize (and potentially eliminate) application downtime during updates.
* Microservices can be developed in **any programming language** and deployed with YAMS (as long as your service can be started with an exe).

YAMS relies on Azure **blob storage** to deploy applications. To deploy an application to a YAMS cluster, simply drop the binaries of the application in blob storage. The binaries are then picked-up by YAMS, deployed to all VMs in the cluster and then launched.

Please read the documentation below for more information.

Documentation 
=======
* [Yams Overview](Docs/Overview.md).
* [Deploy YAMS to your cloud service](Docs/Deploy_YAMS.md).
* [Deploy and host applications in YAMS](Docs/Deploy&Host_an_App_in_YAMS.md)
* [Deploy and host Orleans applications in YAMS](Docs/Deploy_Orleans_App_in_YAMS.md).

Contribute!
=======
We welcome contributions of all sorts including pull requests, suggestions, documentation, etc. Please feel free to open an issue to discuss any matter.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

License
=======
This project is licensed under the [MIT license](LICENSE).
