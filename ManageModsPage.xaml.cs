using LCModManager.Thunderstore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for ManageModsPage.xaml
    /// </summary>
    partial class ManageModsPage : Page
    {
        public ObservableCollection<ModEntryDisplay> ModList = [];

        private Dictionary<string, PackageListing> _PackageCache = WebClient.PackageCache.Instance;
        private StatusBarControl _StatusBarControl;

        public ManageModsPage(StatusBarControl statusBarCtrl)
        {
            _StatusBarControl = statusBarCtrl;

            InitializeComponent();

            ModListControl.ItemsSource = ModList;

            RefreshModList();
        }

        public void RefreshModList()
        {
            ModList.Clear();

            foreach (ModEntryDisplay package in PackageManager.GetPackages())
            {
                ModList.Add(package);
            }

            foreach (ModEntryDisplay mod in ModList) mod.ProcessDependencies(ModList.ToList());
        }

        async private void AddPackage_Click(object sender, RoutedEventArgs e)
        {
            _StatusBarControl.CurrentState = AppState.AddingModPackage;

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
                    _StatusBarControl.Message = "Adding mod package '" + filename.Split("\\")[^1] + "'...";
                    await PackageManager.AddPackage(filename);
                }

                RefreshModList();
            }

            _StatusBarControl.CurrentState = AppState.Idle;

            e.Handled = true;
        }

        async private void RemovePackage_Click(object sender, RoutedEventArgs e)
        {
            _StatusBarControl.CurrentState = AppState.RemovingModPackage;

            foreach (ModPackage package in ModListControl.SelectedItems)
            {
                _StatusBarControl.Message = "Removing mod package from '" + package.Name + "'...";
                await PackageManager.RemovePackage(package);
            }

            _StatusBarControl.CurrentState = AppState.Idle;

            RefreshModList();
            e.Handled = true;
        }

        async private void WebPackage_Click(object sender, RoutedEventArgs e)
        {
            WebClientWindow window = new();

            if (window.ShowDialog() == true)
            {
                foreach (ModEntryDisplay selectedItem in window.ModListControl.SelectedItems)
                {
                    if (selectedItem.SelectedVersions.Count == 0)
                    {
                        selectedItem.SelectedVersions.Add(selectedItem.Versions[0]);
                    }

                    string packageKey = selectedItem.Author + "-" + selectedItem.Name;

                    foreach (string version in selectedItem.SelectedVersions)
                    {
                        if 
                        (
                            WebClient.GetCachedPackage(packageKey) is PackageListing listing &&
                            await _StatusBarControl.DownloadWithProgress(listing.versions.First(v => v.version_number == version)) is string downloadPath
                        )
                        {
                            _StatusBarControl.CurrentState = AppState.AddingModPackage;
                            _StatusBarControl.Message = "Adding mod package '" + downloadPath.Split("\\")[^1] + "'...";
                            PackageManager.AddPackage(downloadPath);
                            _StatusBarControl.CurrentState = AppState.Idle;
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
                foreach (ModEntryDisplay entry in ModList)
                {
                    if (entry.HasIncompatibility)
                    {
                        List<string> missingDependencies = [];

                        foreach (string dep in entry.MissingDependencies) missingDependencies.Add(dep);

                        foreach (string dep in entry.MismatchedDependencies)
                        {
                            if (!missingDependencies.Contains(dep)) missingDependencies.Add(dep);
                        }

                        foreach (string dep in missingDependencies)
                        {
                            string fullname = String.Join("-", dep.Split("-")[..^1]);
                            string version = dep.Split("-")[^1];

                            try
                            {
                                foreach (PackageListingVersionEntry v in _PackageCache[fullname].versions)
                                {
                                    if (v.version_number == version)
                                    {
                                        string destinationPath = await _StatusBarControl.DownloadWithProgress(v);

                                        if (File.Exists(destinationPath))
                                        {
                                            _StatusBarControl.CurrentState = AppState.AddingModPackage;
                                            _StatusBarControl.Message = "Adding mod package '" + destinationPath.Split("\\")[^1] + "'...";
                                            PackageManager.AddPackage(destinationPath);
                                            _StatusBarControl.CurrentState = AppState.Idle;
                                        }

                                        break;
                                    }
                                }
                            }

                            catch(KeyNotFoundException ex)
                            {
                                Debug.Write(ex);

                                ErrorPopupWindow errorPopup = new("Dependency '" + dep + "'" + " was not found in Thunderstore package list.", ex, "Error downloading dependency for '" + entry.Name + "'");
                                errorPopup.Show();

                                goto EndOfFunction;
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
            if (sender is ListView list && list.DataContext is ModEntryDisplay entry)
            {
                entry.SelectedVersions.Clear();
                foreach (string version in list.SelectedItems)
                {
                    entry.SelectedVersions.Add(version);
                }
            }
        }
    }
}
