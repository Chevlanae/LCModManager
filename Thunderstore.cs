using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace LCModManager
{
    namespace Thunderstore
    {
        struct ModManifest
        {
            public string? Name { get; set; }
            public string? Version_Number { get; set; }
            public string? Website_Url { get; set; }
            public string? Description { get; set; }
            public string[]? Dependencies { get; set; }

            public ModManifest(Stream fileData, JsonSerializerOptions options)
            {
                this = JsonSerializer.Deserialize<ModManifest>(fileData, options);
            }

            public ModManifest(string fileString, JsonSerializerOptions options)
            {
                this = JsonSerializer.Deserialize<ModManifest>(fileString, options);
            }
        }

        internal class ModPackage : ModEntry
        {
            private string? _ReadMe;
            private string? _ChangeLog;
            private ModManifest _Manifest;

            public string? ReadMe => _ReadMe;
            public string? ChangeLog => _ChangeLog;

            public ModPackage(string sourcePath)
            {
                Path = sourcePath;
                
                try
                {
                    _ReadMe = File.ReadAllText(sourcePath + "\\README.md");
                } catch
                {
                    _ReadMe = null;
                }
                
                try
                {
                    _ChangeLog = File.ReadAllText(sourcePath + "\\CHANGELOG.md");
                } catch
                {
                    _ChangeLog = null;
                }

                try
                {
                    Icon = new BitmapImage();
                    Icon.BeginInit();
                    Icon.UriSource = new Uri(sourcePath + "\\icon.png");
                    Icon.DecodePixelWidth = 64;
                    Icon.DecodePixelHeight = 64;
                    Icon.CacheOption = BitmapCacheOption.OnLoad;
                    Icon.EndInit();
                } catch
                {
                    Icon = null;
                }

                try
                {
                    _Manifest = new ModManifest(File.ReadAllText(sourcePath + "\\manifest.json"), new(JsonSerializerDefaults.Web));
                    Name = _Manifest.Name ?? "";
                    Description = _Manifest.Description;
                    Version = _Manifest.Version_Number;
                    Website = _Manifest.Website_Url;
                    Dependencies = _Manifest.Dependencies;
                } catch
                {

                    throw new Exception("Could not serialize JSON manifest at: \"" + sourcePath + "\\manifest.json" + "\"");
                }
            }
        }

        static internal partial class PackageManager
        {
            static public string StorePath = AppConfig.PackageStorePath + "\\Thunderstore";

            static public void AddPackage(string packageSourcePath)
            {

                string storeName = packageSourcePath.Split("\\").Last()[..^4];

                if (DuplicateFileRegex().IsMatch(storeName))
                {
                    storeName = storeName[0..^3];
                }

                string destPath = StorePath + "\\" + storeName;

                if (!Directory.Exists(destPath))
                {
                    File.SetAttributes(packageSourcePath, FileAttributes.Normal);
                    using Stream file = File.Open(packageSourcePath, FileMode.Open, FileAccess.Read);
                    using ZipArchive zip = new(file);
                    zip.ExtractToDirectory(destPath);
                    file.Dispose();
                    zip.Dispose();
                }
            }

            static public void AddPackage(string packageSourcePath, bool isDirectory = false)
            {
                if (isDirectory)
                {
                    string dirName = packageSourcePath.Split("\\").Last();
                    string destPath = StorePath + "\\" + dirName;

                    if (!Directory.Exists(destPath))
                    {
                        Utils.CopyDirectory(packageSourcePath, destPath, true);
                    }
                }
                else AddPackage(packageSourcePath);
            }

            static public void RemovePackage(ModPackage package)
            {
                if (Directory.Exists(package.Path)) Directory.Delete(package.Path, true);
            }

            static public void RemovePackage(ModEntry package)
            {
                List<ModPackage> packages = GetPackages();

                IEnumerable<ModPackage> query = packages.Where(p => p.Name == package.Name);

                foreach (ModPackage p in query)
                {
                    RemovePackage(p);
                }
            }

            static public void RemovePackages(IEnumerable<ModPackage> packages)
            {
                foreach (ModPackage p in packages) RemovePackage(p);
            }

            static public void RemovePackages(IEnumerable<ModEntry> packages)
            {
                foreach (ModEntry p in packages) RemovePackage(p);
            }

            static public List<ModPackage> GetPackages()
            {
                List<ModPackage> packages = [];

                foreach (string file in Directory.GetDirectories(StorePath))
                {
                    try
                    {
                        packages.Add(new ModPackage(file));
                    } catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }

                return packages;
            }

            static public ModPackage? GetFromName(string name)
            {

                foreach (ModPackage package in GetPackages())
                {
                    if (package.Name != null && package.Name == name)
                    {
                        return package;
                    }
                }
                return null;
            }

            static public ModPackage? GetFromName(Regex reg)
            {

                foreach (ModPackage package in GetPackages())
                {
                    if (package.Name != null)
                    {
                        if (reg.IsMatch(package.Name)) return package;
                    }
                }
                return null;
            }

            [GeneratedRegex("\\([0-9]\\)\\.[a-zA-Z0-9]+$|\\([0-9][0-9]\\)\\.[a-zA-Z0-9]+$|\\([0-9][0-9][0-9]\\)\\.[a-zA-Z0-9]+$")]
            private static partial Regex DuplicateFileRegex();
        }

        static internal class ModDeployer
        {
            static public string GameDir = GameDirectory.Find();

            static public string InferModParentDirectory(ModEntry modEntry)
            {

                List<string> childDirs = [];

                foreach (string dir in Directory.GetDirectories(modEntry.Path))
                {
                    childDirs.Add(dir.Split("\\")[^1]);
                }

                switch (childDirs.Count)
                {
                    case 0: return GameDir + "\\BepInEx\\plugins";
                    case 1:
                        return childDirs[0] switch
                        {
                            "BepInEx" => GameDir,
                            "BepInExPack" => GameDir,
                            _ => GameDir + "\\BepInEx\\",
                        };
                    default:
                        foreach (string dir in childDirs)
                        {
                            switch (dir)
                            {
                                case "BepInEx": return GameDir;
                            }
                        }

                        return GameDir + "\\BepInEx";
                }
            }

            static public void DeployModFromStore(string name)
            {
                ModPackage? package = PackageManager.GetFromName(name);

                if (package != null) DeployModFromStore(package);
            }

            static public void DeployModFromStore(ModEntry modEntry)
            {
                if (modEntry.Name == "BepInExPack")
                {
                    Utils.CopyDirectory(modEntry.Path + "\\BepInExPack", GameDir, true);
                }
                else
                {
                    string destinationPath = InferModParentDirectory(modEntry);

                    Utils.CopyDirectory(modEntry.Path, destinationPath, true);
                }
            }

            static public void RemoveDeployedMods()
            {
                if (Directory.Exists(GameDir + "\\BepInEx")) Directory.Delete(GameDir + "\\BepInEx", true);
                if (File.Exists(GameDir + "\\doorstop_config.ini")) File.Delete(GameDir + "\\doorstop_config.ini");
                if (File.Exists(GameDir + "\\winhttp.dll")) File.Delete(GameDir + "\\winhttp.dll");
            }
        }

        static internal class WebAPI
        {
            static public string EndPointURL = "";
        }
    }
}
