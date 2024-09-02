using LCModManager.Thunderstore;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for ManageModsPage.xaml
    /// </summary>
    partial class ManageModsPage : Page
    {
        public ObservableCollection<ModEntry> ModList;

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

            foreach (ModEntry package in PackageManager.GetPackages())
            {
                ModList.Add(package);
            }

            foreach(ModEntry mod in ModList) mod.GetMissingDependencies(ModList);


        }

        private void AddPackage_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new()
            {
                // Set filter for file extension and default file extension 
                DefaultExt = ".zip",
                Filter = "ZIP Files (*.zip)|*.zip",
                Multiselect = true,
                
            };

            // Display OpenFileDialog by calling ShowDialog method 
            bool? result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result != false && result != null && dlg.FileNames.Length != 0)
            {
                foreach(var filename in dlg.FileNames)
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
                ModList.Remove(package);
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
