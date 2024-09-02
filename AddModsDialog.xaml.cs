using LCModManager.Thunderstore;
using System.Collections.ObjectModel;
using System.Windows;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for AddModsDialog.xaml
    /// </summary>
    public partial class AddModsDialog : Window
    {
        public ObservableCollection<ModEntry> ModList;

        public AddModsDialog()
        {
            InitializeComponent();

            ModList = [];

            foreach (ModEntry package in PackageManager.GetPackages()) ModList.Add(package);

            ModListControl.ItemsSource = ModList;
        }

        public AddModsDialog(IList<ModEntry> existingEntries)
        {
            InitializeComponent();

            ModList = [];

            foreach (ModEntry package in PackageManager.GetPackages())
            {

                //skip existing entries
                bool found = false;
                foreach (ModEntry entry in existingEntries)
                {
                    if (package.Name == entry.Name)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found) ModList.Add(package);
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
    }
}
