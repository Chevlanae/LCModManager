using LCModManager.Thunderstore;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace LCModManager
{
    public enum AppState
    {
        Idle,
        DownloadingMod,
        AddingModPackage,
        RemovingModPackage,
        CreatingProfile,
        DeletingProfile,
        RefreshingPackageList
    }

    [ValueConversion(typeof(AppState), typeof(String))]
    public class AppStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case AppState.Idle:
                    return "Idle";
                case AppState.AddingModPackage:
                    return "Adding Mod Package...";
                case AppState.RemovingModPackage:
                    return "Removing Mod Package...";
                case AppState.DownloadingMod:
                    return "Downloading Mod Package...";
                case AppState.CreatingProfile:
                    return "Saving Profile...";
                case AppState.DeletingProfile:
                    return "Deleting Profile...";
                case AppState.RefreshingPackageList:
                    return "Refreshing Package List...";
                default:
                    return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case "Idle":
                    return AppState.Idle;
                case "Adding Mod Package...":
                    return AppState.AddingModPackage;
                case "Removing Mod Package...":
                    return AppState.RemovingModPackage;
                case "Downloading Mod Package...":
                    return AppState.DownloadingMod;
                case "Saving Profile...":
                    return AppState.CreatingProfile;
                case "Deleting Profile...":
                    return AppState.DeletingProfile;
                case "Refreshing Package List...":
                    return AppState.RefreshingPackageList;
                default:
                    return AppState.Idle;
            }
        }
    }

    static internal class AppConfig
    {
        static public Uri Resources = new(Environment.GetEnvironmentVariable("LOCALAPPDATA") + "\\LCModManager\\");
        static public Uri PackageStore = new(Resources, "mods\\");
        static public Uri ProfileStore = new(Resources, "profiles\\");
        static public Dictionary<string, Uri> PackageStores = new();
        static public Dictionary<string, int> WebCacheRefreshInterval = new();

        static AppConfig()
        {
            if (!Directory.Exists(Resources.LocalPath)) Directory.CreateDirectory(Resources.LocalPath);
            if (!Directory.Exists(PackageStore.LocalPath)) Directory.CreateDirectory(PackageStore.LocalPath);
            if (!Directory.Exists(ProfileStore.LocalPath)) Directory.CreateDirectory(ProfileStore.LocalPath);

            PackageStores.Add("Thunderstore", new Uri(PackageStore, "Thunderstore\\"));

            foreach (KeyValuePair<string, Uri> pair in PackageStores)
            {
                if (!Directory.Exists(pair.Value.LocalPath)) Directory.CreateDirectory(pair.Value.LocalPath);
            }

            WebCacheRefreshInterval["Hours"] = 12;
            WebCacheRefreshInterval["Minutes"] = 0;
            WebCacheRefreshInterval["Seconds"] = 0;
        }
    }

    static internal class Utils
    {
        static public void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists) return;

            // Cache directories before copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);

                if (!File.Exists(targetFilePath)) file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }

    static internal class GameDirectory
    {
        static public string Substring = "steamapps\\common\\Lethal Company\\";

        static public string? Find()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();

            List<string?> possiblePaths = [];

            foreach (var item in drives) if (!item.Name.Contains('C')) possiblePaths.Add(Path.Combine(item.Name, "SteamLibrary\\", Substring));

            possiblePaths.Add(Path.Combine("C:\\Program Files (x86)\\Steam\\", Substring));
            possiblePaths.Add(Path.Combine("C:\\Program Files\\Steam\\", Substring));

            foreach (var item in possiblePaths) if (Directory.Exists(item)) return item;

            Debug.Write("Could not find local Lethal Company game directory.");
            return null;
        }
    }


    public class StatusUpdatedEventArgs : EventArgs
    {
        public AppState? CurrentState;
        public string? Message;
        public bool? ProgressBarEnabled;
        public int? ProgressBarPosition;

        public StatusUpdatedEventArgs(AppState currentState)
        {
            CurrentState = currentState;
        }

        public StatusUpdatedEventArgs(AppState currentState, string message)
        {
            CurrentState = currentState;
            Message = message;
        }

        public StatusUpdatedEventArgs(AppState currentState, string message, bool progressEnabled)
        {
            CurrentState = currentState;
            Message = message;
            ProgressBarEnabled = progressEnabled;
        }

        public StatusUpdatedEventArgs(AppState currentState, string message, bool progressEnabled, int progressPosition)
        {
            CurrentState = currentState;
            Message = message;
            ProgressBarEnabled = progressEnabled;
            ProgressBarPosition = progressPosition;
        }
    }

    public class StatusBarControl : INotifyPropertyChanged
    {
        private AppState _CurrentState;
        private string _Message;
        private System.Timers.Timer _InactivityTimer; //timer triggered after most setters
        private int _InactivityTimeout; //
        private bool _ProgressBarEnabled;
        private float _ProgressBarPosition;

        public IProgress<float> Progress;

        public AppState CurrentState
        {
            get { return _CurrentState; }
            set
            {
                _CurrentState = value;
                ResetInactivityTimer();
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get { return _Message; }
            set
            {
                _Message = value;
                ResetInactivityTimer();
                OnPropertyChanged();
            }
        }

        public bool ProgressBarEnabled
        {
            get { return _ProgressBarEnabled; }
            set
            {
                _ProgressBarEnabled = value;
                ResetInactivityTimer();
                OnPropertyChanged();
            }
        }
        public float ProgressBarPosition
        {
            get { return _ProgressBarPosition; }
            set
            {
                _ProgressBarPosition = value;
                ResetInactivityTimer();
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public StatusBarControl(int inactivityTimeout = 3000)
        {
            _InactivityTimer = new();
            _InactivityTimer.Elapsed += (obj, args) => {

                Message = "";
                CurrentState = AppState.Idle;
                ProgressBarEnabled = false;
                ProgressBarPosition = 0;
                _InactivityTimer.Enabled = false;
            };

            _InactivityTimeout = inactivityTimeout;

            Message = "";
            CurrentState = AppState.Idle;
            Progress = new Progress<float>(p => ProgressBarPosition = p);
            ProgressBarEnabled = false;
            ProgressBarPosition = 0;
        }

        private void ResetInactivityTimer()
        {
            _InactivityTimer.Interval = _InactivityTimeout;
            _InactivityTimer.Start();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class Page : System.Windows.Controls.Page
    {
        static public event EventHandler<StatusUpdatedEventArgs> StatusUpdated;

        protected void OnStatusUpdated(StatusUpdatedEventArgs e)
        {
            StatusUpdated?.Invoke(this, e);
        }

        async protected Task<string?> DownloadModPackage(string dependencyString, bool useExistingDownloads = true)
        {
            string[] depParts = dependencyString.Split("-");

            PackageListing? query = WebClient.GetCachedPackage(depParts[^3] + "-" + depParts[^2]);

            if (query != null)
            {
                PackageListingVersion versionEntry = query.Value.versions.First(v => v.version_number == depParts[^1]);

                if (await DownloadModPackage(versionEntry, useExistingDownloads) is string downloadLocation)
                {
                    return downloadLocation;
                }
                else return null;
            }
            else return null;
        }

        async protected Task<string?> DownloadModPackage(PackageListingVersion version, bool useExistingDownloads = true)
        {
            string destinationPath = new Uri(AppConfig.PackageStores["Thunderstore"], version.full_name + ".zip").LocalPath;

            if (useExistingDownloads && File.Exists(destinationPath)) return destinationPath;
            else if (File.Exists(destinationPath)) File.Delete(destinationPath);

            using (HttpResponseMessage? response = await WebClient.DownloadPackageHeaders(version))
            {
                if (response != null)
                {
                    await DownloadFromResponseHeaders(response, destinationPath, AppState.DownloadingMod);

                    return destinationPath;
                }
                else return null;
            }
        }

        async protected Task DownloadFromResponseHeaders(HttpResponseMessage response, string destinationPath, AppState state, int bufferSize = 81920, CancellationToken cancellationToken = new CancellationToken())
        {
            OnStatusUpdated(new StatusUpdatedEventArgs(state, "", true, 0));

            if (File.Exists(destinationPath)) File.Delete(destinationPath);

            using Stream source = await response.Content.ReadAsStreamAsync();
            using Stream destination = File.OpenWrite(destinationPath);
            using (response)
            {
                long? contentLength = response.Content.Headers.ContentLength;

                if (!contentLength.HasValue)
                {
                    await source.CopyToAsync(destination);
                    OnStatusUpdated(new StatusUpdatedEventArgs(AppState.Idle, "", false));
                }
                else
                {
                    byte[] buffer = new byte[bufferSize];
                    long totalBytesRead = 0;
                    int bytesRead;

                    while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                    {
                        await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                        
                        totalBytesRead += bytesRead;

                        OnStatusUpdated(
                            new StatusUpdatedEventArgs(
                                state, 
                                "Downloading '" + response.RequestMessage.RequestUri + "'...", 
                                true, 
                                (int)(((float)totalBytesRead / contentLength.Value) * 100)
                            )
                        );
                    }
                }
            }
        }
    }
}
