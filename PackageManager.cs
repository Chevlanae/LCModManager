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
            public string name { get; set; }
            public string version_number { get; set; }
            public string website_url { get; set; }
            public string description { get; set; }
            public string[] dependencies { get; set; }

            public ModManifest(Stream fileData)
            {
                this = JsonSerializer.Deserialize<ModManifest>(fileData);
            }

            public ModManifest(string fileString)
            {
                this = JsonSerializer.Deserialize<ModManifest>(fileString);
            }
        }

        internal class ModPackage
        {

            public string Filename;
            public string Filepath;
            public string? ReadMe;
            public string? ChangeLog;
            public BitmapImage? Icon;
            private ModManifest Manifest;

            public string Name
            {
                get
                {
                    return Manifest.name;
                }
            }

            public string Version
            {

                get
                {
                    return Manifest.version_number;
                }
            }

            public string Website
            {

                get
                {
                    return Manifest.website_url;
                }
            }

            public string Description
            {

                get
                {
                    return Manifest.description;
                }
            }

            public string[] Dependencies
            {

                get
                {
                    return Manifest.dependencies;
                }
            }

            public ModPackage(ZipArchive zip, string sourcePath)
            {
                Filepath = sourcePath;
                Filename = Filepath.Split("\\").Last();

                ZipArchiveEntry? readme = zip.GetEntry("README.md");
                ZipArchiveEntry? changelog = zip.GetEntry("CHANGELOG.md");
                ZipArchiveEntry? icon = zip.GetEntry("icon.png");
                ZipArchiveEntry? manifest = zip.GetEntry("manifest.json");
                StreamReader reader;

                if (readme != null)
                {
                    reader = new StreamReader(readme.Open());

                    ReadMe = reader.ReadToEnd();
                }
                else ReadMe = null;

                if (changelog != null)
                {
                    reader = new(changelog.Open());
                    ChangeLog = reader.ReadToEnd();
                }
                else changelog = null;

                if (icon != null)
                {
                    Icon = new BitmapImage();
                    Icon.BeginInit();
                    Icon.StreamSource = icon.Open();
                    Icon.CacheOption = BitmapCacheOption.OnLoad;
                    Icon.EndInit();
                }
                else Icon = null;

                if (manifest != null)
                {
                    using Stream file = manifest.Open();
                    reader = new(file);
                    Manifest = new ModManifest(reader.ReadToEnd());
                }
                else Manifest = new ModManifest();

            }
        }

        internal class ModPackageEnumerable : IList<ModPackage>, IEnumerable<ModPackage>, INotifyCollectionChanged
        {
            private List<ModPackage> List = new List<ModPackage>();
            public event NotifyCollectionChangedEventHandler? CollectionChanged;

            public int Count
            {
                get { return List.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public ModPackageEnumerable(List<ModPackage> list)
            {
                List = list;
            }

            public ModPackage this[int index]
            {
                get
                {
                    return List[index];
                }
                set
                {
                    Insert(index, value);
                }
            }

            public void Insert(int index, ModPackage package)
            {
                List.Insert(index, package);
                OnCollectionChanged();
            }

            public void Add(ModPackage package)
            {
                List.Add(package);
                OnCollectionChanged();
            }

            public void Clear()
            {
                List.Clear();
                OnCollectionChanged();
            }

            public bool Contains(ModPackage package)
            {
                return List.Contains(package);
            }

            public void CopyTo(ModPackage[] packageArr)
            {
                List.CopyTo(packageArr);
            }

            public void CopyTo(ModPackage[] packageArr, int index)
            {
                List.CopyTo(packageArr, index);
            }

            public bool Remove(ModPackage package)
            {
                try
                {
                    List.Remove(package);
                    OnCollectionChanged();
                    return true;
                }
                catch { return false; }
            }

            public void RemoveAt(int index)
            {
                List.RemoveAt(index);
                OnCollectionChanged();
            }

            public int IndexOf(ModPackage package)
            {
                return List.IndexOf(package);
            }


            public bool Exists(Predicate<ModPackage> predicate)
            {
                return List.Exists(predicate);
            }

            public List<ModPackage> FindAll(Predicate<ModPackage> predicate)
            {
                return List.FindAll(predicate);
            }
            public IEnumerator<ModPackage> GetEnumerator()
            {
                return List.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() 
            {
                return GetEnumerator();
            }

            protected void OnCollectionChanged([CallerMemberName] string name = null)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        internal class PackageManager
        {
            readonly string StorePath = AppConfig.PackageStorePath;
            public ModPackageEnumerable Packages;


            public PackageManager()
            {
                Packages = new ModPackageEnumerable([]);

                if (!Directory.Exists(StorePath)) Directory.CreateDirectory(StorePath);

                RefreshMods();

            }

            public void AddFile(string packagePath)
            {
                if (File.Exists(packagePath))
                {
                    string filename = packagePath.Split("\\").Last();
                    string destPath = StorePath + "\\" + filename;

                    if (!File.Exists(destPath)) File.Copy(packagePath, destPath);

                    using ZipArchive zip = ZipFile.Open(destPath, ZipArchiveMode.Read);

                    Packages.Add(new ModPackage(zip, destPath));
                }
            }

            public void RemoveFile(ModPackage package)
            {
                if (File.Exists(package.Filepath)) File.Delete(package.Filepath);

                Packages.Remove(package);
            }
            
            public void RefreshMods()
            {
                Packages.Clear();
                foreach (var file in Directory.GetFiles(StorePath)) AddFile(file);
            }

            public List<ModPackage> SearchMods(string queryPattern)
            {
                return Packages.FindAll(p => Regex.Match(p.Filename, queryPattern).Success || Regex.Match(p.Name, queryPattern).Success);
            }

            public List<string> GetMissingDependencies()
            {
                List<string> missingDependencies = [];

                foreach (var package in Packages)
                {
                    foreach (var dep in package.Dependencies)
                    {
                        if (SearchMods(dep).Count == 0)
                        {
                            missingDependencies.Add(dep);
                        }
                    }
                }

                return missingDependencies;
            }
        }
    }
}
