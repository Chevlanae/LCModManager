using LCModManager.Thunderstore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LCModManager
{
    public class DependencyItem : INotifyPropertyChanged
    {
        private Downloader _Downloader;
        private float _ProgressPercent;
        private string _ProgressPercentString;
        private IProgress<float> _Progress;

        public string Name { get; set; }
        public ListingVersion? SelectedVersion { get; set; }
        public List<ListingVersion> Versions { get; set; }
        public float ProgressPercent
        {
            get
            {
                return _ProgressPercent;
            }
            set
            {
                _ProgressPercent = value;
                OnPropertyChanged();
            }
        }
        public string ProgressPercentString
        {
            get
            {
                return _ProgressPercentString;
            }
            set
            {
                _ProgressPercentString = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DependencyItem(string name, ListingVersion[] versions)
        {
            Name = name;
            Versions = new(versions);
            ProgressPercent = 0;
            _Progress = new Progress<float>(p => { ProgressPercent = p; ProgressPercentString = ((int)p).ToString() + "%"; }); 
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        async public Task DownloadSelectedVersion()
        {
            ListingVersion selectedVersion = SelectedVersion ?? Versions[0];

            if (await WebClient.DownloadPackageHeaders(selectedVersion) is HttpResponseMessage headers)
            {
                _Downloader = new(selectedVersion.full_name, headers, _Progress);
                _Downloader.PropertyChanged += (sender, e) => 
                {
                    if(sender is Downloader d)
                    {
                        ProgressPercent = (int)d.ProgressPercent;
                    }
                };

                if(await _Downloader.Download() is MemoryStream download)
                {
                    using (download)
                    {
                        PackageManager.AddMod(download, selectedVersion.full_name);
                    }
                }
            }
        }

        async public Task DownloadAllVersions()
        {
            foreach(ListingVersion version in Versions)
            {
                if (await WebClient.DownloadPackageHeaders(version) is HttpResponseMessage headers)
                {
                    _Downloader = new(version.full_name, headers, _Progress);

                    if (await _Downloader.Download() is MemoryStream download)
                    {
                        using (download)
                        {
                            PackageManager.AddMod(download, version.full_name);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Interaction logic for ResolveDependenciesWindow.xaml
    /// </summary>
    public partial class ResolveDependenciesWindow : Window
    {
        public ObservableCollection<DependencyItem> Dependencies = [];
        public List<ListingVersion> DownloadedVersions = [];

        public ResolveDependenciesWindow(List<ListingVersion> dependencies)
        {
            InitializeComponent();

            DependenciesGrid.ItemsSource = Dependencies;

            foreach(ListingVersion dependency in dependencies)
            {
                if(Dependencies.FirstOrDefault(d => d.Name == dependency.name) is DependencyItem item)
                {
                    if(!item.Versions.Contains(dependency)) item.Versions.Add(dependency);
                }
                else
                {
                    Dependencies.Add(new(dependency.name, [dependency]));
                }
            }

            foreach(DependencyItem item in Dependencies)
            {
                item.Versions.Sort((x, y) =>
                {
                    int xValue = int.Parse(x.version_number.Replace(".", ""));
                    int yValue = int.Parse(y.version_number.Replace(".", ""));

                    return yValue.CompareTo(xValue);
                });
            }

        }

        async private void DownloadAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach(DependencyItem dependency in Dependencies)
            {
                await dependency.DownloadAllVersions();
                foreach(ListingVersion version in dependency.Versions)
                {
                    DownloadedVersions.Add(version);
                }
            }
        }

        async private void DownloadSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (DependencyItem dependency in Dependencies)
            {
                await dependency.DownloadSelectedVersion();
                ListingVersion selectedVersion = dependency.SelectedVersion ?? dependency.Versions[0];
                DownloadedVersions.Add(selectedVersion);
            }
        }

        private void VersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(sender is ComboBox comboBox && comboBox.DataContext is DependencyItem dependency && comboBox.SelectedItem is ListingVersion selectedVersion)
            {
                dependency.SelectedVersion = selectedVersion;
            } 
        }
    }
}
