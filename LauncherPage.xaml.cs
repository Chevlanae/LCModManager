using LCModManager.Thunderstore;
using System.Diagnostics;
using System.Windows;

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

        async private void LaunchGame_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileSelectorControl.SelectedItem is ModProfile profile)
            {
                string? gameDir = GameDirectory.Find();

                if (gameDir != null)
                {
                    await ModDeployer.CleanupGameDir();

                    await ModDeployer.DeployProfile(profile);

                    ProcessStartInfo info = new(gameDir + "\\Lethal Company.exe");

                    Process? process = Process.Start(info);

                    await process.WaitForExitAsync();

                    await ModDeployer.CleanupGameDir();
                }
                else
                {
                    ErrorPopupWindow errorPopup = new("Could not find Lethal Company game directory. Either Lethal Company is not installed, or your installation is not in the \"steamapps\\common\" folder.");
                    errorPopup.ShowDialog();
                }
            }
        }
    }
}
