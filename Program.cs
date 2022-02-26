using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;

namespace Obrobka_DSC_Class
{
    class Program
    {
        static char separator = ';';
        static void Main(string[] args)
        {
            InfotionAboutFiles infotionAboutFiles = new InfotionAboutFiles();
            DecimalSeparatorToDot();


            List<float> heatOfPolymerization = new List<float>();
            Createfolder(InfotionAboutFiles.path);
            ShowTextFilesInMainFolder(InfotionAboutFiles.fileInfos, heatOfPolymerization);

            ConvertFiles(InfotionAboutFiles.fileInfos, InfotionAboutFiles.path, heatOfPolymerization);


            // Console.WriteLine("Hello World!");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("All files succesfully saved to C:" + InfotionAboutFiles.path);
            Console.WriteLine("Press any key to finish");
            Console.ReadKey();
            Console.ResetColor();
        }

        private static void ConvertFiles(FileInfo[] fileInfos, string path, List<float> heatOfPolymerization)
        {
            SupportingValue supportingValue = new SupportingValue(0, 0, 0, 0, 0, 0, 0, 0, false, 0);
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
                    Console.Write('▒');
                    Measurement measurement = new Measurement();

                    SplitLinesIntoSingleValues(lines, measurement, supportingValue);
                    CalculateIntegral(measurement, supportingValue);
                    
                    CalculateIntegralSum(measurement);
                    
                    CalculateRpValue(measurement, heatOfPolymerization, supportingValue.fileNumerator);
                    CalculateConversion(measurement, heatOfPolymerization, supportingValue.fileNumerator);
                    // PrintTableReduced(measurement);
                    
                    SaveFileWithCalculatedValues(measurement, file, path);
                    
                    AddDataToBigFuckingData(big, measurement, file, heatOfPolymerization);
                    
                }
                supportingValue.fileNumerator++;
                Console.Write('▒');
            }
            Console.WriteLine();
            
           
            SaveBigFuckingData(big, path);
            //Console.WriteLine("SAving bfg - end");

            //Console.WriteLine("SAving excl file");
            GenerateExcelFile(big, path, supportingValue);
            //Console.WriteLine("SAving excl file - end");

        }



        private static void GenerateExcelFile(BigFuckingListOfAllData big, string path, SupportingValue supportingValue)
        {
            
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            ExcelPackage excel = new ExcelPackage();
            var workSheet = excel.Workbook.Worksheets.Add("Sheet1");

            // var chartSheet2 = excel.Workbook.Worksheets.Add("Chart_Integral");
            int numberOfDataSeries = (int)Math.Ceiling((double)big.allData.Count / (int)supportingValue.fileNumerator);
            workSheet.TabColor = System.Drawing.Color.Black;
            List<string> splited;

            for (int i = 0; i < big.fileNames.Count; i++)
            {
                splited = big.fileNames[i].Split(separator).ToList();
                workSheet.Cells[1, (i * numberOfDataSeries) + 1].Value = splited[0];
            }

            for (int i = 0; i < big.headers.Count; i++)
            {
                splited = big.headers[i].Split(separator).ToList();
                splited = splited.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                for (int j = 0; j < splited.Count; j++)
                {
                    workSheet.Cells[2, i * numberOfDataSeries + j + 1].Value = splited[j];
                }
            }


            for (int j = 0; j < big.allData[1].Count; j++)
            {
                for (int i = 0; i < big.allData.Count; i++)
                {
                    workSheet.Cells[j + 3, i + 1].Value = big.allData[i][j];
                }
            }
            
            CreateExcelChart(excel, workSheet, numberOfDataSeries, big);
            

            string p_strPath = path + "\\obrobione_excel.xlsx";

            if (File.Exists(p_strPath))
                File.Delete(p_strPath);

            // Create excel file on physical disk 
            FileStream objFileStrm = File.Create(p_strPath);
            objFileStrm.Close();

            // Write content to excel file 
            File.WriteAllBytes(p_strPath, excel.GetAsByteArray());
            //Close Excel package
            excel.Dispose();
           
        }

        private static void CreateExcelChart(ExcelPackage excel, ExcelWorksheet workSheet, int numberOfDataSeries, BigFuckingListOfAllData big)
        {

            var chartSheet1 = excel.Workbook.Worksheets.Add("Chart_DSC");
            var chartSheet2 = excel.Workbook.Worksheets.Add("Chart_Conversion");
            var chartSheet3 = excel.Workbook.Worksheets.Add("Chart_Rp");
            var chartSheet4 = excel.Workbook.Worksheets.Add("Chart_Integral_raw");

            var myChart1 = chartSheet1.Drawings.AddChart("DSC", eChartType.XYScatter);
            myChart1.SetSize(1000, 1000);
            myChart1.XAxis.Format = "# ##0";
            myChart1.PlotArea.Border.Width = 5;
            myChart1.XAxis.Title.Text = "Time [s]";
            myChart1.YAxis.Title.Text = "DSC [mW/mg]";
            myChart1.XAxis.MinValue = 0;
            myChart1.YAxis.MinValue = 0;
            myChart1.XAxis.RemoveGridlines();
            myChart1.YAxis.RemoveGridlines();


            for (int column = 0; column < big.allData.Count / numberOfDataSeries; column++)
            {
                string adress1 = GetStandardExcelColumnName(1+ numberOfDataSeries*column);
                string adress2 = GetStandardExcelColumnName(1 + numberOfDataSeries * column + 1);
                string dataSeries1 = "Sheet1!" + adress1 + "3:" + adress1 + big.allData[0].Count;
                string dataSeries2 = "Sheet1!" + adress2 + "3:" + adress2 + big.allData[0].Count;
                var series = myChart1.Series.Add(dataSeries2, dataSeries1);
                series.HeaderAddress = new ExcelAddress("Sheet1!" + adress1 + 1);
            }

            var myChart2 = chartSheet2.Drawings.AddChart("Conversion", eChartType.XYScatter);
            myChart2.SetSize(1000, 1000);
            myChart2.XAxis.Format = "# ##0";
            myChart2.PlotArea.Border.Width = 5;
            myChart2.XAxis.Title.Text = "Time [s]";
            myChart2.YAxis.Title.Text = "Conversion [%]";
            myChart2.XAxis.RemoveGridlines();
            myChart2.YAxis.RemoveGridlines();
            myChart2.XAxis.MinValue = 0;
            myChart2.YAxis.MinValue = 0;

            for (int column = 0; column < big.allData.Count / numberOfDataSeries; column++)
            {
                string adress1 = GetStandardExcelColumnName(1 + numberOfDataSeries * column);
                string adress2 = GetStandardExcelColumnName(1 + numberOfDataSeries * column + 5);
                string dataSeries1 = "Sheet1!" + adress1 + "3:" + adress1 + big.allData[0].Count;
                string dataSeries2 = "Sheet1!" + adress2 + "3:" + adress2 + big.allData[0].Count;
                var series = myChart2.Series.Add(dataSeries2, dataSeries1);
                series.HeaderAddress = new ExcelAddress("Sheet1!" + adress1 + 1);
            }

            var myChart3 = chartSheet3.Drawings.AddChart("Rp", eChartType.XYScatter);
            myChart3.SetSize(1000, 1000);
            myChart3.XAxis.Format = "# ##0";
            myChart3.PlotArea.Border.Width = 5;
            myChart3.XAxis.Title.Text = "Time [s]";
            myChart3.YAxis.Title.Text = "Rp [mol * dm-3 * s-1]";
            myChart3.XAxis.MinValue = 0;
            myChart3.YAxis.MinValue = 0;
            myChart3.XAxis.RemoveGridlines();
            myChart3.YAxis.RemoveGridlines();


            for (int column = 0; column < big.allData.Count / numberOfDataSeries; column++)
            {
                string adress1 = GetStandardExcelColumnName(1 + numberOfDataSeries * column);
                string adress2 = GetStandardExcelColumnName(1 + numberOfDataSeries * column + 4);
                string dataSeries1 = "Sheet1!" + adress1 + "3:" + adress1 + big.allData[0].Count;
                string dataSeries2 = "Sheet1!" + adress2 + "3:" + adress2 + big.allData[0].Count;
                var series = myChart3.Series.Add(dataSeries2, dataSeries1);
                series.HeaderAddress = new ExcelAddress("Sheet1!" + adress1 + 1);
            }

            var myChart4 = chartSheet4.Drawings.AddChart("Integral Raw", eChartType.XYScatter);
            myChart4.SetSize(1000, 1000);
            myChart4.XAxis.Format = "# ##0";
            myChart4.PlotArea.Border.Width = 5;
            myChart4.XAxis.Title.Text = "Time [s]";
            myChart4.YAxis.Title.Text = "DSC [mW/mg]";
            myChart4.XAxis.RemoveGridlines();
            myChart4.YAxis.RemoveGridlines();
            myChart4.XAxis.MinValue = 0;
            myChart4.YAxis.MinValue = 0;

            for (int column = 0; column < big.allData.Count / numberOfDataSeries; column++)
            {
                string adress1 = GetStandardExcelColumnName(1 + numberOfDataSeries * column);
                string adress2 = GetStandardExcelColumnName(1 + numberOfDataSeries * column + 5);
                string dataSeries1 = "Sheet1!" + adress1 + "3:" + adress1 + big.allData[0].Count;
                string dataSeries2 = "Sheet1!" + adress2 + "3:" + adress2 + big.allData[0].Count;
                var series = myChart4.Series.Add(dataSeries2, dataSeries1);
                series.HeaderAddress = new ExcelAddress("Sheet1!" + adress1 + 1);
           
            }
        }

        public static string GetStandardExcelColumnName(int columnNumberOneBased)
        {
            int baseValue = Convert.ToInt32('A');
            int columnNumberZeroBased = columnNumberOneBased - 1;

            string ret = "";

            if (columnNumberOneBased > 26)
            {
                ret = GetStandardExcelColumnName(columnNumberZeroBased / 26);
            }
           // Console.WriteLine("tekst = " + (ret + Convert.ToChar(baseValue + (columnNumberZeroBased % 26))) );
            return ret + Convert.ToChar(baseValue + (columnNumberZeroBased % 26));
        }

        private static void SaveBigFuckingData(BigFuckingListOfAllData big, string path)
        {
            AlignListLength(big);
            StreamWriter streamWriter = File.CreateText(path + "\\sumary_.txt");
            foreach (var item in big.fileNames)
            {
                streamWriter.Write(item);
            }
            streamWriter.WriteLine();
            foreach (var item in big.headers)
            {
                streamWriter.Write(item);
            }
            streamWriter.WriteLine();
            int i = 0;

            for (int j = 0; j < big.allData[i].Count; j++)
            {
                for (i = 0; i < big.allData.Count - 1; i++)
                {
                    streamWriter.Write(big.allData[i][j] + ";");
                }
                streamWriter.WriteLine();
            }
            streamWriter.Close();
        }

        private static void AddDataToBigFuckingData(BigFuckingListOfAllData big, Measurement measurement, FileInfo file, List<float> heatOfPolymerization)
        {

            string headers = "";
            big.fileNames.Add(file.Name + ";" + ";");
            big.allData.Add(measurement.timeOfMeasurement);
            big.allData.Add(measurement.measuredValue);
            big.allData.Add(measurement.integralOfMeasuredValue);
            big.allData.Add(measurement.integralSum);
            big.allData.Add(measurement.RpValues);
            big.allData.Add(measurement.conversion);

            foreach (var item in measurement.headersOfTable)
            {
                headers += item + ";";
            }
            headers += "Integral_sum;Rp[mol dm -3 s-1];Conversion [%];";

            big.headers.Add(headers);
        }

        private static void CalculateConversion(Measurement measurement, List<float> heatOfPolymerization, uint fileNumerator)
        {
            float integral = measurement.integralOfMeasuredValue.Sum();
            float totalConversion = integral / heatOfPolymerization[(int)fileNumerator];

            for (int i = 0; i < measurement.integralOfMeasuredValue.Count; i++)
            {
                measurement.conversion.Add(totalConversion * measurement.integralSum[i]);

            }
        }

        private static void CalculateRpValue(Measurement measurement, List<float> heatOfPolymerization, uint j)
        {
            float baseline = CalculateIntegralBaseline(measurement.measuredValue);
            for (int i = 0; i < measurement.measuredValue.Count; i++)
            {
                if (baseline >= 0)
                {
                    if (measurement.measuredValue[i] - baseline >= 0)
                    {
                        measurement.RpValues.Add(1050 * (measurement.measuredValue[i] - baseline) / heatOfPolymerization[(int)j]);
                    }
                    else
                        measurement.RpValues.Add(0);
                }
            }
        }

        private static void CalculateIntegralSum(Measurement measurement)
        {
            float totalSum = measurement.integralOfMeasuredValue.Sum();
            for (int i = 0; i < measurement.integralOfMeasuredValue.Count; i++)
            { 
                measurement.integralSum.Add(100 * (measurement.integralOfMeasuredValue.Take(i).Sum() / totalSum));
            }
        }


        private static void AlignListLength(BigFuckingListOfAllData big)
        {
            big.maxListLength = 0;
            foreach (var item in big.allData)
            {
                if (item.Count > big.maxListLength)
                {
                    big.maxListLength = item.Count;
                }
            }

            foreach (var item in big.allData)
            {
                if (item.Count < big.maxListLength)
                    for (int i = item.Count; i < big.maxListLength; i++)
                    {
                        item.Add(0);
                    }
            }

        }

        private static void SaveFileWithCalculatedValues(Measurement measurement, FileInfo file, string path)
        {
            CreateFolderIfNotExist();
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
                //Console.WriteLine(measurement.integralOfMeasuredValue.Sum());
            }
            else
            {
                Console.WriteLine("Cannot calculate integral of vectors with different sizes!");
            }
            measurement.integralOfMeasuredValue.Add(0);
            // measurement.integralOfMeasuredValue.Add(0);
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
                if (temporaryValues.Count > supportingValue.longestList)
                {
                    supportingValue.longestList = temporaryValues.Count;
                }

                if ((line.Contains("DSC") && line.Contains("Time")))
                {
                    if (line.Contains("Time"))
                    {
                        if (line.Contains("Time"))
                        {
                            timeIndex = temporaryValues.LastIndexOf("Time/min");
                            istimeInMinutes = true;
                            dataBeginingIndex = i;
                            //  Console.WriteLine("i = " + dataBeginingIndex);
                        }
                        measurement.headersOfTable.Add("Time [s]");
                        //Console.WriteLine("time index = {0}", timeIndex);
                        //Console.WriteLine(measurement.headersOfTable[0]);
                    }

                    if (line.Contains("DSC"))
                    {
                        dscIndex = temporaryValues.LastIndexOf("DSC/(mW/mg)");
                        measurement.headersOfTable.Add("DSC [mW/mg]");
                        //  Console.WriteLine("DSC index = {0}", dscIndex);
                        //  Console.WriteLine(measurement.headersOfTable[1]);
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

        private static void ShowTextFilesInMainFolder(FileInfo[] fileInfos, List<float> heat)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Znaleziono Pliki: ");
            foreach (var file in fileInfos)
            {
                Console.WriteLine(file.Name);
                Console.Write("Enter heat of polymerization in J/g eg. 500 = ");
                heat.Add(float.Parse(Console.ReadLine()));
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
            int indexOfTimeRow, int signalIndex, int secondNumerator, int signalIntegrationEndIndex, bool badFile, int longestList)
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
            this.longestList = longestList;

        }


        public float integralBaseLine;
        public int indexOfTimeRow;
        public int signalIndex;
        public int secondNumerator;
        public int signalIntegrationEndIndex;
        public int longestList;

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
        public List<float> conversion { get; set; }
        public List<float> integralSum { get; set; }
        public List<float> RpValues { get; set; }


        public Measurement()
        {
            headersOfTable = new List<string>();
            timeOfMeasurement = new List<float>();
            measuredValue = new List<float>();
            integralOfMeasuredValue = new List<float>();
            conversion = new List<float>();
            integralSum = new List<float>();
            RpValues = new List<float>();
        }
    }
    class BigFuckingListOfAllData
    {
        public List<List<float>> allData;
        public List<string> headers;
        public List<string> fileNames;
        public int maxListLength;
        public BigFuckingListOfAllData()
        {
            allData = new List<List<float>>();
            headers = new List<string>();
            fileNames = new List<string>();
        }
    }


}
