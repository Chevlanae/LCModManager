using LCModManager.Thunderstore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for AddModsDialog.xaml
    /// </summary>
    public partial class AddModsDialog : Window
    {
        public ObservableCollection<ModEntryDisplay> ModList = new(PackageManager.GetPackages());

        public AddModsDialog()
        {
            InitializeComponent();

            ModListControl.ItemsSource = ModList;
        }

        public AddModsDialog(IList<ModEntryDisplay> existingEntries)
        {
            InitializeComponent();

            ModList = [];

            foreach (ModEntryDisplay package in PackageManager.GetPackages())
            {
                if (existingEntries.Any(e => e.Name == package.Name && e.Author == package.Author)) continue;
                else ModList.Add(package);
            }

            ModListControl.ItemsSource = ModList;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CANCELButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ModListControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = true;
                e.Handled = true;
            }
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

            e.Handled = true;
        }
    }
}
