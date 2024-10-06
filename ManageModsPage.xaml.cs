using LCModManager.Thunderstore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        public ManageModsPage()
        {
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

        private void AddPackage_Click(object sender, RoutedEventArgs e)
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
                    PackageManager.AddPackage(filename);
                }

                RefreshModList();
            }

            e.Handled = true;

        }

        private void RemovePackage_Click(object sender, RoutedEventArgs e)
        {
            foreach (ModPackage package in ModListControl.SelectedItems)
            {
                PackageManager.RemovePackage(package);
            }

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
                    string packageKey = selectedItem.Author + "-" + selectedItem.Name;

                    if (selectedItem.SelectedVersions.Count > 0)
                    {
                        foreach (string version in selectedItem.SelectedVersions)
                        {
                            string? downloadPath = await WebClient.DownloadPackage(window.QueriedPackages[packageKey].versions.First(v => v.version_number == version));

                            if (downloadPath != null) PackageManager.AddPackage(downloadPath);
                        }
                    }
                    else
                    {
                        string? downloadPath = await WebClient.DownloadPackage(window.QueriedPackages[packageKey].versions[0]);

                        if (downloadPath != null) PackageManager.AddPackage(downloadPath);
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
                                        string? downloadPath = await WebClient.DownloadPackage(v);

                                        if (downloadPath != null) PackageManager.AddPackage(downloadPath);

                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.Write(ex);
                                Debug.WriteLine(dep + "not found in package cache");
                            }
                        }
                    }
                }

                RefreshModList();
            }

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
