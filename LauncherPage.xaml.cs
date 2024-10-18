using LCModManager.Thunderstore;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for LauncherPage.xaml
    /// </summary>
    public partial class LauncherPage : Page
    {
        private StatusBarControl _StatusBarControl;

        public LauncherPage(StatusBarControl statusBarCtrl)
        {
            _StatusBarControl = statusBarCtrl;

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
                await ModDeployer.DeployProfile(profile);

                string? gameDir = GameDirectory.Find();

                if (gameDir != null)
                {
                    ProcessStartInfo info = new(gameDir + "\\Lethal Company.exe");

                    Process? process = Process.Start(info);

                    process?.WaitForExit();

                    ModDeployer.ExfiltrateProfile(profile);
                }
            }
        }
    }
}
