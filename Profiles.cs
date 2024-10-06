using LCModManager.Thunderstore;
using System.Globalization;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Windows.Data;
using System.Xml.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LCModManager
{
    public class ModProfile
    {
        public List<PackageState> PackageList = [];

        public string Name { get; set; }

        public int Count => PackageList.Count;

        public PackageState this[int index]
        {
            get
            {
                return PackageList[index];
            }
            set
            {
                PackageList[index] = value;
            }
        }

        public ModProfile()
        {
            Name = "";
        }

        public ModProfile(string name)
        {
            Name = name;
        }

        public ModProfile(string name, IEnumerable<ModEntryDisplay> modList)
        {
            Name = name;

            int i = 0;
            foreach (ModEntryDisplay mod in modList)
            {
                this[i] = new PackageState(mod.SelectedVersions[0], mod.ToModEntry());
                i++;
            }
        }

        async public Task DownloadDependency(ModEntry entry, string version)
        {
            if (entry.Author != null)
            {
                DownloadDependency(entry.Author + "-" + entry.Name + "-" + version);
            }
        }

        async public Task DownloadDependency(string dependencyString)
        {
            string[] depParts = dependencyString.Split("-");
            string owner = depParts[^3];
            string name = depParts[^2];
            string version = depParts[^1];

            PackageListing? query = WebClient.GetCachedPackage(owner + "-" + name);

            if (query != null)
            {
                PackageListingVersionEntry versionEntry = query.Value.versions.First(v => v.version_number == version);

                string? downloadPath = await WebClient.DownloadPackage(versionEntry);

                if (downloadPath != null)
                {
                    ModPackage? newPackage = await PackageManager.AddPackage(downloadPath);

                    if (newPackage != null)
                    {
                        PackageState? existingState = PackageList.Find(p => p.ModEntry.Author == owner &&
                                                                            p.ModEntry.Name == name);

                        if (existingState != null && !existingState.ModEntry.ExistsInPackageStore)
                        {
                            existingState.SelectedVersion = version;
                            existingState.ModEntry = newPackage.ToModEntry();
                        }
                        else if (existingState == null)
                        {
                            Add(new PackageState(version, newPackage.ToModEntry()));
                        }
                    }
                }
            }
        }

        async public Task DownloadDependencies(string[] dependencies)
        {
            foreach (string dep in dependencies)
            {
                DownloadDependency(dep);
            }
        }

        new public string ToString()
        {
            return Name;
        }

        public int IndexOf(PackageState item)
        {
            return PackageList.IndexOf(item);
        }

        public void Insert(int index, PackageState item)
        {
            PackageList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            PackageList.RemoveAt(index);
        }

        internal void RemoveAll(Func<PackageState, bool> value)
        {
            List<PackageState> removedItems = [];

            foreach (PackageState item in PackageList)
            {
                if (value(item)) removedItems.Add(item);
            }

            foreach (PackageState item in removedItems)
            {
                PackageList.Remove(item);
            }
        }

        public void Add(PackageState item)
        {
            PackageList.Add(item);
        }

        public void Clear()
        {
            PackageList.Clear();
        }

        public bool Contains(PackageState item)
        {
            return PackageList.Contains(item);
        }

        public void CopyTo(PackageState[] array, int arrayIndex)
        {
            PackageList.CopyTo(array, arrayIndex);
        }

        public bool Remove(PackageState item)
        {
            return PackageList.Remove(item);
        }
    }

    static internal class ProfileManager
    {
        static public ModProfile? GetProfile(string path)
        {
            if (path[^4..^0].ToString() == ".xml")
            {
                try
                {
                    XmlSerializer x = new(typeof(ModProfile));

                    using Stream fileReader = File.OpenRead(path);

                    ModProfile? result = null;

                    if (x.Deserialize(fileReader) is ModProfile profile)
                    {
                        result = profile;
                    }

                    fileReader.Close();
                    fileReader.Dispose();

                    return result;
                }
                catch
                {
                    return null;
                }
            }
            else return null;
        }

        static public List<ModProfile> GetProfiles()
        {
            List<ModProfile> list = [];

            foreach (string file in Directory.GetFiles(AppConfig.ProfileStorePath))
            {
                if (GetProfile(file) is ModProfile profile) list.Add(profile);
            }

            return list;
        }

        static public void AddProfile(ModProfile newProfile)
        {
            string path = AppConfig.ProfileStorePath + "\\" + newProfile.Name + ".xml";

            if (!File.Exists(path))
            {
                using Stream newFile = File.Create(AppConfig.ProfileStorePath + "\\" + newProfile.Name + ".xml");

                XmlSerializer x = new(newProfile.GetType());

                x.Serialize(newFile, newProfile);

                newFile.Close();
                newFile.Dispose();
            }
        }

        static public void SaveProfile(ModProfile profile)
        {
            string dirPath = AppConfig.ProfileStorePath + "\\" + profile.Name;
            string filePath = dirPath + ".xml";

            if (File.Exists(filePath))
            {
                using Stream file = File.Create(filePath);
                XmlSerializer x = new(profile.GetType());

                x.Serialize(file, profile);

                file.Close();
                file.Dispose();
            }

            if (Directory.Exists(dirPath)) Directory.Delete(dirPath, true);
        }

        static public void DeleteProfile(ModProfile profile)
        {
            string path = AppConfig.ProfileStorePath + "\\" + profile.Name + ".xml";

            if (File.Exists(path)) File.Delete(path);
        }
    }
}
