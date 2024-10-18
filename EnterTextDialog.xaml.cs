using System.Windows;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for EnterTextDialog.xaml
    /// </summary>
    public partial class EnterTextDialog : Window
    {

        public EnterTextDialog()
        {
            InitializeComponent();
        }

        public EnterTextDialog(string title, string message, bool enableColon = true)
        {
            InitializeComponent();

            Title = title;
            
            if(enableColon) MessageTextBox.Text = message + ":";
            else MessageTextBox.Text = message;
        }

        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
