using Avalonia.Platform.Storage;
using NeuroMap_Exporter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroMap_Exporter.ViewModels
{
    public partial class NMExporterViewModel : ViewModelBase
    {
        public NMExporterModel neuroMapExporterModel { get; set; } = new NMExporterModel();

        public NMExporterViewModel() { }

        public async Task<string> OpenOSDialog_ReturnString(bool boolFile)
        {
            var topLevel = App.TopLevel;

            if (topLevel != null && topLevel.StorageProvider.CanOpen)
            {
                if (boolFile)
                {
                    var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        AllowMultiple = false,
                        Title = "Select a File",

                    });

                    // Will always have only one file

                    if (file[0] != null)
                        return file[0].TryGetLocalPath();
                }
                else
                {

                    var file = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                    {
                        AllowMultiple = false,
                        Title = "Select a Folder",

                    });

                    if (file[0] != null)
                        return file[0].TryGetLocalPath();
                }
            }

            return "";
        }

        public async void ClkImportSearchFile()
        {
            try
            {
                await Task.Run(() => neuroMapExporterModel.InputFolder = OpenOSDialog_ReturnString(true).Result);
            }
            catch (Exception ex) { }
        }

        public async void ClkImportSearchFolder()
        {
            try
            {
                await Task.Run(() => neuroMapExporterModel.InputFolder = OpenOSDialog_ReturnString(false).Result);
            }
            catch (Exception ex) { }
        }

        public async void ClkExportSearchFolder()
        {
            try
            {
                await Task.Run(() => neuroMapExporterModel.OutputFolder = OpenOSDialog_ReturnString(false).Result);
            }
            catch (Exception ex) { }
        }

        public async Task UpsampleAsync()
        {
            // Get all files in directory
            string[] files = Directory.GetFiles(neuroMapExporterModel.InputFolder);

            neuroMapExporterModel.DisableUpsample = true;
            neuroMapExporterModel.HideProgress = false;



            neuroMapExporterModel.DisableUpsample = false;
        }
    }
}
