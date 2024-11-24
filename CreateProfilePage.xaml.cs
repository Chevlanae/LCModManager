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
    public partial class CreateProfilePage : Page
    {
        public ObservableCollection<IModEntry> ModList = [];
        public ObservableCollection<ModProfile> ProfileList = [];

        public CreateProfilePage()
        {
            InitializeComponent();

            ModListControl.ItemsSource = ModList;
            ProfileSelectorControl.ItemsSource= ProfileList;

            foreach (ModProfile profile in ProfileManager.GetProfiles())
            {
                ProfileList.Add(profile);
            }

            RefreshModList();
        }

        async public Task RefreshModList()
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                ModList.Clear();
                List<IModEntry> existingMods = PackageManager.GetMods();

                foreach(IModEntry mod in profile.ModList)
                {
                    if (mod.SelectedVersionsExist)
                    {
                        IModEntry existingMod = existingMods.Find(
                            m =>
                            m.Author == mod.Author &&
                            m.Name == mod.Name &&
                            m.Versions.ContainsKey(mod.SelectedVersions[0])
                        );

                        existingMod.SelectedVersions = [mod.SelectedVersions[0]];

                        ModList.Add(existingMod);
                    }
                    else
                    {
                        List<PackageListing> query = WebClient.SearchPackageCache(p => p.Key.Split("-")[1] == mod.Name);

                        switch (query.Count)
                        {
                            case 1:
                                mod.GetIcon(new Uri(query[0].versions[0].icon));
                                ModList.Add(mod);
                                break;
                            default:

                                foreach (PackageListing item in query)
                                {
                                    if (item.owner == mod.Author)
                                    {
                                        mod.GetIcon(new Uri(item.versions[0].icon));
                                        ModList.Add(mod);
                                        break;
                                    }
                                }

                                break;
                        }
                    }
                }

                foreach (IModEntry mod in ModList)
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

                    ProfileList.Add(newProfile);

                    ProfileSelectorControl.SelectedIndex = ProfileList.IndexOf(newProfile);
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

                ProfileList.Remove(profile);

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
                    foreach (IModEntry mod in dialog.ModListControl.SelectedItems)
                    {
                        if (mod.SelectedVersions.Count > 0)
                        {
                            mod.SelectedVersions = [mod.SelectedVersions[0]];
                        }
                        else
                        {
                            mod.SelectedVersions = [mod.Versions.Keys.First()];
                        }

                        if (!profile.ModList.Contains(mod)) profile.ModList.Add(mod);
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
                foreach (IModEntry mod in ModListControl.SelectedItems)
                {
                    profile.ModList.RemoveAt(
                        profile.ModList.FindIndex(
                            m =>
                            m.Author == mod.Author &&
                            m.Name == mod.Name
                        )
                    );
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

                    foreach (IModEntry item in ModList)
                    {
                        foreach (string dep in item.MissingDependencies)
                        {
                            if (await base.DownloadModPackage(dep) is string downloadPath && await PackageManager.AddMod(downloadPath) is IModEntry newMod)
                            {
                                string version = dep.Split("-")[2];

                                newMod.Versions[version] = new Uri(downloadPath);
                                newMod.SelectedVersions = [dep.Split("-")[2]];

                                if(profile.ModList.Find(m => m.Name == newMod.Name && m.Author == newMod.Author) == null) profile.ModList.Add(newMod);
                            }
                        }
                    }

                    await RefreshModList();
                }

                ProfileManager.SaveProfile(profile);

                foreach (IModEntry item in ModList)
                {
                    if (!item.ExistsInPackageStore)
                    {
                        string fullName = item.Author + "-" + item.Name + "-" + item.SelectedVersions[0];

                        if (await base.DownloadModPackage(fullName) is string download)
                        {
                            await PackageManager.AddMod(download);
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
            Regex base64Regex = new("^[-A-Za-z0-9+/]*={0,3}$", RegexOptions.Compiled);

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
