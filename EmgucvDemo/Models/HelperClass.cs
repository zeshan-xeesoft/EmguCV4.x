﻿using DocumentFormat.OpenXml.Drawing.Charts;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmgucvDemo.Models
{
   public class HelperClass
    {
        public static (Matrix<float>,Matrix<int>) ReadCSV(string path,bool FirstRowHeader=true,char sep=',',int LabelIndex = 0)
        {
            try
            {
                var list = File.ReadAllLines(path).ToList();
                if (list!=null)
                {
                    if (FirstRowHeader==true)
                    {
                        list.RemoveAt(0);
                    }

                    int ROWS = list.Count;
                    int COLS = list[0].Split(sep).Length - 1;

                    Matrix<float> x_data = new Matrix<float>(ROWS, COLS);
                    Matrix<int> y_labels = new Matrix<int>(ROWS, 1);

                    for (int i = 0; i < list.Count; i++)
                    {
                        var line = list[i].Split(sep).Select(x => float.Parse(x, CultureInfo.InvariantCulture))
                            .ToList();
                        var label = int.Parse(line.ElementAt(LabelIndex).ToString());
                        line.RemoveAt(LabelIndex);

                        for (int j = 0; j < line.Count; j++)
                        {
                            x_data[i, j] = line[j];
                        }
                        y_labels[i, 0] = label;
                    }

                    return (x_data, y_labels);
                }
                else
                {
                    return (null, null);
                }
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }
        public static (Matrix<float>, Matrix<int>, Matrix<float>, Matrix<int>) TestTrainSplit(
            Matrix<float> Data,Matrix<int> Labels,float split=0.2f)
        {
            try
            {
                int N = (int)(Labels.Rows * split);
                int Min = 0;
                int Max = Labels.Rows - 1;
                var TestIndices = HelperClass.RandomNumGenerator(Min, Max, N);

                Matrix<float> x_test = new Matrix<float>(N, Data.Cols);
                Matrix<int> y_test = new Matrix<int>(N, 1);

                Matrix<float> x_train = new Matrix<float>(Data.Rows - N, Data.Cols);
                Matrix<int> y_train = new Matrix<int>(Data.Rows - N, 1);

                int testIndex = 0, trainIndex = 0;

                for (int row = 0; row < Data.Rows; row++)
                {
                    if (TestIndices.Contains(row))
                    {
                        for (int j = 0; j < Data.Cols; j++)
                        {
                            x_test[testIndex, j] = Data[row, j];
                        }
                        y_test[testIndex, 0] = Labels[row, 0];
                        testIndex++;
                    }
                    else
                    {
                        for (int j = 0; j < Data.Cols; j++)
                        {
                            x_train[trainIndex, j] = Data[row, j];
                        }
                        y_train[trainIndex, 0] = Labels[row, 0];
                        trainIndex++;
                    }
                }

                return (x_train, y_train, x_test, y_test);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static int[] RandomNumGenerator(int Min,int Max,int N)
        {
            Random randNum = new Random();
            List<int> list = new List<int>();
            while (list.Count<N)
            {
                int num = randNum.Next(Min, Max);
                if (!list.Contains(num))
                {
                    list.Add(num);
                }
            }
            return list.ToArray();
        }

        public static int GetClassCount(Matrix<int> y_labels)
        {
            try
            {
                var labels = new int[y_labels.Rows];
                y_labels.Mat.CopyTo(labels);
                return labels.Distinct().Count();
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }
        //  0 =>1 0  1=> 0 1 

        public static Matrix<int> HotMatrix(Matrix<int> y_label,int ClassCount)
        {
            var HotVector = new Matrix<int>(y_label.Rows, ClassCount);
            HotVector.SetZero();
            for (int row = 0; row < y_label.Rows; row++)
            {
                HotVector[row, y_label[row, 0]] = 1;
            }

            return HotVector;
        }

        public static void WriteConfigParameters(string key, string value)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

                config.AppSettings.Settings[key].Value = value;

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static string ReadConfigParameters(string key)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
                string val = config.AppSettings.Settings[key].Value;
                return val;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static int[,] ComputeConfusionMatrix(int []actual,int[]predicted)
        {
            try
            {
                if (actual.Length != predicted.Length)
                {
                    throw new Exception("Vectors lengths not matched");
                }

                int NoClasses = actual.Distinct().Count();
                int[,] CM = new int[NoClasses, NoClasses];
                for (int i = 0; i < actual.Length; i++)
                {
                    int r = predicted[i];
                    int c = actual[i];
                    CM[r, c]++;
                }
                return CM;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static double[] CalculateMetrics(int[,] CM,int[] actual,int []predicted)
        {
            try
            {
                double[] metrics = new double[3];
                int samples = actual.Length;
                int classes = (int)CM.GetLongLength(0);
                var diagonal = GetDiagonal(CM);
                var diagnolSum = diagonal.Sum();

                int[] ColTotal = GetSumCols(CM);
                int[] RowTotal = GetSumRows(CM);

                // Accuracy
                var accuracy = diagnolSum / (double)samples;

                // predicion
                var precision = new double[classes];
                for (int i = 0; i < classes; i++)
                {
                    precision[i] = diagonal[i] == 0 ? 0 : (double)diagonal[i] / ColTotal[i];
                }

                // Recall
                var recall = new double[classes];
                for (int i = 0; i < classes; i++)
                {
                    recall[i] = diagonal[i] == 0 ? 0 : (double)diagonal[i] / RowTotal[i];
                }

                metrics[0] = accuracy;
                metrics[1] = precision.Average();
                metrics[2] = recall.Average();

                return metrics;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static int [] GetDiagonal(int [,] matrix)
        {
            return Enumerable.Range(0, matrix.GetLength(0)).Select(i => matrix[i, i]).ToArray();
        }
        public static int[] GetSumCols(int[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            int[] colSum = new int[cols];

            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    colSum[col] += matrix[row, col];
                }
            }
            return colSum;
        }

        public static int [] GetSumRows(int [,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            int[] rowSum = new int[cols];

            for (int row = 0; row < rows; row++) 
            {
                for (int col = 0; col < cols; col++)
                {
                    rowSum[row] += matrix[row, col];
                }
            }
            return rowSum;
        }

        public static System.Data.DataTable Array2DataTable(int[,] matrix)
        {
           System.Data.DataTable dt = new System.Data.DataTable();
            dt.Columns.Add("Classes");
            for (int i = 0; i < matrix.GetLength(1); i++)
            {
                dt.Columns.Add("Class" + (i));
            }

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                DataRow row = dt.NewRow();
                row[0] = "Class" + i;
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    row[j+1] = matrix[i, j];
                }
                dt.Rows.Add(row);
            }

            return dt;
        }
    }
}