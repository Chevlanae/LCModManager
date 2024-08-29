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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LCModManager
{

    /// <summary>
    /// Interaction logic for ManageModsPage.xaml
    /// </summary>
    partial class ManageModsPage : Page
    {
        PackageManager packageManager;

        public ManageModsPage()
        {
            try
            {
                packageManager = new PackageManager();

            } catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            InitializeComponent();

            if (packageManager != null)
            {
                ModList.ItemsSource = packageManager.Packages;
            }
        }

        private void AddMod_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                packageManager.RefreshMods();
            } catch
            {

            }
        }
    }
}
