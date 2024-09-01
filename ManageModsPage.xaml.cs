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
    public class ModEntry : IModEntry
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? Website { get; set; }
        public BitmapImage? Icon { get; set; }
        public string[]? Dependencies { get; set; }
        public string[]? MissingDependencies { get; set; }
        public bool IsMissingDependencies
        {
            get
            {
                if (MissingDependencies != null && MissingDependencies.Length > 0) return true;
                else return false;
            }
        }

        public void GetMissingDependencies(IEnumerable<ModEntry> entries)
        {
            if(Dependencies != null)
            {
                List<string> missingDeps = [];

                foreach (string depStr in Dependencies)
                {
                    bool found = false;
                    foreach (ModEntry entry in entries)
                    {
                        if(entry.Name != null && entry.Version != null)
                        {
                            //Entry Regex Generation
                            string[] ints = entry.Version.Split('.');
                            for (int i = 0; i < ints.Length; i++)
                            {
                                //Skip regex replace for major version
                                if (i == 0) continue;
                                else
                                {
                                    char[] chars = ints[i].ToCharArray();
                                    string newStr = "";

                                    //Converts integer to a pattern matching any number less than or equal to the integer
                                    for (int j = 0; j < chars.Length; j++)
                                    {
                                        if (j == 0)
                                        {
                                            newStr += "[0-" + chars[j] + "]";
                                        }
                                        else
                                        {
                                            newStr += "[0-9]";
                                        }
                                    }

                                    ints[i] = newStr;
                                }
                            }

                            //Pattern to match to end of depStr
                            Regex reg = new(entry.Name + "-" + String.Join("\\.", ints) + "$");

                            //Check dependency string against regex
                            if (reg.IsMatch(depStr))
                            {
                                found = true;
                                break;
                            }
                        }
                        
                    }

                    if (!found) missingDeps.Add(depStr);
                }

                MissingDependencies = [.. missingDeps];
            }
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

            RefreshModList(true);
        }

        private void RefreshModList()
        {
            ModList.Clear();

            foreach (ModEntry package in thunderStorePkgMgr.Packages)
            {
                ModList.Add(package);
            }

            foreach(ModEntry mod in ModList) mod.GetMissingDependencies(ModList);


        }


        private void RefreshModList(bool refreshCache)
        {
            if (refreshCache)
            {
                thunderStorePkgMgr.RefreshPackages();
            }

            RefreshModList();
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
                    thunderStorePkgMgr.AddPackage(filename);
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
                thunderStorePkgMgr.RemovePackage(package);
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

        private void EntryDependenciesButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button senderBtn && senderBtn.Content is Popup popup && popup.Child is Grid grid)
            {
                popup.IsOpen = !popup.IsOpen;
            }

            e.Handled = true;
        }
    }
}
