﻿using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Windows.Foundation.Diagnostics;

namespace LCModManager
{
    /// <summary>
    /// Interaction logic for ErrorPopupWindow.xaml
    /// </summary>
    public partial class ErrorPopupWindow : Window
    {
        public ErrorPopupWindow(string errorMessage, Exception ex, string title = "Error occured")
        {
            InitializeComponent();
            Title = title;
            HeaderMessageTextBox.Text = errorMessage;
            ErrorMessageTextBox.Text = ex.ToString();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
