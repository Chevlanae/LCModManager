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
        public ObservableCollection<IModEntry> ModList;

        public AddModsDialog()
        {
            InitializeComponent();

            ModListControl.ItemsSource = ModList;
        }

        public AddModsDialog(IList<IModEntry> existingEntries)
        {
            InitializeComponent();

            ModList = [];

            foreach (IModEntry mod in PackageManager.GetMods())
            {
                if (existingEntries.Any(e => e.Name == mod.Name && e.Author == mod.Author)) continue;
                else ModList.Add(mod);
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
            if (sender is ListView list && list.DataContext is IModEntry mod)
            {
                mod.SelectedVersions.Clear();
                foreach (string version in list.SelectedItems)
                {
                    mod.SelectedVersions.Add(version);
                }
            }

            e.Handled = true;
        }
    }
}
