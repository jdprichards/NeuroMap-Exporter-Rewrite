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
            string[] fileEnds = ["Firings", "MFR", "MUAPs", "Stats"];

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

        public async Task WriteHeaders(StreamWriter sw, string[] headers)
        {
            try
            {
                for (int i = 3; i < headers.Length; i += 2)
                {
                    sw.Write("\tfile01");
                }
                sw.WriteLine("");

                sw.Write("\tTime\tANALOGTIME");
                for (int i = 1; i < headers.Length; i += 2)
                {
                    sw.Write("\t" + headers[i]);
                }
                sw.WriteLine();

                sw.Write("\tFRAME_NUMBERS\tFRAME_NUMBERS");
                for (int i = 1; i < headers.Length; i += 2)
                {
                    sw.Write("\tANALOG");
                }
                sw.WriteLine();

                sw.WriteLine("ITEM");
                for (int i = 3; i < headers.Length; i += 2)
                {
                    sw.Write("\t0");
                }
                sw.WriteLine();
            }
            catch (Exception e)
            {
                Exception ex = e;
            }
        }

        public async Task UpsampleFileAsync(string file, string localPath)
        {
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    UpsamplerModel.RowAmount = File.ReadLines(file).Count();
                });

                StreamReader sr = new StreamReader(file);
                StreamWriter sw = new StreamWriter(UpsamplerModel.OutputFolder + localPath);

                // string strHeaders = sr.ReadLine();

                string stringHeaders = sr.ReadLine();
                string[] headers = stringHeaders.Replace("\"", "").Split(",");
               
                // Writing Headers
                WriteHeaders(sw, headers);

                string[] splitData = sr.ReadToEnd().Split("\n");
                string[] lineSplitData = splitData[0].Split(",");

                float lowestInterval = 100.0f;
                float highestInterval = 0.0f;
                float highestTime = 0.0f;

                int[] latestRow = [];
                int[] targetSplitLineData = [];
                float[] lastKnowValues = [];

                for (int i = 0; i < headers.Length; i++)
                {
                    lastKnowValues.Append(0);
                    latestRow.Append(1);
                }

                for (int i = 0; i < splitData.Length; i++)
                {
                    targetSplitLineData.Append(0);
                }

                //  Find Lowest and Highest Interval

                // Lowest Interval
                for (int i = 0; i < lineSplitData.Length - 2; i += 2)
                    if (float.Parse(lineSplitData[i]) < lowestInterval)
                        lowestInterval = float.Parse(lineSplitData[i]);

                // Highest Interval
                for (int i = 0; i < lineSplitData.Length - 2; i += 2)
                    if (float.Parse(lineSplitData[i]) > highestInterval)
                        highestInterval = float.Parse(lineSplitData[i]);

                for (int i = 0; i < lineSplitData.Length - 2; i += 2)
                    for (int k = 1; k < lineSplitData.Length - 1; k++)
                        if (lineSplitData[k].Split(",")[i] != "")
                            highestTime = float.Parse(lineSplitData[k].Split(",")[i]);

                int row = 0;
                float timeValue = lowestInterval * row;

                while(timeValue < highestTime)
                {
                    sw.Write(row + 1 + "\t" +  timeValue + "\t" + timeValue);
                    for (int i = 0; i < headers.Length; i += 2) 
                    {
                        if (lineSplitData[latestRow[i] - 1].Split(",")[i-1] != "")
                        {
                            if (float.Parse(lineSplitData[latestRow[i] - 1].Split(',')[i-1]) <= timeValue)
                            {
                                if (lastKnowValues[i] != float.Parse(lineSplitData[latestRow[i] - 1].Split(",")[i]))
                                    lastKnowValues[i] = float.Parse(lineSplitData[latestRow[i] - 1].Split(",")[i]);

                                if (latestRow[i] < lineSplitData.Length - 1)
                                    latestRow[i]++;
                            }
                        }

                        sw.Write("\t" + lastKnowValues[i]);
                    }
                    sw.WriteLine();
                    row++;
                    timeValue = lowestInterval * row;
                }
                sr.Close();
                sw.Close();

            }
            catch (Exception e)
            {
                Exception ex = e;
            }
        }
    }
}
