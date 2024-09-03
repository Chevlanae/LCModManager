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
            private ModManifest? _Manifest;

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
                    Name = _Manifest.Value.Name ?? "";
                    Description = _Manifest.Value.Description;
                    Version = _Manifest.Value.Version_Number;
                    Website = _Manifest.Value.Website_Url;
                    Dependencies = _Manifest.Value.Dependencies;
                } catch
                {
                    throw new Exception("Could not serialize JSON manifest at: \"" + sourcePath + "\\manifest.json" + "\"");
                }
            }
        }

        static internal class PackageManager
        {
            static public string StorePath = AppConfig.PackageStorePath + "\\Thunderstore";


            static public void AddPackage(string packageSourcePath)
            {

                string storeName = packageSourcePath.Split("\\").Last()[..^4];

                Regex duplicateFilenameReg = new("\\([0-9]\\)$"); 
                if (duplicateFilenameReg.IsMatch(storeName))
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
        }

        static internal class WebAPI
        {
            static public string EndPointURL = "";
        }
    }
}
