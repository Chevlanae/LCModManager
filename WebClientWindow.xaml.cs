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
using System.Xml.Linq;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for WebClientWindow.xaml
    /// </summary>
    public partial class WebClientWindow : Window
    {
        public Dictionary<string, PackageListing> QueriedPackages = new();
        public ObservableCollection<ModEntryDisplay> ModList = [];
        private StatusBarControl _StatusBarControl;

        public WebClientWindow(StatusBarControl statusBarCtrl)
        {
            _StatusBarControl = statusBarCtrl;

            InitializeComponent();

            ModListControl.ItemsSource = ModList;
        }

        private void StartQueryButton_Click()
        {
            if (QueryTextBox.Text.Length < 2) return;

            ModList.Clear();
            QueriedPackages.Clear();

            Regex reg = new(Regex.Escape(QueryTextBox.Text), RegexOptions.IgnoreCase);

            foreach (PackageListing listing in WebClient.SearchPackageCache(k => reg.IsMatch(k.Value.name)))
            {
                ModPackage newPackage = new ModPackage(listing);
                newPackage.Website = listing.package_url;
                ModList.Add(newPackage);
                QueriedPackages[listing.full_name] = listing;
            }

            ItemCountTextBlock.Text = "Returned " + ModList.Count.ToString() + " Results";
        }

        private void StartQueryButton_Click(object sender, RoutedEventArgs e)
        {
            StartQueryButton_Click();

            e.Handled = true;
        }

        private void QueryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) StartQueryButton_Click();
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

        private void AddPackage_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            e.Handled = true;
        }
    }
}
