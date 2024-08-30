using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
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
                try
                {
                    this = JsonSerializer.Deserialize<ModManifest>(fileData, options);
                } catch { }
            }

            public ModManifest(string fileString, JsonSerializerOptions options)
            {
                try
                {
                    this = JsonSerializer.Deserialize<ModManifest>(fileString, options);
                }
                catch { }
            }
        }

        internal class ModPackage : ModEntry
        {

            private string? _Path;
            private string? _ReadMe;
            private string? _ChangeLog;
            private BitmapImage? _Icon;
            private ModManifest? _Manifest;

            public string? Path
            {
                get { return _Path; }
            }
            
            public string? ReadMe
            {
                get { return _ReadMe; }
            }

            public string? ChangeLog
            {
                get { return _ChangeLog; } 
            }

            new public string? Name
            {
                get { return _Manifest?.Name; }
            }

            new public string? Version
            {

                get { return _Manifest?.Version_Number; }
            }

            new public string? Website
            {

                get { return _Manifest?.Website_Url; }
            }

            new public string? Description
            {

                get { return _Manifest?.Description; }
            }

            new public string[]? Dependencies
            {

                get { return _Manifest?.Dependencies; }
            }

            new public BitmapImage? Icon
            {
                get { return _Icon; }
            }

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
                    _Icon = new BitmapImage();
                    _Icon.BeginInit();
                    _Icon.UriSource = new Uri(sourcePath + "\\icon.png");
                    _Icon.DecodePixelWidth = 64;
                    _Icon.DecodePixelHeight = 64;
                    _Icon.CacheOption = BitmapCacheOption.OnLoad;
                    _Icon.EndInit();
                } catch
                {
                    _Icon = null;
                }

                try
                {
                    _Manifest = new ModManifest(File.ReadAllText(sourcePath + "\\manifest.json"), new(JsonSerializerDefaults.Web));
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

            public void AddPackage(string packageSourcePath)
            {
                if (Directory.Exists(packageSourcePath))
                {
                    Packages.Add(new ModPackage(packageSourcePath));
                } 
                else if (File.Exists(packageSourcePath))
                {
                    string fileName = packageSourcePath.Split("\\").Last();
                    string dirName = fileName[..^4];
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

                    Packages.Add(new ModPackage(destPath));
                }
            }

            public void RemovePackage(ModPackage package)
            {
                if (Directory.Exists(package.Path)) Directory.Delete(package.Path, true);

                Packages.Remove(package);
            }
            
            public void RefreshPackages()
            {
                Packages.Clear();
                foreach (var dir in Directory.GetDirectories(StorePath)) AddPackage(dir);
            }

            public List<ModPackage> GetPackages()
            {
                return [.. Packages];
            }
        }
    }
}
