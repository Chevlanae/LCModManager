using System.Collections.ObjectModel;
using System.Data;
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
            private ModManifest? _Manifest;
            private string _Path;

            public string? ReadMe => _ReadMe;
            public string? ChangeLog => _ChangeLog;
            public string Path => _Path;

            public ModPackage(string sourcePath)
            {
                _Path = sourcePath;
                
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
                    Name = _Manifest.Value.Name;
                    Description = _Manifest.Value.Description;
                    Version = _Manifest.Value.Version_Number;
                    Website = _Manifest.Value.Website_Url;
                    Dependencies = _Manifest.Value.Dependencies;
                } catch
                {
                    _Manifest = null;
                }
            }
        }

        static internal class PackageManager
        {
            static public string StorePath = AppConfig.PackageStorePath + "\\Thunderstore";


            static public void AddPackage(string packageSourcePath)
            {

                string dirName = packageSourcePath.Split("\\").Last()[..^4];
                string destPath = StorePath + "\\" + dirName;

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
                    packages.Add(new ModPackage(file));
                }

                return packages;
            }

            static public ModPackage? GetFromName(string name)
            {
                List<ModPackage> packages = GetPackages();

                foreach (ModPackage package in packages)
                {
                    if (package.Name != null)
                    {
                        Regex reg = new(package.Name);
                        if (reg.IsMatch(name)) return package;
                    }
                }
                return null;
            }
        }
    }
}
