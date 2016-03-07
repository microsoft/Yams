using System.Windows;
using System.Windows.Forms;

namespace YamsStudio
{
    /// <summary>
    /// Interaction logic for SelectDirectoryControl.xaml
    /// </summary>
    public partial class SelectDirectoryControl : System.Windows.Controls.UserControl
    {
	    private static string _selectedPath;

        public SelectDirectoryControl()
        {
            InitializeComponent();
        }

        private void OnBrowse(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
	        dialog.SelectedPath = _selectedPath;
			if (dialog.ShowDialog() == DialogResult.OK)
            {
                _selectedPath = dialog.SelectedPath;
                txt_DirPath.Text = _selectedPath;
            }
        }

        public string DirPath => txt_DirPath.Text;
    }
}
