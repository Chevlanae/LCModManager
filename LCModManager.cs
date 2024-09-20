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
        static public class PackageStorePaths
        {
            static public string Thunderstore = PackageStorePath + "\\Thunderstore";
        }

        static public string ResourcePath = Environment.GetEnvironmentVariable("APPDATA") + "\\LCModManager";
        static public string PackageStorePath = ResourcePath + "\\mods";
        static public string ProfileStorePath = ResourcePath + "\\profiles";
        static public string DownloadStorePath = ResourcePath + "\\downloads";

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
        public string Path { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? Website { get; set; }
        public string? IconUri { get; set; }
        public string[]? Dependencies { get; set; }
        public string[]? MissingDependencies { get; set; }
        public string[]? MismatchedDependencies { get; set; }
    }

    public class ModEntry : IModEntry
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? Website { get; set; }
        public string? IconUri { get; set; }
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
                if (MissingDependencies != null && MissingDependencies.Length > 0) return true;
                else return false;
            }
        }

        public bool HasMismatchedDependencies
        {
            get
            {
                if (MismatchedDependencies != null && MismatchedDependencies.Length > 0) return true;
                else return false;
            }
        }

        public bool HasIncompatibility
        {
            get
            {
                return HasMissingDependencies || HasMismatchedDependencies;
            }
        }

        public bool ExistsInPackageStore = false;

        public void ProcessDependencies(IEnumerable<ModEntry> entries)
        {
            if (Dependencies != null)
            {
                List<ModEntry> modlist = new(entries);
                List<string> missingDeps = [];
                List<string> mismatchedDeps = [];

                foreach (string depStr in Dependencies)
                {
                    string[] depStrParts = depStr.Split('-');

                    List<ModEntry> foundDependencies = modlist.FindAll(e => e.Name == depStrParts[^2]);

                    if (foundDependencies.Count > 0)
                    {
                        bool versionMatch = false;
                        foreach (ModEntry entry in foundDependencies)
                        {
                            if (entry.Version == depStrParts[^1])
                            {
                                versionMatch = true;
                                break;
                            }
                        }

                        if (!versionMatch) mismatchedDeps.Add(depStr);
                    }
                    else missingDeps.Add(depStr);


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
                Description = Description,
                Version = Version,
                Website = Website,
                IconUri = IconUri,
                Dependencies = Dependencies,
                MissingDependencies = MissingDependencies
            };
        }
    }
}
