using Avalonia.Platform.Storage;
using System;
using System.Threading.Tasks;

using NeuroMap_Exporter.Models;
using System.IO;
using System.Collections.Generic;

namespace NeuroMap_Exporter.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            CurrentPage = _pages["Home"];
        }

        
        private ViewModelBase _currentPage;

        Dictionary<string, ViewModelBase> _pages = new Dictionary<string, ViewModelBase>()
        {
            {"Home",  new HomeViewModel() },
            {"NM Exporter", new NMExporterViewModel() },
            {"EMG-IMU Upsampler",  new UpsamplerViewModel()}
        };


        public string Greeting { get; } = "Welcome to Avalonia!";

        public ViewModelBase CurrentPage
        {
            get  => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public Dictionary<string, ViewModelBase> Pages
        {
            get => _pages;
            set => SetProperty(ref _pages, value);
        }

        public void NavigatePage(string pageName)
        {
            CurrentPage = Pages[pageName];
        }
    }

}
