using LCModManager.Thunderstore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for CreateProfileDialog.xaml
    /// </summary>
    public partial class CreateProfileDialog : Window
    {
        public ObservableCollection<ModEntry> ModList;
        public List<ModEntry> SelectedEntries;

        public CreateProfileDialog()
        {
            InitializeComponent();

            ModList = [];
            SelectedEntries = [];

            foreach (ModEntry package in PackageManager.GetPackages()) ModList.Add(package);

            ModListControl.ItemsSource = ModList;
        }

        public CreateProfileDialog(IList<ModEntry> existingEntries)
        {
            InitializeComponent();

            ModList = [];
            SelectedEntries = [];

            foreach (ModEntry package in PackageManager.GetPackages())
            {

                //skip existing entries
                bool found = false;
                foreach(ModEntry entry in existingEntries)
                {
                    if(package.Name == entry.Name)
                    {
                        found = true;
                        break;
                    }
                }

                if(!found) ModList.Add(package);
            }

            ModListControl.ItemsSource = ModList;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CANCELButton_Click(Object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
