using System;
using System.Collections.Generic;
using System.Windows;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for ResolveDependencyConflictWindow.xaml
    /// </summary>
    public partial class ResolveDependencyConflictDialog : Window
    {
        public List<string> Versions = new();

        public ResolveDependencyConflictDialog(string entryName, string neededDep, string existingDep)
        {
            InitializeComponent();

            EntryNameSpan.Inlines.Add(entryName);
            NeededVersionSpan.Inlines.Add(neededDep);
            ExistingVersionSpan.Inlines.Add(existingDep);

            Versions.Add(existingDep);
            Versions.Add(neededDep);

            VersionsListbox.ItemsSource = Versions;
        }

        private void ConfirmSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
