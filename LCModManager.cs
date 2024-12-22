using LCModManager.Thunderstore;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;

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
        static public Dictionary<string, Uri> PackageStores = new()
        {
            {"Thunderstore", new Uri(PackageStore, "Thunderstore\\")}
        };
        static public Dictionary<string, int> WebCacheRefreshInterval = new()
        {
            {"Hours", 12 },
            {"Minutes", 0 },
            {"Seconds", 0}
        };

        static AppConfig()
        {
            if (!Directory.Exists(Resources.LocalPath)) Directory.CreateDirectory(Resources.LocalPath);
            if (!Directory.Exists(PackageStore.LocalPath)) Directory.CreateDirectory(PackageStore.LocalPath);
            if (!Directory.Exists(ProfileStore.LocalPath)) Directory.CreateDirectory(ProfileStore.LocalPath);

            foreach (KeyValuePair<string, Uri> pair in PackageStores)
            {
                if (!Directory.Exists(pair.Value.LocalPath)) Directory.CreateDirectory(pair.Value.LocalPath);
            }
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

        public StatusUpdatedEventArgs(AppState? currentState, string? message, bool? progressEnabled, int? progressPosition)
        {
            CurrentState = currentState;
            Message = message;
            ProgressBarEnabled = progressEnabled;
            ProgressBarPosition = progressPosition;
        }
    }

    public class Page : System.Windows.Controls.Page
    {
        public static event EventHandler<StatusUpdatedEventArgs> StatusUpdated;

        async static public void OnStatusUpdated(AppState currentState, string message, bool? progressEnabled = null, int? progressPosition = null, object sender = null)
        {
            StatusUpdated?.Invoke(sender, new StatusUpdatedEventArgs(currentState, message, progressEnabled, progressPosition));
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
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get { return _Message; }
            set
            {
                _Message = value;
                OnPropertyChanged();
            }
        }

        public bool ProgressBarEnabled
        {
            get { return _ProgressBarEnabled; }
            set
            {
                _ProgressBarEnabled = value;
                OnPropertyChanged();
            }
        }
        public float ProgressBarPosition
        {
            get { return _ProgressBarPosition; }
            set
            {
                _ProgressBarPosition = value;
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

        public void ResetInactivityTimer()
        {
            _InactivityTimer.Interval = _InactivityTimeout;
            _InactivityTimer.Start();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class Downloader : INotifyPropertyChanged
    {
        private string _Name;
        private float _ProgressPercent;
        private IProgress<float> _Progress;
        private HttpResponseMessage _Headers;

        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }
        public float ProgressPercent
        {
            get
            {
                return _ProgressPercent;
            }

            set
            {
                _ProgressPercent = value;
                OnPropertyChanged();
            }
        }
        public bool IsDownloaded
        {
            get
            {
                return ProgressPercent >= 100;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Downloader(HttpResponseMessage headers)
        {
            _Name = "";
            _ProgressPercent = 0;
            _Progress = new Progress<float>(p => ProgressPercent = p);
            _Headers = headers;
        }

        public Downloader(HttpResponseMessage headers, IProgress<float> progress)
        {
            _Name = "";
            _ProgressPercent = 0;
            _Progress = progress;
            _Headers = headers;
        }

        public Downloader(string name, HttpResponseMessage headers)
        {
            _Name = name;
            _ProgressPercent = 0;
            _Progress = new Progress<float>(p => ProgressPercent = p);
            _Headers = headers;
        }

        public Downloader(string name, HttpResponseMessage headers, IProgress<float> progress)
        {
            _Name = name;
            _ProgressPercent = 0;
            _Progress = progress;
            _Headers = headers;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        async public Task<MemoryStream> Download(int bufferSize = 81920, CancellationToken cancellationToken = new CancellationToken())
        {
            //set locals
            Stream source;
            MemoryStream result = new();
            long? contentLength = _Headers.Content.Headers.ContentLength;
            byte[] buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;

            //set source to GZipStream in decompression mode if response header Content-Encoding contains "gzip"
            if (_Headers.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                source = new GZipStream(await _Headers.Content.ReadAsStreamAsync(), CompressionMode.Decompress);
            }
            else
            {
                source = await _Headers.Content.ReadAsStreamAsync();
            }

            //dispose of response and source stream once read operation is complete.
            using (_Headers)
            using (source)
            {
                //read bytes from source stream
                while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    //write bytes to result stream
                    await result.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                    //increment totalBytesRead with the amount of bytes that were read
                    totalBytesRead += bytesRead;

                    //update progress if response contains Content-Length header.
                    if (contentLength.HasValue)
                    {
                        _Progress.Report(((float)totalBytesRead / contentLength.Value) * 100);
                    }
                }

                return result;
            }
        }
    }
}
