using System.IO;

namespace LCModManager
{
    internal class LCModManager
    {

        static public string gameDir = FindGameDir();
        static public string workingDirectory = Directory.GetCurrentDirectory();


        static string? FindGameDir()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            List<string> possiblePaths = new List<string>(); ;
            string gameDirSubstring = "steamapps\\common\\Lethal Company";
            string ProgramFilesx86 = Path.Combine("C:\\Program Files (x86)\\Steam\\", gameDirSubstring);
            string ProgramFiles = Path.Combine("C:\\Program Files\\Steam\\", gameDirSubstring);

            foreach (var item in drives)
            {
                if (!item.Name.Contains('C'))
                {
                    possiblePaths.Add(Path.Combine(item.Name, "SteamLibrary\\", gameDirSubstring));
                }
            }

            possiblePaths.Add(ProgramFilesx86);
            possiblePaths.Add(ProgramFiles);

            foreach (var item in possiblePaths)
            {
                if (Directory.Exists(item))
                {
                    return item;
                }
            }

            return null;
        }

        static void CopyDirectory(string sourceDir, string destinationDir)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists) return;

            // Create the destination directory
            if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);

                if (!File.Exists(targetFilePath)) file.CopyTo(targetFilePath);
            }
        }

        static void CopyDirectory(string sourceDir, string destinationDir, bool force)
        {
            if (force)
            {
                // Get information about the source directory
                var dir = new DirectoryInfo(sourceDir);

                // Check if the source directory exists
                if (!dir.Exists) return;

                // Delete destination directory if it already exists
                if (Directory.Exists(destinationDir)) Directory.Delete(destinationDir, force);

                // Create new directory
                Directory.CreateDirectory(destinationDir);

                // Get the files in the source directory and copy to the destination directory
                foreach (FileInfo file in dir.GetFiles())
                {
                    string targetFilePath = Path.Combine(destinationDir, file.Name);

                    if (File.Exists(targetFilePath)) File.Delete(targetFilePath);

                    file.CopyTo(targetFilePath);
                }
            }
            else
            {
                CopyDirectory(sourceDir, destinationDir);
            }
        }

        static void CopyDirectory(string sourceDir, string destinationDir, bool force, bool recursive)
        {
            CopyDirectory(sourceDir, destinationDir, force);

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                // Get information about the source directory
                var dir = new DirectoryInfo(sourceDir);

                // Cache directories before copying
                DirectoryInfo[] dirs = dir.GetDirectories();

                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, force);
                }
            }
        }
    }
}

