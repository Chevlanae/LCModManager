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
        public ObservableCollection<ModEntry> ModList = [];

        public CreateProfilePage()
        {
            InitializeComponent();


            ModListControl.ItemsSource = ModList;

            RefreshProfiles();
            RefreshModList();

        }



        private void RefreshProfiles()
        {
            ProfileSelectorControl.Items.Clear();

            foreach (ModProfile profile in Profiles.GetProfiles())
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

                    if(mod != null)
                    {
                        ModList.Add(mod);
                    }
                }

                foreach (ModEntry mod in ModList)
                {
                    mod.GetMissingDependencies(ModList);
                }
            }
        }

        private void CreateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            CreateProfileDialog dialog = new();

            if (dialog.ShowDialog() == true)
            {
                ModProfile newProfile = new(dialog.NewProfileName.Text);

                Profiles.AddProfile(newProfile);

                ProfileSelectorControl.SelectedIndex = ProfileSelectorControl.Items.Add(newProfile);

            }

            e.Handled = true;
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                Profiles.DeleteProfile(profile);

                RefreshProfiles();

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

                    Profiles.SaveProfile(profile);

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
