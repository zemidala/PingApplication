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

        // Проверяем, что размеры корректны
        if (canvasWidth <= 0 || canvasHeight <= 0) return;

        canvas.Children.Clear();
        DrawBackground(canvas, canvasWidth, canvasHeight);

        // Всегда рисуем сетку и оси
        DrawGrid(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);
        DrawAxes(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);

        // Всегда рисуем шкалы (даже без данных)
        // Используем минимальные значения для отображения шкал
        DrawScales(canvas, margin, canvasWidth, canvasHeight, 0, 0, 100, 1);

        // Только если есть данные, рисуем график
        if (results == null || results.Count == 0) return;

        var displayResults = GetDisplayResults(results);

        if (displayResults.Count == 0) return;

        var (minTime, maxTime) = CalculateTimeRange(displayResults);
        var timeRange = maxTime - minTime;
        if (timeRange == 0) timeRange = 100; // Минимальный диапазон для отображения

        // Рисуем линии графика
        for (var i = 1; i < displayResults.Count; i++)
        {
            var x1 = margin + (i - 1) * (canvasWidth - 2 * margin) / Math.Max(MAX_DISPLAY_PINGS - 1, 1);
            var y1 = CalculateYPosition(displayResults[i - 1], minTime, timeRange, canvasHeight, margin);

            var x2 = margin + i * (canvasWidth - 2 * margin) / Math.Max(MAX_DISPLAY_PINGS - 1, 1);
            var y2 = CalculateYPosition(displayResults[i], minTime, timeRange, canvasHeight, margin);

            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = displayResults[i].IsSuccess ? Brushes.Blue : Brushes.Red,
                StrokeThickness = 2
            };
            canvas.Children.Add(line);
        }

        // Рисуем точки
        for (var i = 0; i < displayResults.Count; i++)
        {
            var x = margin + i * (canvasWidth - 2 * margin) / Math.Max(MAX_DISPLAY_PINGS - 1, 1);
            var y = CalculateYPosition(displayResults[i], minTime, timeRange, canvasHeight, margin);

            var ellipse = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = displayResults[i].IsSuccess ? Brushes.Green : Brushes.Red
            };
            Canvas.SetLeft(ellipse, x - 3);
            Canvas.SetTop(ellipse, y - 3);
            canvas.Children.Add(ellipse);
        }

        DrawScales(canvas, margin, canvasWidth, canvasHeight, displayResults.Count, minTime, maxTime, startIndex);
    }

    private List<PingResult> GetDisplayResults(List<PingResult> results)
    {
        List<PingResult> displayResults;
        var totalCount = results.Count;

        if (totalCount > MAX_DISPLAY_PINGS)
        {
            displayResults = results.Skip(totalCount - MAX_DISPLAY_PINGS).Take(MAX_DISPLAY_PINGS).ToList();
            startIndex = totalCount - MAX_DISPLAY_PINGS + 1;
        }
        else
        {
            displayResults = new List<PingResult>(results);
            startIndex = 1;
        }

        return displayResults;
    }

    private (double minTime, double maxTime) CalculateTimeRange(List<PingResult> results)
    {
        var successfulResults = results.Where(r => r.IsSuccess).ToList();

        if (successfulResults.Count == 0)
            // Если нет успешных результатов, используем диапазон 0-100
            return (0, 100);

        var times = successfulResults.Select(r => (double)r.RoundTripTime).ToList();
        var minTime = Math.Max(0, times.Min() - 5);
        var maxTime = times.Max() + 5;

        return (minTime, maxTime);
    }

    private double CalculateYPosition(PingResult result, double minTime, double timeRange, double canvasHeight,
        double margin)
    {
        if (!result.IsSuccess)
            // Для ошибок показываем минимальное значение (самая нижняя линия)
            return canvasHeight - margin;

        return canvasHeight - margin - (result.RoundTripTime - minTime) * (canvasHeight - 2 * margin) / timeRange;
    }

    private void DrawGrid(Canvas canvas, double left, double right, double top, double bottom)
    {
        var width = right - left;
        var height = bottom - top;

        // Вертикальные точки сетки (30 секций)
        for (var i = 0; i < 30; i++)
        {
            var x = left + i * width / Math.Max(30 - 1, 1);

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

        // Шкала по оси X (номера пингов)
        for (var i = 0; i < 30; i++) // Всегда 30 секций
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

            // Подписи рисуем только если есть смысл
            if (displayCount > 0 || i % 5 == 0) // Рисуем каждую 5-ю метку при отсутствии данных
            {
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
        }

        // Шкала по оси Y (время)
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