using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace DDBot.Services
{
    public class ChartService
    {
        public void GeneratePlot(List<DataPoint> series)
        {
            using (var chart = new Chart())
            {
                chart.Series.Add("Series1");

                chart.ChartAreas.Add(new ChartArea());
                chart.ChartAreas[0].Area3DStyle.Enable3D = false;

                int dpCount = 0;
                foreach (var datapoint in series)
                {
                    chart.Series["Series1"].Points.Add(datapoint);

                    if (datapoint.YValues[0] < 40)
                    {
                        chart.Series["Series1"].Points[dpCount].Color = Color.DarkRed;
                        chart.Series["Series1"].Points[dpCount].BackSecondaryColor = Color.Red;
                    }
                    else if (datapoint.YValues[0] <= 60)
                    {
                        chart.Series["Series1"].Points[dpCount].Color = Color.Yellow;
                        chart.Series["Series1"].Points[dpCount].BackSecondaryColor = Color.LightGoldenrodYellow;
                    }
                    else
                    {
                        chart.Series["Series1"].Points[dpCount].Color = Color.ForestGreen;
                        chart.Series["Series1"].Points[dpCount].BackSecondaryColor = Color.Green;
                    }
                    dpCount++;
                }

                chart.Series["Series1"].ChartArea = "ChartArea1";
                chart.Series["Series1"].ChartType = SeriesChartType.Bar;
                chart.Series["Series1"]["DrawingStyle"] = "Emboss";

                chart.Series["Series1"].Font = new Font("Segoe UI", 12, FontStyle.Bold);

                chart.ChartAreas["ChartArea1"].AxisX.MajorGrid.LineWidth = 0;
                chart.ChartAreas["ChartArea1"].AxisX.Minimum = 0.5;
                chart.ChartAreas["ChartArea1"].AxisX.Maximum = dpCount+0.5;
                chart.ChartAreas["ChartArea1"].AxisX.Interval = 0.50;
                chart.ChartAreas["ChartArea1"].AxisX.LineColor = Color.White;
                chart.ChartAreas["ChartArea1"].AxisX.IsLabelAutoFit = false;
                chart.ChartAreas["ChartArea1"].AxisX.LabelStyle.ForeColor = Color.White;

                chart.ChartAreas["ChartArea1"].AxisY.MajorGrid.LineWidth = 0;
                chart.ChartAreas["ChartArea1"].AxisY.Maximum = 100;
                chart.ChartAreas["ChartArea1"].AxisY.Interval = 10;
                chart.ChartAreas["ChartArea1"].AxisY.Title = "Sentiment %";
                chart.ChartAreas["ChartArea1"].AxisY.TitleForeColor = Color.White;
                chart.ChartAreas["ChartArea1"].AxisY.TitleFont = new Font("Segoe UI", 12, FontStyle.Bold);
                chart.ChartAreas["ChartArea1"].AxisY.LineColor = Color.White;
                chart.ChartAreas["ChartArea1"].AxisY.LabelStyle.ForeColor = Color.White;

                chart.Size = new Size(600, 350);
                chart.BackColor = Color.FromArgb(37, 39, 42);
                chart.ChartAreas["ChartArea1"].BackColor = Color.FromArgb(57, 59, 65);
                chart.ChartAreas["ChartArea1"].BorderDashStyle = ChartDashStyle.NotSet;

                chart.SaveImage("a_mypic.png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
