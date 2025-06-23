using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HarfBuzzSharp;
using NeuroMap_Exporter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static NeuroMap_Exporter.ViewModels.CombineUpsampleViewModel;

namespace NeuroMap_Exporter.ViewModels
{

    
    public partial class CombineUpsampleViewModel : ViewModelBase
    {
        public CombineUpsampleModel CombineUpsampleModel { get; set; } = new CombineUpsampleModel();
        public CombineUpsampleViewModel() { }

        public struct FileTypeAndPath
        {
            public string EMGFilePath { get; set; }
            public string Sensor1FilePath { get; set; }
            public string Sensor2FilePath { get; set; }
        }

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
                await Task.Run(() => CombineUpsampleModel.InputFolder = OpenOSDialog_ReturnString(true).Result);
            }
            catch (Exception ex) { }
        }

        public async void ClkImportSearchFolder()
        {
            try
            {
                await Task.Run(() => CombineUpsampleModel.InputFolder = OpenOSDialog_ReturnString(false).Result);
            }
            catch (Exception ex) { }
        }

        public async void ClkExportSearchFolder()
        {
            try
            {
                await Task.Run(() => CombineUpsampleModel.OutputFolder = OpenOSDialog_ReturnString(false).Result);
            }
            catch (Exception ex) { }
        }

        //public async Task<string[]> AsyncCreateDirectoryLayout_ReturnLocalPaths(string[] files)
        public async Task<string[]> AsyncReturnLocalPaths(string[] files)
        {

            string[] localPaths = new string[files.Length];
            int i = 0;
            foreach (string file in files)
            {
                int index = file.IndexOf(CombineUpsampleModel.InputFolder);

                localPaths[i] = (index < 0) ? CombineUpsampleModel.InputFolder : file.Remove(index, CombineUpsampleModel.InputFolder.Length);
                i++;
            }

            // No need to create directory layout here, as we are just returning the local paths
            // Only single file will be produced so no need to create subdirectories

            /*            foreach (string localPath in localPaths)
                        {
                            string[] splitPath = localPath.Split("\\");
                            for (int j = 0; j < splitPath.Length; j++)
                            {
                                string checkPath = CombineUpsampleModel.OutputFolder;
                                for (int k = 0; k < j; k++)
                                {
                                    checkPath += ("\\" + splitPath[k]);
                                }

                                if (!Directory.Exists(checkPath))
                                {
                                    Directory.CreateDirectory(checkPath);
                                }
                            }
                        }*/

            return localPaths;
        }

        public async Task<string[]> FindFilesInDirectoryAsync(string[] files, string Directory )
        {
            string[] foundFiles = new string[0];

            foreach (string file in files)
            {
                if (file.StartsWith(Directory))
                {
                    Array.Resize(ref foundFiles, foundFiles.Length + 1);
                    foundFiles[foundFiles.Length - 1] = file;
                }
            }

            return foundFiles;
        }

        public async Task<FileTypeAndPath[]> PopulateFileTypeAndPathAsync(string[] localPaths)
        {
            FileTypeAndPath[] fileTypeAndPaths = new FileTypeAndPath[0];
            string[] dictionairyPaths = [];

            foreach (string path in localPaths)
            {
                string[] splitPath = path.Split("\\");
                dictionairyPaths = dictionairyPaths.Concat(splitPath.Take(splitPath.Length - 1)).ToArray();
            }
            dictionairyPaths = dictionairyPaths.Distinct().ToArray();

            foreach (string path in dictionairyPaths)
            {
                string[] matchingFiles = await FindFilesInDirectoryAsync(localPaths, path);

                if (matchingFiles.Length == 0)
                    continue;

                // Determine file type based on the file name or extension

                string emgFilePath = matchingFiles.FirstOrDefault(f => f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
                string sensor1FilePath = matchingFiles.FirstOrDefault(f => f.EndsWith("(Sensor 1)_MFR.txt", StringComparison.OrdinalIgnoreCase));
                string sensor2FilePath = matchingFiles.FirstOrDefault(f => f.EndsWith("(Sensor 2)_MFR.txt", StringComparison.OrdinalIgnoreCase));


                FileTypeAndPath fileTypeAndPath = new FileTypeAndPath
                {
                    EMGFilePath = emgFilePath ?? string.Empty,
                    Sensor1FilePath = sensor1FilePath ?? string.Empty,
                    Sensor2FilePath = sensor2FilePath ?? string.Empty
                };
                    
                Array.Resize(ref fileTypeAndPaths, fileTypeAndPaths.Length + 1);
                fileTypeAndPaths[fileTypeAndPaths.Length - 1] = fileTypeAndPath;
                
            }


            return fileTypeAndPaths;
        }

        public async Task CombineUpsampleAsync()
        {
            CombineUpsampleModel.DisableUpsample = true;
            CombineUpsampleModel.HideProgress = false;

            // All Files in Sub-Directories
            string[] files = { };
            string[] fileEndExtensions = { ".txt", ".csv" };

            foreach (string fileEndExtension in fileEndExtensions)
            {
                files = files.Concat(Directory.GetFiles(CombineUpsampleModel.InputFolder, "*" + fileEndExtension, SearchOption.AllDirectories)).ToArray();
            }

            // Create Directory Layout at Output Folder
            string[] localPaths = await Task.Run(() => AsyncReturnLocalPaths(files));
        
            FileTypeAndPath[] fileTypesAndPaths = PopulateFileTypeAndPathAsync(localPaths).Result;


            Dispatcher.UIThread.Invoke(() =>
            {
                CombineUpsampleModel.FileComplete = 0;
                CombineUpsampleModel.FileAmount = files.Length;
                CombineUpsampleModel.FileCompleteRatio = CombineUpsampleModel.FileComplete + " / " + CombineUpsampleModel.FileAmount;
                CombineUpsampleModel.FilePercentage = ((float)CombineUpsampleModel.FileComplete / (float)CombineUpsampleModel.FileAmount) * 100f;
                CombineUpsampleModel.FilePercentageString = Math.Round(CombineUpsampleModel.FilePercentage, 2) + "%";
            });

            for (int i = 0; i < fileTypesAndPaths.Length; i++)
            {
                await Task.Run(() => CombineUpsampleFileAsync(fileTypesAndPaths[i]));
                Dispatcher.UIThread.Invoke(() =>
                {
                    CombineUpsampleModel.FileComplete += 3;
                    CombineUpsampleModel.FileCompleteRatio = CombineUpsampleModel.FileComplete + " / " + CombineUpsampleModel.FileAmount;
                    CombineUpsampleModel.FilePercentage = ((float)CombineUpsampleModel.FileComplete / (float)CombineUpsampleModel.FileAmount) * 100f;
                    CombineUpsampleModel.FilePercentageString = Math.Round(CombineUpsampleModel.FilePercentage, 2) + "%";
                
                    CombineUpsampleModel.RowComplete = 0;
                });

            }

            CombineUpsampleModel.DisableUpsample = false;
        }

        public async Task CombineUpsampleFileAsync(FileTypeAndPath fileTypesAndPaths)
        {
            try
            {
                string outputFileName = fileTypesAndPaths.EMGFilePath.Split("\\").Last().Replace(".csv", ".txt");

                Dispatcher.UIThread.Invoke(() =>
                {
                    CombineUpsampleModel.RowComplete = 0;
                    CombineUpsampleModel.RowAmount = 0; // Reset row count for each file
                });

                int emgRowCount = File.ReadLines(fileTypesAndPaths.EMGFilePath).Count();
                int sensor1RowCount = File.ReadLines(fileTypesAndPaths.Sensor1FilePath).Count();
                int sensor2RowCount = File.ReadLines(fileTypesAndPaths.Sensor2FilePath).Count();

                float sensor1RowRatio = (float)emgRowCount / sensor1RowCount;
                float sensor2RowRatio = (float)emgRowCount / sensor2RowCount;

                // Create tmp directory to store temporary files
                string tmpDirectory = Path.Combine(CombineUpsampleModel.OutputFolder, "tmp");
                if (!Directory.Exists(tmpDirectory))
                {
                    Directory.CreateDirectory(tmpDirectory);
                }

                string tempEMGFileName = Path.Combine(tmpDirectory, outputFileName);

                UpsampleEMGFileAsync(fileTypesAndPaths.EMGFilePath, tempEMGFileName).Wait();


               
                // Creating Main Output File
                StreamWriter sw = new StreamWriter(CombineUpsampleModel.OutputFolder + outputFileName);

                // Read File
                string[] lines = await File.ReadAllLinesAsync("file");
                // Process Lines
                List<string> processedLines = new List<string>();
                foreach (string line in lines)
                {
                    // Example processing: just trim whitespace
                    processedLines.Add(line.Trim());
                }



                // Write to New File
                string outputFilePath = Path.Combine(CombineUpsampleModel.OutputFolder, outputFileName);
                
                await File.WriteAllLinesAsync(outputFilePath, processedLines);



                Directory.Delete(tmpDirectory, true); // Delete tmp directory after processing
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log them, show a message to the user, etc.)
                Console.WriteLine($"Error processing file {fileTypesAndPaths.EMGFilePath}: {ex.Message}");
            }
        }

        public async Task UpsampleEMGFileAsync(string emgFilePath, string tempEMGFileName)
        {
            // Create temporary file for upsampled EMG-IMU data
            tempEMGFileName = tempEMGFileName.Replace(".txt", " temp.csv"); // Ensure the file path is correct
            StreamWriter TempEMGsw = new StreamWriter(tempEMGFileName);

            StreamReader EMGsr = new StreamReader(emgFilePath);

            string allData = EMGsr.ReadToEnd();
            EMGsr.Close();

            string[] dataLineSplit = allData.Replace("\r","").Split("\n"); // Replace line breaks with commas for easier processing

            string[,] dataSplit = new string[dataLineSplit.Length, dataLineSplit[0].Split(",").Length];

            for (int i = 0; i < dataLineSplit.Length; i++)
            {
                for (int j = 0; j < dataLineSplit[i].Split(",").Length; j++)
                {
                    dataSplit[i, j] = dataLineSplit[i].Split(",")[j].Trim(); // Split each line by commas and trim whitespace
                }
            }

            // Get None time series headers from EMG file
            string[] EMGHeaders = new string[0];
            string[] fullHeaders = dataLineSplit[0].Split(","); // Read headers from EMG file

            foreach (string header in fullHeaders)
            {
                if (!header.Contains("X[s]"))
                {
                    EMGHeaders = EMGHeaders.Append(header).ToArray();
                }
            }

            string[] timeIntervals = dataLineSplit[2].Split(","); // Get first none-zero time series data line

            float lowestTime = float.MaxValue;
            foreach (string timeInterval in timeIntervals)
            {
                if (float.TryParse(timeInterval, out float timeValue) && timeValue < lowestTime && timeValue != 0)
                {
                    lowestTime = timeValue;
                }
            }

            EMGsr = new StreamReader(emgFilePath); // Return Stream Reader to start of file

            // Initialize temporary EMG file with headers and first data line
            // Note: The first data line is assumed to be all zeroes, as per the original code logic
            string writeHeaders = "Time";
            string writeFirstDataLine = "0";

            foreach (string header in EMGHeaders)
            {
                writeHeaders += "," + header;
                writeFirstDataLine += ",0"; // Assuming the first data line is all zeroes
            }
            
            // Write Headers to Temporary EMG File
            TempEMGsw.WriteLine(writeHeaders);
            TempEMGsw.WriteLine(writeFirstDataLine); 

            float[] nextTime = new float[EMGHeaders.Length];
            int[] currentRow = new int[EMGHeaders.Length];

            for (int i = 0; i < EMGHeaders.Length; i++)
            {
                currentRow[i] = 2; // Initialize current row index
            }

            // Fill previous time and last datum as zero
            foreach (string header in EMGHeaders)
            {
                nextTime[Array.IndexOf(EMGHeaders, header)] = 0f;
            }


            string outRow = "";
            int mainRow = 1; // Start from the second row since the first row is headers
            bool readingFile = true;


            try
            {
                do
                {
                    readingFile = true;


                    outRow = mainRow * lowestTime + "";

                    for (int i = 0; i < EMGHeaders.Length; i += 1)
                    {

                        if (dataSplit[currentRow[i], i * 2] != "" && dataSplit[currentRow[i], i * 2] != null)
                        {
                            nextTime[i] = float.Parse(dataSplit[currentRow[i], i * 2]);
                        }
                    }

                    for (int i = 0; i < EMGHeaders.Length; i += 1)
                    {
                        if (nextTime[i] <= lowestTime * mainRow)
                        {
                            currentRow[i]++;
                        }

                        if (dataSplit[currentRow[i], i * 2] != "" && dataSplit[currentRow[i], i * 2] != null)
                        {
                            outRow += ("," + dataSplit[currentRow[i], i * 2 + 1]);
                        }
                    }

                    mainRow++;
                    for (int i = 0; i < EMGHeaders.Length; i++)
                    {
                        int row = currentRow[i];
                        if (dataSplit[row, i * 2] == "" || dataSplit[row, i * 2] == null)
                        {
                            readingFile = false; // Any of the time data is not null, so we are still reading the file
                            break;
                        }
                    }


                    if (readingFile)
                        TempEMGsw.WriteLine(outRow);

                    

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        CombineUpsampleModel.RowComplete++;
                        CombineUpsampleModel.RowCompleteRatio = CombineUpsampleModel.RowComplete + " / " + CombineUpsampleModel.RowAmount;
                        CombineUpsampleModel.RowPercentage = ((float)CombineUpsampleModel.RowComplete / (float)CombineUpsampleModel.RowAmount) * 100f;
                        CombineUpsampleModel.RowPercentageString = Math.Round(CombineUpsampleModel.RowPercentage, 2) + "%";

                    });
                } while (readingFile);
            }
            catch(Exception ex)
            {
                Exception exception = ex;
            }

            TempEMGsw.Close();
        }
    }
}