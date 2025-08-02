using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PingApp.Models;

namespace PingApp.Graphs;

public class PingGraph : GraphBase
{
    private const int MAX_DISPLAY_PINGS = 30;
    private int startIndex = 1;

    public void Draw(Canvas canvas, List<PingResult> results)
    {
        var canvasWidth = canvas.ActualWidth;
        var canvasHeight = canvas.ActualHeight;
        double margin = 40;

        canvas.Children.Clear();
        DrawBackground(canvas, canvasWidth, canvasHeight);

        if (results == null || results.Count == 0) return;

        var successfulResults = results.Where(r => r.IsSuccess).ToList();
        if (successfulResults.Count == 0) return;

        List<PingResult> displayResults;
        var totalSuccessfulCount = successfulResults.Count;

        if (totalSuccessfulCount > MAX_DISPLAY_PINGS)
        {
            displayResults = successfulResults.Skip(totalSuccessfulCount - MAX_DISPLAY_PINGS).Take(MAX_DISPLAY_PINGS)
                .ToList();
            startIndex = totalSuccessfulCount - MAX_DISPLAY_PINGS + 1;
        }
        else
        {
            displayResults = new List<PingResult>(successfulResults);
            startIndex = 1;
        }

        var times = displayResults.Select(r => (double)r.RoundTripTime).ToList();
        if (times.Count == 0) return;

        var minTime = Math.Max(0, times.Min() - 5);
        var maxTime = times.Max() + 5;
        var timeRange = maxTime - minTime;
        if (timeRange == 0) timeRange = 1;

        // Всегда рисуем сетку с 30 секциями
        DrawGrid(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);
        DrawAxes(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);

        // Рисуем линии графика с правильной привязкой к сетке
        for (var i = 1; i < displayResults.Count; i++)
        {
            // Привязываем точки к сетке с шагом 1
            var x1 = margin + (i - 1) * (canvasWidth - 2 * margin) / Math.Max(MAX_DISPLAY_PINGS - 1, 1);
            var y1 = canvasHeight - margin - (displayResults[i - 1].RoundTripTime - minTime) *
                (canvasHeight - 2 * margin) / timeRange;

            var x2 = margin + i * (canvasWidth - 2 * margin) / Math.Max(MAX_DISPLAY_PINGS - 1, 1);
            var y2 = canvasHeight - margin -
                     (displayResults[i].RoundTripTime - minTime) * (canvasHeight - 2 * margin) / timeRange;

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
        for (var i = 0; i < displayResults.Count; i++)
        {
            // Привязываем точки к сетке с шагом 1
            var x = margin + i * (canvasWidth - 2 * margin) / Math.Max(MAX_DISPLAY_PINGS - 1, 1);
            var y = canvasHeight - margin -
                    (displayResults[i].RoundTripTime - minTime) * (canvasHeight - 2 * margin) / timeRange;

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

        var canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 800;
        var canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : 400;
        double margin = 40;

        canvas.Children.Clear();
        DrawBackground(canvas, canvasWidth, canvasHeight);
        // Рисуем пустую сетку с 30 секциями
        DrawGrid(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);
        DrawAxes(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);
    }

    private void DrawGrid(Canvas canvas, double left, double right, double top, double bottom)
    {
        var width = right - left;
        var height = bottom - top;

        // Всегда рисуем 30 секций (точечная сетка как в других графиках)
        for (var i = 0; i < 30; i++)
        {
            var x = left + i * width / Math.Max(30 - 1, 1);

            // Рисуем вертикальные точки (каждые 4 пикселя)
            for (var y = top; y <= bottom; y += 4)
            {
                var dot = new Ellipse
                {
                    Width = 1.5,
                    Height = 1.5,
                    Fill = Brushes.Gray
                };
                Canvas.SetLeft(dot, x - 0.75);
                Canvas.SetTop(dot, y - 0.75);
                canvas.Children.Add(dot);
            }
        }

        // Горизонтальные точки сетки
        for (var i = 0; i <= 8; i++)
        {
            var y = top + i * height / 8;

            // Рисуем горизонтальные точки (каждые 4 пикселя)
            for (var x = left; x <= right; x += 4)
            {
                var dot = new Ellipse
                {
                    Width = 1.5,
                    Height = 1.5,
                    Fill = Brushes.Gray
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
        var width = canvasWidth - 2 * margin;

        // Показываем метки с шагом 1 (всегда 30 секций)
        for (var i = 0; i < 30; i++)
        {
            var x = margin + i * width / Math.Max(30 - 1, 1);
            var pingNumber = startNumber + i;

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
                Text = pingNumber.ToString(),
                FontSize = 10,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(text, x - 8);
            Canvas.SetTop(text, canvasHeight - margin + 8);
            canvas.Children.Add(text);
        }

        for (var i = 0; i <= 8; i++)
        {
            var timeValue = minTime + i * (maxTime - minTime) / 8;
            var y = canvasHeight - margin - i * (canvasHeight - 2 * margin) / 8;

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