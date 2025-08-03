using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PingApp.Models;

namespace PingApp.Graphs;

public class StatisticsGraph : GraphBase
{
    public void Draw(Canvas canvas, List<PingResult> results)
    {
        var canvasWidth = canvas.ActualWidth;
        var canvasHeight = canvas.ActualHeight;
        double margin = 40;
        double topMargin = 40; // Уменьшенный верхний отступ

        // Проверяем, что размеры корректны
        if (canvasWidth <= 0 || canvasHeight <= 0) return;

        canvas.Children.Clear();
        DrawBackground(canvas, canvasWidth, canvasHeight);

        // ВСЕГДА рисуем сетку и оси
        DrawGrid(canvas, margin, canvasWidth - margin, topMargin, canvasHeight - margin, 10);
        DrawAxes(canvas, margin, canvasWidth - margin, topMargin, canvasHeight - margin);

        // ВСЕГДА рисуем шкалы (даже без данных)
        DrawScales(canvas, margin, topMargin, canvasWidth, canvasHeight, 10);

        // Если нет данных, прекращаем отрисовку
        if (results == null) results = new List<PingResult>();
        if (results.Count == 0) return;

        var successCount = results.Count(r => r.IsSuccess);
        var failCount = results.Count(r => !r.IsSuccess);
        var totalCount = results.Count;
        double barWidth = 60; // Фиксированная ширина столбиков

        // Высота доступной области для графика (полная высота)
        var availableHeight = canvasHeight - topMargin - margin;

        // Проверяем, что доступная высота положительна
        if (availableHeight <= 0) return;

        // Рассчитываем высоты столбиков (используем полную высоту без сжатия)
        var successHeight = totalCount > 0 ? availableHeight * successCount / totalCount : 0;
        var failHeight = totalCount > 0 ? availableHeight * failCount / totalCount : 0;

        // Убеждаемся, что высоты не отрицательны
        successHeight = Math.Max(0, successHeight);
        failHeight = Math.Max(0, failHeight);

        // Позиционируем столбцы по центру
        var centerX = canvasWidth / 2;
        double spacing = 100; // Расстояние между столбцами
        var successX = centerX - spacing / 2 - barWidth / 2; // Левый столбец
        var failX = centerX + spacing / 2 - barWidth / 2; // Правый столец
        var successY = canvasHeight - margin - successHeight;
        var failY = canvasHeight - margin - failHeight;

        // Создаем и рисуем зеленый столбец (успешные пинги)
        if (successHeight > 0)
        {
            var successRect = new Rectangle
            {
                Width = barWidth,
                Height = successHeight,
                Fill = Brushes.Green
            };
            Canvas.SetLeft(successRect, successX);
            Canvas.SetTop(successRect, successY);
            canvas.Children.Add(successRect);
        }

        // Создаем и рисуем красный столбец (неуспешные пинги)
        if (failHeight > 0)
        {
            var failRect = new Rectangle
            {
                Width = barWidth,
                Height = failHeight,
                Fill = Brushes.Red
            };
            Canvas.SetLeft(failRect, failX);
            Canvas.SetTop(failRect, failY);
            canvas.Children.Add(failRect);
        }

        // Рассчитываем проценты
        var successPercent = totalCount > 0 ? Math.Round((double)successCount / totalCount * 100, 1) : 0;
        var failPercent = totalCount > 0 ? Math.Round((double)failCount / totalCount * 100, 1) : 0;

        // Добавляем блоки со значениями НАД столбцами (внутри области графика)
        AddValueBlocks(canvas, successX, failX, barWidth, successY, failY, successCount, failCount);

        // Ярлыки под столбиками - строго по центру столбцов
        var successText = new TextBlock
        {
            Text = $"Успешно: {successPercent:F1}%",
            Foreground = Brushes.Black,
            FontSize = 10,
            FontWeight = FontWeights.Normal
        };
        // Центрируем ярлык строго по центру столбца
        Canvas.SetLeft(successText, successX + barWidth / 2 - MeasureTextWidth(successText.Text, successText) / 2);
        Canvas.SetTop(successText, canvasHeight - margin + 5); // Под столбиком
        canvas.Children.Add(successText);

        var failText = new TextBlock
        {
            Text = $"Не успешно: {failPercent:F1}%",
            Foreground = Brushes.Black,
            FontSize = 10,
            FontWeight = FontWeights.Normal
        };
        // Центрируем ярлык строго по центру столбца
        Canvas.SetLeft(failText, failX + barWidth / 2 - MeasureTextWidth(failText.Text, failText) / 2);
        Canvas.SetTop(failText, canvasHeight - margin + 5); // Под столбиком
        canvas.Children.Add(failText);

        // Рисуем шкалы еще раз с реальными значениями
        DrawScales(canvas, margin, topMargin, canvasWidth, canvasHeight, Math.Max(successCount, failCount));
    }

    private void AddValueBlocks(Canvas canvas, double successX, double failX, double barWidth,
        double successY, double failY, int successCount, int failCount)
    {
        // Блок со значением для зеленого столбца
        var successValueText = new TextBlock
        {
            Text = successCount.ToString(),
            Foreground = Brushes.White,
            FontSize = 10,
            FontWeight = FontWeights.Bold
        };
        // Измеряем ширину текста
        var successTextWidth = MeasureTextWidth(successValueText.Text, successValueText);
        var successTextHeight = MeasureTextHeight(successValueText.Text, successValueText);
        var successBlockWidth = Math.Max(successTextWidth + 10, 30); // Минимум 30 пикселей
        var successBlockHeight = Math.Max(successTextHeight + 6, 20); // Минимум 20 пикселей

        // Фоновый прямоугольник для значения зеленого столбца
        var successValueBlock = new Rectangle
        {
            Width = successBlockWidth,
            Height = successBlockHeight,
            Fill = Brushes.Green,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(successValueBlock, successX + barWidth / 2 - successBlockWidth / 2);
        Canvas.SetTop(successValueBlock, successY - successBlockHeight - 2); // Ближе к столбцу
        canvas.Children.Add(successValueBlock);

        // Текст значения зеленого столбца
        Canvas.SetLeft(successValueText, successX + barWidth / 2 - successTextWidth / 2);
        Canvas.SetTop(successValueText,
            successY - successBlockHeight + (successBlockHeight - successTextHeight) / 2 - 2);
        canvas.Children.Add(successValueText);

        // Блок со значением для красного столбца
        var failValueText = new TextBlock
        {
            Text = failCount.ToString(),
            Foreground = Brushes.White,
            FontSize = 10,
            FontWeight = FontWeights.Bold
        };
        // Измеряем ширину текста
        var failTextWidth = MeasureTextWidth(failValueText.Text, failValueText);
        var failTextHeight = MeasureTextHeight(failValueText.Text, failValueText);
        var failBlockWidth = Math.Max(failTextWidth + 10, 30); // Минимум 30 пикселей
        var failBlockHeight = Math.Max(failTextHeight + 6, 20); // Минимум 20 пикселей

        // Фоновый прямоугольник для значения красного столбца
        var failValueBlock = new Rectangle
        {
            Width = failBlockWidth,
            Height = failBlockHeight,
            Fill = Brushes.Red,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(failValueBlock, failX + barWidth / 2 - failBlockWidth / 2);
        Canvas.SetTop(failValueBlock, failY - failBlockHeight - 2); // Ближе к столбцу
        canvas.Children.Add(failValueBlock);

        // Текст значения красного столбца
        Canvas.SetLeft(failValueText, failX + barWidth / 2 - failTextWidth / 2);
        Canvas.SetTop(failValueText, failY - failBlockHeight + (failBlockHeight - failTextHeight) / 2 - 2);
        canvas.Children.Add(failValueText);
    }

    // Метод для измерения ширины текста
    private double MeasureTextWidth(string text, TextBlock textBlock)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
            textBlock.FontSize,
            Brushes.Black,
            new NumberSubstitution(),
            1.0);
        return formattedText.Width;
    }

    // Метод для измерения высоты текста
    private double MeasureTextHeight(string text, TextBlock textBlock)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
            textBlock.FontSize,
            Brushes.Black,
            new NumberSubstitution(),
            1.0);
        return formattedText.Height;
    }

    private void DrawGrid(Canvas canvas, double left, double right, double top, double bottom, int maxValue)
    {
        var width = right - left;
        var height = bottom - top;

        // Проверяем, что размеры положительные
        if (width <= 0 || height <= 0) return;

        // Точечная сетка - только 2 вертикальные линии для 2 столбцов
        var centerX = (left + right) / 2;
        double spacing = 100; // То же расстояние, что и между столбцами
        var successLineX = centerX - spacing / 2; // Линия для левого столбца
        var failLineX = centerX + spacing / 2; // Линия для правого столбца

        // Рисуем точки для левой вертикальной линии
        for (var y = top; y <= bottom; y += 4) // Точки каждые 4 пикселя
        {
            var dot = new Ellipse
            {
                Width = 1.5,
                Height = 1.5,
                Fill = Brushes.Gray
            };
            Canvas.SetLeft(dot, successLineX - 0.75);
            Canvas.SetTop(dot, y - 0.75);
            canvas.Children.Add(dot);
        }

        // Рисуем точки для правой вертикальной линии
        for (var y = top; y <= bottom; y += 4) // Точки каждые 4 пикселя
        {
            var dot = new Ellipse
            {
                Width = 1.5,
                Height = 1.5,
                Fill = Brushes.Gray
            };
            Canvas.SetLeft(dot, failLineX - 0.75);
            Canvas.SetTop(dot, y - 0.75);
            canvas.Children.Add(dot);
        }

        // Горизонтальные точки сетки
        var horizontalLines = 8;
        for (var i = 0; i <= horizontalLines; i++)
        {
            var y = top + i * height / horizontalLines;
            // Рисуем точки по горизонтали
            for (var x = left; x <= right; x += 4) // Точки каждые 4 пикселя
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

    private void DrawScales(Canvas canvas, double margin, double topMargin, double canvasWidth, double canvasHeight,
        int maxValue)
    {
        // Проверяем, что размеры положительные
        if (canvasWidth <= 0 || canvasHeight <= 0) return;

        var availableHeight = canvasHeight - topMargin - margin;
        if (availableHeight <= 0) return;

        var maxScale = Math.Max(10, (int)Math.Ceiling(Math.Max(maxValue, 5) / 5.0) * 5);
        var step = Math.Max(1, maxScale / 5);
        for (var i = 0; i <= maxScale; i += step)
        {
            var y = canvasHeight - margin - i * (canvasHeight - topMargin - margin) / Math.Max(maxScale, 1);
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
    }
}