using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace LCModManager
{
    public interface IModEntry
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? Website { get; set; }
        public string? IconUri { get; set; }
        public string[]? Dependencies { get; set; }
        public string[]? MissingDependencies { get; set; }
    }

    public class ModEntryBase : IModEntry
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? Website { get; set; }
        public string? IconUri { get; set; }
        public string[]? Dependencies { get; set; }
        public string[]? MissingDependencies { get; set; }
    }

    public class ModEntry : ModEntryBase
    {
        public BitmapImage? Icon { get; set; }
        public bool IsMissingDependencies
        {
            get
            {
                if (MissingDependencies != null && MissingDependencies.Length > 0) return true;
                else return false;
            }
        }

        public void GetMissingDependencies(IEnumerable<ModEntry> entries)
        {
            if (Dependencies != null)
            {
                List<string> missingDeps = [];

                foreach (string depStr in Dependencies)
                {
                    bool found = false;
                    foreach (ModEntry entry in entries)
                    {
                        if (entry.Name != null && entry.Version != null)
                        {
                            string pattern = entry.Name.Split("-")[0];

                            //Pattern to match to end of depStr
                            Regex reg = new(pattern);

                            //Check dependency string against regex
                            if (reg.IsMatch(depStr))
                            {
                                found = true;
                                break;
                            }
                        }

                    }

                    if (!found) missingDeps.Add(depStr);
                }

                MissingDependencies = [.. missingDeps];
            }
        }

        public ModEntryBase ToModEntryBase()
        {
            return new ModEntryBase
            {
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

    static internal class AppConfig
    {
        static public string ResourcePath = Environment.GetEnvironmentVariable("APPDATA") + "\\LCModManager";
        static public string PackageStorePath = ResourcePath + "\\mods";
    }

    static internal class GameDirectory
    {
        static public string substring = "steamapps\\common\\Lethal Company";
        static public string? path = Find();

        static public string? Find()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();

            List<string?> possiblePaths = [];

            foreach (var item in drives) if (!item.Name.Contains('C')) possiblePaths.Add(Path.Combine(item.Name, "SteamLibrary\\", substring));

            possiblePaths.Add(Path.Combine("C:\\Program Files (x86)\\Steam\\", substring));
            possiblePaths.Add(Path.Combine("C:\\Program Files\\Steam\\", substring));

            foreach (var item in possiblePaths) if (Directory.Exists(item)) return item;

            return null;
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
}
