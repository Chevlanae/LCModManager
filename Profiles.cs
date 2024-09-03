using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Xml.Serialization;

namespace LCModManager
{
    namespace Converters
    {
        [ValueConversion(typeof(ModProfile), typeof(String))]
        public class ModProfileConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is ModProfile profile)
                {
                    return profile.Name;
                }
                else return "";
            }

            public object ConvertBack(object value, Type targetType, object paramter, CultureInfo culture)
            {
                if (value is string name)
                {
                    string path = Profiles.StorePath + "\\" + name + ".xml";
                    ModProfile? profile = Profiles.GetProfile(path);

                    if (profile != null) return profile;
                    else return new ModProfile();
                }
                else return new ModProfile();
            }
        }
    }

    public class ModProfile
    {

        public List<ModEntryBase> ModList = [];

        public string Name { get; set; }

        public int Count => ModList.Count;


        public ModEntryBase this[int index] 
        { 
            get
            {
                return ModList[index];
            }
            set
            {
                if( value is ModEntry modEntry)
                {
                    ModList[index] = modEntry.ToModEntryBase();
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

        public ModProfile(string name, IEnumerable<ModEntryBase> modList)
        {
            Name = name;

            int i = 0;
            foreach(ModEntryBase mod in modList)
            {
                this[i] = mod;
                i++;
            }
        }

        new public string ToString()
        {
            return Name;
        }

        public int IndexOf(ModEntryBase item)
        {
            return ModList.IndexOf(item);
        }

        public void Insert(int index, ModEntryBase item)
        {
            ModList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ModList.RemoveAt(index);
        }

        internal void RemoveAll(Func<ModEntryBase, bool> value)
        {
            List<ModEntryBase> removedItems = [];

            foreach(ModEntryBase item in ModList)
            {
                if (value(item)) removedItems.Add(item);
            }

            foreach(ModEntryBase item in removedItems)
            {
                ModList.Remove(item);
            }
        }

        public void Add(ModEntryBase item)
        {
            ModList.Add(item);
        }

        public void Clear()
        {
            ModList.Clear();
        }

        public bool Contains(ModEntryBase item)
        {
            return ModList.Contains(item);
        }

        public void CopyTo(ModEntryBase[] array, int arrayIndex)
        {
            ModList.CopyTo(array, arrayIndex);
        }

        public bool Remove(ModEntryBase item)
        {
            return ModList.Remove(item);
        }
    }

    static internal class Profiles
    {
        static public string StorePath = AppConfig.ResourcePath + "\\profiles";

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
            if (!Directory.Exists(StorePath)) Directory.CreateDirectory(StorePath);

            List<ModProfile> list = [];

            foreach (string file in Directory.GetFiles(StorePath))
            {
                if (GetProfile(file) is ModProfile profile) list.Add(profile);
            }

            return list;
        }

        static public void AddProfile(ModProfile newProfile)
        {
            if (!Directory.Exists(StorePath)) Directory.CreateDirectory(StorePath);
            string path = StorePath + "\\" + newProfile.Name + ".xml";

            if (!File.Exists(path))
            {
                using Stream newFile = File.Create(StorePath + "\\" + newProfile.Name + ".xml");

                XmlSerializer x = new(newProfile.GetType());

                x.Serialize(newFile, newProfile);

                newFile.Close();
                newFile.Dispose();
            }
        }

        static public void SaveProfile(ModProfile profile)
        {
            if (!Directory.Exists(StorePath)) Directory.CreateDirectory(StorePath);
            string path = StorePath + "\\" + profile.Name + ".xml";

            if (File.Exists(path))
            {
                using Stream file = File.OpenWrite(path);
                XmlSerializer x = new(profile.GetType());

                x.Serialize(file, profile);

                file.Close();
                file.Dispose();
            }
        }

        static public void DeleteProfile(ModProfile profile)
        {
            if (!Directory.Exists(StorePath)) Directory.CreateDirectory(StorePath);
            string path = StorePath + "\\" + profile.Name + ".xml";

            if (File.Exists(path)) File.Delete(path);
        }
    }
}
