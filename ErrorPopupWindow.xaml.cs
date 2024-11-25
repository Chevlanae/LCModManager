using System.Collections.Generic;
using System.Windows;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for ErrorPopupWindow.xaml
    /// </summary>
    public partial class ErrorPopupWindow : Window
    {
        public ErrorPopupWindow(string errorMessage, Exception? ex = null, string title = "Error occured")
        {
            InitializeComponent();
            Title = title;
            HeaderMessageTextBox.Text = errorMessage;

            if (ex == null)
            {
                ExceptionGrid.Visibility = Visibility.Hidden;
                ExceptionRow.Height = new GridLength(0);
            }
            else
            {
                ExceptionTextBox.Text = ex.ToString();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
