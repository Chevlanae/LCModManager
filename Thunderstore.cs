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

            private string? _Path;
            private string? _ReadMe;
            private string? _ChangeLog;
            private ModManifest? _Manifest;

            public string? Path => _Path;
            public string? ReadMe => _ReadMe;
            public string? ChangeLog => _ChangeLog;

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

        internal class PackageManagerAPI
        {
            readonly string StorePath = AppConfig.PackageStorePath + "\\Thunderstore";
            public ObservableCollection<ModPackage> Packages;


            public PackageManagerAPI()
            {
                Packages = [];

                if (!Directory.Exists(StorePath)) Directory.CreateDirectory(StorePath);
            }

            public void AddPackage(string packageSourcePath, bool isDirectory = false)
            {
                if (isDirectory)
                {
                    ModPackage entry = new(packageSourcePath);
                    if(entry.Name != null) Packages.Add(entry);
                } else
                {
                    string dirName = packageSourcePath.Split("\\").Last()[..^4];
                    string destPath = StorePath + "\\" + dirName;

                    if (GetFromName(dirName) == null && !Directory.Exists(destPath))
                    {
                        File.SetAttributes(packageSourcePath, FileAttributes.Normal);
                        using Stream file = File.Open(packageSourcePath, FileMode.Open, FileAccess.Read);
                        using ZipArchive zip = new(file);
                        zip.ExtractToDirectory(destPath);
                        file.Dispose();
                        zip.Dispose();

                        ModPackage entry = new(destPath);

                        if (entry.Name != null) Packages.Add(entry);
                        else Directory.Delete(destPath, true);
                    }
                }
            }

            public void RemovePackage(ModPackage package)
            {
                if (Directory.Exists(package.Path)) Directory.Delete(package.Path, true);

                Packages.Remove(package);
            }

            public void RemovePackage(ModEntry package)
            {
                IEnumerable<ModPackage> query = Packages.Where(p => p.Name == package.Name);

                foreach (ModPackage p in query)
                {
                    RemovePackage(p);
                }
            }

            public void RemovePackages(IEnumerable<ModPackage> packages)
            {
                foreach (ModPackage p in packages) RemovePackage(p);
            }

            public void RemovePackages(IEnumerable<ModEntry> packages)
            {
                foreach (ModEntry p in packages) RemovePackage(p);
            }
            
            public void RefreshPackages()
            {
                Packages.Clear();
                foreach (var dir in Directory.GetDirectories(StorePath)) AddPackage(dir, true);
            }

            public List<ModPackage> GetPackages()
            {
                return [.. Packages];
            }

            public ModPackage? GetFromName(string name)
            {
                foreach (ModPackage package in Packages)
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
