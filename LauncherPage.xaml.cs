using LCModManager.Thunderstore;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for LauncherPage.xaml
    /// </summary>
    public partial class LauncherPage : Page
    {
        public LauncherPage()
        {
            InitializeComponent();

            RefreshProfiles();
        }

        public void RefreshProfiles()
        {
            ProfileSelectorControl.Items.Clear();

            foreach (ModProfile profile in ProfileManager.GetProfiles())
            {
                ProfileSelectorControl.Items.Add(profile);
            }
        }

        private void LaunchGame_Click(object sender, RoutedEventArgs e)
        {
            if(ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                foreach(ModEntryBase modEntry in profile.ModList)
                {
                    if(PackageManager.GetFromName(modEntry.Name) is ModPackage package)
                    {
                        ModDeployer.DeployModFromStore(package);
                    }
                }

            }

            ProcessStartInfo info = new(GameDirectory.Find() + "\\Lethal Company.exe");

            Process? process = Process.Start(info);

            process?.WaitForExit();

            ModDeployer.RemoveDeployedMods();
        }
    }
}
