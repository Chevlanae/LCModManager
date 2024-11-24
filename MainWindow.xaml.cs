using LCModManager.Thunderstore;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
        public StatusBarControl StatusBarCtrl;

        public MainWindow()
        {
            StatusBarCtrl = new StatusBarControl();

            Page.StatusUpdated += UpdateStatusBar;

            ManageMods = new ManageModsPage();
            CreateProfile = new CreateProfilePage();
            Launcher = new LauncherPage();

            InitializeComponent();

            StatusBar.DataContext = StatusBarCtrl;

            ViewFrame.Navigate(ManageMods);
        }

        private void UpdateStatusBar(object sender, StatusUpdatedEventArgs e)
        {
            StatusBarCtrl.CurrentState = e.CurrentState ?? StatusBarCtrl.CurrentState;
            StatusBarCtrl.Message = e.Message ?? StatusBarCtrl.Message;
            StatusBarCtrl.ProgressBarEnabled = e.ProgressBarEnabled ?? StatusBarCtrl.ProgressBarEnabled;
            StatusBarCtrl.ProgressBarPosition = e.ProgressBarPosition ?? StatusBarCtrl.ProgressBarPosition;
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