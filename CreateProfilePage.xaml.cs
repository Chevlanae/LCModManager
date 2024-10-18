using LCModManager.Thunderstore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for CreateProfilePage.xaml
    /// </summary>
    public partial class CreateProfilePage : Page
    {
        public ObservableCollection<ModEntryDisplay> ModList = [];
        private StatusBarControl _StatusBarControl;

        public CreateProfilePage(StatusBarControl statusBarCtrl)
        {
            _StatusBarControl = statusBarCtrl;

            InitializeComponent();

            ModListControl.ItemsSource = ModList;

            foreach (ModProfile profile in ProfileManager.GetProfiles())
            {
                ProfileSelectorControl.Items.Add(profile);
            }
        }

        async public void RefreshModList()
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                ModList.Clear();

                foreach (Package package in profile.PackageList)
                {
                    try
                    {
                        ModPackage newPackage = new(package.ModEntry.Path);
                        newPackage.Versions = [package.SelectedVersion];
                        ModList.Add(newPackage);
                    }
                    catch
                    {
                        List<PackageListing> query = WebClient.SearchPackageCache(p => p.Key.Split("-")[^1] == package.ModEntry.Name);

                        switch (query.Count)
                        {
                            case 1:

                                ModList.Add(new ModPackage(query[0], package.SelectedVersion));
                                break;
                            default:

                                bool match = false;
                                foreach (PackageListing item in query)
                                {
                                    if (item.owner == package.ModEntry.Author)
                                    {
                                        ModList.Add(new ModPackage(item, package.SelectedVersion));
                                        match = true;
                                        break;
                                    }
                                }

                                if (!match) ModList.Add(new ModPackage(package.ModEntry, true));
                                break;
                        }
                    }
                }

                foreach (ModEntryDisplay mod in ModList)
                {
                    mod.ProcessDependencies(ModList.ToList());
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

            if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
            {
                foreach (string filename in dialog.FileNames)
                {
                    if (ProfileManager.GetProfile(filename) is ModProfile profile && !ProfileManager.GetProfiles().Exists(p => p.Name == profile.Name))
                    {
                        ProfileManager.AddProfile(profile);
                        ProfileSelectorControl.SelectedIndex = ProfileSelectorControl.Items.Add(profile);
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
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                AddModsDialog dialog = new(ModList);

                if (dialog.ShowDialog() == true)
                {
                    foreach (ModPackage package in dialog.ModListControl.SelectedItems)
                    {
                        if (package.SelectedVersions.Count > 0)
                        {
                            package.Versions = [package.SelectedVersions[0]];
                        }
                        else
                        {
                            package.Versions = [package.Versions[0]];
                        }

                        Package state = new(package.Versions[0], package.ToModEntry());

                        if (!profile.Contains(state)) profile.Add(state);
                    }

                    ProfileManager.SaveProfile(profile);

                    RefreshModList();
                }
            }

            e.Handled = true;
        }

        private void RemoveModsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                foreach (ModEntry entry in ModListControl.SelectedItems)
                {
                    profile.PackageList.RemoveAll(e => e.ModEntry.Name == entry.Name);
                }

                ProfileManager.SaveProfile(profile);

                RefreshModList();
            }

            e.Handled = true;
        }

        async private void ResolveDependencies_Click()
        {
            ResolveDependencies_Click(null, new RoutedEventArgs());
        }

        private async void ResolveDependencies_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                while (ModList.Any(m => m.HasIncompatibility))
                {
                    List<string> missingDependencies = new();

                    foreach (ModEntryDisplay item in ModList)
                    {
                        foreach (string dep in item.MissingDependencies)
                        {
                            if (!missingDependencies.Contains(dep)) missingDependencies.Add(dep);
                        }

                        foreach (string dep in item.MismatchedDependencies)
                        {
                            if (!missingDependencies.Contains(dep)) missingDependencies.Add(dep);
                        }
                    }

                    foreach(string dep in missingDependencies)
                    {
                        string[] depParts = dep.Split('-');
                        string downloadPath = await _StatusBarControl.DownloadWithProgress(dep);

                        if (downloadPath != null)
                        {
                            ModPackage? newPackage = await PackageManager.AddPackage(downloadPath);

                            if (newPackage != null)
                            {
                                Package? existingState = profile.PackageList.Find(p => p.ModEntry.Author == depParts[0] &&
                                                                                    p.ModEntry.Name == depParts[1]);

                                if (existingState != null && !existingState.ModEntry.ExistsInPackageStore)
                                {
                                    existingState.SelectedVersion = depParts[2];
                                    existingState.ModEntry = newPackage.ToModEntry();
                                }
                                else if (existingState == null)
                                {
                                    profile.Add(new Package(depParts[2], newPackage.ToModEntry()));
                                }
                            }
                        }
                    }

                    ProfileManager.SaveProfile(profile);

                    foreach (ModEntryDisplay item in ModList)
                    {
                        if (!item.ExistsInPackageStore)
                        {
                            _StatusBarControl.DownloadWithProgress(item.Versions[0]);
                        }
                    }

                    RefreshModList();
                }
            }

            e.Handled = true;
        }
    }
}
