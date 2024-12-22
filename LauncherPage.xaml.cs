using LCModManager.Thunderstore;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Windows.Gaming.UI;

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
                ModDeployer.StartGameWithProfile(profile);
            }
        }
    }
}
