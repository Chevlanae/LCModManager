﻿using LCModManager.Thunderstore;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App() { }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo info = new()
            {
                FileName = "explorer.exe",
                Arguments = e.Uri.ToString(),
            };

            Process.Start(info);
            e.Handled = true;
        }
    }
}
