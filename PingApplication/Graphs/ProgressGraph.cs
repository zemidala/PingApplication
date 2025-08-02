using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PingApp.Models;

namespace PingApp.Graphs
{
    public class ProgressGraph : GraphBase
    {
        public void Draw(Canvas canvas, List<PingResult> results)
        {
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            double margin = 40;

            canvas.Children.Clear();
            DrawBackground(canvas, canvasWidth, canvasHeight);

            // Рисуем сетку даже если нет данных
            DrawGrid(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin, 0, 100);
            DrawAxes(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);

            if (results == null || results.Count == 0) return;

            var successfulResults = results.Where(r => r.IsSuccess).ToList();
            if (successfulResults.Count == 0) return;

            var times = successfulResults.Select(r => (double)r.RoundTripTime).ToList();
            double minTime = Math.Max(0, times.Min() - 10);
            double maxTime = times.Max() + 10;

            double currentTime = successfulResults.Last().RoundTripTime;
            double maxAllTime = times.Count > 0 ? times.Max() : 0;
            double minAllTime = times.Count > 0 ? times.Min() : 0;
            double avgTime = times.Count > 0 ? times.Average() : 0;

            double usableHeight = canvasHeight - 2 * margin;
            double lineHeightSpacing = usableHeight / 5;

            double maxY = margin + lineHeightSpacing;
            DrawProgressBar(canvas, margin, canvasWidth - margin, maxY, maxAllTime, minTime, maxTime, Brushes.Red, $"Max: {maxAllTime:F0} мс");

            double currentY = margin + 2 * lineHeightSpacing;
            DrawProgressBar(canvas, margin, canvasWidth - margin, currentY, currentTime, minTime, maxTime, Brushes.Blue, $"Current: {currentTime:F0} мс");

            double avgY = margin + 3 * lineHeightSpacing;
            DrawProgressBar(canvas, margin, canvasWidth - margin, avgY, avgTime, minTime, maxTime, Brushes.Green, $"Average: {avgTime:F1} мс");

            double minY = margin + 4 * lineHeightSpacing;
            DrawProgressBar(canvas, margin, canvasWidth - margin, minY, minAllTime, minTime, maxTime, Brushes.Orange, $"Min: {minAllTime:F0} мс");

            DrawScales(canvas, margin, canvasWidth, canvasHeight, minTime, maxTime);
        }

        private void DrawGrid(Canvas canvas, double left, double right, double top, double bottom, double minTime, double maxTime)
        {
            double width = right - left;
            int timeSteps = 10;

            for (int i = 0; i <= timeSteps; i++)
            {
                double x = left + i * width / timeSteps;
                var line = new Line
                {
                    X1 = x,
                    Y1 = top,
                    X2 = x,
                    Y2 = bottom,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                canvas.Children.Add(line);
            }

            double height = bottom - top;
            int ySteps = 8;

            for (int i = 0; i <= ySteps; i++)
            {
                double y = top + i * height / ySteps;
                var line = new Line
                {
                    X1 = left,
                    Y1 = y,
                    X2 = right,
                    Y2 = y,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                canvas.Children.Add(line);
            }
        }

        private void DrawProgressBar(Canvas canvas, double left, double right, double y, double value, double minValue, double maxValue, Brush color, string label)
        {
            double width = right - left;
            double valueRange = Math.Max(maxValue - minValue, 1); // Избегаем деления на 0

            double fillWidth = 0;
            if (valueRange > 0)
            {
                fillWidth = Math.Max(0, (value - minValue) / valueRange * width);
            }

            var backgroundBar = new Rectangle
            {
                Width = width,
                Height = 8,
                Fill = Brushes.LightGray
            };
            Canvas.SetLeft(backgroundBar, left);
            Canvas.SetTop(backgroundBar, y - 4);
            canvas.Children.Add(backgroundBar);

            if (fillWidth > 0)
            {
                var fillBar = new Rectangle
                {
                    Width = fillWidth,
                    Height = 8,
                    Fill = color
                };
                Canvas.SetLeft(fillBar, left);
                Canvas.SetTop(fillBar, y - 4);
                canvas.Children.Add(fillBar);
            }

            var text = new TextBlock
            {
                Text = label,
                Foreground = color,
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(text, left + width / 2 - 50);
            Canvas.SetTop(text, y - 20);
            canvas.Children.Add(text);

            var valueText = new TextBlock
            {
                Text = $"{value:F0} мс",
                Foreground = color,
                FontSize = 10
            };
            Canvas.SetLeft(valueText, right - 50);
            Canvas.SetTop(valueText, y - 15);
            canvas.Children.Add(valueText);
        }

        private void DrawScales(Canvas canvas, double margin, double canvasWidth, double canvasHeight, double minTime, double maxTime)
        {
            for (int i = 0; i <= 10; i++)
            {
                double timeValue = minTime + i * (maxTime - minTime) / 10;
                double x = margin + i * (canvasWidth - 2 * margin) / 10;

                var tick = new Line
                {
                    X1 = x,
                    Y1 = canvasHeight - margin,
                    X2 = x,
                    Y2 = canvasHeight - margin + 5,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                canvas.Children.Add(tick);

                var text = new TextBlock
                {
                    Text = Math.Round(timeValue).ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(text, x - 15);
                Canvas.SetTop(text, canvasHeight - margin + 8);
                canvas.Children.Add(text);
            }

            var xAxisLabel = new TextBlock
            {
                Text = "Задержка (мс)",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(xAxisLabel, canvasWidth / 2 - 60);
            Canvas.SetTop(xAxisLabel, canvasHeight - 20);
            canvas.Children.Add(xAxisLabel);

            var yAxisLabel = new TextBlock
            {
                Text = "Значения",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(yAxisLabel, 5);
            Canvas.SetTop(yAxisLabel, 10);
            canvas.Children.Add(yAxisLabel);
        }
    }
}