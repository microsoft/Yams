using System;
using System.Windows;
using System.Windows.Forms;

namespace YamsStudio
{
    /// <summary>
    /// Interaction logic for AddNewApplicationDialog.xaml
    /// </summary>
    public partial class AddNewApplicationDialog : Window
    {
        public AddNewApplicationDialog(string appId, string version, string deploymentId, string binariesPath=null)
        {
            InitializeComponent();

            txt_AplicationName.Text = appId;
            txt_Version.Text = version;
            txt_DeploymentId.Text = deploymentId;
        }

        public AddNewApplicationDialog() : this(null, null, null)
        {
        }

        public AddNewApplicationDialog(string appId) : this(appId, null, null)
        {
        }

        public AddNewApplicationDialog(string appId, string version) : this(appId, version, null)
        {
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txt_AplicationName.SelectAll();
            txt_AplicationName.Focus();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public string ApplicationName => txt_AplicationName.Text;
        public string Version => txt_Version.Text;
        public string BinariesPath => uc_SelectBinaries.DirPath;
        public string DeploymentId => txt_DeploymentId.Text;
    }
}
