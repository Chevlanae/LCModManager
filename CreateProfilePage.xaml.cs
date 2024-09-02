using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Serialization;
using LCModManager.Thunderstore;
using System.Windows.Documents;

namespace LCModManager
{
    public class ModProfile
    {
        public string? Name { get; set; }
        public List<ModEntryBase> ModList { get; set; }

        public ModProfile()
        {
            Name = "";
            ModList = [];
        }

        public ModProfile(string name)
        {
            Name = name;
            ModList = [];
        }

        public ModProfile(string name, IEnumerable<ModEntryBase> modList)
        {
            Name = name;
            ModList = [];

            foreach (ModEntryBase item in modList) ModList.Add(item);
        }
    }

    /// <summary>
    /// Interaction logic for CreateProfilePage.xaml
    /// </summary>
    public partial class CreateProfilePage : Page
    {
        static private string ProfileStore = AppConfig.ResourcePath + "\\profiles";
        public ObservableCollection<ModProfile> Profiles;
        public ModProfile? SelectedProfile;
        public ObservableCollection<ModEntry> ModList;

        public CreateProfilePage()
        {
            if (!Directory.Exists(ProfileStore)) Directory.CreateDirectory(ProfileStore);

            Profiles = [];
            ModList = [];

            RefreshProfiles();

            InitializeComponent();

            ProfileSelectorControl.ItemsSource = Profiles;
            ModListControl.ItemsSource = ModList;
        }

        static private ModProfile? GetProfile(string path)
        {
            if (path[^4..^0].ToString() == ".xml")
            {
                XmlSerializer x = new(typeof(ModProfile));

                using Stream fileReader = File.OpenRead(path);

                if (x.Deserialize(fileReader) is ModProfile profile)
                {
                    fileReader.Close();
                    fileReader.Dispose();

                    return profile;
                }
                else
                {
                    fileReader.Close();
                    fileReader.Dispose();

                    return null;
                }
            }
            else return null;
        }

        static private List<ModProfile> GetProfiles()
        {
            List<ModProfile> list = [];

            foreach (string file in Directory.GetFiles(ProfileStore))
            {
                if(GetProfile(file) is ModProfile profile) list.Add(profile);
            }

            return list;
        }

        static private void AddProfile(ModProfile newProfile)
        {
            string path = ProfileStore + "\\" + newProfile.Name + ".xml";

            if (!File.Exists(path))
            {
                using Stream newFile = File.Create(ProfileStore + "\\" + newProfile.Name + ".xml");

                XmlSerializer x = new(newProfile.GetType());

                x.Serialize(newFile, newProfile);

                newFile.Close();
                newFile.Dispose();
            }
        }

        static private void SaveProfile(ModProfile profile)
        {
            string path = ProfileStore + "\\" + profile.Name + ".xml";

            if (File.Exists(path))
            {
                using Stream file = File.OpenWrite(path);
                XmlSerializer x = new(profile.GetType());

                x.Serialize(file, profile);

                file.Close();
                file.Dispose();
            }

        }

        private void RefreshProfiles()
        {
            Profiles.Clear();

            foreach (ModProfile profile in GetProfiles())
            {
                Profiles.Add(profile);
            }
        }

        private void RefreshModList()
        {
            if (SelectedProfile != null)
            {
                ModList.Clear();

                List<ModPackage> packages = PackageManager.GetPackages();

                foreach (ModEntryBase entry in SelectedProfile.ModList)
                {
                    if(entry.Name != null)
                    {
                        bool found = false;
                        foreach (ModPackage package in packages)
                        {
                            //If package is installed
                            if (package.Name != null && entry.Name == package.Name)
                            {
                                ModList.Add(package);
                                found = true;
                                break;
                            }

                        }

                        if (!found) { }
                    }
                }

                foreach (ModEntry mod in ModList) mod.GetMissingDependencies(ModList);
            }
        }

        private void AddModsButton_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedProfile != null)
            {
                CreateProfileDialog dialog = new(ModList);

                if (dialog.ShowDialog() == true)
                {
                    foreach (ModPackage package in dialog.ModListControl.SelectedItems)
                    {
                        SelectedProfile.ModList.Add(package.ToModEntryBase());
                    }

                    SaveProfile(SelectedProfile);

                    RefreshModList();
                }
            }

            e.Handled = true;
        }

        private void ProfileSelectorControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] is ModProfile profile)
            {
                SelectedProfile = profile;
                RefreshModList();
            }

            e.Handled = true;
        }

        private void RemoveModsButton_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedProfile != null)
            {
                List<ModPackage> items = [];

                foreach (ModPackage entry in ModListControl.SelectedItems)
                {
                    items.Add(entry);
                }

                foreach (ModPackage package in items)
                {
                    SelectedProfile.ModList.RemoveAll(x => x.Name == package.Name);
                }

                RefreshModList();
            }

            e.Handled = true;
        }
    }
}
