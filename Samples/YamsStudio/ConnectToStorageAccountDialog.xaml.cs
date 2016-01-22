using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace YamsStudio
{
    /// <summary>
    /// Interaction logic for ConnectToStorageAccountDialog.xaml
    /// </summary>
    public partial class ConnectToStorageAccountDialog : Window
    {
        public ConnectToStorageAccountDialog()
        {
            InitializeComponent();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txt_AccountName.SelectAll();
            txt_AccountName.Focus();
        }

        public string AccountName => txt_AccountName.Text;
        public string DataConnectionString => txt_DataConnectionString.Text;
    }
}
