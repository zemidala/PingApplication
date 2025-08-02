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
    public class StatisticsGraph : GraphBase
    {
        public void Draw(Canvas canvas, List<PingResult> results)
        {
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            double margin = 40;

            canvas.Children.Clear();
            DrawBackground(canvas, canvasWidth, canvasHeight);

            // Рисуем сетку даже если нет данных
            DrawGrid(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin, 10);
            DrawAxes(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);

            if (results == null || results.Count == 0) return;

            int successCount = results.Count(r => r.IsSuccess);
            int failCount = results.Count(r => !r.IsSuccess);

            double barWidth = (canvasWidth - 3 * margin) / 3;

            double maxBarHeight = canvasHeight - 2 * margin;
            int maxCount = Math.Max(1, Math.Max(successCount, failCount));
            double successHeight = maxBarHeight * successCount / maxCount;
            var successRect = new Rectangle
            {
                Width = barWidth,
                Height = successHeight,
                Fill = Brushes.Green
            };
            Canvas.SetLeft(successRect, margin + barWidth / 2);
            Canvas.SetTop(successRect, canvasHeight - margin - successHeight);
            canvas.Children.Add(successRect);

            double failHeight = maxBarHeight * failCount / maxCount;
            var failRect = new Rectangle
            {
                Width = barWidth,
                Height = failHeight,
                Fill = Brushes.Red
            };
            Canvas.SetLeft(failRect, margin + barWidth * 1.5 + margin / 2);
            Canvas.SetTop(failRect, canvasHeight - margin - failHeight);
            canvas.Children.Add(failRect);

            var successText = new TextBlock
            {
                Text = $"Успешные: {successCount}",
                Foreground = Brushes.Green
            };
            Canvas.SetLeft(successText, margin + barWidth / 2 - 30);
            Canvas.SetTop(successText, canvasHeight - margin - successHeight - 25);
            canvas.Children.Add(successText);

            var failText = new TextBlock
            {
                Text = $"Ошибки: {failCount}",
                Foreground = Brushes.Red
            };
            Canvas.SetLeft(failText, margin + barWidth * 1.5 + margin / 2 - 25);
            Canvas.SetTop(failText, canvasHeight - margin - failHeight - 25);
            canvas.Children.Add(failText);

            DrawScales(canvas, margin, canvasWidth, canvasHeight, Math.Max(successCount, failCount));
        }

        private void DrawGrid(Canvas canvas, double left, double right, double top, double bottom, int maxValue)
        {
            double width = right - left;
            var line1 = new Line
            {
                X1 = left + width / 3,
                Y1 = top,
                X2 = left + width / 3,
                Y2 = bottom,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1
            };
            canvas.Children.Add(line1);

            var line2 = new Line
            {
                X1 = left + 2 * width / 3,
                Y1 = top,
                X2 = left + 2 * width / 3,
                Y2 = bottom,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1
            };
            canvas.Children.Add(line2);

            double height = bottom - top;
            int maxScale = Math.Max(10, (int)Math.Ceiling(Math.Max(maxValue, 5) / 5.0) * 5);
            int steps = Math.Max(5, maxScale / 5);

            for (int i = 0; i <= steps; i++)
            {
                double y = top + i * height / steps;
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

        private void DrawScales(Canvas canvas, double margin, double canvasWidth, double canvasHeight, int maxValue)
        {
            int maxScale = Math.Max(10, (int)Math.Ceiling(Math.Max(maxValue, 5) / 5.0) * 5);
            int step = Math.Max(1, maxScale / 5);

            for (int i = 0; i <= maxScale; i += step)
            {
                double y = canvasHeight - margin - i * (canvasHeight - 2 * margin) / Math.Max(maxScale, 1);

                var tick = new Line
                {
                    X1 = margin - 5,
                    Y1 = y,
                    X2 = margin,
                    Y2 = y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                canvas.Children.Add(tick);

                var text = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(text, margin - 25);
                Canvas.SetTop(text, y - 8);
                canvas.Children.Add(text);
            }

            var xAxisLabel = new TextBlock
            {
                Text = "Тип пинга",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(xAxisLabel, canvasWidth / 2 - 50);
            Canvas.SetTop(xAxisLabel, canvasHeight - 20);
            canvas.Children.Add(xAxisLabel);

            var yAxisLabel = new TextBlock
            {
                Text = "Количество",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(yAxisLabel, 5);
            Canvas.SetTop(yAxisLabel, 10);
            canvas.Children.Add(yAxisLabel);

            var successLabel = new TextBlock
            {
                Text = "Успешные",
                FontSize = 10,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(successLabel, margin + 20);
            Canvas.SetTop(successLabel, canvasHeight - margin + 10);
            canvas.Children.Add(successLabel);

            var failLabel = new TextBlock
            {
                Text = "Ошибки",
                FontSize = 10,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(failLabel, margin + (canvasWidth - 3 * margin) / 3 * 1.5 + margin / 2 - 15);
            Canvas.SetTop(failLabel, canvasHeight - margin + 10);
            canvas.Children.Add(failLabel);
        }
    }
}