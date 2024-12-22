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

        static public IModEntry FromLocalPath(string localPath) => throw new NotImplementedException();
        static public IModEntry FromUri(Uri uri) => throw new NotImplementedException();
        static public IModEntry FromModEntry(IModEntry modEntry) => throw new NotImplementedException();

        public void GetIcon(Uri uri);
        public void GetIcon(Stream stream);
        public void ProcessDependencies(List<IModEntry> modlist);
    }

    [DataContract]
    public partial class ModPackage : IModEntry
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
        public string[]? MissingDependencies { get; set; }
        public string[]? MismatchedDependencies { get; set; }

        public bool SelectedVersionsExist
        {
            get
            {
                return SelectedVersions.All(v => Versions.Keys.Contains(v) && AppConfig.PackageStore.IsBaseOf(Versions[v]) && File.Exists(Versions[v].LocalPath));
            }
        }

        public bool ExistsInPackageStore
        {
            get
            {
                return Versions.Any(v => AppConfig.PackageStore.IsBaseOf(v.Value) && File.Exists(v.Value.LocalPath));
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

        public void GetIcon(Uri uri)
        {
            Icon = new BitmapImage();
            Icon.BeginInit();
            Icon.UriSource = uri;
            Icon.DecodePixelWidth = 64;
            Icon.DecodePixelHeight = 64;
            Icon.CacheOption = BitmapCacheOption.OnLoad;
            Icon.EndInit();
        }

        public void GetIcon(Stream sourceStream)
        {
            using (sourceStream)
            using (MemoryStream ms = new())
            {
                sourceStream.CopyTo(ms);
                ms.Position = 0;
                Icon = new BitmapImage();
                Icon.BeginInit();
                Icon.StreamSource = ms;
                Icon.DecodePixelWidth = 64;
                Icon.DecodePixelHeight = 64;
                Icon.CacheOption = BitmapCacheOption.OnLoad;
                Icon.EndInit();
                Icon.Freeze();
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
                    string[] depSplit = depStr.Split('-');
                    string owner = depSplit[0];
                    string name = depSplit[1];
                    string version = depSplit[2];

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
                            foreach (IModEntry item in foundDependencies)
                            {
                                if (item.Versions.ContainsKey(version))
                                {
                                    mismatchedDeps.Add(depStr);
                                    break;
                                }

                            }
                            break;
                    }
                }

                MissingDependencies = [.. missingDeps];
                MismatchedDependencies = [.. mismatchedDeps];
            }
        }
    }
}
