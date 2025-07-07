using Avalonia.Controls.Documents;
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
        private string _outputFolder = "";


        private int _fileAmount;
        private int _fileComplete;

        private int _rowAmount;
        private int _rowComplete;
        
        private float _filePercentage;
        private float _rowPercentage;

        private string _rowCompleteRatio;
        private string _fileCompleteRatio;

        private string _filePercentageString;
        private string _rowPercentageString;

        private string _currentFile = "";

        private bool _disableUpsample = false;
        private bool _hideProgress = true;

        private bool _selectFirings = false;
        private bool _selectMFR = false;
        private bool _selectMUAPs = false;
        private bool _selectStats = false;
        private bool _selectAll = false;

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

        public bool SelectFirings
        {
            get => _selectFirings;
            set => SetProperty(ref _selectFirings, value);
        }

        public bool SelectMFR
        {
            get => _selectMFR;
            set => SetProperty(ref _selectMFR, value);
        }

        public bool SelectMUAPS
        {
            get => _selectMUAPs; 
            set => SetProperty(ref _selectMUAPs, value);
        }

        public bool SelectStats
        {
            get => _selectStats;
            set => SetProperty(ref _selectStats, value);
        }

        public bool SelectAll
        {
            get => _selectAll;
            set => SetProperty(ref _selectAll, value);
        }
    }
}
