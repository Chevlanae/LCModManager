using LCModManager.Thunderstore;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        private StatusBarControl StatusBarCtrl;

        public MainWindow()
        {
            StatusBarCtrl = new StatusBarControl();

            Page.StatusUpdated += UpdateStatusBar;

            ManageMods = new ManageModsPage();
            CreateProfile = new CreateProfilePage();
            Launcher = new LauncherPage();

            InitializeComponent();

            StatusBar.DataContext = StatusBarCtrl;

            NavTo_ManageMods();

            UpdateThunderstoreCache();
        }

        private void UpdateStatusBar(object sender, StatusUpdatedEventArgs e)
        {
            StatusBarCtrl.CurrentState = e.CurrentState ?? StatusBarCtrl.CurrentState;
            StatusBarCtrl.Message = e.Message ?? StatusBarCtrl.Message;
            StatusBarCtrl.ProgressBarEnabled = e.ProgressBarEnabled ?? StatusBarCtrl.ProgressBarEnabled;
            StatusBarCtrl.ProgressBarPosition = e.ProgressBarPosition ?? StatusBarCtrl.ProgressBarPosition;
            StatusBarCtrl.ResetInactivityTimer();
        }

        //Updates WebClient.PackageCache if cache file does not exist, or needs refresh
        async public Task UpdateThunderstoreCache()
        {
            if (WebClient.NeedsRefresh && await WebClient.DownloadPackageListHeaders() is HttpResponseMessage headers)
            {
                Downloader downloader = new(headers);
                downloader.PropertyChanged += (sender, e) =>
                {
                    if (sender is Downloader downloader)
                    {
                        StatusUpdatedEventArgs args = new(AppState.RefreshingPackageList, "Downloading Thunderstore package list...", true, (int)downloader.ProgressPercent);

                        UpdateStatusBar(this, args);
                    }
                };

                using (MemoryStream ms = await downloader.Download())
                {
                    await WebClient.SetCache(ms);
                }
            }

            await WebClient.LoadCache();
        }

        async private void NavTo_ManageMods()
        {
            NavTo_ManageMods(new object(), new RoutedEventArgs());
        }

        async private void NavTo_ManageMods(object sender, RoutedEventArgs e)
        {
            ViewFrame.Navigate(ManageMods);
            await ManageMods.RefreshModList();
        }

        async private void NavTo_CreateProfile(object sender, RoutedEventArgs e)
        {
            ViewFrame.Navigate(CreateProfile);
            await CreateProfile.RefreshModList();
        }

        private void NavTo_Launcher(object sender, RoutedEventArgs e)
        {
            ViewFrame.Navigate(Launcher);
            Launcher.RefreshProfiles();
        }
    }
}