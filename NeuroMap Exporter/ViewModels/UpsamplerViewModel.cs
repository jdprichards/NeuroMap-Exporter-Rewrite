using Avalonia.Platform.Storage;
using NeuroMap_Exporter.Models;
using NeuroMap_Exporter.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NeuroMap_Exporter.Models;
using Avalonia.Threading;
using System.IO;

namespace NeuroMap_Exporter.ViewModels
{
    public partial class UpsamplerViewModel : ViewModelBase
    {
        public UpsamplerModel UpsamplerModel { get; set; } = new UpsamplerModel();
        public UpsamplerViewModel() { }

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
                await Task.Run(() => UpsamplerModel.InputFolder = OpenOSDialog_ReturnString(true).Result);
            }
            catch (Exception ex) { }
        }

        public async void ClkImportSearchFolder()
        {
            try
            {
                await Task.Run(() => UpsamplerModel.InputFolder = OpenOSDialog_ReturnString(false).Result);
            }
            catch (Exception ex) { }
        }

        public async void ClkExportSearchFolder()
        {
            try
            {
                await Task.Run(() => UpsamplerModel.OutputFolder = OpenOSDialog_ReturnString(false).Result);
            }
            catch (Exception ex) { }
        }

        public async Task<string[]> AsyncCreateDirectoryLayout_ReturnLocalPaths(string[] files)
        {

            string[] localPaths = new string[files.Length];
            int i = 0;
            foreach (string file in files)
            {
                int index = file.IndexOf(UpsamplerModel.InputFolder);

                localPaths[i] = (index < 0) ? UpsamplerModel.InputFolder : file.Remove(index, UpsamplerModel.InputFolder.Length);
                i++;
            }

            foreach (string localPath in localPaths)
            {
                string[] splitPath = localPath.Split("\\");
                for (int j = 0; j < splitPath.Length; j++)
                {
                    string checkPath = UpsamplerModel.OutputFolder;
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

        public async Task UpsampleAsync()
        {

            UpsamplerModel.DisableUpsample = true;
            UpsamplerModel.HideProgress = false;

            // types of NeuroMap files denoted at the end of file
            // E.g. xxxxx_(Sensor 1)_Firings.txt

            // Get all files in directory
            string[] files = { };

            string fileEndExtenton = "*.csv";
            files = files.Concat(Directory.GetFiles(UpsamplerModel.InputFolder, fileEndExtenton, SearchOption.AllDirectories)).ToArray();

            // Create Directory Layout at Output
            string[] localPaths = await Task.Run(() => AsyncCreateDirectoryLayout_ReturnLocalPaths(files));

            Dispatcher.UIThread.Invoke(() =>
            {
                UpsamplerModel.FileComplete = 0;
                UpsamplerModel.FileAmount = files.Length;

                UpsamplerModel.FileCompleteRatio = UpsamplerModel.FileComplete + "/" + UpsamplerModel.FileAmount;
                UpsamplerModel.FilePercentage = ((float)UpsamplerModel.FileComplete / (float)UpsamplerModel.FileAmount) * 100.0f;
                UpsamplerModel.FilePercentageString = Math.Round(UpsamplerModel.FilePercentage, 2) + "%";
            });

            for (int i = 0; i < files.Length; i++)
            {
                await Task.Run(() => UpsampleFileAsync(files[i], localPaths[i]));
                Dispatcher.UIThread.Invoke(() =>
                {
                    UpsamplerModel.FileComplete++;

                    UpsamplerModel.FileCompleteRatio = UpsamplerModel.FileComplete + "/" + UpsamplerModel.FileAmount;
                    UpsamplerModel.FilePercentage = ((float)UpsamplerModel.FileComplete / (float)UpsamplerModel.FileAmount) * 100.0f;
                    UpsamplerModel.FilePercentageString = Math.Round(UpsamplerModel.FilePercentage, 2) + "%";

                    UpsamplerModel.RowComplete = 0;
                });
            }



            UpsamplerModel.DisableUpsample = false;
        }

        public async Task WriteHeaders(StreamWriter outputSw, string[] headers)
        {

            try
            {
                // file01 row
                string outputLine = "\tfile01\tfile01";
                foreach (string header in headers)
                {
                    if (header != "")
                        outputLine += "\tfile01"; // Add tab before each header
                }
                outputSw.WriteLine(outputLine);

                // time Analogtime row
                outputLine = "\tTIME\tANALOGTIME";
                foreach (string header in headers)
                {
                    if (header != "")
                        outputLine += "\t" + header; // Add tab before each header
                }
                outputSw.WriteLine(outputLine);

                // Frame_Numbers Analog row
                outputLine = "\tFRAME_NUMBERS\tFRAME_NUMBERS";
                foreach (string header in headers)
                {
                    if (header != "")
                        outputLine += "\t" + "ANALOG"; // Add tab before each header
                }
                outputSw.WriteLine(outputLine);

                outputLine = "\tORIGINAL\tORIGINAL"; // New line after headers
                foreach (string header in headers)
                {
                    if (header != "")
                        outputLine += "\t" + "ORIGINAL"; // Add tab before each header
                }
                outputSw.WriteLine(outputLine);

                // ITEM row
                outputLine = "ITEM\t0\t0"; // New line after headers
                foreach (string header in headers)
                {
                    if (header != "")
                        outputLine += "\t" + "0"; // Add tab before each header
                }
                outputSw.WriteLine(outputLine);
            }
            catch (Exception e)
            {
                Exception ex = e;
            }
        }

        public async Task UpsampleFileAsync(string file, string localPath)
        {

            // This creates a local file name ending with upsampled and the files original extension
            string[] splitExtention = localPath.Split(".");
            localPath = localPath.Replace("." + splitExtention.Last(), "_Upsampled." + splitExtention.Last());

            StreamReader sr = new StreamReader(file);
            StreamWriter sw = new StreamWriter(UpsamplerModel.OutputFolder + localPath);

            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    UpsamplerModel.RowComplete = 0;
                    UpsamplerModel.RowAmount = File.ReadLines(file).Count();

                    UpsamplerModel.RowCompleteRatio = UpsamplerModel.RowComplete + " / " + UpsamplerModel.RowAmount;
                    UpsamplerModel.RowPercentage = ((float)UpsamplerModel.RowComplete) / (float)UpsamplerModel.RowAmount * 100.0f;
                    UpsamplerModel.RowPercentageString = Math.Round(UpsamplerModel.RowPercentage, 2) + "%";


                    UpsamplerModel.CurrentFile = file;
                });

                string allData = sr.ReadToEnd();
                sr.Close();

                string[] dataLineSplit = allData.Replace("\r", "").Split("\n");
                string[][] dataSplit = new string[dataLineSplit.Length][];

                for (int i = 0; i < dataLineSplit.Length; i++)
                {
                    string[] splitLine = dataLineSplit[i].Split(",");
                    dataSplit[i] = splitLine;
/*                    for (int j = 0; j < dataLineSplit[i].Length; j++)
                        dataSplit[i][j] = splitLine[j].Trim();*/
                }

                // string strHeaders = sr.ReadLine();

                string[] fullHeaders = dataLineSplit[0].Split(",");
                string[] headers = new string[0];

                foreach (string header in fullHeaders)
                {
                    if (!header.Contains("X[s]"))
                    {
                        string newHeader = header.Replace(":", "").Replace(" ", "_"); // Remove colons and replace spaces with underscores
                        newHeader = newHeader.Replace("\"", ""); // Remove Quotation marks
                        headers = headers.Append(newHeader).ToArray();
                    }
                }

                string[] timeIntervals = dataLineSplit[2].Split(","); // Get first none-zero time series data line
                float lowestTime = float.MaxValue;

                foreach (string timeInterval in timeIntervals)
                {
                    if (float.TryParse(timeInterval, out float timeValue) && timeValue > 0)
                    {
                        lowestTime = Math.Min(lowestTime, timeValue);
                    }
                }

                // Writing Headers
                WriteHeaders(sw, headers);

                Dispatcher.UIThread.Invoke(() =>
                {
                    UpsamplerModel.RowComplete++;
                    UpsamplerModel.RowCompleteRatio = UpsamplerModel.RowComplete + " / " + UpsamplerModel.RowAmount;
                    UpsamplerModel.RowPercentage = ((float)UpsamplerModel.RowComplete) / (float)UpsamplerModel.RowAmount * 100.0f;
                    UpsamplerModel.RowPercentageString = Math.Round(UpsamplerModel.RowPercentage, 2) + "%";
                });

                bool readFile = true;
                int currentItem = 1; // Start from 1

                float[] nextTime = new float[headers.Length];
                int[] currentRow = new int[headers.Length];

                for (int i = 0; i < headers.Length; i++)
                {
                    nextTime[i] = 0.0f;
                    currentRow[i] = 2;
                }

                string outRow = "";


                // Writing Data
                do
                {
                    readFile = true;
                    outRow = currentItem + "\t" + currentItem * lowestTime + "\t" + currentItem * lowestTime;

                    for (int i = 0; i < headers.Length; i++)
                    {
                        if (dataSplit[currentRow[i]][i * 2] != "" && dataSplit[currentRow[i]][i * 2] != null)
                        {
                            nextTime[i] = float.Parse(dataSplit[currentRow[i]][i * 2]);
                        }
                    }

                    for (int i = 0; i < headers.Length; i++)
                    {
                        if (nextTime[i] <= currentItem * lowestTime)
                        {
                           
                            currentRow[i]++;
                        }
                        if (dataSplit[currentRow[i]][i * 2] != "" && dataSplit[currentRow[i]][i * 2] != null)
                        {
                            if (currentRow[i] < dataSplit.Length)
                                outRow += "\t" + dataSplit[currentRow[i]][i * 2 + 1]; // Add tab before each data
                        }
                    }

                    currentItem++;

                    for (int i = 0; i < headers.Length; i++)
                    {
                        if (dataSplit[currentRow[i]][i * 2 + 1] == "" || dataSplit[currentRow[i]][i * 2 + 1] == null)
                        {
                            readFile = false;
                        }
                    }

                    if (readFile)
                    {
                        sw.WriteLine(outRow);

                        Dispatcher.UIThread.Invoke(() =>
                        {
                            UpsamplerModel.RowComplete++;
                            UpsamplerModel.RowCompleteRatio = UpsamplerModel.RowComplete + " / " + UpsamplerModel.RowAmount;
                            UpsamplerModel.RowPercentage = ((float)UpsamplerModel.RowComplete) / (float)UpsamplerModel.RowAmount * 100.0f;
                            UpsamplerModel.RowPercentageString = Math.Round(UpsamplerModel.RowPercentage, 2) + "%";
                        });
                    }
                } while (readFile);

                
            }
            catch (Exception e)
            {
                Exception ex = e;
            }

            sw.Flush();
            sw.Close();
            
        }
    }
}
