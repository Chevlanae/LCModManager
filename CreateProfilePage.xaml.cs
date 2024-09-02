using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LCModManager.Thunderstore;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for CreateProfilePage.xaml
    /// </summary>
    public partial class CreateProfilePage : Page
    {
        public ObservableCollection<ModProfile> ProfileList;
        public ObservableCollection<ModEntry> ModList;
        public ModProfile? SelectedProfile;

        public CreateProfilePage()
        {
            ProfileList = [];
            ModList = [];

            InitializeComponent();

            ProfileSelectorControl.ItemsSource = ProfileList;
            ModListControl.ItemsSource = ModList;

            RefreshProfiles();
            RefreshModList();
        }



        private void RefreshProfiles()
        {
            ProfileList.Clear();

            foreach (ModProfile profile in Profiles.GetProfiles())
            {
                ProfileList.Add(profile);
            }
        }

        private void RefreshModList()
        {
            if (SelectedProfile != null)
            {
                ModList.Clear();

                foreach (ModEntryBase entry in SelectedProfile.ModList)
                {
                    ModEntry? mod = PackageManager.GetFromName(entry.Name);

                    if(mod != null)
                    {
                        ModList.Add(mod);
                    }
                }

                foreach (ModEntry mod in ModList) mod.GetMissingDependencies(SelectedProfile.ModList);
            }
        }

        private void ProfileSelectorControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ModProfile profile)
            {
                SelectedProfile = profile;
                RefreshModList();
            }

            e.Handled = true;
        }

        private void AddModsButton_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedProfile != null)
            {
                AddModsDialog dialog = new(ModList);

                if (dialog.ShowDialog() == true)
                {
                    foreach (var package in dialog.ModListControl.SelectedItems)
                    {
                        if (package is ModEntry modEntry)
                        {
                            SelectedProfile.Add(modEntry.ToModEntryBase());
                        }
                        else if(package is ModEntryBase modEntryBase)
                        {
                            SelectedProfile.Add(modEntryBase);
                        }
                    }

                    Profiles.SaveProfile(SelectedProfile);

                    RefreshModList();
                }
            }

            e.Handled = true;
        }

        private void RemoveModsButton_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedProfile != null)
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
                        SelectedProfile.RemoveAll(x => x.Name == modEntry.Name);
                    }
                }

                RefreshModList();
            }

            e.Handled = true;
        }

        private void CreateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            CreateProfileDialog dialog = new();

            if(dialog.ShowDialog() == true)
            {
                ModProfile newProfile = new(dialog.NewProfileName.Text);

                Profiles.AddProfile(newProfile);

                SelectedProfile = newProfile;

                ProfileSelectorControl.SelectedValue = SelectedProfile;

                RefreshProfiles();
            }

            e.Handled = true;
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedProfile != null)
            {
                ProfileList.Remove(SelectedProfile);
                Profiles.DeleteProfile(SelectedProfile);

                SelectedProfile = null;
                ModList.Clear();

                RefreshProfiles();
            }

            e.Handled = true;
        }
    }
}
