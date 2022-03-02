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
            Console.WindowWidth = 150;
              DisplayInfo();
           // Console.WriteLine("Current WindowWidth: {0}",
           //                     Console.WindowWidth);
        loopX:
            Console.WriteLine("Paste directory here:");
            string pathFromUser = Console.ReadLine();
            try
            {
                InfotionAboutFiles infotionAboutFiles = new InfotionAboutFiles(pathFromUser);
                Console.Clear();
            }
            catch (DirectoryNotFoundException e)
            {

                Console.WriteLine("Path not correct");
                goto loopX;
            }

            Console.WriteLine(InfotionAboutFiles.path);
            DecimalSeparatorToDot();
            PrintTableOfHeats();
            List<float> heatOfPolymerization = new List<float>();
            List<float> heatOfFunctionalGroup = new List<float>();
            Createfolder(InfotionAboutFiles.path);

            if (InfotionAboutFiles.fileInfos.Length != 0)
            {
                ShowTextFilesInMainFolder(InfotionAboutFiles.fileInfos, heatOfPolymerization, heatOfFunctionalGroup);
                ConvertFiles(InfotionAboutFiles.fileInfos, InfotionAboutFiles.path, heatOfPolymerization, heatOfFunctionalGroup);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All files succesfully saved to: " + InfotionAboutFiles.path);
                Console.WriteLine("Press any key to finish");
                Console.ResetColor();
                Console.ReadKey();
            }

            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Chosen folder does not contain *.txt files!");
                Console.ResetColor();
            }

            Console.ReadKey();

        }

        private static void PrintTableOfHeats()
        {
            Console.WriteLine("Monomer          Polymerization heat J/g   PolymHeat 1f group [J/mol] ");
            Console.WriteLine("CADE             768,9                     96700");
            Console.WriteLine("HDDGE            877,1                     101000");
            Console.WriteLine("BDDGE            998,5                     101000");
            Console.WriteLine("TMPTGE           1002,1                    101000");
            Console.WriteLine("BPADGE           593,4                     101000");
            Console.WriteLine("RDGE             908,9                     101000");
            Console.WriteLine("TMTGE            658,0                     101000");
            Console.WriteLine("OXT101           722,9                     84000");
            Console.WriteLine("OXT121           477,0                     84000");
            Console.WriteLine("OXT221           783,9                     84000");
            Console.WriteLine("S140             407,2                     84000");
            Console.WriteLine("S160             409,2                     84000");
            Console.WriteLine("For mixtures, use proportional values");

        }

        private static void ConvertFiles(FileInfo[] fileInfos, string path, List<float> heatOfPolymerization, List<float> heatOfFunctionalGroup)
        {
            SupportingValue supportingValue = new SupportingValue(0, 0, 0, 0, 0, 0, 0, 0, false, 0, 0);
            BigFuckingListOfAllData big = new BigFuckingListOfAllData();
            supportingValue.longestFile = DetectLongestFile(fileInfos, supportingValue);

            foreach (var file in InfotionAboutFiles.fileInfos)
            {
                List<string> lines = FileContentToList(file);
                supportingValue.badFile = false;
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
                    if (measurement.measuredValue.Count != 0)
                    {
                        CalculateIntegral(measurement, supportingValue);
                        CalculateIntegralSum(measurement);
                        CalculateRpValue(measurement, heatOfFunctionalGroup, supportingValue.fileNumerator);
                        CalculateConversion(measurement, heatOfPolymerization, supportingValue.fileNumerator);
                        SaveFileWithCalculatedValues(measurement, file, path);
                        AddDataToBigFuckingData(big, measurement, file, heatOfPolymerization);
                        supportingValue.fileNumerator++;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("File {0} doesnt countain Time or DSC data!", file.Name);
                        Console.ResetColor();
                    }
                }
            }
            Console.WriteLine();

            if (big.allData.Count != 0)
            {
                SaveBigFuckingData(big, path);
                GenerateExcelFile(big, path, supportingValue);
            }

            if (supportingValue.fileNumerator == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No proper files found!");
                Console.ResetColor();
            }
        }

        private static int DetectLongestFile(FileInfo[] fileInfos, SupportingValue supportingValue)
        {
            foreach (var file in InfotionAboutFiles.fileInfos)
            {
                if (supportingValue.longestFile < FileContentToList(file).Count)
                {
                    supportingValue.longestFile = FileContentToList(file).Count;
                }
            }
            return supportingValue.longestFile + 100;
        }

        private static void GenerateExcelFile(BigFuckingListOfAllData big, string path, SupportingValue supportingValue)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            ExcelPackage excel = new ExcelPackage();
            var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
            var summaryTable = excel.Workbook.Worksheets.Add("Summary Table");
            int numberOfDataSeries = (int)Math.Ceiling((double)big.allData.Count / (int)supportingValue.fileNumerator);
            workSheet.TabColor = System.Drawing.Color.Black;
            List<string> splited;

            for (int i = 0; i < big.fileNames.Count; i++)
            {
                big.fileNames[i] = big.fileNames[i].Replace("ExpDat_", "");
                big.fileNames[i] = big.fileNames[i].Replace(".txt", "");
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

            for (int i = 0; i < supportingValue.fileNumerator; i++)
            {
                summaryTable.Cells[i + 2, 1].Value = big.fileNames[i].Replace(";", " ");
            }

            summaryTable.Cells[1, 2].Value = "max DSC [mW/mg]";
            summaryTable.Cells[1, 3].Value = "Time [s]";
            summaryTable.Cells[1, 4].Value = "Max Rp [mol dm-3 s-1]";
            summaryTable.Cells[1, 5].Value = "Max Rp time [s]";
            summaryTable.Cells[1, 6].Value = "Max Conversion [%]";
            summaryTable.Cells[1, 7].Value = "T ind [s]";
            summaryTable.Cells[1, 8].Value = "directional coefficient DSC";
            summaryTable.Cells[1, 9].Value = "directional coefficient Rp";
            summaryTable.Cells[1, 10].Value = "directional coefficient Conversion";

            for (int i = 0; i < (int)supportingValue.fileNumerator; i++)
            {
                summaryTable.Cells[i + 2, 2].Value = big.maxDSC[i];
                summaryTable.Cells[i + 2, 3].Value = big.maxDSCTime[i];
                summaryTable.Cells[i + 2, 4].Value = big.maxRp[i];
                summaryTable.Cells[i + 2, 5].Value = big.maxRpTime[i];
                summaryTable.Cells[i + 2, 6].Value = big.maxConversion[i];
                summaryTable.Cells[i + 2, 7].Value = big.inductionTime[i];
                summaryTable.Cells[i + 2, 8].Value = big.tangentialDSC[i];
                summaryTable.Cells[i + 2, 9].Value = big.tangentialRp[i];
                summaryTable.Cells[i + 2, 10].Value = big.tangentialConversion[i];
            }
            summaryTable.Cells["A1:ZZZ30"].AutoFitColumns();

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
                string adress1 = GetStandardExcelColumnName(1 + numberOfDataSeries * column);
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
            myChart4.YAxis.Title.Text = "Integral [%]";
            myChart4.XAxis.RemoveGridlines();
            myChart4.YAxis.RemoveGridlines();
            myChart4.XAxis.MinValue = 0;
            myChart4.YAxis.MinValue = 0;

            for (int column = 0; column < big.allData.Count / numberOfDataSeries; column++)
            {
                string adress1 = GetStandardExcelColumnName(1 + numberOfDataSeries * column);
                string adress2 = GetStandardExcelColumnName(1 + numberOfDataSeries * column + 3);
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

            for (int j = 0; j < (big.allData[i].Count == 0 ? -1 : big.allData[i].Count); j++)
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

            AddMaxValToBigLists(measurement, big);
            AddTangentialToBigList(measurement, big);
        }



        private static void AddTangentialToBigList(Measurement measurement, BigFuckingListOfAllData big)
        {
            float timeLightOn = CalculateLightOnTime(measurement.measuredValue, measurement.timeOfMeasurement);
            float baseline = CalculateIntegralBaseline(measurement.measuredValue);
            List<int> firstIndex = new List<int>();
            for (int i = 0; i < measurement.measuredValue.Count; i++)
            {
                if (measurement.measuredValue[i] >= baseline)
                {
                    firstIndex.Add(i);
                    break;
                }
            }
            for (int i = 0; i < firstIndex.Count; i++)
            {
                big.inductionTime.Add(measurement.timeOfMeasurement[firstIndex[i]] - timeLightOn);
                big.tangentialConversion.Add(((measurement.conversion[firstIndex[i] + 10] - measurement.conversion[firstIndex[i]]) /
                    (measurement.timeOfMeasurement[firstIndex[i] + 10] - measurement.timeOfMeasurement[firstIndex[i]])).ToString());
                big.tangentialDSC.Add(((measurement.measuredValue[firstIndex[i] + 10] - measurement.measuredValue[firstIndex[i]]) /
                    (measurement.timeOfMeasurement[firstIndex[i] + 10] - measurement.timeOfMeasurement[firstIndex[i]])).ToString());
                big.tangentialRp.Add(((measurement.RpValues[firstIndex[i] + 10] - measurement.RpValues[firstIndex[i]]) /
                    (measurement.timeOfMeasurement[firstIndex[i] + 10] - measurement.timeOfMeasurement[firstIndex[i]])).ToString());
            }
        }

        private static float CalculateLightOnTime(List<float> measuredValue, List<float> time)
        {

            for (int i = 0; i < measuredValue.Count; i++)
            {
                if (measuredValue[i] < (measuredValue[i + 10] - 0.05))
                {
                    return time[i];
                }
            }
            return -1;
        }

        private static void AddMaxValToBigLists(Measurement measurement, BigFuckingListOfAllData big)
        {
            big.maxDSC.Add(measurement.measuredValue.Max());
            big.maxDSCTime.Add(measurement.timeOfMeasurement[measurement.measuredValue.IndexOf(measurement.measuredValue.Max())]);
            big.maxConversion.Add(measurement.conversion.Max());
            big.maxRp.Add(measurement.RpValues.Max());
            big.maxRpTime.Add(measurement.timeOfMeasurement[measurement.RpValues.IndexOf(measurement.RpValues.Max())]);
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
            }
            else
            {
                Console.WriteLine("Cannot calculate integral of vectors with different sizes!");
            }
            measurement.integralOfMeasuredValue.Add(0);
        }

        private static float CalculateIntegralBaseline(List<float> measuredValue)
        {
            for (int i = 1000; i < measuredValue.Count; i++)
            {
                if ((measuredValue[i] <= measuredValue[i - 10] - 0.12) ||
                    (measuredValue[i] < measuredValue[i + 50]) && measuredValue[i] < measuredValue[i - 50])
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
            List<string> temporaryValues;
            int i = 0;
            bool istimeInMinutes = false;
            bool isFileOK = false;
            foreach (var line in lines)
            {
                i++;
                temporaryValues = (line.Split(separator).ToList());
                if (line.Contains("Time") && line.Contains("DSC"))
                {
                    isFileOK = true;
                }

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
            if (isFileOK)
            {
                if (measurement.timeOfMeasurement.Count < supportingValue.longestFile)
                {
                    for (int x = 0; x < (supportingValue.longestFile - measurement.timeOfMeasurement.Count); x++)
                    {
                        measurement.timeOfMeasurement.Add((float)(measurement.timeOfMeasurement[measurement.timeOfMeasurement.Count - 1] + 0.01));
                        measurement.measuredValue.Add(0);
                    }
                }
            }
            else
            {
                measurement.timeOfMeasurement.Clear();
                measurement.measuredValue.Clear();
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

        private static void ShowTextFilesInMainFolder(FileInfo[] fileInfos, List<float> heat, List<float> heatOfFunctionalGroup)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Detected files: ");
            Console.ResetColor();
            foreach (var file in fileInfos)
            {
                bool badfile = false;
                List<string> lines = FileContentToList(file);
                if (file.Length < 10000)
                {
                    badfile = true;
                }

                if (!badfile)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(file.Name);
                    Console.ResetColor();
                loop1:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("Enter heat of polymerization in J/g eg. 500 = ");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Green;
                    string s1 = Console.ReadLine();
                    Console.ResetColor();
                    Console.ResetColor();
                    s1 = s1.Replace(',', '.');

                    float heat1ToParse;
                    if (float.TryParse(s1, out heat1ToParse))
                    {
                        heat.Add(heat1ToParse);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("invalid value");
                        Console.ResetColor();
                        goto loop1;

                    }
                loop2:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("Enter heat of polymerization of 1 functional group eg epox 96700 = ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    s1 = Console.ReadLine();
                    Console.ResetColor();
                    s1 = s1.Replace(',', '.');
                    float heat2ToParse;
                    if (float.TryParse(s1, out heat2ToParse))
                    {
                        heatOfFunctionalGroup.Add(heat2ToParse);
                        // Console.WriteLine("heat 2 to parse" + heat2ToParse);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("invalid value");
                        Console.ResetColor();
                        goto loop2;
                    }
                }


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

        public static void DisplayInfo()
        {
        Console.WriteLine(@" ______   _______  _______       _______  _______  _______             _______     ______     ______              _______  _______ ");
        Console.WriteLine(@"(  __  \ (  ____ \(  ____ \     (  ___  )(  ____ )(  ____ )  |\     /|/ ___   )   / ___  \   (  ___ \ |\     /|  (  ____ )(  ____ \ ");
        Console.WriteLine(@"| (  \  )| (    \/| (    \/     | (   ) || (    )|| (    )|  | )   ( |\/   )  |   \/   \  \  | (   ) )( \   / )  | (    )|| (    \/");
        Console.WriteLine(@"| |   ) || (_____ | |           | (___) || (____)|| (____)|  | |   | |    /   )      ___) /  | (__/ /  \ (_) /   | (____)|| (__    ");
        Console.WriteLine(@"| |   | |(_____  )| |           |  ___  ||  _____)|  _____)  ( (   ) )  _/   /      (___ (   |  __ (    \   /    |  _____)|  __)   ");
        Console.WriteLine(@"| |   ) |      ) || |           | (   ) || (      | (         \ \_/ /  /   _/           ) \  | (  \ \    ) (     | (      | (      ");
        Console.WriteLine(@"| (__/  )/\____) || (____/\     | )   ( || )      | )          \   /  (   (__/\ _ /\___/  /  | )___) )   | |     | )      | )      ");
        Console.WriteLine(@"(______/ \_______)(_______/_____|/     \||/       |/            \_/   \_______/(_)\______/   |/ \___/    \_/     |/       |/       ");
            Console.WriteLine("\n");
            List<string> a = new List<string>()
            {"/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////",
            "/////////////////////////   Application designed for conversion of  multiple *.txt data files gathered  /////////////////////////",
            "/////////////////////////////////  from photo-DSC measurement using Netszch Phoenix F1 204  /////////////////////////////////////",
            "/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////",
            "///////////////////////////// File must be at least 1001 lines long and contain over 10k charcters /////////////////////////////",
            "////////////////////////////////////////// must also contain DSC and Time data /////////////////////////////////////////////////",
            "//////////////////////////////////////////////// Decimal separator -> '.' ///////////////////////////////////////////////////////",
            "///////////////////////////////////////////////// Column separator is ';' ///////////////////////////////////////////////////////",
            "/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////",
            "//////////////////////////// Not For commercial use! Developed on EPPlus 5.0 free licence framework /////////////////////////////",};
            //////////
            foreach (var item in a)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine("\n");

    }

}



class InfotionAboutFiles
{
    public static DirectoryInfo directoryInfo;
    public static FileInfo[] fileInfos;
    public static string pathMain;
    public static string path;

    public InfotionAboutFiles(string path1)
    {
        directoryInfo = new DirectoryInfo(path1);
        fileInfos = directoryInfo.GetFiles("*.txt");
        pathMain = path1;
        path = path1 /*Directory.GetCurrentDirectory()*/ + "\\" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString()
           + DateTime.Now.Day.ToString() + "_obrobione" + "_" + DateTime.Now.Hour.ToString() + "h" + DateTime.Now.Minute.ToString() + "m" + DateTime.Now.Second.ToString();
    }

}


struct SupportingValue
{

    public SupportingValue(uint countError1, uint countError2, uint fileNumerator, float integralBaseLine,
        int indexOfTimeRow, int signalIndex, int secondNumerator, int signalIntegrationEndIndex, bool badFile, int longestList, int longestFile)
    {
        this.longestFile = longestFile;
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

    public int longestFile;
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
    public List<float> maxDSC { get; set; }
    public List<float> maxDSCTime { get; set; }
    public List<float> maxRp { get; set; }
    public List<float> maxRpTime { get; set; }
    public List<float> maxConversion { get; set; }
    public List<string> tangentialDSC { get; set; }
    public List<string> tangentialRp { get; set; }
    public List<string> tangentialConversion { get; set; }
    public List<float> inductionTime { get; set; }

    public BigFuckingListOfAllData()
    {
        allData = new List<List<float>>();
        headers = new List<string>();
        fileNames = new List<string>();
        maxDSC = new List<float>();
        maxDSCTime = new List<float>();
        maxConversion = new List<float>();
        maxRp = new List<float>();
        maxRpTime = new List<float>();
        tangentialConversion = new List<string>();
        tangentialDSC = new List<string>();
        tangentialRp = new List<string>();
        inductionTime = new List<float>();
    }
}


}
