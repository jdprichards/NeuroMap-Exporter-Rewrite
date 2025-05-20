using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

using NeuroMap_Exporter.ViewModels;
using NeuroMap_Exporter.Models;

namespace NeuroMap_Exporter.Views
{
    public partial class MainWindowView : Window
    {
        public MainWindowView()
        {
            InitializeComponent();
        }

        private void clkExit(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}