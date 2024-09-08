using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using LCModManager.Thunderstore;
using Microsoft.Win32;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for CreateProfilePage.xaml
    /// </summary>
    public partial class CreateProfilePage : Page
    {
        public ObservableCollection<ModEntryDisplay> ModList = [];

        public CreateProfilePage()
        {
            InitializeComponent();

            ModListControl.ItemsSource = ModList;

            foreach (ModProfile profile in ProfileManager.GetProfiles())
            {
                ProfileSelectorControl.Items.Add(profile);
            }

            RefreshModList();
        }

        private void RefreshModList()
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                ModList.Clear();

                foreach (ModEntry entry in profile.ModList)
                {
                    Regex entryRegex = new (entry.Name + "-" + entry.Version.Replace(".", "\\."));

                    foreach (ModEntryDisplay mod in PackageManager.GetPackages(entryRegex))
                    {
                        ModList.Add(mod);
                    }
                }

                foreach (ModEntryDisplay mod in ModList)
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
                        if (package is ModEntryDisplay modEntry)
                        {
                            profile.Add(modEntry.ToModEntry());
                        }
                        else if(package is ModEntry modEntryBase)
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

                foreach (ModEntry entry in ModListControl.SelectedItems)
                {
                    profile.ModList.RemoveAll(e => e.Name == entry.Name && e.Version == entry.Version);
                }

                ProfileManager.SaveProfile(profile);

                RefreshModList();
            }

            e.Handled = true;
        }
    }
}
