using System;
using System.Collections.Generic;
using System.Windows;

namespace YamsStudio
{
    /// <summary>
    /// Interaction logic for UpdateVersionDialog.xaml
    /// </summary>
    public partial class UpdateVersionDialog : Window
    {
        public UpdateVersionDialog(string appName, string currentVersion, IEnumerable<string> deploymentIds)
        {
            InitializeComponent();

            txt_AplicationName.Text = appName;
            txt_CurrentVersion.Text = currentVersion;
            foreach(string deploymentId in deploymentIds)
            {
                lv_DeploymentIds.Items.Add(deploymentId);
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txt_NewVersion.SelectAll();
            txt_NewVersion.Focus();
            lv_DeploymentIds.SelectAll();
        }

        public string NewVersion => txt_NewVersion.Text;

        public IEnumerable<string> SelectedDeploymentIds
        {
            get
            {
                List<string> selectedItems = new List<string>();
                foreach(object obj in lv_DeploymentIds.SelectedItems)
                {
                    selectedItems.Add((string)obj);
                }
                return selectedItems;
            }
        }

        public string BinariesPath => uc_SelectBinaries.DirPath;

        private void OnOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
