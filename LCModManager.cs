using LCModManager.Thunderstore;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace LCModManager
{
    static internal class AppConfig
    {
        static public string ResourcePath = Environment.GetEnvironmentVariable("LOCALAPPDATA") + "\\LCModManager";
        static public string PackageStorePath = ResourcePath + "\\mods";
        static public string ProfileStorePath = ResourcePath + "\\profiles";
        static public string DownloadStorePath = ResourcePath + "\\downloads";
        static public Tuple<int, int, int> WebCacheRefreshInterval = new(30, 0, 0);

        static public class PackageStorePaths
        {
            static public string Thunderstore = PackageStorePath + "\\Thunderstore";
        }

        static public void CreateDataStores()
        {
            if (!Directory.Exists(PackageStorePath)) Directory.CreateDirectory(PackageStorePath);
            if (!Directory.Exists(ProfileStorePath)) Directory.CreateDirectory(ProfileStorePath);
            if (!Directory.Exists(DownloadStorePath)) Directory.CreateDirectory(DownloadStorePath);
            if (!Directory.Exists(PackageStorePaths.Thunderstore)) Directory.CreateDirectory(PackageStorePaths.Thunderstore);
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
        static public string Substring = "steamapps\\common\\Lethal Company";

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

    public interface IModEntry
    {
        public bool ExistsInPackageStore { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string? Author { get; set; }
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? IconUri { get; set; }
        public List<string> Versions { get; set; }
        public string[]? Dependencies { get; set; }
        public string[]? MissingDependencies { get; set; }
        public string[]? MismatchedDependencies { get; set; }
    }

    public class ModEntry : IModEntry
    {
        public bool ExistsInPackageStore { get; set; } = false;
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Author { get; set; }
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? IconUri { get; set; }
        public List<string> Versions { get; set; } = [];
        public string[]? Dependencies { get; set; }
        public string[]? MissingDependencies { get; set; }
        public string[]? MismatchedDependencies { get; set; }
    }

    public class ModEntryDisplay : ModEntry
    {
        public BitmapImage? Icon { get; set; }
        public bool HasMissingDependencies
        {
            get
            {
                return MissingDependencies != null && MissingDependencies.Length > 0;
            }
        }

        public bool HasMismatchedDependencies
        {
            get
            {
                return MismatchedDependencies != null && MismatchedDependencies.Length > 0;
            }
        }

        public bool HasIncompatibility
        {
            get
            {
                return HasMissingDependencies || HasMismatchedDependencies;
            }
        }

        public List<string> SelectedVersions = [];

        public void ProcessDependencies(List<ModEntryDisplay> modlist)
        {
            if (Dependencies != null)
            {
                List<string> missingDeps = [];
                List<string> mismatchedDeps = [];

                foreach (string depStr in Dependencies)
                {
                    string[] depStrParts = depStr.Split('-');
                    string owner = depStrParts[^3];
                    string name = depStrParts[^2];
                    string version = depStrParts[^1];

                    List<ModEntryDisplay> foundDependencies = modlist.FindAll(e => e.Name == name);

                    switch (foundDependencies.Count)
                    {
                        case 0:

                            missingDeps.Add(depStr);
                            break;

                        case 1:

                            if (!foundDependencies[0].ExistsInPackageStore)
                            {
                                missingDeps.Add(depStr);
                            }
                            else if (!foundDependencies[0].Versions.Contains(version))
                            {
                                mismatchedDeps.Add(depStr);
                            }
                            break;

                        default:
                            bool match = false;
                            foreach (ModEntryDisplay item in foundDependencies)
                            {
                                if (item.Versions.Contains(version))
                                {
                                    match = true;
                                    break;
                                }

                            }
                            if (!match) mismatchedDeps.Add(depStr);
                            break;
                    }
                }

                MismatchedDependencies = [.. mismatchedDeps];
                MissingDependencies = [.. missingDeps];
            }
        }

        public ModEntry ToModEntry()
        {
            return new ModEntry
            {
                Path = Path,
                Name = Name,
                Author = Author,
                Description = Description,
                Versions = Versions,
                Website = Website,
                IconUri = IconUri,
                Dependencies = Dependencies,
                MissingDependencies = MissingDependencies,
                MismatchedDependencies = MismatchedDependencies,
                ExistsInPackageStore = ExistsInPackageStore
            };
        }
    }

    public class PackageState
    {
        public string SelectedVersion;
        public ModEntry ModEntry;

        public PackageState()
        {
            SelectedVersion = "";
            ModEntry = new ModEntry();
        }

        public PackageState(string selectedVersion, ModEntry modEntry)
        {
            SelectedVersion = selectedVersion;
            ModEntry = modEntry;
        }
    }
}
