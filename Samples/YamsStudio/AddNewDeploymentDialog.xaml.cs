using System;
using System.Windows;

namespace YamsStudio
{
    /// <summary>
    /// Interaction logic for AddNewDeploymentDialog.xaml
    /// </summary>
    public partial class AddNewDeploymentDialog : Window
    {
        public AddNewDeploymentDialog(string appId, string version)
        {
            InitializeComponent();

            txt_AplicationName.Text = appId;
            txt_Version.Text = version;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txt_DeploymentId.SelectAll();
            txt_DeploymentId.Focus();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public string DeploymentId => txt_DeploymentId.Text;
    }
}
