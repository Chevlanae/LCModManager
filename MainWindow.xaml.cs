using LCModManager.Thunderstore;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LCModManager
{
    public class StatusBarControl : INotifyPropertyChanged
    {
        private AppState _CurrentState;
        private string _Message;
        private System.Timers.Timer _MessageTimer;
        private int _MessageTimeoutMilliseconds;
        private bool _ProgressBarEnabled;
        private float _ProgressBarPosition;

        public IProgress<float> Progress;

        public string Message
        {
            get { return _Message; }
            set
            {
                if ((_Message = value) != "")
                {
                    _MessageTimer.Interval = _MessageTimeoutMilliseconds;
                    _MessageTimer.Start();
                }
                OnPropertyChanged();
            }
        }
        public AppState CurrentState
        {
            get { return _CurrentState; }
            set { _CurrentState = value; OnPropertyChanged(); }
        }
        public bool ProgressBarEnabled
        {
            get { return _ProgressBarEnabled; }
            set { _ProgressBarEnabled = value; OnPropertyChanged(); }
        }
        public float ProgressBarPosition
        {
            get { return _ProgressBarPosition; }
            set { _ProgressBarPosition = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public StatusBarControl()
        {
            _MessageTimer = new();
            _MessageTimer.Elapsed += (obj, args) => { Message = ""; _MessageTimer.Enabled = false; };
            _MessageTimeoutMilliseconds = 3000;

            Message = "";
            CurrentState = AppState.Idle;
            Progress = new Progress<float>(p => ProgressBarPosition = p);
            ProgressBarEnabled = false;
            ProgressBarPosition = 0;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        async public Task<string?> DownloadWithProgress(string dependencyString, bool useExistingDownloads = true)
        {
            string[] depParts = dependencyString.Split("-");

            PackageListing? query = WebClient.GetCachedPackage(depParts[^3] + "-" + depParts[^2]);

            if (query != null)
            {
                PackageListingVersionEntry versionEntry = query.Value.versions.First(v => v.version_number == depParts[^1]);

                if (await DownloadWithProgress(versionEntry, useExistingDownloads) is string downloadLocation)
                {
                    return downloadLocation;
                }
                else return null;
            }
            else return null;
        }

        async public Task<string?> DownloadWithProgress(PackageListingVersionEntry version, bool useExistingDownloads = true)
        {
            string destinationPath = AppConfig.DownloadStorePath + "\\" + version.full_name + ".zip";

            if (useExistingDownloads && File.Exists(destinationPath)) return destinationPath;

            using (HttpResponseMessage? response = await WebClient.DownloadPackage(version))
            {
                if (response != null)
                {
                    await DownloadWithProgress(response, destinationPath, AppState.DownloadingMod);

                    return destinationPath;
                }
                else return null;
            }
        }

        async public Task DownloadWithProgress(HttpResponseMessage response, string destinationPath, AppState state, bool progressBar = true)
        {
            CurrentState = state;

            if (progressBar)
            {
                ProgressBarEnabled = true;
                ProgressBarPosition = 0;
            }

            using (response)
            {
                using (Stream source = await response.Content.ReadAsStreamAsync())
                {
                    using (Stream destination = File.OpenWrite(destinationPath))
                    {
                        long? contentLength = response.Content.Headers.ContentLength;

                        if (!contentLength.HasValue)
                        {
                            await source.CopyToAsync(destination);
                            Progress.Report(100);
                        }
                        else
                        {
                            byte[] buffer = new byte[81920];
                            long totalBytesRead = 0;
                            int bytesRead;
                            CancellationToken cancellationToken = new CancellationToken();

                            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                            {
                                Message = "Downloading '" + response.RequestMessage.RequestUri + "'...";
                                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                                totalBytesRead += bytesRead;
                                Progress.Report(((float)totalBytesRead / contentLength.Value) * 100);
                            }
                        }
                    }
                }
            }

            CurrentState = AppState.Idle;
            Message = "";

            if (progressBar)
            {
                ProgressBarEnabled = false;
                ProgressBarPosition = 0;
            }
        }
    }

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

            ManageMods = new ManageModsPage(StatusBarCtrl);
            CreateProfile = new CreateProfilePage(StatusBarCtrl);
            Launcher = new LauncherPage(StatusBarCtrl);

            InitializeComponent();
            NavTo_ManageMods();

            StatusBar.DataContext = StatusBarCtrl;

            WebClient.PackageCache.Refresh(StatusBarCtrl);
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

        private async Task Refresh()
        {

        }
    }
}