using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LCModManager
{
    public interface IModEntry
    {
        public BitmapImage? Icon { get; set; }
        public string SourceName { get; set; }
        public string Name { get; set; }
        public string? Author { get; set; }
        public string? Description { get; set; }
        public string? Website { get; set; }
        public Dictionary<string, Uri> Versions { get; set; }
        public List<string> SelectedVersions { get; set; }
        public string[]? Dependencies { get; set; }
        public string[]? MissingDependencies { get; set; }
        public string[]? MismatchedDependencies { get; set; }
        public bool SelectedVersionsExist { get; }
        public bool ExistsInPackageStore { get; }
        public bool HasMissingDependencies { get; }
        public bool HasMismatchedDependencies { get; }
        public bool HasIncompatibility { get; }

        public void FromLocalPath(string localPath);
        public void GetIcon(Uri uri);
        public void GetIcon(Stream stream);
        public void ProcessDependencies(List<IModEntry> modlist);
    }

    [DataContract]
    public abstract class ModPackage : IModEntry
    {
        public BitmapImage? Icon { get; set; }

        [DataMember]
        public string SourceName { get; set; }
        [DataMember]
        public string Name { get; set; } = "";
        [DataMember]
        public string? Author { get; set; }
        [DataMember]
        public string? Description { get; set; }
        [DataMember]
        public string? Website { get; set; }
        [DataMember]
        public Dictionary<string, Uri> Versions { get; set; } = new();
        [DataMember]
        public List<string> SelectedVersions { get; set; } = [];
        [DataMember]
        public string[]? Dependencies { get; set; }
        [DataMember]
        public string[]? MissingDependencies { get; set; }
        [DataMember]
        public string[]? MismatchedDependencies { get; set; }

        public bool SelectedVersionsExist
        {
            get
            {
                return SelectedVersions.All(v => AppConfig.PackageStore.IsBaseOf(Versions[v]) && File.Exists(Versions[v].LocalPath));
            }
        }

        public bool ExistsInPackageStore
        {
            get
            {
                return Versions.Any(v => AppConfig.PackageStore.IsBaseOf(v.Value) && File.Exists(v.Value.AbsolutePath));
            }
        }

        public bool HasMissingDependencies
        {
            get
            {
                return MissingDependencies != null && MissingDependencies.Length > 0;
            }
        }

        public bool HasMismatchedDependencies
        {
            get
            {
                return MismatchedDependencies != null && MismatchedDependencies.Length > 0;
            }
        }

        public bool HasIncompatibility
        {
            get
            {
                return HasMissingDependencies || HasMismatchedDependencies;
            }
        }

        public abstract void FromUri(Uri uri);
        public abstract void FromModEntry(IModEntry modEntry);
        public void FromLocalPath(string localPath)
        {
            FromUri(new Uri(localPath));
        }

        public void GetIcon(Uri uri)
        {
            if (uri.IsFile)
            {
                Icon = new BitmapImage();
                Icon.BeginInit();
                Icon.UriSource = uri;
                Icon.DecodePixelWidth = 64;
                Icon.DecodePixelHeight = 64;
                Icon.CacheOption = BitmapCacheOption.OnLoad;
                Icon.Freeze();
                Icon.EndInit();
            }

        }

        public void GetIcon(Stream stream)
        {
            using (stream)
            {
                Icon = new BitmapImage();
                Icon.BeginInit();
                Icon.StreamSource = stream;
                Icon.DecodePixelWidth = 64;
                Icon.DecodePixelHeight = 64;
                Icon.CacheOption = BitmapCacheOption.OnLoad;
                Icon.Freeze();
                Icon.EndInit();
            }
        }

        public void ProcessDependencies(List<IModEntry> modlist)
        {
            if (Dependencies != null)
            {
                List<string> missingDeps = [];
                List<string> mismatchedDeps = [];

                foreach (string depStr in Dependencies)
                {
                    string[] depStrParts = depStr.Split('-');
                    string owner = depStrParts[^3];
                    string name = depStrParts[^2];
                    string version = depStrParts[^1];

                    List<IModEntry> foundDependencies = modlist.FindAll(e => e.Name == name && e.Author == owner);

                    switch (foundDependencies.Count)
                    {
                        case 0:

                            missingDeps.Add(depStr);
                            break;

                        case 1:

                            if (!foundDependencies[0].ExistsInPackageStore)
                            {
                                missingDeps.Add(depStr);
                            }
                            else if (!foundDependencies[0].Versions.ContainsKey(version))
                            {
                                mismatchedDeps.Add(depStr);
                            }
                            break;

                        default:
                            bool match = false;
                            foreach (IModEntry item in foundDependencies)
                            {
                                if (item.Versions.ContainsKey(version))
                                {
                                    match = true;
                                    break;
                                }

                            }
                            if (!match) mismatchedDeps.Add(depStr);
                            break;
                    }
                }

                MismatchedDependencies = [.. mismatchedDeps];
                MissingDependencies = [.. missingDeps];
            }
        }
    }
}
