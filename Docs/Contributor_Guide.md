# YAMS Contributor Guide

## How to debug YAMS?

To debug YAMS code, simply use the `Etg.Yams.Host` console app available in the Etg.Yams solution (Make `Etg.Yams.Host` a startup project and hit `F5`). You can then easily make changes to YAMS code and run the console app.

The `Etg.Yams.Host` console app will start a YAMS cluster with the following properties:
* ClusterId = "TestClusterId"
* InstanceId = "instance_0"
* Update domain = "0"
* The current directory (e.g. `Etg.Yams.Host\bin\Debug`) is where binaries will be installed.
* The storage account used by default is the development storage (change it as needed but make sure to not check in your connection string!).

Don't forget to submit a pull request when you're done fixing an issue or adding a new feature!
 