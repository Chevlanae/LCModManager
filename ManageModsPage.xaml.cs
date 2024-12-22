using LCModManager.Thunderstore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

using StatusArgs = LCModManager.StatusUpdatedEventArgs;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for ManageModsPage.xaml
    /// </summary>
    partial class ManageModsPage : Page
    {
        public ObservableCollection<IModEntry> ModList = [];

        public ManageModsPage()
        {
            InitializeComponent();

            ModListControl.ItemsSource = ModList;

            Task.Run(RefreshModList);
        }

        async public Task RefreshModList()
        {
            ModList.Clear();

            foreach (IModEntry mod in await PackageManager.GetMods()) ModList.Add(mod);

            ModList.AsParallel().ForAll(m => m.ProcessDependencies(ModList.ToList()));
        }

        async private void AddPackage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                DefaultExt = ".zip",
                Filter = "ZIP Files (*.zip)|*.zip",
                Multiselect = true,
            };

            if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
            {
                foreach (string filename in dialog.FileNames)
                {
                    OnStatusUpdated(AppState.AddingModPackage, "Adding mod package '" + filename.Split("\\")[^1] + "'...");
                    await PackageManager.AddMod(filename);
                }

                await RefreshModList();
            }

            e.Handled = true;
        }

        private void Downloader_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Downloader downloader)
            {
                OnStatusUpdated(AppState.DownloadingMod, "Downloading " + downloader.Name + "...", true, (int)downloader.ProgressPercent);
            }
        }

        async private void RemovePackage_Click(object sender, RoutedEventArgs e)
        {
            List<IModEntry> selection = new();

            foreach(IModEntry mod in ModListControl.SelectedItems) selection.Add(mod);

            foreach (IModEntry mod in selection)
            {
                OnStatusUpdated(AppState.RemovingModPackage, "Removing " + mod.Name + " from package store...");
                await PackageManager.RemoveMod(mod);
                ModList.Remove(mod);
            }

            await RefreshModList();

            e.Handled = true;
        }

        async private void WebPackage_Click(object sender, RoutedEventArgs e)
        {
            WebClientWindow window = new();

            if (window.ShowDialog() == true)
            {
                foreach (IModEntry selectedItem in window.ModListControl.SelectedItems)
                {
                    if (selectedItem.SelectedVersions.Count == 0)
                    {
                        selectedItem.SelectedVersions.Add(selectedItem.Versions.Keys.First());
                    }

                    string packageKey = selectedItem.Author + "-" + selectedItem.Name;

                    foreach (string version in selectedItem.SelectedVersions)
                    {
                        if
                        (
                            ModList.All(m => m.Author + "-" + m.Name != packageKey && !m.Versions.ContainsKey(version)) &&
                            WebClient.GetCachedListing(packageKey) is Listing listing &&
                            listing.versions.First(v => v.version_number == version) is ListingVersion versionListing &&
                            await WebClient.DownloadPackageHeaders(versionListing) is HttpResponseMessage headers
                        )
                        {
                            Downloader downloader = new(versionListing.full_name, headers);
                            downloader.PropertyChanged += Downloader_PropertyChanged;

                            if (await downloader.Download() is MemoryStream download)
                            {
                                using (download)
                                {
                                    await PackageManager.AddMod(download, versionListing.full_name);
                                }
                            }
                        }
                        else
                        {
                            Exception ex = new("'" + selectedItem.Name + "' could not be downloaded. Either the package does not exist on Thunderstore.io, or your network cannot reach Thunderstore.io.");
                            ErrorPopupWindow errorPopup = new("Error occured while downloading '" + selectedItem.Name + "'", ex);
                        }
                    }
                }
            }

            await RefreshModList();
            e.Handled = true;
        }

        private void RefreshModList_Click(object sender, RoutedEventArgs e)
        {
            RefreshModList();
            e.Handled = true;
        }

        async private void ResolveDependencies_Click(object sender, RoutedEventArgs e)
        {
            List<string> missingDependencies = [];

            foreach (IModEntry entry in ModList)
            {
                if (entry.HasIncompatibility)
                {
                    foreach (string dep in entry.MissingDependencies) if (!missingDependencies.Contains(dep)) missingDependencies.Add(dep);

                    foreach (string dep in entry.MismatchedDependencies) if (!missingDependencies.Contains(dep)) missingDependencies.Add(dep);
                }
            }

            List<ListingVersion> dependencies = new();

            foreach (string dep in missingDependencies)
            {
                string[] depSplit = dep.Split("-");
                string author = depSplit[0];
                string name = depSplit[1];
                string fullname = String.Join("-", depSplit[..^1]);
                string version = depSplit[2];

                if (WebClient.GetCachedListing(fullname) is Listing packageListing)
                {
                    foreach (ListingVersion listingVersion in packageListing.versions)
                    {
                        if (listingVersion.version_number == version) dependencies.Add(listingVersion);
                    }
                }
            }

            ResolveDependenciesWindow resolveDepsWindow = new(dependencies);
            resolveDepsWindow.Closed += async (sender, e) => await RefreshModList();
            resolveDepsWindow.Show();

            e.Handled = true;
        }

        private void VersionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView list && list.DataContext is IModEntry mod)
            {
                mod.SelectedVersions.Clear();
                foreach (string version in list.SelectedItems)
                {
                    mod.SelectedVersions.Add(version);
                }
            }
        }
    }
}
