using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
        public NMExporterModel NMExporterModel { get; set; } = new NMExporterModel();

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
                await Task.Run(() => NMExporterModel.InputFolder = OpenOSDialog_ReturnString(true).Result);
            }
            catch (Exception ex) { }
        }

        public async void ClkImportSearchFolder()
        {
            try
            {
                await Task.Run(() => NMExporterModel.InputFolder = OpenOSDialog_ReturnString(false).Result);
            }
            catch (Exception ex) { }
        }

        public async void ClkExportSearchFolder()
        {
            try
            {
                await Task.Run(() => NMExporterModel.OutputFolder = OpenOSDialog_ReturnString(false).Result);
            }
            catch (Exception ex) { }
        }

        public async Task ClkAll()
        {
            if (NMExporterModel.SelectAll)
            {
                if (NMExporterModel.SelectFirings && NMExporterModel.SelectMFR && NMExporterModel.SelectMUAPS && NMExporterModel.SelectStats)
                {
                    NMExporterModel.SelectFirings = false;
                    NMExporterModel.SelectMFR = false;
                    NMExporterModel.SelectMUAPS = false;
                    NMExporterModel.SelectStats = false;
                    NMExporterModel.SelectAll = false;
                }
                else
                {
                    NMExporterModel.SelectFirings = true;
                    NMExporterModel.SelectMFR = true;
                    NMExporterModel.SelectMUAPS = true;
                    NMExporterModel.SelectStats = true;
                    NMExporterModel.SelectAll = true;
                }
            }
            else
            {
                NMExporterModel.SelectFirings = false;
                NMExporterModel.SelectMFR = false;
                NMExporterModel.SelectMUAPS = false;
                NMExporterModel.SelectStats = false;
                NMExporterModel.SelectAll = false;
            }
        }

        public async Task ConvertAsync()
        {

            NMExporterModel.DisableUpsample = true;
            NMExporterModel.HideProgress = false;

            // types of NeuroMap files denoted at the end of file
            // E.g. xxxxx_(Sensor 1)_Firings.txt
            string[] fileEnds = [];
            if (NMExporterModel.SelectFirings)
                fileEnds = fileEnds.Append("Firings").ToArray();
            if (NMExporterModel.SelectMFR)
                fileEnds = fileEnds.Append("MFR").ToArray();
            if (NMExporterModel.SelectMUAPS)
                fileEnds = fileEnds.Append("MUAPs").ToArray();
             if (NMExporterModel.SelectStats)
                fileEnds = fileEnds.Append("Stats").ToArray();

            // Get all files in directory
            string[] files = { };

            foreach (string fileEnd in fileEnds)
            {
                string fileEndExtenton = "*" + fileEnd + ".txt";
                files = files.Concat(Directory.GetFiles(NMExporterModel.InputFolder, fileEndExtenton, SearchOption.AllDirectories)).ToArray();
            }

            // Create Directory Layout at Output
            string[] localPaths = await Task.Run(() => AsyncCreateDirectoryLayout_ReturnLocalPaths(files));
            
            Dispatcher.UIThread.Invoke(() =>
            {
                NMExporterModel.FileComplete = 0;
                NMExporterModel.FileAmount = files.Length;

                NMExporterModel.FileCompleteRatio = NMExporterModel.FileComplete + "/" + NMExporterModel.FileAmount;
                NMExporterModel.FilePercentage = ((float)NMExporterModel.FileComplete / (float)NMExporterModel.FileAmount) * 100.0f;
                NMExporterModel.FilePercentageString = Math.Round(NMExporterModel.FilePercentage, 2) + "%";
            });

            for (int i = 0; i < files.Length; i++)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    NMExporterModel.CurrentFile = files[i];
                });

                await Task.Run(() => ConvertFileAsync(files[i], localPaths[i]));
                Dispatcher.UIThread.Invoke(() =>
                {
                    NMExporterModel.FileComplete++;

                    NMExporterModel.FileCompleteRatio = NMExporterModel.FileComplete + "/" + NMExporterModel.FileAmount;
                    NMExporterModel.FilePercentage = ((float)NMExporterModel.FileComplete / (float)NMExporterModel.FileAmount) * 100.0f;
                    NMExporterModel.FilePercentageString = Math.Round(NMExporterModel.FilePercentage, 2) + "%";

                    NMExporterModel.RowComplete = 0;
                });
            }

            

            NMExporterModel.DisableUpsample = false;
        }

        public async Task<string[]> AsyncCreateDirectoryLayout_ReturnLocalPaths(string[] files)
        {

            string[] localPaths = new string[files.Length];
            int i = 0;
            foreach(string file in files)
            {
                int index = file.IndexOf(NMExporterModel.InputFolder);

                localPaths[i] = (index < 0) ? NMExporterModel.InputFolder : file.Remove(index, NMExporterModel.InputFolder.Length);
                i++;
            }

            foreach (string localPath in localPaths)
            {
                string[] splitPath = localPath.Split("\\");
                for (int j = 0; j < splitPath.Length; j++)
                {
                    string checkPath = NMExporterModel.OutputFolder;
                    for (int k = 0; k < j; k++)
                    {
                        checkPath += ("\\" + splitPath[k]);
                    }    

                    if (!Directory.Exists(checkPath))
                    {
                        Directory.CreateDirectory(checkPath);
                    }
                }
            }

            return localPaths;
        }

        public async Task ConvertFileAsync(string file, string localPath)
        {
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    NMExporterModel.RowAmount = File.ReadLines(file).Count();

                });

                // Returns the type of Decomp file (E.g. Firings, MFR, etc.)
                string fileType = file.Split("_").Last().Split(".").First();

                StreamReader sr = new StreamReader(file);
                StreamWriter sw = new StreamWriter(NMExporterModel.OutputFolder + localPath);

                string headers = sr.ReadLine();
                string[] splitHeaders = headers.Split("\t");

                // If the Decomp file type is not Stats
                if (fileType != "Stats" && fileType != "MFR")
                {
                    // File01 Row
                    sw.Write("\tfile01\tfile01");
                    for (int i = 0; i < splitHeaders.Length; i++)
                    {
                        sw.Write("\tfile01");
                    }
                    sw.WriteLine();

                    // Time Analog Row
                    sw.Write("\tTIME\tANALOGTIME");
                    foreach (string header in splitHeaders)
                    {
                        sw.Write("\t" + header);
                    }
                    sw.WriteLine();

                    // Frame Numbers Row

                    sw.Write("\tFRAME_NUMBERS\tFRAME_NUMBERS");
                    for (int i = 0; i < splitHeaders.Length; i++)
                    {
                        sw.Write("\tANALOG");
                    }
                    sw.WriteLine();

                    // Original Row
                    for (int i = 0; i < splitHeaders.Length + 2; i++)
                    {
                        sw.Write("\tORIGINAL");
                    }
                    sw.WriteLine();

                    // Item Row
                    sw.Write("ITEM");
                    for (int i = 0; i < splitHeaders.Length + 2; i++)
                    {
                        sw.Write("\t0");
                    }
                }
                else if (fileType == "MFR")
                {
                    // File01 Row
                    sw.Write("\tfile01\tfile01");
                    for (int i = 0; i < splitHeaders.Length - 1; i++)
                    {
                            sw.Write("\tfile01");
                    }
                    sw.WriteLine();

                    // Time Analog Row
                    sw.Write("\tTIME\tANALOGTIME");
                    foreach (string header in splitHeaders)
                    {
                        if (header != "Time")
                            sw.Write("\t" + header);
                    }
                    sw.WriteLine();

                    // Frame Numbers Row

                    sw.Write("\tFRAME_NUMBERS\tFRAME_NUMBERS");
                    for (int i = 0; i < splitHeaders.Length -1 ; i++)
                    {
                            sw.Write("\tANALOG");
                    }
                    sw.WriteLine();

                    // Original Row
                    for (int i = 0; i < splitHeaders.Length + 1; i++)
                    {
                            sw.Write("\tORIGINAL");
                    }
                    sw.WriteLine();

                    // Item Row
                    sw.Write("ITEM");
                    for (int i = 0; i < splitHeaders.Length + 1 ; i++)
                    {
                            sw.Write("\t0");
                    }
                }
                // If Decomp File type is Stats
                else
                {  
                    // File01 Row
                    sw.Write("\tfile01\tfile01");
                    for (int i = 1; i < splitHeaders.Length; i++) 
                    {
                        sw.Write("\tfile01");
                    }
                    sw.WriteLine();

                    // TIME, ANALOGTIME and Headers
                    sw.Write("\tTIME\tANALOGTIME");
                    foreach(string header in splitHeaders)
                    {
                        sw.Write("\t" + header);
                    }
                    sw.WriteLine();

                    // FRAME_NUMBER ANALOG Row
                    sw.Write("\tFRAME_NUMBERS\tFRAME_NUMBERS");
                    for (int i = 1; i < splitHeaders.Length; i++)
                    {
                        sw.Write("\tANALOG");
                    }
                    sw.WriteLine();

                    // ORIGINAL Row
                    for (int i = 0; i < splitHeaders.Length + 1; i++)
                    {
                        sw.Write("\tORIGINAL");
                    }
                    sw.WriteLine();

                    // ITEM row
                    sw.Write("ITEM");
                    for (int i = 0; i < splitHeaders.Length + 1; i++)
                    {
                        sw.Write("\t0");
                    }
                }

                Dispatcher.UIThread.Invoke(() =>
                {
                    NMExporterModel.RowComplete++;
                    NMExporterModel.RowCompleteRatio = NMExporterModel.RowComplete + "/" + NMExporterModel.RowAmount;
                    NMExporterModel.RowPercentage = ((float)NMExporterModel.RowComplete / (float)NMExporterModel.RowAmount) * 100.0f;
                    NMExporterModel.RowPercentageString = Math.Round(NMExporterModel.RowPercentage, 2) + "%";
                });

                sw.WriteLine();

                string line = sr.ReadLine();
                int index = 0;

                // Read Data From Original File to Converted File
                do
                {
                    string[] splitLine = line.Split("\t");
                    int item = index + 1;
                    if (fileType == "MFR")
                        sw.Write(item + "\t" + splitLine[0] + "\t" + splitLine[0] + "\t");
                    else
                        sw.Write(item + "\t" + index + "\t" + index + "\t");

                    for (int i = 0; i < splitLine.Length; i++) 
                    {

                        if (fileType != "MFR")
                        {
                            sw.Write(splitLine[i]);
                            if (i < splitLine.Length - 1)
                                sw.Write("\t");
                        }

                        else
                        {
                            int tempI = i + 1;
                            if (tempI < splitLine.Length)
                                sw.Write(splitLine[tempI]);
                                if (tempI < splitLine.Length - 1)
                                    sw.Write("\t");
                        }

                        
                    }

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        NMExporterModel.RowComplete++;
                        NMExporterModel.RowCompleteRatio = NMExporterModel.RowComplete + "/" + NMExporterModel.RowAmount;
                        NMExporterModel.RowPercentage = ((float)NMExporterModel.RowComplete / (float)NMExporterModel.RowAmount) * 100.0f;
                        NMExporterModel.RowPercentageString = Math.Round(NMExporterModel.RowPercentage, 2) + "%";
                    });

                    sw.WriteLine();
                    line = sr.ReadLine();
                    index++;
                } while (line != null);


                sr.Close();
                sw.Close();

            }
            catch(Exception e) 
            {
                Exception error = e;
            }
        }

    }
}