using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace LCModManager
{
    public interface IModEntry
    {
        public string? Name { get; }
        public string? Description { get; }
        public string? Version { get; }
        public string? Website { get; }
        public BitmapImage? Icon { get; }
        public string[]? Dependencies { get; }
    }

    public class ModEntry : IModEntry
    {
        private string? _Name;
        private string? _Description;
        private string? _Version;
        private string? _Website;
        private BitmapImage? _Icon;
        private string[]? _Dependencies;

        public string? Name => _Name;
        public string? Description => _Description;
        public string? Version => _Version;
        public string? Website => _Website;
        public BitmapImage? Icon => _Icon;
        public string[]? Dependencies => _Dependencies;
    }

    static internal class AppConfig
    {
        static public string ResourcePath = Environment.GetEnvironmentVariable("APPDATA") + "\\LCModManager";
        static public string PackageStorePath = ResourcePath + "\\mods";
    }

    static internal class GameDirectory
    {
        static public string substring = "steamapps\\common\\Lethal Company";
        static public string? path = GameDirectory.Find();

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


    internal class Utils
    {
        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
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
