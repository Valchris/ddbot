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

                    if (datapoint.YValues[0] <= 25)
                    {
                        chart.Series["Series1"].Points[dpCount].Color = Color.DarkRed;
                        chart.Series["Series1"].Points[dpCount].BackSecondaryColor = Color.Red;
                        chart.Series["Series1"].BackGradientStyle = GradientStyle.TopBottom;
                    }
                    else if (datapoint.YValues[0] <= 50)
                    {
                        chart.Series["Series1"].Points[dpCount].Color = Color.DarkOrange;
                        chart.Series["Series1"].Points[dpCount].BackSecondaryColor = Color.Orange;
                        chart.Series["Series1"].BackGradientStyle = GradientStyle.TopBottom;
                    }
                    else if (datapoint.YValues[0] <= 75)
                    {
                        chart.Series["Series1"].Points[dpCount].Color = Color.Yellow;
                        chart.Series["Series1"].Points[dpCount].BackSecondaryColor = Color.LightGoldenrodYellow;
                        chart.Series["Series1"].BackGradientStyle = GradientStyle.TopBottom;
                    }
                    else
                    {
                        chart.Series["Series1"].Points[dpCount].Color = Color.ForestGreen;
                        chart.Series["Series1"].Points[dpCount].BackSecondaryColor = Color.Green;
                        chart.Series["Series1"].BackGradientStyle = GradientStyle.TopBottom;
                    }
                    dpCount++;
                }

                chart.Series["Series1"].ChartArea = "ChartArea1";
                chart.Series["Series1"].ChartType = SeriesChartType.Bar;
                chart.Series["Series1"]["DrawingStyle"] = "Cylinder";
                
                chart.Series["Series1"].Font = new Font("Segoe UI", 8, FontStyle.Bold);
                //chart.Series["Series1"].Sort(PointSortOrder.Ascending, "X");

                chart.ChartAreas["ChartArea1"].AxisX.MajorGrid.LineWidth = 0;
                chart.ChartAreas["ChartArea1"].AxisX.Minimum = 0.5;
                chart.ChartAreas["ChartArea1"].AxisX.Maximum = dpCount+0.5;
                chart.ChartAreas["ChartArea1"].AxisX.Interval = 0.5;

                chart.ChartAreas["ChartArea1"].AxisY.MajorGrid.LineWidth = 0;
                chart.ChartAreas["ChartArea1"].AxisY.Maximum = 100;
                chart.ChartAreas["ChartArea1"].AxisY.Title = "Debbie %";

                chart.ChartAreas["ChartArea1"].BackColor = Color.Transparent;
                chart.ChartAreas["ChartArea1"].BorderDashStyle = ChartDashStyle.NotSet;

                chart.SaveImage("a_mypic.png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
