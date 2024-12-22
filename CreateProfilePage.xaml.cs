using LCModManager.Thunderstore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

                foreach (IModEntry profileMod in profile.ModList)
                {
                    if (profileMod.SelectedVersions.Count() > 0 && profileMod.SelectedVersionsExist)
                    {
                        List<IModEntry> existingMods = await PackageManager.GetMods();

                        IModEntry existingMod = existingMods.Find(
                            m =>
                            m.Author == profileMod.Author &&
                            m.Name == profileMod.Name &&
                            m.Versions.ContainsKey(profileMod.SelectedVersions[0])
                        );

                        existingMod.SelectedVersions = [profileMod.SelectedVersions[0]];
                        ModList.Add(existingMod);
                    }
                    else
                    {
                        List<Listing> query = WebClient.SearchCache(p => profileMod.Author + "-" + profileMod.Name == p.Key);

                        if (query.Count() > 0)
                        {
                            profileMod.GetIcon(new Uri(query[0].versions[0].icon));
                            ModList.Add(profileMod);
                        }
                    }
                }

                ModList.AsParallel().ForAll((m) => m.ProcessDependencies(ModList.ToList()));
            }
        }

        private void Downloader_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Downloader downloader)
            {
                OnStatusUpdated(AppState.DownloadingMod, "Downloading " + downloader.Name + "...", true, (int)downloader.ProgressPercent);
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
            await RefreshModList();
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
                            mod.SelectedVersions = [mod.SelectedVersions[^1]];
                        }
                        else
                        {
                            mod.SelectedVersions = [mod.Versions.Keys.Last()];
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
                List<ListingVersion> needDownload = [];

                foreach (IModEntry item in ModList)
                {
                    foreach (string dep in item.MissingDependencies)
                    {
                        string[] depParts = dep.Split("-");
                        string fullName = System.String.Join("-", depParts[0..2]);
                        string version = depParts[2];

                        List<IModEntry> query = await PackageManager.GetMods(new Regex(dep));

                        switch (query.Count)
                        {
                            case 0:
                                if
                                (
                                    WebClient.GetCachedListing(fullName) is Listing listing &&
                                    listing.versions.FirstOrDefault(v => v.version_number == version) is ListingVersion listingVersion
                                )
                                {
                                    needDownload.Add(listingVersion);
                                }
                                break;
                            default:
                                query[0].SelectedVersions.Add(version);

                                if (profile.ModList.All(m => m.Author != query[0].Author && m.Name != query[0].Name))
                                {
                                    profile.ModList.Add(query[0]);
                                }
                                break;
                        }
                    }
                }

                ProfileManager.SaveProfile(profile);
                await RefreshModList();

                if(needDownload.Count > 0)
                {
                    ResolveDependenciesWindow resolveDependenciesWindow = new(needDownload);
                    resolveDependenciesWindow.Closed += ResolveDependenciesWindow_Closed;
                    resolveDependenciesWindow.Show();
                }
            }

            e.Handled = true;
        }

        async private void ResolveDependenciesWindow_Closed(object? sender, EventArgs e)
        {
            if (sender is ResolveDependenciesWindow window && ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                foreach (ListingVersion downloadedVersion in window.DownloadedVersions)
                {
                    List<IModEntry> query = await PackageManager.GetMods(new Regex(downloadedVersion.full_name));

                    switch (query.Count)
                    {
                        case 0:
                            break;
                        default:
                            query[0].SelectedVersions.Add(downloadedVersion.version_number);
                            if (profile.ModList.All(m => m.Author != query[0].Author && m.Name != query[0].Name))
                            {
                                profile.ModList.Add(query[0]);
                            }
                            break;
                    }
                }

                ProfileManager.SaveProfile(profile);
                await RefreshModList();
            }
        }

        private void ShareProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                DataContractSerializer serializer = new(typeof(ModProfile));

                using (MemoryStream stream = new())
                {
                    try
                    {
                        serializer.WriteObject(stream, profile);

                        switch (((MenuItem)sender).Name)
                        {
                            case "ExportXml":
                                stream.Position = 0;
                                using (StreamReader reader = new(stream, encoding: Encoding.UTF8))
                                {
                                    Clipboard.SetText(reader.ReadToEnd());
                                }
                                break;
                            case "ExportBase64":
                                Clipboard.SetText(Convert.ToBase64String(stream.ToArray()));
                                break;
                            case "ExportJSON":
                                Clipboard.SetText(JsonSerializer.Serialize<ModProfile>(profile));
                                break;
                        }
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

        async private void DownloadMissingDependencies_Click(object sender, RoutedEventArgs e)
        {
            foreach (IModEntry item in ModList)
            {
                string name = item.Author + "-" + item.Name;
                string fullName = item.Author + "-" + item.Name + "-" + item.SelectedVersions[0];

                if
                (
                    !item.ExistsInPackageStore &&
                    WebClient.GetCachedListing(name) is Listing listing &&
                    listing.versions.First(v => v.version_number == item.SelectedVersions[0]) is ListingVersion versionListing &&
                    await WebClient.DownloadPackageHeaders(versionListing) is HttpResponseMessage headers
                )
                {
                    Downloader downloader = new(fullName, headers);
                    downloader.PropertyChanged += Downloader_PropertyChanged;

                    if (await downloader.Download() is MemoryStream download)
                    {
                        await PackageManager.AddMod(download, fullName);
                    }
                }
            }

            await RefreshModList();
        }
    }
}
