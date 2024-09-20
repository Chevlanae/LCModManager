using LCModManager.Thunderstore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Diagnostics;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for ManageModsPage.xaml
    /// </summary>
    partial class ManageModsPage : Page
    {
        public ObservableCollection<ModEntryDisplay> ModList;
        private Dictionary<string, PackageListing> _PackageCache = WebClient.PackageCache.Instance;

        public ManageModsPage()
        {
            ModList = [];

            InitializeComponent();

            ModListControl.ItemsSource = ModList;

            RefreshModList();
        }

        private void RefreshModList()
        {
            ModList.Clear();

            foreach (ModEntryDisplay package in PackageManager.GetPackages())
            {
                ModList.Add(package);
            }

            foreach(ModEntryDisplay mod in ModList) mod.ProcessDependencies(ModList);


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
                foreach(string filename in dialog.FileNames)
                {
                    PackageManager.AddPackage(filename);
                }

                RefreshModList();
            }

            e.Handled = true;

        }

        private void RemovePackage_Click(object sender, RoutedEventArgs e)
        {
            List<ModPackage> items = [];

            foreach (ModPackage entry in ModListControl.SelectedItems)
            {
                items.Add(entry);
            }

            foreach (ModPackage package in items)
            {
                PackageManager.RemovePackage(package);
            }

            RefreshModList();
            e.Handled = true;
        }

        private void WebPackage_Click(object sender, RoutedEventArgs e)
        {
            WebClientWindow window = new();

            window.ShowDialog();

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
            List<string> nonExistantDependencies = [];

            foreach(ModEntryDisplay entry in ModList)
            {
                foreach(string dep in entry.MissingDependencies)
                {
                    if (!missingDependencies.Contains(dep)) missingDependencies.Add(dep);
                }

                foreach (string dep in entry.MismatchedDependencies)
                {
                    if (!missingDependencies.Contains(dep)) missingDependencies.Add(dep);
                }
            }

            foreach(string dep in missingDependencies)
            {
                string fullname = String.Join("-", dep.Split("-")[..^1]);
                string version = dep.Split("-")[^1];
                bool found = false;

                foreach (PackageListingVersionEntry v in _PackageCache[fullname].versions)
                {
                    if (v.version_number == version)
                    {
                        found = true;

                        string? downloadPath = await WebClient.DownloadPackage(v);

                        if (downloadPath != null)
                        {
                            PackageManager.AddPackage(downloadPath);
                        }
                    }
                }

                if (!found) nonExistantDependencies.Add(dep);
            }

            RefreshModList();
        }
    }
}
