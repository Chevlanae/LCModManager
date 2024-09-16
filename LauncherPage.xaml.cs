﻿using LCModManager.Thunderstore;
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
            ModProfile profile = ProfileSelectorControl.SelectedItem as ModProfile;

            await ModDeployer.DeployProfile(profile);

            string? gameDir = GameDirectory.Find();

            if(gameDir != null)
            {
                ProcessStartInfo info = new(gameDir + "\\Lethal Company.exe");

                Process? process = Process.Start(info);

                process?.WaitForExit();

                ModDeployer.ExfiltrateProfile(profile);
            }
        }
    }
}