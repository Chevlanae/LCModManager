using LCModManager.Thunderstore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for WebClientWindow.xaml
    /// </summary>
    public partial class WebClientWindow : Window
    {
        public Dictionary<string, Listing> QueriedPackages = new();
        public ObservableCollection<IModEntry> ModList = [];

        public WebClientWindow()
        {
            InitializeComponent();

            ModListControl.ItemsSource = ModList;
        }

        private void StartQueryButton_Click()
        {
            if (QueryTextBox.Text.Length < 2) return;

            ModList.Clear();

            Regex reg = new(Regex.Escape(QueryTextBox.Text), RegexOptions.IgnoreCase);

            foreach (Listing listing in WebClient.SearchCache(k => reg.IsMatch(k.Value.name)))
            {
                ModList.Add(Mod.FromListing(listing));
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

        private void AddPackage_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            e.Handled = true;
        }
    }
}
