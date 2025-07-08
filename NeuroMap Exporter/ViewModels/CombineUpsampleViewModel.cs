using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HarfBuzzSharp;
using NeuroMap_Exporter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
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
            /*public string Sensor1FilePath { get; set; }
            public string Sensor2FilePath { get; set; }*/

            public string[] SensorFilePaths { get; set; }
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

        public async Task<string[]> FindFilesInDirectoryAsync(string[] files, string Directory)
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

        public async Task<string[]> GetUniqueDirectories(string[] files)
        {
            string[] uniqueDirectories = new string[0];

            foreach(string filePath in files)
            {
                Array.Resize(ref uniqueDirectories, uniqueDirectories.Length + 1);
                string path = "";
                string[] splitPath = filePath.Split("\\");
                path = splitPath[0];
                for (int i = 1; i < splitPath.Length - 1; i++) 
                {
                    path = Path.Combine(path, splitPath[i]);
                }

                uniqueDirectories[uniqueDirectories.Length - 1] = path;
            }

            uniqueDirectories = uniqueDirectories.Distinct().ToArray();

            return uniqueDirectories;
        }

        public async Task<FileTypeAndPath[]> PopulateFileTypeAndPathAsync(string[] emgPaths)
        {
            FileTypeAndPath[] fileTypeAndPaths = new FileTypeAndPath[0];
            string[] matchingFiles = [];
            string[] missingFiles = [];

            string[] sensorPaths = [];

            try
            {
                foreach (string path in emgPaths)
                {
                    sensorPaths = [];
                    matchingFiles = Directory.GetFiles(CombineUpsampleModel.InputFolder, "*" + path.Split("\\").Last().Replace(".csv", "*"), SearchOption.AllDirectories).ToArray();

                    if (matchingFiles.Length == 0)
                        continue;

                    // Determine file type based on the file name or extension
                    string emgFilePath = matchingFiles.FirstOrDefault(f => f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
                    
                    foreach (string filePath in matchingFiles)
                    {
                        if (filePath.Contains("_MFR.txt"))
                            sensorPaths = sensorPaths.Append(filePath).ToArray();
                    }
                    string sensor1FilePath = matchingFiles.FirstOrDefault(f => f.EndsWith("(Sensor 1)_MFR.txt", StringComparison.OrdinalIgnoreCase));
                    string sensor2FilePath = matchingFiles.FirstOrDefault(f => f.EndsWith("(Sensor 2)_MFR.txt", StringComparison.OrdinalIgnoreCase));

                    // If any of the file paths are null or empty, skip this iteration
                    /*if (string.IsNullOrEmpty(emgFilePath) || string.IsNullOrEmpty(sensor1FilePath) || string.IsNullOrEmpty(sensor2FilePath))
                    {
                        if(string.IsNullOrEmpty(emgFilePath))
                        {
                            missingFiles = missingFiles.Append("EMG file not found for: " + path).ToArray();
                        }
                        if (string.IsNullOrEmpty(sensor1FilePath))
                        { 
                            missingFiles = missingFiles.Append("Sensor 1 file not found for: " + path).ToArray();
                        }
                        if (string.IsNullOrEmpty(sensor2FilePath))
                        {
                            missingFiles = missingFiles.Append("Sensor 2 file not found for: " + path).ToArray();
                        }
                        continue; // Skip this iteration if any file path is null or empty
                    }*/
                    /*else
                    {*/

                        FileTypeAndPath fileTypeAndPath = new FileTypeAndPath
                        {
                            EMGFilePath = emgFilePath ?? string.Empty,
                            SensorFilePaths = sensorPaths
                        };

                        Array.Resize(ref fileTypeAndPaths, fileTypeAndPaths.Length + 1);
                        fileTypeAndPaths[fileTypeAndPaths.Length - 1] = fileTypeAndPath;
                    //}
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log them, show a message to the user, etc.)
                Console.WriteLine($"Error processing files: {ex.Message}");
            }

            return fileTypeAndPaths;
        }

        public async Task CombineUpsampleAsync()
        {
            string tempDirectory = Path.Combine(CombineUpsampleModel.OutputFolder, "temporary files");

            CombineUpsampleModel.DisableUpsample = true;
            CombineUpsampleModel.HideProgress = false;

            // All Files in Sub-Directories
            string[] EMGFiles = { };
            string[] fileEndExtensions = { ".txt", ".csv" };


            EMGFiles = EMGFiles.Concat(Directory.GetFiles(CombineUpsampleModel.InputFolder, "*" + ".csv", SearchOption.AllDirectories)).ToArray();
            

            // Create Directory Layout at Output Folder
            string[] localPaths = await Task.Run(() => AsyncReturnLocalPaths(EMGFiles));

            string[] uniquePaths = GetUniqueDirectories(EMGFiles).Result;

            

            string[] files = [];
            foreach (string uniquePath in uniquePaths)
            {
                
                files = files.Concat(Directory.GetFiles(uniquePath, "*" + uniquePath.Split("\\").Last() + "*", SearchOption.AllDirectories)).ToArray();
            }

            FileTypeAndPath[] fileTypesAndPaths = PopulateFileTypeAndPathAsync(files).Result;
            int totalFiles = 0;
            foreach( FileTypeAndPath fileTypeAndPath in fileTypesAndPaths)
            {
                totalFiles += fileTypeAndPath.SensorFilePaths.Length;
                totalFiles += 2;
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                CombineUpsampleModel.FileComplete = 0;
                CombineUpsampleModel.FileAmount = totalFiles;

                CombineUpsampleModel.FileCompleteRatio = CombineUpsampleModel.FileComplete + " / " + CombineUpsampleModel.FileAmount;
                CombineUpsampleModel.FilePercentage = ((float)CombineUpsampleModel.FileComplete / (float)CombineUpsampleModel.FileAmount) * 100f;
                CombineUpsampleModel.FilePercentageString = Math.Round(CombineUpsampleModel.FilePercentage, 2) + "%";
            });

            for (int i = 0; i < fileTypesAndPaths.Length; i++)
            {
                await Task.Run(() => CombineUpsampleFileAsync(fileTypesAndPaths[i], tempDirectory));
            }

            if (!CombineUpsampleModel.KeepTemporaryFiles)
                Directory.Delete(tempDirectory, true); // Delete temp directory after processing

            CombineUpsampleModel.DisableUpsample = false;
        }

        public async Task CombineUpsampleFileAsync(FileTypeAndPath fileTypesAndPaths, string tempDirectory)
        {
            try
            {
                string[] splitEMGPath = fileTypesAndPaths.EMGFilePath.Split("\\"); 
                string outputFileName = splitEMGPath.Last().Replace(".csv", ".txt");
                string[] reverseSubDirectory = splitEMGPath.Take(splitEMGPath.Length - 1).Reverse().ToArray();

                // Create Output file path with subdirectory
                string outputFilePath = Path.Combine(CombineUpsampleModel.OutputFolder, reverseSubDirectory[2], reverseSubDirectory[1], outputFileName);
                

                string directoryName = outputFileName.Replace(".txt", "");

                // Create tmp directory to store temporary files
                tempDirectory = Path.Combine(tempDirectory, directoryName);

                if (!Directory.Exists(tempDirectory))
                {
                    Directory.CreateDirectory(tempDirectory);
                }

                string tempEMGFilePath = Path.Combine(tempDirectory, directoryName + "_(EMG-IMU)_temp.csv");
                string[] tempSensorFilePaths = [];

                for (int i = 0; i < fileTypesAndPaths.SensorFilePaths.Length; i++)
                {
                    tempSensorFilePaths = tempSensorFilePaths.Append(Path.Combine(tempDirectory, directoryName + "_(Sensor_" + (i + 1) + ")_temp.csv")).ToArray();
                }
           
               /* string tempSensor1FilePath = Path.Combine(tempDirectory, directoryName + "_(Sensor1)_temp.csv");
                string tempSensor2FilePath = Path.Combine(tempDirectory, directoryName + "_(Sensor2)_temp.csv");*/

                CombineUpsampleModel.CurrentFile = fileTypesAndPaths.EMGFilePath;
                UpsampleEMGFileAsync(fileTypesAndPaths.EMGFilePath, tempEMGFilePath).Wait();
                Dispatcher.UIThread.Invoke(() =>
                {
                    CombineUpsampleModel.FileComplete++;
                    CombineUpsampleModel.FileCompleteRatio = CombineUpsampleModel.FileComplete + " / " + CombineUpsampleModel.FileAmount;
                    CombineUpsampleModel.FilePercentage = ((float)CombineUpsampleModel.FileComplete / (float)CombineUpsampleModel.FileAmount) * 100f;
                    CombineUpsampleModel.FilePercentageString = Math.Round(CombineUpsampleModel.FilePercentage, 2) + "%";

                    CombineUpsampleModel.RowComplete = 0;
                });

                
                for (int i = 0; i < fileTypesAndPaths.SensorFilePaths.Length; i++) 
                {
                    CombineUpsampleModel.CurrentFile = fileTypesAndPaths.SensorFilePaths[i];
                    UpsampleSensorFileAsync(fileTypesAndPaths.SensorFilePaths[i], tempSensorFilePaths[i], tempEMGFilePath).Wait();
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        CombineUpsampleModel.FileComplete++;
                        CombineUpsampleModel.FileCompleteRatio = CombineUpsampleModel.FileComplete + " / " + CombineUpsampleModel.FileAmount;
                        CombineUpsampleModel.FilePercentage = ((float)CombineUpsampleModel.FileComplete / (float)CombineUpsampleModel.FileAmount) * 100f;
                        CombineUpsampleModel.FilePercentageString = Math.Round(CombineUpsampleModel.FilePercentage, 2) + "%";

                        CombineUpsampleModel.RowComplete = 0;
                    });
                }

                /*UpsampleSensorFileAsync(fileTypesAndPaths.Sensor2FilePath, tempSensor2FilePath, tempEMGFilePath).Wait();
                Dispatcher.UIThread.Invoke(() =>
                {
                    CombineUpsampleModel.FileComplete++;
                    CombineUpsampleModel.FileCompleteRatio = CombineUpsampleModel.FileComplete + " / " + CombineUpsampleModel.FileAmount;
                    CombineUpsampleModel.FilePercentage = ((float)CombineUpsampleModel.FileComplete / (float)CombineUpsampleModel.FileAmount) * 100f;
                    CombineUpsampleModel.FilePercentageString = Math.Round(CombineUpsampleModel.FilePercentage, 2) + "%";

                    CombineUpsampleModel.RowComplete = 0;
                });*/


                CombineUpsampleModel.CurrentFile =  outputFilePath;
                CombineEMGandSensorFiles(tempEMGFilePath, tempSensorFilePaths, outputFilePath).Wait();
                Dispatcher.UIThread.Invoke(() =>
                {
                    CombineUpsampleModel.FileComplete++;
                    CombineUpsampleModel.FileCompleteRatio = CombineUpsampleModel.FileComplete + " / " + CombineUpsampleModel.FileAmount;
                    CombineUpsampleModel.FilePercentage = ((float)CombineUpsampleModel.FileComplete / (float)CombineUpsampleModel.FileAmount) * 100f;
                    CombineUpsampleModel.FilePercentageString = Math.Round(CombineUpsampleModel.FilePercentage, 2) + "%";

                    CombineUpsampleModel.RowComplete = 0;
                });

                System.Console.Write("Finished Combine Upsample");
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

            StreamWriter TempEMGSw = new StreamWriter(tempEMGFileName);

            StreamReader EMGSr = new StreamReader(emgFilePath);

            Dispatcher.UIThread.Invoke(() =>
            {
                CombineUpsampleModel.RowComplete = 0;
                CombineUpsampleModel.RowAmount = File.ReadAllLines(emgFilePath).Count(); // Reset row count for each file

                CombineUpsampleModel.RowCompleteRatio = CombineUpsampleModel.RowComplete + " / " + CombineUpsampleModel.RowAmount;
                CombineUpsampleModel.RowPercentage = ((float)CombineUpsampleModel.RowComplete / (float)CombineUpsampleModel.RowAmount) * 100f;
                CombineUpsampleModel.RowPercentageString = Math.Round(CombineUpsampleModel.RowPercentage, 2) + "%";
            });

            string allData = EMGSr.ReadToEnd();
            EMGSr.Close();

            string[] dataLineSplit = allData.Replace("\r", "").Split("\n"); // Replace line breaks with commas for easier processing

            string[][] dataSplit = new string[dataLineSplit.Length][];

            for (int i = 0; i < dataLineSplit.Length; i++)
            {
                string[] splitLine = dataLineSplit[i].Split(",");
                dataSplit[i] = splitLine;

            }

            // Get None time series headers from EMG file
            string[] EMGHeaders = new string[0];
            string[] fullHeaders = dataLineSplit[0].Split(","); // Read headers from EMG file

            foreach (string header in fullHeaders)
            {
                if (!header.Contains("X[s]"))
                {
                    string newHeader = header.Replace(":", "").Replace(" ", "_"); // Remove colons and replace spaces with underscores
                    newHeader = newHeader.Replace("\"", ""); // Remove quotation mark
                    EMGHeaders = EMGHeaders.Append(newHeader).ToArray();
                }
            }

            string[] timeIntervals = dataLineSplit[2].Split(","); // Get first none-zero time series data line

            float lowestTime = float.MaxValue;
            foreach (string timeInterval in timeIntervals)
            {
                if (float.TryParse(timeInterval, out float timeValue) &&  timeValue > 0)
                {
                    lowestTime = Math.Min(lowestTime, timeValue);
                }
            }

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
            TempEMGSw.WriteLine(writeHeaders);
            TempEMGSw.WriteLine(writeFirstDataLine);

            float[] nextTime = new float[EMGHeaders.Length];
            int[] currentRow = new int[EMGHeaders.Length];

            for (int i = 0; i < EMGHeaders.Length; i++)
            {
                nextTime[i] = 0.0f;
                currentRow[i] = 2; // Initialize current row index
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

                    for (int i = 0; i < EMGHeaders.Length; i ++)
                    {

                        if (dataSplit[currentRow[i]][i * 2] != "" && dataSplit[currentRow[i]][i * 2] != null)
                        {
                            nextTime[i] = float.Parse(dataSplit[currentRow[i]][i * 2]);
                        }
                    }

                    for (int i = 0; i < EMGHeaders.Length; i++)
                    {
                        if (nextTime[i] <= lowestTime * mainRow)
                        {
                            currentRow[i]++;
                        }

                        if (dataSplit[currentRow[i]][i * 2] != "" && dataSplit[currentRow[i]][i * 2] != null)
                        {
                            outRow += ("," + dataSplit[currentRow[i]][  i * 2 + 1]);
                        }
                    }

                    mainRow++;
                    for (int i = 0; i < EMGHeaders.Length; i++)
                    {
                        int row = currentRow[i];
                        if (dataSplit[row][i * 2] == "" || dataSplit[row][i * 2] == null)
                        {
                            readingFile = false; // Any of the time data is not null, so we are still reading the file
                            break;
                        }
                    }


                    if (readingFile)
                        TempEMGSw.WriteLine(outRow);



                    Dispatcher.UIThread.Invoke(() =>
                    {
                        CombineUpsampleModel.RowComplete++;

                        CombineUpsampleModel.RowCompleteRatio = CombineUpsampleModel.RowComplete + " / " + CombineUpsampleModel.RowAmount;
                        CombineUpsampleModel.RowPercentage = ((float)CombineUpsampleModel.RowComplete / (float)CombineUpsampleModel.RowAmount) * 100f;
                        CombineUpsampleModel.RowPercentageString = Math.Round(CombineUpsampleModel.RowPercentage, 2) + "%";

                    });

                } while (readingFile);
            }
            catch (Exception ex)
            {
                Exception exception = ex;
            }

            TempEMGSw.Close();
        }

        public async Task UpsampleSensorFileAsync(string sensorFilePath, string tempSensorFileName, string tempEMGFilePath)
        {
            // Create temporary file for upsampled sensor data

            StreamWriter TempSensorSw = new StreamWriter(tempSensorFileName);
            StreamReader SensorSr = new StreamReader(sensorFilePath);
            StreamReader EMGSr = new StreamReader(tempEMGFilePath);

            Dispatcher.UIThread.Invoke(() =>
            {
                CombineUpsampleModel.RowComplete = 0;
                CombineUpsampleModel.RowAmount = File.ReadAllLines(tempEMGFilePath).Count(); // Reset row count for each file

                CombineUpsampleModel.RowCompleteRatio = CombineUpsampleModel.RowComplete + " / " + CombineUpsampleModel.RowAmount;
                CombineUpsampleModel.RowPercentage = ((float)CombineUpsampleModel.RowComplete / (float)CombineUpsampleModel.RowAmount) * 100f;
                CombineUpsampleModel.RowPercentageString = Math.Round(CombineUpsampleModel.RowPercentage, 2) + "%";
            });

            string allData = SensorSr.ReadToEnd();
            SensorSr.Close();

            string[] dataLineSplit = allData.Replace("\r", "").Split("\n"); // Replace line breaks with commas for easier processing
            string[][] dataSplit = new string[dataLineSplit.Length][]; // .txt files are seperated by tabs (\t)

            for (int i = 0; i < dataLineSplit.Length; i++)
            {
                for (int j = 0; j < dataLineSplit[i].Split("\t").Length; j++)
                {
                    dataSplit[i][j] = dataLineSplit[i].Split("\t")[j].Trim(); // Split each line by commas and trim whitespace
                }
            }
            // Get None time series headers from Sensor file
            string[] sensorHeaders = new string[0];
            string[] fullHeaders = dataLineSplit[0].Split("\t"); // Read headers from Sensor file
            foreach (string header in fullHeaders)
            {
                if (!header.Contains("Time"))
                {
                    string newHeader = header.Replace(":", "").Replace(" ", "_"); // Remove colons and replace spaces with underscores
                    newHeader = newHeader.Replace("\"", ""); // Remove quotation mark
                    sensorHeaders = sensorHeaders.Append(newHeader).ToArray();
                }
            }

            float lowestTime = float.MaxValue;

            if (float.TryParse(dataSplit[2][0], out float timeValue) && timeValue < lowestTime && timeValue != 0)
            {
                lowestTime = timeValue;
            }

            // Initialize temporary Sensor file with headers and first data line
            // Note: The first data line is assumed to be all zeroes, as per the original code logic
            string writeHeaders = "Time";
            string writeFirstDataLine = "0";
            foreach (string header in sensorHeaders)
            {
                writeHeaders += "," + header;
            }

            float currentTime = 0f;
            // Write Headers to Temporary Sensor File
            TempSensorSw.WriteLine(writeHeaders);

            EMGSr.ReadLine(); // Skip the first line of the EMG file (headers)

            bool readingFile = true;

            float emgTime = 0f;
            float nextSensorTime = 0f;

            string emgLine = "";

            int currentRow = 1; // Start from the third row since the first two rows are headers and first data line
            string outRow = "";


            try
            {
                do
                {
                    emgLine = EMGSr.ReadLine();
                    if (emgLine != "" && emgLine != null)
                    {
                        emgTime = float.Parse(emgLine.Split(",")[0]); // Get tme from EMG file
                    }
                    else
                        break; // If no time data, break the loop

                    if (dataSplit[currentRow][0] != "" && dataSplit[currentRow][0]!= null)
                    {
                        nextSensorTime = float.Parse(dataSplit[currentRow + 1][0]); // Get time from Sensor file
                    }
                    else
                        break; // If no time data, break the loop


                    if (nextSensorTime < emgTime)
                    {
                        currentRow++;
                    }

                    outRow = emgTime + ""; // Start the output row with the EMG time

                    for (int i = 0; i < sensorHeaders.Length; i++)
                    {
                        if (currentRow < dataSplit.GetLength(0) && dataSplit[currentRow][0] != "" && dataSplit[currentRow][0] != null)
                        {
                            if (float.TryParse(dataSplit[currentRow][0], out float sensorTime))
                            {
                                outRow += "," + dataSplit[currentRow][i + 1]; // Append the sensor data
                            }
                            else
                            {
                                outRow += ",0"; // If no data, append zero
                            }
                        }
                    }

                    if (currentRow >= dataSplit.GetLength(0) || dataSplit[currentRow][0] == "" || dataSplit[currentRow][0] == null)
                    {
                        readingFile = false; // If any of the time data is not null, we are still reading the file
                    }

                    TempSensorSw.WriteLine(outRow); // Write the output row to the temporary sensor file

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
                Exception e = ex;
            }

            TempSensorSw.Close();
            EMGSr.Close();
        }

        public async Task CombineEMGandSensorFiles(string tempEMGFilePath, string[] tempSensorFilePaths, string outputFilePath)
        {
            StreamReader tempEmgSr = new StreamReader(tempEMGFilePath);

            StreamReader[] tempSensorSrs = new StreamReader[tempSensorFilePaths.Length];
            /*StreamReader tempSensor1Sr = new StreamReader(tempSensor1FilePath);
            StreamReader tempSensor2Sr = new StreamReader(tempSensor2FilePath);*/

            string[] splitOutputFilePath = outputFilePath.Split("\\");
            string[] outputDirectoryArray = splitOutputFilePath.Take(splitOutputFilePath.Length - 1).ToArray();
            string outputDirectory = "";

            foreach(string directory in outputDirectoryArray)
            {
                outputDirectory += directory + "\\";
            }

            Directory.CreateDirectory(outputDirectory);

            // Creating Main Output File
            StreamWriter outputSw = new StreamWriter(outputFilePath);

            Dispatcher.UIThread.Invoke(() =>
            {
                CombineUpsampleModel.RowComplete = 0;
                CombineUpsampleModel.RowAmount = File.ReadAllLines(tempEMGFilePath).Count(); // Reset row count for each file
            });

            string allEMGData = tempEmgSr.ReadToEnd();
            tempEmgSr.Close();

            string[] allSensorsData = new string[tempSensorFilePaths.Length];
            /*string allSensor1Data = tempSensor1Sr.ReadToEnd();
            string allSensor2Data = tempSensor2Sr.ReadToEnd();*/

            for (int i = 0; i < tempSensorFilePaths.Length; i++)
            {
                tempSensorSrs[i] = new StreamReader(tempSensorFilePaths[i]);
                allSensorsData[i] = tempSensorSrs[i].ReadToEnd();
                tempSensorSrs[i].Close();
            }

            string[] emgDataLineSplit = allEMGData.Replace("\r", "").Split("\n"); // Replace line breaks with commas for easier processing


            string[][] sensorsDataLineSplit = new string[tempSensorFilePaths.Length][];

            for (int i = 0; i < tempSensorFilePaths.Length; i++)
            {
                sensorsDataLineSplit[i] = allSensorsData[i].Replace("\r", "").Split("\n"); // Replace line breaks with commas for easier processing
            }

            /*string[] sensor1DataLineSplit = allSensor1Data.Replace("\r", "").Split("\n"); // Replace line breaks with commas for easier processing
            string[] sensor2DataLineSplit = allSensor2Data.Replace("\r", "").Split("\n"); // Replace line breaks with commas for easier processing*/


            string[][] emgDataSplit = new string[emgDataLineSplit.Length][]; // .txt files are seperated by commas
            /*string[,] sensor1DataSplit = new string[sensor1DataLineSplit.Length, sensor1DataLineSplit[0].Split(",").Length]; // .txt files are seperated by commas
            string[,] sensor2DataSplit = new string[sensor2DataLineSplit.Length, sensor2DataLineSplit[0].Split(",").Length]; // .txt files are seperated by commas*/


            for (int i = 0; i < emgDataLineSplit.Length; i++)
            {
                for (int j = 0; j < emgDataLineSplit[i].Split("\t").Length; j++)
                {
                    emgDataSplit[i][j] = emgDataLineSplit[i].Split("\t")[j].Trim(); // Split each line by commas and trim whitespace
                }
            }

            for (int i = 0; i < tempSensorFilePaths.Length; i++)
            {
                for (int j = 0; j < sensorsDataLineSplit[i].Length; j++)
                {
                    for (int k = 0; k < sensorsDataLineSplit[i][j].Split("\t").Length; k++)
                    {
                        sensorsDataLineSplit[i][j] = sensorsDataLineSplit[i][j].Trim(); // Trim whitespace
                    }
                }
            }

            /*for (int i = 0; i < sensor1DataLineSplit.Length; i++)
            {
                for (int j = 0; j < sensor1DataLineSplit[i].Split("\t").Length; j++)
                {
                    sensor1DataSplit[i, j] = sensor1DataLineSplit[i].Split("\t")[j].Trim(); // Split each line by commas and trim whitespace
                }
            }

            for (int i = 0; i < sensor2DataLineSplit.Length; i++)
            {
                for (int j = 0; j < sensor2DataLineSplit[i].Split("\t").Length; j++)
                {
                    sensor2DataSplit[i, j] = sensor2DataLineSplit[i].Split("\t")[j].Trim(); // Split each line by commas and trim whitespace
                }
            }*/


            string outputLine = "";
            string allHeaders = "";


            string[] emgHeaders = emgDataLineSplit[0].Split(",");
            string[][] sensorHeaders = new string[tempSensorFilePaths.Length][];
            for (int i = 0; i < tempSensorFilePaths.Length; i++)
            {
                sensorHeaders[i] = sensorsDataLineSplit[i][0].Split(","); // Get headers from each sensor file
            }

            /*string[] sensor1Headers = sensor1DataLineSplit[0].Split(",");
            string[] sensor2Headers = sensor2DataLineSplit[0].Split(",");*/

            // Combine headers from all three files
            for (int i = 0; i < emgHeaders.Length; i++)
            {
                if (emgHeaders[i] != "Time" && emgHeaders[i] != "")
                {
                    allHeaders += "\t" + emgHeaders[i]; // EMG headers
                }
            }
            for (int i = 0; i < sensorHeaders.Length; i++)
            {
                for (int j = 0; j < sensorHeaders[i].Length; j++)
                {
                    if (sensorHeaders[i][j] != "Time" && sensorHeaders[i][j] != "")
                    {
                        allHeaders += "\t" + "Sensor_" + (i + 1) + "_" + sensorHeaders[i][j]; // Sensor headers
                    }
                }
            }


            /* for (int i = 0; i < sensor1Headers.Length; i++)
             {
                 if (sensor1Headers[i] != "Time" && sensor1Headers[i] != "")
                 {
                     allHeaders += "\t" + "Sensor_1_" + sensor1Headers[i]; // Sensor 1 headers
                 }
             }
             for (int i = 0; i < sensor2Headers.Length; i++)
             {
                 if (sensor2Headers[i] != "Time" && sensor2Headers[i] != "")
                 {
                     allHeaders += "\t" + "Sensor_2_" + sensor2Headers[i]; // Sensor 2 headers
                 }
             }
 */
            string[] splitHeaders = allHeaders.Split("\n");
            int numHeaders = splitHeaders.Length;

            // Write headers to the output file

            // file01 row
            outputLine = "\tfile01\tfile01";
            foreach (string header in allHeaders.Split("\t"))
            {
                if (header != "")
                    outputLine += "\tfile01"; // Add tab before each header
            }
            outputSw.WriteLine(outputLine);

            // time Analogtime row
            outputLine = "\tTIME\tANALOGTIME";
            foreach (string header in allHeaders.Split("\t"))
            {
                if (header != "")
                    outputLine += "\t" + header; // Add tab before each header
            }
            outputSw.WriteLine(outputLine);

            // Frame_Numbers Analog row
            outputLine = "\tFRAME_NUMBERS\tFRAME_NUMBERS";
            foreach (string header in allHeaders.Split("\t"))
            {
                if (header != "")
                    outputLine += "\t" + "ANALOG"; // Add tab before each header
            }
            outputSw.WriteLine(outputLine);

            outputLine = "\tORIGINAL\tORIGINAL"; // New line after headers
            foreach (string header in allHeaders.Split("\t"))
            {
                if (header != "")
                    outputLine += "\t" + "ORIGINAL"; // Add tab before each header
            }
            outputSw.WriteLine(outputLine);

            // ITEM row
            outputLine = "ITEM\t0\t0"; // New line after headers
            foreach (string header in allHeaders.Split("\t"))
            {
                if (header != "")
                    outputLine += "\t" + "0"; // Add tab before each header
            }
            outputSw.WriteLine(outputLine);

            bool readFiles = true;
            int currentItem = 1; // Start from 1


            string[] emgDataLine;
            string[][] sensorDataLine = new string[tempSensorFilePaths.Length][];

            float time = 0f;
            try
            {
                // Read lines from all three files and combine them
                do
                {
                    emgDataLine = emgDataLineSplit[currentItem].Split(",");
                    sensorDataLine = new string [tempSensorFilePaths.Length][];

                    for (int i = 0; i < tempSensorFilePaths.Length; i++)
                    {
                        sensorDataLine[i] = sensorsDataLineSplit[i][currentItem].Split(",");

                        //sensorDataLine[i] = sensorDataLine[i].Concat(sensorsDataLineSplit[i][currentItem].Split(",")).ToArray();
                    }

                    /*sensorDataLine = sensorsDataLineSplit[currentItem].Split(",");

                    sensor1DataLine = sensor1DataLineSplit[currentItem].Split(",");
                    sensor2DataLine = sensor2DataLineSplit[currentItem].Split(",");*/

                    if (emgDataLine.First() != "")
                        time = float.Parse(emgDataLine.First());

                    outputLine = currentItem + "\t" + time  + "\t" + time;


                    // Add EMG data
                    if (emgDataLine[0] != "")
                    {
                        for (int i = 1; i < emgDataLine.Length; i++) // Start from 1 to skip the "Time" header
                        {
                            outputLine += "\t" + emgDataLine[i];
                        }
                    }
                    else
                        readFiles = false;


                    // Add Sensor data
                    for (int i = 0; i < tempSensorFilePaths.Length; i++)
                    {
                        if (sensorDataLine[i][0] != "")
                        {
                            for (int j = 1; j < sensorDataLine[i].Length; j++) // Start from 1 to skip the "Time" header
                            {
                                outputLine += "\t" + sensorDataLine[i][j];
                            }
                        }
                        else
                        {
                            readFiles = false;
                            break;
                        }
                    }

                    /*// Add Sensor 1 data
                    if (sensorDataLine[0] != "")
                    {
                        for (int i = 1; i < sensorDataLine.Length; i++) // Start from 1 to skip the "Time" header
                        {
                            outputLine += "\t" + sensorDataLine[i];
                        }
                    }
                    else
                        readFiles = false;


                    // Add Sensor 2 data
                    if (sensorDataLine[0] != "")
                    {
                        for (int i = 1; i < sensorDataLine.Length; i++) // Start from 1 to skip the "Time" header
                        {
                            outputLine += "\t" + sensorDataLine[i];
                        }
                    }
                    else
                        readFiles = false;*/

                    // Write the combined line to the output file
                    if (readFiles)
                        outputSw.WriteLine(outputLine);

                    currentItem++;

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        CombineUpsampleModel.RowComplete++;

                        CombineUpsampleModel.RowCompleteRatio = CombineUpsampleModel.RowComplete + " / " + CombineUpsampleModel.RowAmount;
                        CombineUpsampleModel.RowPercentage = ((float)CombineUpsampleModel.RowComplete / (float)CombineUpsampleModel.RowAmount) * 100f;
                        CombineUpsampleModel.RowPercentageString = Math.Round(CombineUpsampleModel.RowPercentage, 2) + "%";
                    });


                } while (readFiles);
            }
            catch(Exception e)
            {
                Exception exception = e;
            }


            outputSw.Flush();
            outputSw.Close();
        }
    }
}