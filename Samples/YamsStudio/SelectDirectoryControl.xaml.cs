using System.Windows;
using System.Windows.Forms;

namespace YamsStudio
{
    /// <summary>
    /// Interaction logic for SelectDirectoryControl.xaml
    /// </summary>
    public partial class SelectDirectoryControl : System.Windows.Controls.UserControl
    {
        public SelectDirectoryControl()
        {
            InitializeComponent();
        }

        private void OnBrowse(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string path = dialog.SelectedPath;
                txt_DirPath.Text = path;
            }
        }

        public string DirPath => txt_DirPath.Text;
    }
}
