using LCModManager.Thunderstore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
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

            UpdateWebCache();

            RefreshModList();
        }

        async public void UpdateWebCache()
        {
            if(WebClient.PackageCache.NeedsRefresh && await WebClient.DownloadPackageListHeaders() is HttpResponseMessage headers)
            {
                await base.DownloadFromResponseHeaders(headers, WebClient.PackageCachePath, AppState.RefreshingPackageList);
            }

            await WebClient.PackageCache.LoadCache();
        }

        async public Task RefreshModList()
        {
            ModList.Clear();

            foreach (IModEntry mod in PackageManager.GetMods()) ModList.Add(mod);

            foreach (IModEntry mod in ModList) mod.ProcessDependencies(ModList.ToList());
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
                    base.OnStatusUpdated(new StatusArgs(AppState.AddingModPackage, "Adding mod package '" + filename.Split("\\")[^1] + "'..."));
                    await PackageManager.AddMod(filename);
                }

                await RefreshModList();
            }

            e.Handled = true;
        }

        async private void RemovePackage_Click(object sender, RoutedEventArgs e)
        {
            foreach (Mod mod in ModListControl.SelectedItems)
            {
                base.OnStatusUpdated(new StatusArgs(AppState.RemovingModPackage, "Removing " + mod.Name + " from package store..."));
                await PackageManager.RemoveMod(mod);
            }

            RefreshModList();
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
                            WebClient.GetCachedPackage(packageKey) is PackageListing listing &&
                            await base.DownloadModPackage(listing.versions.First(v => v.version_number == version)) is string downloadPath
                        )
                        {
                            base.OnStatusUpdated(new StatusArgs(AppState.AddingModPackage, "Adding mod package '" + downloadPath.Split("\\")[^1] + "'..."));
                            await PackageManager.AddMod(downloadPath);
                        }
                        else
                        {
                            Exception ex = new("'" + selectedItem.Name + "' could not be downloaded. Either the package does not exist on Thunderstore.io, or your network cannot reach Thunderstore.io.");
                            ErrorPopupWindow errorPopup = new("Error occured while downloading '" + selectedItem.Name + "'", ex);
                        }
                    }
                }
            }

            RefreshModList();
            e.Handled = true;
        }

        private void RefreshModList_Click(object sender, RoutedEventArgs e)
        {
            RefreshModList();
            e.Handled = true;
        }

        async private void ResolveDependencies_Click(object sender, RoutedEventArgs e)
        {
            while (ModList.Any(m => m.HasIncompatibility))
            {
                foreach (IModEntry entry in ModList)
                {
                    if (entry.HasIncompatibility)
                    {
                        List<string> missingDependencies = [];

                        foreach (string dep in entry.MissingDependencies) missingDependencies.Add(dep);

                        foreach (string dep in entry.MismatchedDependencies) if (!missingDependencies.Contains(dep)) missingDependencies.Add(dep);

                        foreach (string dep in missingDependencies)
                        {
                            string[] depSplit = dep.Split("-");
                            string fullname = String.Join("-", depSplit[..^1]);
                            string version = depSplit[^1];

                            try
                            {
                                if (WebClient.GetCachedPackage(fullname) is PackageListing packageListing)
                                {
                                    foreach (PackageListingVersion v in packageListing.versions)
                                    {
                                        if (v.version_number == version)
                                        {
                                            string destinationPath = await base.DownloadModPackage(v);

                                            if (File.Exists(destinationPath))
                                            {
                                                base.OnStatusUpdated(new StatusArgs(AppState.AddingModPackage, "Unpacking \"" + fullname + "\""));
                                                await PackageManager.AddMod(destinationPath);
                                            }

                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    ErrorPopupWindow errorPopup = new("Dependency '" + dep + "'" + " was not found in Thunderstore package list.", new Exception("Dependency " + fullname + " does not exist on Thunderstore.io"), "Error downloading dependency for '" + entry.Name + "'");
                                    errorPopup.Show();

                                    goto EndOfFunction;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.Write(ex);

                                ErrorPopupWindow errorPopup = new("An error occured while downloading '" + dep + "'", ex);
                                errorPopup.Show();

                                goto EndOfFunction;
                            }
                        }
                    }
                }

                RefreshModList();
            }

        EndOfFunction:
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
