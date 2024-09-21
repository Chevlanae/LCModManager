using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Xml.Serialization;

namespace LCModManager
{
    public class ModProfile
    {
        public List<ModEntry> ModList = [];

        public string Name { get; set; }

        public int Count => ModList.Count;

        public ModEntry this[int index] 
        { 
            get
            {
                return ModList[index];
            }
            set
            {
                if (value is ModEntryDisplay modEntry)
                {
                    ModList[index] = modEntry.ToModEntry();
                }
                else
                {
                    ModList[index] = value;
                }
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

        public ModProfile(string name, IEnumerable<ModEntry> modList)
        {
            Name = name;

            int i = 0;
            foreach(ModEntry mod in modList)
            {
                this[i] = mod;
                i++;
            }
        }

        new public string ToString()
        {
            return Name;
        }

        public int IndexOf(ModEntry item)
        {
            return ModList.IndexOf(item);
        }

        public void Insert(int index, ModEntry item)
        {
            ModList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ModList.RemoveAt(index);
        }

        internal void RemoveAll(Func<ModEntry, bool> value)
        {
            List<ModEntry> removedItems = [];

            foreach(ModEntry item in ModList)
            {
                if (value(item)) removedItems.Add(item);
            }

            foreach(ModEntry item in removedItems)
            {
                ModList.Remove(item);
            }
        }

        public void Add(ModEntry item)
        {
            ModList.Add(item);
        }

        public void Clear()
        {
            ModList.Clear();
        }

        public bool Contains(ModEntry item)
        {
            return ModList.Contains(item);
        }

        public void CopyTo(ModEntry[] array, int arrayIndex)
        {
            ModList.CopyTo(array, arrayIndex);
        }

        public bool Remove(ModEntry item)
        {
            return ModList.Remove(item);
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

                    if(x.Deserialize(fileReader) is ModProfile profile)
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
