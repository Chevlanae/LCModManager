using LCModManager.Thunderstore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LCModManager
{

    public interface IModEntry
    {
        public string? Name { get; }
        public string? Description { get; }
        public string? Version { get; }
        public string? Website { get; }
        public BitmapImage? Icon { get; }
        public string[]? Dependencies { get; }
    }

    public class ModEntry : IModEntry
    {
        private string? _Name;
        private string? _Description;
        private string? _Version;
        private string? _Website;
        private BitmapImage? _Icon;
        private string[]? _Dependencies;

        public string? Name
        {
            get { return _Name; }
        }
        public string? Description
        {
            get { return _Description; }
        }
        public string? Version
        {
            get { return _Version; }
        }
        public string? Website
        {
            get { return _Website; }
        }
        public BitmapImage? Icon
        {
            get { return _Icon; }
        }
        public string[]? Dependencies
        {
            get { return _Dependencies; }
        }
    }

    /// <summary>
    /// Interaction logic for ManageModsPage.xaml
    /// </summary>
    partial class ManageModsPage : Page
    {
        private readonly PackageManagerAPI thunderStorePkgMgr;
        public ObservableCollection<ModEntry> ModList;

        public ManageModsPage()
        {
            try
            {
                thunderStorePkgMgr = new PackageManagerAPI();

            } catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw new Exception("Failed to initialize local Thunderstore package manager.", ex);
            }

            ModList = [];

            InitializeComponent();

            ModListControl.ItemsSource = ModList;

            RefreshList_Click(true);
        }

        private void AddMod_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                // Set filter for file extension and default file extension 
                DefaultExt = ".zip",
                Filter = "ZIP Files (*.zip)|*.zip",
                Multiselect = true
            };

            // Display OpenFileDialog by calling ShowDialog method 
            bool? result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result != false && result != null && dlg.FileNames.Length != 0)
            {
                foreach(var filename in dlg.FileNames)
                {
                    thunderStorePkgMgr.AddPackage(filename);
                }

                RefreshList_Click(true);
            }
        }

        private void RefreshList_Click()
        {
            ModList.Clear();

            foreach(ModPackage package in thunderStorePkgMgr.Packages)
            {
                ModList.Add(package);
            }
        }


        private void RefreshList_Click(bool refreshCache)
        {
            if (refreshCache)
            {
                thunderStorePkgMgr.RefreshPackages();
            }

            RefreshList_Click();
        }

        private void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            RefreshList_Click();
        }

        private void ModList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
