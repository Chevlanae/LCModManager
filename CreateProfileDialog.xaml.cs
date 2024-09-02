using System.Windows;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for CreateProfileDialog.xaml
    /// </summary>
    public partial class CreateProfileDialog : Window
    {

        public CreateProfileDialog()
        {
            InitializeComponent();
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
