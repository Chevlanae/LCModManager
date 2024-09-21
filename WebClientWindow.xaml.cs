using LCModManager.Thunderstore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for WebClientWindow.xaml
    /// </summary>
    public partial class WebClientWindow : Window
    {
        public ObservableCollection<ModEntryDisplay> ModList = [];
        private Dictionary<string, PackageListing> _PackageCache = WebClient.PackageCache.Instance;

        public WebClientWindow()
        {
            InitializeComponent();

            ModListControl.ItemsSource = ModList;
        }

        private void StartQueryButton_Click()
        {
            ModList.Clear();

            Regex reg = new Regex(QueryTextBox.Text.ToLower());

            foreach (KeyValuePair<string, PackageListing> listing in _PackageCache.Where(k => reg.IsMatch(k.Value.name.ToLower())))
            {
                ModList.Add(new ModPackage(listing.Value.versions[0]));
            }

            ItemCountTextBlock.Text = "Returned " + ModList.Count.ToString() + " Results";
        }

        private void StartQueryButton_Click(object sender, RoutedEventArgs e)
        {
            StartQueryButton_Click();
        }

        private void QueryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) StartQueryButton_Click();
        }
    }
}
