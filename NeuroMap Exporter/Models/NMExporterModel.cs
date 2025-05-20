using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroMap_Exporter.Models
{
    public class NMExporterModel : ObservableObject
    { 
        private string _inputFolder = "";
        private string _outputFolder;


        private bool _disableUpsample = false;
        private bool _hideProgress = true;

        public string InputFolder
        {
            get => _inputFolder;
            set => SetProperty(ref _inputFolder, value);
        }

        public string OutputFolder
        {
            get => _outputFolder;
            set => SetProperty(ref _outputFolder, value);
        }

        public bool DisableUpsample
        {
            get => _disableUpsample;
            set => SetProperty(ref _disableUpsample, value);
        }

        public bool HideProgress
        {
            get => _hideProgress;
            set => SetProperty(ref _hideProgress, value);
        }
    }
}
