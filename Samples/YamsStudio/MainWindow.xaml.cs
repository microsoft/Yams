using Etg.Yams.Application;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Config;
using Etg.Yams.Utils;

namespace YamsStudio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string SettingsFilePath = "settings.json";
        private const string DeploymentsConfigsDirPath = "DeploymentsConfigs";
        private readonly List<StorageAccountConnectionInfo> _storageAccountConnections = new List<StorageAccountConnectionInfo>();
        private readonly DeploymentRepositoryManager _deploymentRepositoryManager;
        private DeploymentConfig _deploymentConfig;
        private readonly FileSystemWatcher _deploymentConfigFileWatcher;
        public MainWindow()
        {
            InitializeComponent();
            ConnectionsListView.ItemsSource = _storageAccountConnections;
            IDeploymentRepositoryFactory deploymentRepositoryFactory = new DeploymentRepositoryFactory();
            _deploymentRepositoryManager = new DeploymentRepositoryManager(deploymentRepositoryFactory);
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                _storageAccountConnections = JsonUtils.Deserialize<List<StorageAccountConnectionInfo>>(json);
                ConnectionsListView.ItemsSource = _storageAccountConnections;
            }

            if (!Directory.Exists(DeploymentsConfigsDirPath))
            {
                Directory.CreateDirectory(DeploymentsConfigsDirPath);
            }

	        _deploymentConfigFileWatcher = new FileSystemWatcher
	        {
		        Path = Path.GetFullPath(DeploymentsConfigsDirPath),
		        NotifyFilter = NotifyFilters.LastWrite,
		        Filter = "*.json"
	        };
	        _deploymentConfigFileWatcher.Changed += OnDeploymentConfigFileChanged;
            _deploymentConfigFileWatcher.EnableRaisingEvents = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            string json = JsonUtils.Serialize(_storageAccountConnections);
            File.WriteAllText(SettingsFilePath, json);
            _deploymentConfigFileWatcher.Dispose();
            base.OnClosing(e);
        }

        private void OnAddNewConnection(object sender, RoutedEventArgs e)
        {
            ConnectToStorageAccountDialog inputDialog = new ConnectToStorageAccountDialog();
            if (inputDialog.ShowDialog() == true)
            {
                StorageAccountConnectionInfo connection = new StorageAccountConnectionInfo(inputDialog.AccountName, inputDialog.DataConnectionString);
                _storageAccountConnections.Add(connection);
                RefreshView(_storageAccountConnections);
            }
        }

        private void OnDeleteConnection(object sender, RoutedEventArgs e)
        {
            StorageAccountConnectionInfo connectionInfo = (StorageAccountConnectionInfo)ConnectionsListView.SelectedItem;
            MessageBoxResult res = MessageBox.Show("The connection will be removed\n Do you want to continue", "Remove Connection", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
            {
                _storageAccountConnections.Remove(connectionInfo);
                RefreshView(_storageAccountConnections);
            }
        }

        private async void OnAddApplication(object sender, RoutedEventArgs e)
        {
            StorageAccountConnectionInfo connectionInfo = GetCurrentConnection();
            AddNewApplicationDialog dialog = new AddNewApplicationDialog();
            if (dialog.ShowDialog() == true)
            {
                AppIdentity appIdentity = new AppIdentity(dialog.ApplicationName, new Version(dialog.Version));
                await AddApplication(appIdentity, dialog.DeploymentId, dialog.BinariesPath);
            }
        }

        private async Task AddApplication(AppIdentity appIdentity, string deploymentId, string binariesPath)
        {
            StorageAccountConnectionInfo connectionInfo = GetCurrentConnection();
            IDeploymentRepository repository = _deploymentRepositoryManager.GetRepository(connectionInfo);
            BusyWindow busyWindow = new BusyWindow{Message = "Please wait..\n\n" + "The binaries are being uploaded to blob storage"};
            busyWindow.Show();
            await repository.UploadApplicationBinaries(appIdentity, binariesPath, ConflictResolutionMode.DoNothingIfBinariesExist);
            busyWindow.Close();
            _deploymentConfig = _deploymentConfig.AddApplication(appIdentity, deploymentId);
            SaveLocalDeploymentConfig(connectionInfo);
        }

        private async Task RefreshAll()
        {
            await HandleConnectionSelection();
            HandleAppSelection();
            HandleVersionSelection();
        }

        private void SaveLocalDeploymentConfig(StorageAccountConnectionInfo connectionInfo)
        {
            string json = _deploymentConfig.RawData();
            SaveLocalDeploymentConfig(connectionInfo, json);
        }

        private void SaveLocalDeploymentConfig(StorageAccountConnectionInfo connectionInfo, string json)
        {
            string localDeploymentConfigPath = GetDeploymentConfigLocalPath(connectionInfo.AccountName);
            File.WriteAllText(localDeploymentConfigPath, json);
        }

        private StorageAccountConnectionInfo GetCurrentConnection()
        {
            return (StorageAccountConnectionInfo)ConnectionsListView.SelectedItem;
        }

        private async void OnRemoveApplication(object sender, RoutedEventArgs e)
        {
            StorageAccountConnectionInfo connectionInfo = GetCurrentConnection();
            string appId = GetSelectedAppId();
			_deploymentConfig = _deploymentConfig.RemoveApplication(appId);
            SaveLocalDeploymentConfig(connectionInfo);
            await HandleConnectionSelection();
        }

        private async void OnAddNewVersion(object sender, RoutedEventArgs e)
        {
            StorageAccountConnectionInfo connectionInfo = GetCurrentConnection();
            string appId = GetSelectedAppId();
            AddNewApplicationDialog dialog = new AddNewApplicationDialog(appId);
            if (dialog.ShowDialog() == true)
            {
                AppIdentity appIdentity = new AppIdentity(dialog.ApplicationName, new Version(dialog.Version));
                await AddApplication(appIdentity, dialog.DeploymentId, dialog.BinariesPath);
            }
        }

        private void OnVersionAddDeployment(object sender, RoutedEventArgs e)
        {
            StorageAccountConnectionInfo connectionInfo = GetCurrentConnection();
            string appId = GetSelectedAppId();
            string version = GetSelectedVersion();
            AddNewDeploymentDialog dialog = new AddNewDeploymentDialog(appId, version);
            if (dialog.ShowDialog() == true)
            {
                AppIdentity appIdentity = new AppIdentity(appId, new Version(version));
                _deploymentConfig = _deploymentConfig.AddApplication(appIdentity, dialog.DeploymentId);
                SaveLocalDeploymentConfig(connectionInfo);
            }
        }

        private void OnUpdateVersion(object sender, RoutedEventArgs e)
        {
            StorageAccountConnectionInfo connectionInfo = GetCurrentConnection();
            string appId = GetSelectedAppId();
            string version = GetSelectedVersion();
	        AppIdentity appIdentity = new AppIdentity(appId, version);
			IEnumerable<string> availableDeploymentIds = _deploymentConfig.ListDeploymentIds(appIdentity);
            UpdateVersionDialog dialog = new UpdateVersionDialog(appId, version, availableDeploymentIds);
            if (dialog.ShowDialog() == true)
            {
                string newVersion = dialog.NewVersion;
                IEnumerable<string> selectedDeploymentIds = dialog.SelectedDeploymentIds;
                foreach (string deploymentId in selectedDeploymentIds)
                {
					_deploymentConfig = _deploymentConfig.RemoveApplication(appIdentity, deploymentId);
					_deploymentConfig = _deploymentConfig.AddApplication(appIdentity, deploymentId);
                }

                SaveLocalDeploymentConfig(connectionInfo);
            }
        }

        private void OnRemoveVersion(object sender, RoutedEventArgs e)
        {
            StorageAccountConnectionInfo connectionInfo = GetCurrentConnection();
            string appId = GetSelectedAppId();
            if (appId == null)
            {
                return;
            }

            string version = GetSelectedVersion();
            if (version == null)
            {
                return;
            }

			_deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity(appId, version));
            SaveLocalDeploymentConfig(connectionInfo);
        }

        private void OnRemoveDeployment(object sender, RoutedEventArgs e)
        {
            StorageAccountConnectionInfo connectionInfo = GetCurrentConnection();
            DeploymentInfo deploymendInfo = deploymentIdsListView.SelectedItem as DeploymentInfo;
            if (deploymendInfo == null)
            {
                return;
            }

			_deploymentConfig = _deploymentConfig.RemoveApplication(deploymendInfo.AppIdentity, deploymendInfo.DeploymentId);
            SaveLocalDeploymentConfig(connectionInfo);
        }

        private string GetSelectedVersion()
        {
            return ((AppIdentity)VersionsListView.SelectedItem)?.Version.ToString();
        }

        private async void OnPublishToBlob(object sender, RoutedEventArgs e)
        {
            BusyWindow busyWindow = new BusyWindow{Message = "Please wait..\n\n" + "The DeploymentConfig.json file is being uploaded to blob storage"};
            busyWindow.Show();
            StorageAccountConnectionInfo connectionInfo = GetCurrentConnection();
            IDeploymentRepository connection = _deploymentRepositoryManager.GetRepository(connectionInfo);
            await connection.PublishDeploymentConfig(_deploymentConfig);
            busyWindow.Close();
        }

        private async void OnSyncFromBlob(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("This will ovewrite any local changes\n\n Are you sure you want to continue?",
                "Sync From Blob", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
            {
                StorageAccountConnectionInfo connectionInfo = GetCurrentConnection();
                IDeploymentRepository connection = _deploymentRepositoryManager.GetRepository(connectionInfo);
                DeploymentConfig deploymentConfig = await connection.FetchDeploymentConfig();
                SaveLocalDeploymentConfig(connectionInfo, deploymentConfig.RawData());
            }
        }

        private void OnEditDeploymentConfig(object sender, RoutedEventArgs e)
        {
	        string path = GetDeploymentConfigLocalPath(GetCurrentConnection().AccountName);
			Process.Start(path);
        }

        private void RefreshView(object itemsSource)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(itemsSource);
            view.Refresh();
        }

        private async void OnConnectionSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                await HandleConnectionSelection();
            }
        }

        private async Task HandleConnectionSelection()
        {
            StorageAccountConnectionInfo connectionInfo = GetCurrentConnection();
            IEnumerable<string> appIds = new List<string>();
            try
            {
                _deploymentConfig = await FetchDeploymentConfig(connectionInfo);
				appIds = _deploymentConfig.ListApplications();
            }
            catch (StorageException ex)
            {
                Debug.WriteLine("Failed to fetch the DeploymentConfig file from account " + connectionInfo.AccountName + " Exception: " + ex);
            }

            AppsListView.ItemsSource = appIds;
            RefreshView(appIds);
        }

        private async void OnDeploymentConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            await Dispatcher.Invoke(RefreshAll);
        }

        private async Task<DeploymentConfig> FetchDeploymentConfig(StorageAccountConnectionInfo connectionInfo)
        {
            string path = GetDeploymentConfigLocalPath(connectionInfo.AccountName);
            if (File.Exists(path))
            {
                return new DeploymentConfig(File.ReadAllText(path));
            }
	        IDeploymentRepository connection = _deploymentRepositoryManager.GetRepository(connectionInfo);
	        DeploymentConfig deploymentConfig = await connection.FetchDeploymentConfig();
			SaveLocalDeploymentConfig(connectionInfo, deploymentConfig.RawData());
	        return deploymentConfig;
        }

        private string GetDeploymentConfigLocalPath(string accountName)
        {
            return Path.Combine(DeploymentsConfigsDirPath + Path.DirectorySeparatorChar, accountName + "_DeploymentConfig.json");
        }

        private void OnAppSelected(object sender, SelectionChangedEventArgs e)
        {
            HandleAppSelection();
        }

        private void HandleAppSelection()
        {
            string id = GetSelectedAppId();
            IEnumerable<AppIdentity> apps;
            if (id != null)
            {
                apps = _deploymentConfig.ListVersions(id).Select(v => new AppIdentity(id, new Version(v)));
            }
            else
            {
                apps = new List<AppIdentity>();
            }

            VersionsListView.ItemsSource = apps;
            RefreshView(apps);
        }

        private string GetSelectedAppId()
        {
            return (string)AppsListView.SelectedItem;
        }

        private void OnVersionSelected(object sender, SelectionChangedEventArgs e)
        {
            HandleVersionSelection();
        }

        private void HandleVersionSelection()
        {
            AppIdentity appIdentity = VersionsListView.SelectedItem as AppIdentity;
            IEnumerable<DeploymentInfo> deployments;
            if (appIdentity != null)
            {
                deployments = _deploymentConfig.ListDeploymentIds(appIdentity).Select(deploymentId => new DeploymentInfo(appIdentity, deploymentId));
            }
            else
            {
                deployments = new List<DeploymentInfo>();
            }

            deploymentIdsListView.ItemsSource = deployments;
            RefreshView(deployments);
        }
    }
}