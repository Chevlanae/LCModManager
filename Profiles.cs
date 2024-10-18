using LCModManager.Thunderstore;
using System.Globalization;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Serialization;
using System.Windows.Data;
using System.Xml.Serialization;

namespace LCModManager
{
    [DataContract]
    public class ModProfile
    {
        [DataMember]
        public List<ModEntrySelection> PackageList = [];

        [DataMember]
        public string Name { get; set; }

        public ModEntrySelection this[int index]
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
                this[i] = mod.ToModEntrySelection(mod.SelectedVersions[0]);
                i++;
            }
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
            string filePath = AppConfig.ProfileStorePath + "\\" + profile.Name + ".xml";
            string directoryPath = AppConfig.ProfileStorePath + "\\" + profile.Name;

            if (File.Exists(filePath)) File.Delete(filePath);
            if (Directory.Exists(directoryPath)) Directory.Delete(directoryPath, true);
        }
    }
}
