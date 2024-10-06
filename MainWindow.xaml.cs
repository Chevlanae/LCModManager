using System.Windows;
using System.Windows.Controls;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ManageModsPage ManageMods;
        private CreateProfilePage CreateProfile;
        private LauncherPage Launcher;

        public MainWindow()
        {
            ManageMods = new ManageModsPage();
            CreateProfile = new CreateProfilePage();
            Launcher = new LauncherPage();

            InitializeComponent();
            NavTo_ManageMods();
        }

        private void NavTo_ManageMods()
        {
            ViewFrame.Navigate(ManageMods);
        }

        private void NavTo_ManageMods(object sender, RoutedEventArgs e)
        {
            ManageMods.RefreshModList();
            ViewFrame.Navigate(ManageMods);
        }

        private void NavTo_CreateProfile(object sender, RoutedEventArgs e)
        {
            CreateProfile.RefreshModList();
            ViewFrame.Navigate(CreateProfile);
        }

        private void NavTo_Launcher(object sender, RoutedEventArgs e)
        {
            Launcher.RefreshProfiles();
            ViewFrame.Navigate(Launcher);
        }
    }
}