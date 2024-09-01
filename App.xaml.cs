using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace LCModManager
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {


        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo info = new()
            {
                FileName = "explorer.exe",
                Arguments = e.Uri.ToString(),
            };

            Process.Start(info);
            e.Handled = true;
        }
    }

}
