using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LCModManager.Thunderstore;
using Microsoft.Win32;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for CreateProfilePage.xaml
    /// </summary>
    public partial class CreateProfilePage : Page
    {
        public ObservableCollection<ModEntry> ModList = [];

        public CreateProfilePage()
        {
            InitializeComponent();

            ModListControl.ItemsSource = ModList;

            RefreshModList();

            foreach (ModProfile profile in ProfileManager.GetProfiles())
            {
                ProfileSelectorControl.Items.Add(profile);
            }
        }

        private void RefreshModList()
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                ModList.Clear();

                foreach (ModEntryBase entry in profile.ModList)
                {
                    ModEntry? mod = PackageManager.GetFromName(entry.Name);

                    if(mod != null) ModList.Add(mod);
                }

                foreach (ModEntry mod in ModList)
                {
                    mod.ProcessDependencies(ModList);
                }
            }
        }

        private void CreateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            CreateProfileDialog dialog = new();

            if (dialog.ShowDialog() == true)
            {
                ModProfile newProfile = new(dialog.NewProfileName.Text);

                if (!ProfileSelectorControl.Items.Contains(newProfile))
                {
                    ProfileManager.AddProfile(newProfile);

                    ProfileSelectorControl.SelectedIndex = ProfileSelectorControl.Items.Add(newProfile);
                }
            }

            e.Handled = true;
        }

        private void ImportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                DefaultExt = ".xml",
                Filter = "XML Files (*.xml)|*.xml",
                Multiselect = true,

            };

            bool? result = dialog.ShowDialog();

            if (result == true && dialog.FileNames.Length > 0)
            {
                foreach (string filename in dialog.FileNames)
                {
                    if (ProfileManager.GetProfile(filename) is ModProfile profile)
                    {
                        bool found = false;
                        foreach(ModProfile existingProfile in ProfileManager.GetProfiles())
                        {
                            if(existingProfile.Name == profile.Name)
                            {
                                found = true; 
                                break;
                            }
                        }

                        if (!found)
                        {
                            ProfileManager.AddProfile(profile);
                            ProfileSelectorControl.SelectedIndex = ProfileSelectorControl.Items.Add(profile);
                        }
                    }
                }
            }

            e.Handled = true;
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                ProfileManager.DeleteProfile(profile);

                ProfileSelectorControl.Items.Remove(profile);

                ModList.Clear();
            }

            e.Handled = true;
        }

        private void ProfileSelectorControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshModList();
        }

        private void AddModsButton_Click(object sender, RoutedEventArgs e)
        {
            if(ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                AddModsDialog dialog = new(ModList);

                if (dialog.ShowDialog() == true)
                {
                    foreach (var package in dialog.ModListControl.SelectedItems)
                    {
                        if (package is ModEntry modEntry)
                        {
                            profile.Add(modEntry.ToModEntryBase());
                        }
                        else if(package is ModEntryBase modEntryBase)
                        {
                            profile.Add(modEntryBase);
                        }
                    }

                    ProfileManager.SaveProfile(profile);

                    RefreshModList();
                }
            }

            e.Handled = true;
        }

        private void RemoveModsButton_Click(object sender, RoutedEventArgs e)
        {
            if(ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                List<ModEntry> items = [];

                foreach (var entry in ModListControl.SelectedItems)
                {
                    if(entry is ModPackage modPackage)
                    {
                        items.Add(modPackage);
                    }
                }

                foreach (var package in items)
                {
                    if(package is ModEntry modEntry)
                    {
                        profile.RemoveAll(x => x.Name == modEntry.Name);
                    }
                }

                RefreshModList();
            }

            e.Handled = true;
        }
    }
}
