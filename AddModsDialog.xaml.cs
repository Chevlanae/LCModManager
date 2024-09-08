using LCModManager.Thunderstore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for AddModsDialog.xaml
    /// </summary>
    public partial class AddModsDialog : Window
    {
        public ObservableCollection<ModEntryDisplay> ModList = [];

        public AddModsDialog()
        {
            InitializeComponent();

            foreach (ModEntryDisplay package in PackageManager.GetPackages()) ModList.Add(package);

            ModListControl.ItemsSource = ModList;
        }

        public AddModsDialog(IList<ModEntryDisplay> existingEntries)
        {
            InitializeComponent();

            ModList = [];

            foreach (ModEntryDisplay package in PackageManager.GetPackages())
            {

                //skip existing entries
                bool found = false;
                foreach (ModEntryDisplay entry in existingEntries)
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

        private void ModListControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                DialogResult = true;
                e.Handled = true;
            }
        }
    }
}
