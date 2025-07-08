using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroMap_Exporter.Models
{
    public class UpsamplerModel : ObservableObject
    {
        private string _inputFolder = "";
        private string _outputFolder = "";

        private int _fileAmount;
        private int _fileComplete;

        private int _rowAmount;
        private int _rowComplete;

        private float _filePercentage;
        private float _rowPercentage;

        private string _rowCompleteRatio;
        private string _fileCompleteRatio;

        private string _rowPercentageString;
        private string _filePercentageString;

        private string _currentFile;

        private bool _disableUpsample;
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

        public int FileAmount
        {
            get => _fileAmount;
            set => SetProperty(ref _fileAmount, value);
        }

        public int FileComplete
        {
            get => _fileComplete;
            set => SetProperty(ref _fileComplete, value);
        }

        public int RowAmount
        {
            get => _rowAmount; 
            set => SetProperty(ref _rowAmount, value);
        }

        public int RowComplete
        {
            get => _rowComplete;
            set => SetProperty(ref _rowComplete, value);
        }

        public float FilePercentage
        {
            get => _filePercentage; 
            set => SetProperty(ref _filePercentage, value);
        }

        public float RowPercentage
        {
            get => _rowPercentage; 
            set => SetProperty(ref _rowPercentage, value);
        }

        public string FilePercentageString
        {
            get => _filePercentageString; 
            set => SetProperty(ref _filePercentageString, value);
        }

        public string RowPercentageString
        {
            get => _rowPercentageString; 
            set => SetProperty(ref _rowPercentageString, value);
        }

        public string FileCompleteRatio
        {
            get => _fileCompleteRatio; 
            set => SetProperty(ref _fileCompleteRatio, value);
        }

        public string RowCompleteRatio
        {
            get => _rowCompleteRatio; 
            set => SetProperty(ref _rowCompleteRatio, value);
        }

        public string CurrentFile
        {
            get => _currentFile;
            set => SetProperty(ref _currentFile, value);
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
