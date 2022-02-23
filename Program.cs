using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Obrobka_DSC_Class
{
    class Program
    {
        static char separator = ';';
        static void Main(string[] args)
        {
            InfotionAboutFiles infotionAboutFiles = new InfotionAboutFiles();
            DecimalSeparatorToDot();



            Createfolder(InfotionAboutFiles.path);
            ShowTextFilesInMainFolder(InfotionAboutFiles.fileInfos);

            ConvertFiles(InfotionAboutFiles.fileInfos, InfotionAboutFiles.path);


            // Console.WriteLine("Hello World!");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("All files succesfully saved to C:" + InfotionAboutFiles.path);
            Console.WriteLine("Press any key to finish");
            Console.ReadKey();
            Console.ResetColor();
        }

        private static void ConvertFiles(FileInfo[] fileInfos, string path)
        {
            SupportingValue supportingValue = new SupportingValue(0, 0, 0, 0, 0, 0, 0, 0, false);
            BigFuckingListOfAllData big = new BigFuckingListOfAllData();

            foreach (var file in InfotionAboutFiles.fileInfos)
            {
                List<string> lines = FileContentToList(file);

                if (!lines.Contains("#INSTRUMENT:NETZSCH DSC 204F1 Phoenix"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n\nFile: [" + file.Name.ToString() + "] does not contain the expected data and will be skipped!\n\n");
                    Console.ResetColor();
                    supportingValue.badFile = true;
                }
                if (!supportingValue.badFile)
                {
                    List<string> fileLines = new List<string>();
                    Measurement measurement = new Measurement();

                    SplitLinesIntoSingleValues(lines, measurement, supportingValue);
                    CalculateIntegral(measurement, supportingValue);
                    // PrintTableReduced(measurement);
                    SaveFileWithCalculatedValues(measurement, file, path);
                    AddDataToBigFuckingData(big, measurement, file);
                }
                supportingValue.fileNumerator++;
            }
            SaveBigFuckingData(big, path);
        }

        private static void SaveBigFuckingData(BigFuckingListOfAllData big, string path)
        {
            StreamWriter streamWriter = File.CreateText(path + "\\sumary_.txt");
            foreach (var item in big.fileNames)
            {
                streamWriter.Write(item + "; ");
            }
            streamWriter.WriteLine("\n\r");
            foreach (var item in big.headers)
            {
                streamWriter.Write(item + "; ");
            }
            streamWriter.WriteLine("\n\r");


            
            for (int j = 0; j < big.allData[1].Count; j++)
            {
                for (int i = 0; i < big.allData.Count; i++)
                {
     
                    streamWriter.Write(big.allData[i][j] + "; ");


                }
                streamWriter.WriteLine();
            }
            



            streamWriter.Close();
        }

        private static void AddDataToBigFuckingData(BigFuckingListOfAllData big, Measurement measurement, FileInfo file)
        {

            string headers = "";
            // Console.WriteLine(measurement.timeOfMeasurement.Count);
            big.fileNames.Add(file.Name + ";" + ";");
            big.allData.Add(measurement.timeOfMeasurement);
            big.allData.Add(measurement.measuredValue);
            big.allData.Add(measurement.integralOfMeasuredValue);

            foreach (var item in measurement.headersOfTable)
            {
                headers += item + "; ";

            }
            big.headers.Add(headers);

        }

        private static void SaveFileWithCalculatedValues(Measurement measurement, FileInfo file, string path)
        {
            Console.WriteLine(path);
            CreateFolderIfNotExist();
            Console.WriteLine(path);
            StreamWriter streamWriter = File.CreateText(path + "\\obrobiony_" + file.Name);
            for (int i = 0; i < measurement.timeOfMeasurement.Count; i++)
            {
                if (measurement.timeOfMeasurement.Count == measurement.integralOfMeasuredValue.Count)
                {
                    streamWriter.WriteLine(measurement.timeOfMeasurement[i].ToString() + "; " + measurement.measuredValue[i].ToString()
                        + " ;" + measurement.integralOfMeasuredValue[i].ToString());
                }
                else
                {
                    streamWriter.WriteLine(measurement.timeOfMeasurement[i].ToString() + "; " + measurement.measuredValue[i].ToString()
                       + " ;" + "0");
                }
            }
            streamWriter.Close();

        }


        private static void CreateFolderIfNotExist()
        {
            if (!Directory.Exists(InfotionAboutFiles.path))
            {
                Directory.CreateDirectory(InfotionAboutFiles.path);
            }
            else
            {
                Console.WriteLine("Folder {0} already exists", InfotionAboutFiles.path);
            }
        }

        private static void CalculateIntegral(Measurement measurement, SupportingValue supportingValue)
        {
            supportingValue.integralBaseLine = CalculateIntegralBaseline(measurement.measuredValue);
            if (measurement.measuredValue.Count == measurement.timeOfMeasurement.Count)
            {
                measurement.headersOfTable.Add("Integral");
                float fValueToSend = 0;
                for (int i = 1; i < measurement.measuredValue.Count; i++)
                {
                    fValueToSend = ((measurement.measuredValue[i - 1] + measurement.measuredValue[i] - 2 * supportingValue.integralBaseLine)
                         / 2) * (measurement.timeOfMeasurement[i] - measurement.timeOfMeasurement[i - 1]);
                    if (fValueToSend >= 0)
                    {
                        measurement.integralOfMeasuredValue.Add(fValueToSend);

                    }
                    else
                    {
                        measurement.integralOfMeasuredValue.Add(0);

                    }
                }
                Console.WriteLine(measurement.integralOfMeasuredValue.Sum());
            }
            else
            {
                Console.WriteLine("Cannot calculate integral of vectors with different sizes!");
            }
             measurement.integralOfMeasuredValue.Add(0);
        }

        private static float CalculateIntegralBaseline(List<float> measuredValue)
        {
            for (int i = measuredValue.Count / 2; i < measuredValue.Count; i++)
            {
                if (measuredValue[i] <= measuredValue[i - 10] - 0.12)
                {
                    return measuredValue[i - 10];
                }
            }
            return -1;
        }

        private static Measurement SplitLinesIntoSingleValues(List<string> lines, Measurement measurement, SupportingValue supportingValue)
        {

            int dscIndex = -1;
            int timeIndex = -1;
            int dataBeginingIndex = 0;
            List<List<string>> plik = new List<List<string>>();
            List<string> temporaryValues;
            int i = 0;
            bool istimeInMinutes = false;
            foreach (var line in lines)
            {
                i++;
                temporaryValues = (line.Split(separator).ToList());
                float fValueToParseOn = 0.0f;

                if ((line.Contains("DSC") && line.Contains("Time")))
                {
                    if (line.Contains("Time"))
                    {
                        if (line.Contains("Time"))
                        {
                            timeIndex = temporaryValues.LastIndexOf("Time/min");
                            istimeInMinutes = true;
                            dataBeginingIndex = i;
                            Console.WriteLine("i = " + dataBeginingIndex);
                        }
                        measurement.headersOfTable.Add("Time [s]");
                        Console.WriteLine("time index = {0}", timeIndex);
                        Console.WriteLine(measurement.headersOfTable[0]);
                    }

                    if (line.Contains("DSC"))
                    {
                        dscIndex = temporaryValues.LastIndexOf("DSC/(mW/mg)");
                        measurement.headersOfTable.Add("DSC [mW/mg]");
                        Console.WriteLine("DSC index = {0}", dscIndex);
                        Console.WriteLine(measurement.headersOfTable[1]);
                    }
                }
                else
                {
                    if (i > dataBeginingIndex && dscIndex != -1 && timeIndex != -1 && temporaryValues.Count > 1)
                    {
                        if (istimeInMinutes)
                        {
                            if (float.TryParse(temporaryValues[timeIndex], out fValueToParseOn))
                            {
                                fValueToParseOn = fValueToParseOn * 60;
                                measurement.timeOfMeasurement.Add(fValueToParseOn);
                            }
                            else
                            {
                                Console.WriteLine("Błąd konwersji czasu w sekundach");
                            }
                        }
                        else
                        {
                            if (float.TryParse(temporaryValues[timeIndex], out fValueToParseOn))
                            {
                                measurement.timeOfMeasurement.Add(fValueToParseOn);
                                Console.WriteLine("czas już był w sekundach");
                            }
                        }
                        if (float.TryParse(temporaryValues[dscIndex], out fValueToParseOn))
                        {
                            measurement.measuredValue.Add(fValueToParseOn);
                        }
                        else
                        {
                            Console.WriteLine("Blad konwersji sygnalu dsc");
                        }
                    }
                }
            }
            return measurement;
        }


        private static void DecimalSeparatorToDot()
        {
            var newCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            newCulture.NumberFormat.NumberDecimalSeparator = ".";
            CultureInfo.CurrentCulture = newCulture;
        }

        private static void Createfolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

        }

        private static void ShowTextFilesInMainFolder(FileInfo[] fileInfos)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Znaleziono Pliki: ");
            foreach (var file in fileInfos)
            {
                Console.WriteLine(file.Name);
            }
            Console.ResetColor();
        }

        public static List<string> FileContentToList(FileInfo file)
        {
            List<string> lines;
            return lines = File.ReadAllLines(file.ToString()).ToList();
        }

        public static void PrintTableReduced(Measurement measurement)
        {
            if (measurement.timeOfMeasurement.Count == measurement.measuredValue.Count)
            {
                for (int i = 0; i < measurement.measuredValue.Count; i++)
                {
                    if (i % 1000 == 0) Console.WriteLine("{0:F2}, {1:F4}", measurement.timeOfMeasurement[i], measurement.measuredValue[i]);
                }
                Console.WriteLine("Integral of measured value {0:F4}", measurement.integralOfMeasuredValue.Sum());
            }

        }

    }
    class InfotionAboutFiles
    {
        public static DirectoryInfo directoryInfo;
        public static FileInfo[] fileInfos;

        public static string path;

        public InfotionAboutFiles()
        {
            directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            fileInfos = directoryInfo.GetFiles("*.txt");
            path = Directory.GetCurrentDirectory() + "\\" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString()
               + DateTime.Now.Day.ToString() + "_obrobione" + "_" + DateTime.Now.Hour.ToString() + "h" + DateTime.Now.Minute.ToString() + "m" + DateTime.Now.Second.ToString();
        }
    }


    struct SupportingValue
    {

        public SupportingValue(uint countError1, uint countError2, uint fileNumerator, float integralBaseLine,
            int indexOfTimeRow, int signalIndex, int secondNumerator, int signalIntegrationEndIndex, bool badFile)
        {

            this.countError1 = countError1;
            this.countError2 = countError2;
            this.fileNumerator = fileNumerator;
            this.badFile = badFile;
            this.integralBaseLine = integralBaseLine;
            this.indexOfTimeRow = indexOfTimeRow;
            this.signalIndex = signalIndex;
            this.secondNumerator = secondNumerator;
            this.signalIntegrationEndIndex = signalIntegrationEndIndex;
        }


        public float integralBaseLine;
        public int indexOfTimeRow;
        public int signalIndex;
        public int secondNumerator;
        public int signalIntegrationEndIndex;

        public uint countError1;
        public uint countError2;
        public uint fileNumerator;
        public bool badFile;
    }

    class TableOfValues
    {
        public List<string> headersOfTable { get; set; }
        public List<float> valuesToDisplay1 { get; set; }
        public List<float> valuesToDisplay2 { get; set; }
        public List<float> valuesToDisplay3 { get; set; }
        public List<float> valuesToDisplay4 { get; set; }
        public List<float> valuesToDisplay5 { get; set; }

        public TableOfValues()
        {
            headersOfTable = new List<string>();
            valuesToDisplay1 = new List<float>();
            valuesToDisplay2 = new List<float>();
            valuesToDisplay3 = new List<float>();
            valuesToDisplay4 = new List<float>();
            valuesToDisplay5 = new List<float>();
        }
    }
    class TangentialKineticCurves
    {
        public List<float> tangentialToMeasuredValue { get; set; }
        public List<float> tangentTointegal { get; set; }
        public List<float> tangentToSomeOtherValue { get; set; }

        public TangentialKineticCurves()
        {
            tangentialToMeasuredValue = new List<float>();
            tangentTointegal = new List<float>();
            tangentToSomeOtherValue = new List<float>();
        }


    }

    class Measurement
    {
        public List<string> headersOfTable { get; set; }
        public List<float> timeOfMeasurement { get; set; }
        public List<float> measuredValue { get; set; }
        public List<float> integralOfMeasuredValue { get; set; }
        public List<float> someOtherParams { get; set; }

        public Measurement()
        {
            headersOfTable = new List<string>();
            timeOfMeasurement = new List<float>();
            measuredValue = new List<float>();
            integralOfMeasuredValue = new List<float>();
            someOtherParams = new List<float>();
        }
    }
    class BigFuckingListOfAllData
    {
        public List<List<float>> allData;
        public List<string> headers;
        public List<string> fileNames;

        public BigFuckingListOfAllData()
        {
            allData = new List<List<float>>();
            headers = new List<string>();
            fileNames = new List<string>();
        }
    }


}
