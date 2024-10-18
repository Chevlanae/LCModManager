using LCModManager.Thunderstore;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using LCModManager.Thunderstore;
using System.Net.Http;

namespace LCModManager
{


    public class StatusBarControl : INotifyPropertyChanged
    {
        private AppState _CurrentState;
        private string _Message;
        private bool _ProgressBarEnabled;
        private float _ProgressBarPosition;

        public IProgress<float> Progress;

        public string Message
        {
            get { return _Message; }
            set { _Message = value; OnPropertyChanged(); }
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

        async public Task<string?> DownloadWithProgress(string dependencyString)
        {
            string[] depParts = dependencyString.Split("-");

            PackageListing? query = WebClient.GetCachedPackage(depParts[^3] + "-" + depParts[^2]);

            if (query != null)
            {
                PackageListingVersionEntry versionEntry = query.Value.versions.First(v => v.version_number == depParts[^1]);

                if (await DownloadWithProgress(versionEntry) is string downloadLocation)
                {
                    return downloadLocation;
                }
                else return null;
            }
            else return null;
        }

        async public Task<string?> DownloadWithProgress(PackageListingVersionEntry version)
        {
            string destinationPath = AppConfig.DownloadStorePath + "\\" + version.full_name + ".zip";

            CurrentState = AppState.DownloadingMod;
            ProgressBarEnabled = true;
            ProgressBarPosition = 0;

            using (HttpResponseMessage? response = await WebClient.DownloadPackage(version))
            {
                if (response != null)
                {
                    using (Stream source = await response.Content.ReadAsStreamAsync())
                    {
                        using (Stream destination = File.OpenWrite(destinationPath))
                        {
                            long? contentLength = response.Content.Headers.ContentLength;

                            if (!contentLength.HasValue)
                            {
                                await source.CopyToAsync(destination);
                            }
                            else
                            {
                                byte[] buffer = new byte[81920];
                                long totalBytesRead = 0;
                                int bytesRead;
                                CancellationToken cancellationToken = new CancellationToken();

                                while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                                {
                                    await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                                    totalBytesRead += bytesRead;
                                    Progress.Report(((float)totalBytesRead / contentLength.Value) * 100);
                                }
                            }
                        }
                    }
                }
                else return null;
            }

            CurrentState = AppState.Idle;
            ProgressBarEnabled = false;
            ProgressBarPosition = 0;

            return destinationPath;
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