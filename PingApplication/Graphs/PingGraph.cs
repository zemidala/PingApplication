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
    public class PingGraph : GraphBase
    {
        private int startIndex = 1;
        private const int MAX_DISPLAY_PINGS = 30;

        public void Draw(Canvas canvas, List<PingResult> results)
        {
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            double margin = 40;

            canvas.Children.Clear();
            DrawBackground(canvas, canvasWidth, canvasHeight);

            if (results == null || results.Count == 0) return;

            var successfulResults = results.Where(r => r.IsSuccess).ToList();
            if (successfulResults.Count == 0) return;

            List<PingResult> displayResults;
            int totalSuccessfulCount = successfulResults.Count;

            if (totalSuccessfulCount > MAX_DISPLAY_PINGS)
            {
                displayResults = successfulResults.Skip(totalSuccessfulCount - MAX_DISPLAY_PINGS).Take(MAX_DISPLAY_PINGS).ToList();
                startIndex = totalSuccessfulCount - MAX_DISPLAY_PINGS + 1;
            }
            else
            {
                displayResults = new List<PingResult>(successfulResults);
                startIndex = 1;
            }

            var times = displayResults.Select(r => (double)r.RoundTripTime).ToList();
            if (times.Count == 0) return;

            double minTime = Math.Max(0, times.Min() - 5);
            double maxTime = times.Max() + 5;
            double timeRange = maxTime - minTime;
            if (timeRange == 0) timeRange = 1;

            // Всегда рисуем сетку с 30 секциями
            DrawGrid(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin, displayResults.Count, minTime, maxTime);
            DrawAxes(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);

            // Рисуем линии графика с правильной привязкой к сетке
            for (int i = 1; i < displayResults.Count; i++)
            {
                // Привязываем точки к сетке с шагом 1
                double x1 = margin + (i - 1) * (canvasWidth - 2 * margin) / Math.Max(MAX_DISPLAY_PINGS - 1, 1);
                double y1 = canvasHeight - margin - (displayResults[i - 1].RoundTripTime - minTime) * (canvasHeight - 2 * margin) / timeRange;

                double x2 = margin + i * (canvasWidth - 2 * margin) / Math.Max(MAX_DISPLAY_PINGS - 1, 1);
                double y2 = canvasHeight - margin - (displayResults[i].RoundTripTime - minTime) * (canvasHeight - 2 * margin) / timeRange;

                var line = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2
                };
                canvas.Children.Add(line);
            }

            // Рисуем точки с правильной привязкой к сетке
            for (int i = 0; i < displayResults.Count; i++)
            {
                // Привязываем точки к сетке с шагом 1
                double x = margin + i * (canvasWidth - 2 * margin) / Math.Max(MAX_DISPLAY_PINGS - 1, 1);
                double y = canvasHeight - margin - (displayResults[i].RoundTripTime - minTime) * (canvasHeight - 2 * margin) / timeRange;

                var ellipse = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.Red
                };
                Canvas.SetLeft(ellipse, x - 3);
                Canvas.SetTop(ellipse, y - 3);
                canvas.Children.Add(ellipse);
            }

            DrawScales(canvas, margin, canvasWidth, canvasHeight, displayResults.Count, minTime, maxTime, startIndex);
        }

        // Новый метод для инициализации только сетки
        public void InitializeGrid(Canvas canvas)
        {
            if (canvas == null) return;

            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 800;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : 400;
            double margin = 40;

            canvas.Children.Clear();
            DrawBackground(canvas, canvasWidth, canvasHeight);
            // Рисуем пустую сетку с 30 секциями
            DrawGrid(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin, 0, 0, 100);
            DrawAxes(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);
        }

        private void DrawGrid(Canvas canvas, double left, double right, double top, double bottom, int pingCount, double minTime, double maxTime)
        {
            double width = right - left;
            double height = bottom - top;

            // Всегда рисуем 30 секций (по количеству пингов)
            for (int i = 0; i < 30; i++)
            {
                double x = left + i * width / Math.Max(30 - 1, 1);

                // Рисуем вертикальные точки
                for (double y = top; y <= bottom; y += 4) // Точки каждые 4 пикселя
                {
                    var dot = new Ellipse
                    {
                        Width = 1.5, // Размер точек
                        Height = 1.5,
                        Fill = Brushes.Gray // Более видимый цвет
                    };
                    Canvas.SetLeft(dot, x - 0.75);
                    Canvas.SetTop(dot, y - 0.75);
                    canvas.Children.Add(dot);
                }
            }

            // Горизонтальные линии сетки по времени
            double timeSteps = 8;

            for (int i = 0; i <= timeSteps; i++)
            {
                double y = top + i * height / timeSteps;

                // Рисуем горизонтальные точки
                for (double x = left; x <= right; x += 4) // Точки каждые 4 пикселя
                {
                    var dot = new Ellipse
                    {
                        Width = 1.5, // Размер точек
                        Height = 1.5,
                        Fill = Brushes.Gray // Более видимый цвет
                    };
                    Canvas.SetLeft(dot, x - 0.75);
                    Canvas.SetTop(dot, y - 0.75);
                    canvas.Children.Add(dot);
                }
            }
        }

        private void DrawScales(Canvas canvas, double margin, double canvasWidth, double canvasHeight,
                              int displayCount, double minTime, double maxTime, int startNumber)
        {
            double width = canvasWidth - 2 * margin;

            // Показываем метки с шагом 1
            for (int i = 0; i < MAX_DISPLAY_PINGS; i++)
            {
                double x = margin + i * width / Math.Max(MAX_DISPLAY_PINGS - 1, 1);
                int pingNumber = startNumber + i;

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

                // Показываем каждую метку (шаг 1)
                var text = new TextBlock
                {
                    Text = pingNumber.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(text, x - 8);
                Canvas.SetTop(text, canvasHeight - margin + 8);
                canvas.Children.Add(text);
            }

            for (int i = 0; i <= 8; i++)
            {
                double timeValue = minTime + i * (maxTime - minTime) / 8;
                double y = canvasHeight - margin - i * (canvasHeight - 2 * margin) / 8;

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
                    Text = Math.Round(timeValue).ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(text, margin - 35);
                Canvas.SetTop(text, y - 8);
                canvas.Children.Add(text);
            }

            var xAxisLabel = new TextBlock
            {
                Text = "Количество пингов",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(xAxisLabel, canvasWidth / 2 - 70);
            Canvas.SetTop(xAxisLabel, canvasHeight - 20);
            canvas.Children.Add(xAxisLabel);

            var yAxisLabel = new TextBlock
            {
                Text = "Задержка (мс)",
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