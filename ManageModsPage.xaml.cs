using LCModManager.Thunderstore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for ManageModsPage.xaml
    /// </summary>
    partial class ManageModsPage : Page
    {
        public ObservableCollection<ModEntryDisplay> ModList;

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

        }

        private void RefreshModList_Click(object sender, RoutedEventArgs e)
        {
            RefreshModList();
            e.Handled = true;
        }
    }
}
