using System.Windows;

namespace YamsStudio
{
    /// <summary>
    /// Interaction logic for BusyWindow.xaml
    /// </summary>
    public partial class BusyWindow : Window
    {
        public BusyWindow()
        {
            InitializeComponent();
        }

        public string Message
        {
            get
            {
                return lbl_message.Content.ToString();
            }
            set
            {
                lbl_message.Content = value;
            }
        }
    }
}
