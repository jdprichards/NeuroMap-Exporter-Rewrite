using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace NeuroMap_Exporter.Models
{
    public partial class MainWindowModel : ObservableObject
    {
        bool _isDarkModeEnabled = true;
        bool _helpVisible = false;
        string _helpIcon = "Right_Chevron";

        public bool IsDarkModeEnabled
        {
            get => _isDarkModeEnabled;
            set => SetProperty(ref _isDarkModeEnabled, value);
        }
        public bool HelpVisible
        {
            get => _helpVisible;
            set => SetProperty(ref _helpVisible, value);
        }
        public string HelpIcon
        {
            get => _helpIcon;
            set => SetProperty(ref _helpIcon, value);
        }

        
    }
}
