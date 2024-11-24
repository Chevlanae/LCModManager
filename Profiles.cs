using LCModManager.Thunderstore;
using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;

namespace LCModManager
{
    [DataContract]
    [KnownType(typeof(ModPackage))]
    [KnownType(typeof(Mod))]
    public class ModProfile
    {
        [DataMember]
        public List<IModEntry> ModList { get; set; }

        [DataMember]
        public string Name { get; set; }

        public ModProfile()
        {
            ModList = [];
            Name = "";
        }

        public ModProfile(string name)
        {
            ModList = [];
            Name = name;
        }

        public ModProfile(string name, string storeName, List<IModEntry> modList)
        {
            ModList = [];
            Name = name;

            foreach(IModEntry mod in modList)
            {
                ModList.Add(mod);
            }
        }
    }

    static internal class ProfileManager
    {
        static private string StorePath = AppConfig.ProfileStore.AbsolutePath;

        static public ModProfile? GetProfile(string path)
        {
            if (path[^4..^0].ToString() == ".xml")
            {
                try
                {
                    DataContractSerializer serializer = new(typeof(ModProfile));

                    ModProfile? result = null;

                    using (Stream file = File.OpenRead(path))
                    {
                        result = (ModProfile)serializer.ReadObject(file);
                    }

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

            foreach (string file in Directory.GetFiles(StorePath))
            {
                if (GetProfile(file) is ModProfile profile) list.Add(profile);
            }

            return list;
        }

        static public void AddProfile(ModProfile newProfile)
        {
            string filePath = StorePath + "\\" + newProfile.Name + ".xml";

            if (!File.Exists(filePath))
            {
                DataContractSerializer serializer = new(newProfile.GetType());

                using (Stream newFile = File.Create(filePath))
                {
                    serializer.WriteObject(newFile, newProfile);
                }
            }
        }

        static public void SaveProfile(ModProfile profile)
        {
            string filePath = StorePath + "\\" + profile.Name + ".xml";

            if (File.Exists(filePath))
            {
                DataContractSerializer serializer = new(profile.GetType());

                using (Stream newFile = File.Create(filePath))
                {
                    serializer.WriteObject(newFile, profile);
                }
            }
        }

        static public void DeleteProfile(ModProfile profile)
        {
            string filePath = StorePath + "\\" + profile.Name + ".xml";

            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }
}
