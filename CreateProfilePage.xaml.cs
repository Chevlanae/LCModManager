using LCModManager.Thunderstore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
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

        async public Task RefreshModList()
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                ModList.Clear();

                foreach (ModEntrySelection package in profile.PackageList)
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

        async private void CreateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            EnterTextDialog dialog = new("Create Profile", "Enter new profile name");

            if (dialog.ShowDialog() == true)
            {
                ModProfile newProfile = new(dialog.InputTextBox.Text);

                if (!ProfileSelectorControl.Items.Contains(newProfile))
                {
                    ProfileManager.AddProfile(newProfile);

                    ProfileSelectorControl.SelectedIndex = ProfileSelectorControl.Items.Add(newProfile);
                }
            }

            e.Handled = true;
        }

        async private void ImportProfileButton_Click(object sender, RoutedEventArgs e)
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

        async private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                ProfileManager.DeleteProfile(profile);

                ProfileSelectorControl.Items.Remove(profile);

                ModList.Clear();
            }

            e.Handled = true;
        }

        async private void ProfileSelectorControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshModList();
        }

        async private void AddModsButton_Click(object sender, RoutedEventArgs e)
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

                        ModEntrySelection state = new(package.Versions[0], package.ToModEntry());

                        if (!profile.PackageList.Contains(state)) profile.PackageList.Add(state);
                    }

                    ProfileManager.SaveProfile(profile);

                    await RefreshModList();
                }
            }

            e.Handled = true;
        }

        async private void RemoveModsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                foreach (ModEntry entry in ModListControl.SelectedItems)
                {
                    profile.PackageList.RemoveAll(e => e.ModEntry.Name == entry.Name);
                }

                ProfileManager.SaveProfile(profile);

                await RefreshModList();
            }

            e.Handled = true;
        }

        async private void ResolveDependencies_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                while (ModList.Any(m => m.HasMissingDependencies))
                {
                    List<string> missingDependencies = new();

                    foreach (ModEntryDisplay item in ModList)
                    {
                        foreach (string dep in item.MissingDependencies)
                        {
                            if (await _StatusBarControl.DownloadWithProgress(dep) is string downloadPath && await PackageManager.AddPackage(downloadPath) is ModPackage newPackage)
                            {
                                profile.PackageList.Add(new ModEntrySelection(dep.Split('-')[2], newPackage.ToModEntry()));
                            }
                        }
                    }

                    await RefreshModList();
                }

                ProfileManager.SaveProfile(profile);

                foreach (ModEntryDisplay item in ModList)
                {
                    if (!item.ExistsInPackageStore)
                    {
                        string fullName = item.Author + "-" + item.Name + "-" + item.Versions[0];

                        if (await _StatusBarControl.DownloadWithProgress(fullName) is string download)
                        {
                            PackageManager.AddPackage(download);
                        }
                    }
                }

                await RefreshModList();
            }

            e.Handled = true;
        }

        async private void ShareProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                DataContractSerializer serializer = new(typeof(ModProfile));

                using (MemoryStream stream = new())
                {
                    try
                    {
                        serializer.WriteObject(stream, profile);

                        string text = Convert.ToBase64String(stream.ToArray());

                        Clipboard.SetText(text);
                    }
                    catch (Exception ex)
                    {
                        Debug.Write(ex);

                        ErrorPopupWindow errorPopup = new("Error occured while serializing profile '" + profile.Name + "'", ex);
                        errorPopup.ShowDialog();
                    }
                }
            }
        }

        async private void ImportStringButton_Click(object sender, RoutedEventArgs e)
        {
            EnterTextDialog dialog = new("Import profile string", "Paste profile string here");
            Regex base64Regex = new("^[-A-Za-z0-9+/]*={0,3}$");

            if (dialog.ShowDialog() == true && base64Regex.IsMatch(dialog.InputTextBox.Text))
            {
                DataContractSerializer serializer = new(typeof(ModProfile));

                try
                {
                    byte[] byteArray = Convert.FromBase64String(dialog.InputTextBox.Text);

                    using (MemoryStream stream = new(byteArray))
                    {
                        ModProfile newProfile = (ModProfile)serializer.ReadObject(stream);

                        ProfileManager.AddProfile(newProfile);
                        ProfileSelectorControl.SelectedIndex = ProfileSelectorControl.Items.Add(newProfile);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);

                    ErrorPopupWindow errorPopup = new("Exception occured while deserializing base64 string", ex);
                    errorPopup.Show();
                }
            }

            e.Handled = true;
        }
    }
}
