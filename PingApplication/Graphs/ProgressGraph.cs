using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PingApp.Models;

namespace PingApp.Graphs;

public class ProgressGraph : GraphBase
{
    private double _lastAvgValue;

    private double _lastCurrentValue;

    // Храним последние значения для легенды
    private double _lastMaxValue;
    private double _lastMinValue;

    public void Draw(Canvas canvas, List<PingResult> results)
    {
        var canvasWidth = canvas.ActualWidth;
        var canvasHeight = canvas.ActualHeight;
        double labelAreaRightEdge = 100; // Правый край области ярлыков - фиксированная позиция
        double rightMargin = 160; // Отступ для легенды
        double topMargin = 20; // Уменьшенный верхний отступ для большей высоты графика
        double bottomMargin = 40; // Уменьшенный нижний отступ для большей высоты графика
        double legendWidth = 120; // Ширина блока легенды
        var graphWidth = canvasWidth - labelAreaRightEdge - rightMargin; // Ширина графика
        canvas.Children.Clear();
        DrawBackground(canvas, canvasWidth, canvasHeight);
        // Рисуем сетку даже если нет данных
        DrawGrid(canvas, labelAreaRightEdge, labelAreaRightEdge + graphWidth, topMargin, canvasHeight - bottomMargin, 0,
            100);
        DrawAxes(canvas, labelAreaRightEdge, labelAreaRightEdge + graphWidth, topMargin, canvasHeight - bottomMargin);
        // Рисуем блок легенды (центрирован по вертикали)
        double legendH = 100;
        var legendT = (canvasHeight - legendH) / 2; // ЦЕНТРИРОВАНИЕ ПО ВЕРТИКАЛИ
        DrawLegendBox(canvas, canvasWidth - rightMargin + 10, legendT, legendWidth, legendH);
        if (results == null || results.Count == 0) return;
        var successfulResults = results.Where(r => r.IsSuccess).ToList();
        if (successfulResults.Count == 0) return;
        var times = successfulResults.Select(r => (double)r.RoundTripTime).ToList();
        var minTime = Math.Max(0, times.Min() - 10);
        var maxTime = times.Max() + 10;
        double currentTime = successfulResults.Last().RoundTripTime;
        var maxAllTime = times.Count > 0 ? times.Max() : 0;
        var minAllTime = times.Count > 0 ? times.Min() : 0;
        var avgTime = times.Count > 0 ? times.Average() : 0;
        // Сохраняем значения для легенды
        _lastMaxValue = maxAllTime;
        _lastMinValue = minAllTime;
        _lastAvgValue = avgTime;
        _lastCurrentValue = currentTime;
        var usableHeight = canvasHeight - topMargin - bottomMargin;
        var lineHeightSpacing = usableHeight / 5; // Используем всю доступную высоту
        // Порядок линий (сверху вниз):
        // 1. Макс. (красный)
        var maxY = topMargin + lineHeightSpacing;
        DrawProgressLine(canvas, labelAreaRightEdge, labelAreaRightEdge + graphWidth, maxY, maxAllTime, minTime,
            maxTime, Brushes.Red, "Макс.", maxAllTime, labelAreaRightEdge);
        // 2. Мин. (оранжевый)
        var minY = topMargin + 2 * lineHeightSpacing;
        DrawProgressLine(canvas, labelAreaRightEdge, labelAreaRightEdge + graphWidth, minY, minAllTime, minTime,
            maxTime, Brushes.Orange, "Мин.", minAllTime, labelAreaRightEdge);
        // 3. Среднее. (зеленый)
        var avgY = topMargin + 3 * lineHeightSpacing;
        DrawProgressLine(canvas, labelAreaRightEdge, labelAreaRightEdge + graphWidth, avgY, avgTime, minTime, maxTime,
            Brushes.Green, "Среднее.", avgTime, labelAreaRightEdge);
        // 4. Текущее. (синий)
        var currentY = topMargin + 4 * lineHeightSpacing;
        DrawProgressLine(canvas, labelAreaRightEdge, labelAreaRightEdge + graphWidth, currentY, currentTime, minTime,
            maxTime, Brushes.Blue, "Текущее.", currentTime, labelAreaRightEdge);
        // Рисуем только шкалу по оси X (без оси Y)
        DrawXScales(canvas, labelAreaRightEdge, labelAreaRightEdge + graphWidth, canvasWidth, canvasHeight, topMargin,
            bottomMargin, minTime, maxTime);
    }

    private void DrawGrid(Canvas canvas, double left, double right, double top, double bottom, double minTime,
        double maxTime)
    {
        var gridWidth = right - left;
        var gridHeight = bottom - top;
        // Рисуем вертикальную точечную сетку (привязана к значениям оси X)
        var xSteps = 10; // Соответствует 11 отметкам на оси X
        for (var i = 0; i <= xSteps; i++)
        {
            var x = left + i * gridWidth / xSteps;
            // Рисуем точки по всей вертикали
            for (var y = top; y <= bottom; y += 4) // Точки каждые 4 пикселя
            {
                var dot = new Ellipse
                {
                    Width = 1.5, // Увеличен размер точек
                    Height = 1.5,
                    Fill = Brushes.Gray // Более темный цвет вместо LightGray
                };
                Canvas.SetLeft(dot, x - 0.75);
                Canvas.SetTop(dot, y - 0.75);
                canvas.Children.Add(dot);
            }
        }

        // Рисуем горизонтальную точечную сетку (только для 4 линий графика)
        // Позиции 4 линий графика
        double[] linePositions = { 0.2, 0.4, 0.6, 0.8 }; // Относительные позиции
        foreach (var relativePos in linePositions)
        {
            var y = top + relativePos * gridHeight;
            // Рисуем точки по горизонтали
            for (var x = left; x <= right; x += 4) // Точки каждые 4 пикселя
            {
                var dot = new Ellipse
                {
                    Width = 1.5, // Увеличен размер точек
                    Height = 1.5,
                    Fill = Brushes.Gray // Более темный цвет вместо LightGray
                };
                Canvas.SetLeft(dot, x - 0.75);
                Canvas.SetTop(dot, y - 0.75);
                canvas.Children.Add(dot);
            }
        }
    }

    private void DrawProgressLine(Canvas canvas, double left, double right, double y, double value, double minValue,
        double maxValue, Brush color, string label, double displayValue, double labelAreaRightEdge)
    {
        var width = right - left;
        var valueRange = Math.Max(maxValue - minValue, 100); // Минимум 100 для начального отображения
        // Рассчитываем позицию точки в соответствии со значением
        var xPosition = left;
        if (valueRange > 0)
        {
            xPosition = left + (value - minValue) / valueRange * width;
            xPosition = Math.Max(left, Math.Min(right, xPosition)); // Ограничиваем границами
        }

        // Рисуем горизонтальную линию
        var line = new Line
        {
            X1 = left,
            Y1 = y,
            X2 = xPosition, // Линия заканчивается на позиции значения
            Y2 = y,
            Stroke = color,
            StrokeThickness = 3
        };
        canvas.Children.Add(line);
        // Рисуем маркер значения на конце линии
        var marker = new Ellipse
        {
            Width = 8,
            Height = 8,
            Stroke = color,
            StrokeThickness = 2,
            Fill = Brushes.White
        };
        Canvas.SetLeft(marker, xPosition - 4);
        Canvas.SetTop(marker, y - 4);
        canvas.Children.Add(marker);
        // Добавляем ярлык слева, выровненный по правому краю своей области
        var labelText = new TextBlock
        {
            Text = label,
            Foreground = color,
            FontSize = 11,
            FontWeight = FontWeights.Bold
        };
        // ВСЕ ярлыки выровнены по правому краю в одной позиции
        Canvas.SetLeft(labelText, labelAreaRightEdge - MeasureTextWidth(label) - 10); // Выровнен по правому краю
        Canvas.SetTop(labelText, y - 7);
        canvas.Children.Add(labelText);
        // Добавляем числовое значение на конце линии
        var valueText = new TextBlock
        {
            Text = $"{displayValue:F0} ms",
            Foreground = color,
            FontSize = 10,
            FontWeight = FontWeights.Bold
        };
        Canvas.SetLeft(valueText, xPosition + 8);
        Canvas.SetTop(valueText, y - 7);
        canvas.Children.Add(valueText);
    }

    // Метод для измерения ширины текста
    private double MeasureTextWidth(string text)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            11,
            Brushes.Black,
            new NumberSubstitution(),
            1.0);
        return formattedText.Width;
    }

    private void DrawLegendBox(Canvas canvas, double left, double top, double width, double height)
    {
        // Рисуем рамку легенды (центрирована по вертикали)
        var border = new Rectangle
        {
            Width = width,
            Height = height,
            Stroke = Brushes.Gray,
            StrokeThickness = 1,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(border, left);
        Canvas.SetTop(border, top); // УЖЕ ЦЕНТРИРОВАНО: top = (canvasHeight - height) / 2
        canvas.Children.Add(border);
        // Добавляем элементы легенды с равномерным распределением
        var itemCount = 4; // Количество элементов
        var itemHeight = (height - 20) / itemCount; // Больше отступов
        // 1. Макс. (красный) - [■] значение ярлык
        DrawLegendItem(canvas, left + 8, top + 10, Brushes.Red, _lastMaxValue, "Макс.", itemHeight);
        // 2. Мин. (оранжевый) - [■] значение ярлык
        DrawLegendItem(canvas, left + 8, top + 10 + itemHeight, Brushes.Orange, _lastMinValue, "Мин.", itemHeight);
        // 3. Среднее. (зеленый) - [■] значение ярлык
        DrawLegendItem(canvas, left + 8, top + 10 + 2 * itemHeight, Brushes.Green, _lastAvgValue, "Сред.", itemHeight);
        // 4. Текущее. (синий) - [■] значение ярлык
        DrawLegendItem(canvas, left + 8, top + 10 + 3 * itemHeight, Brushes.Blue, _lastCurrentValue, "Текущ.",
            itemHeight);
    }

    private void DrawLegendItem(Canvas canvas, double left, double top, Brush color, double value, string label,
        double itemHeight)
    {
        // Цветной квадратик
        var colorBox = new Rectangle
        {
            Width = 12, // Увеличен размер квадратика
            Height = 12,
            Fill = color,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(colorBox, left);
        Canvas.SetTop(colorBox, top + (itemHeight - 12) / 2); // Центрируем по вертикали
        canvas.Children.Add(colorBox);
        // Числовое значение
        var valueText = new TextBlock
        {
            Text = $"{value:F0} ms",
            Foreground = Brushes.Black,
            FontSize = 11, // Увеличен шрифт с 9 до 11
            VerticalAlignment = VerticalAlignment.Center
        };
        Canvas.SetLeft(valueText, left + 20); // Увеличен отступ
        Canvas.SetTop(valueText, top + (itemHeight - 14) / 2); // Центрируем по вертикали
        canvas.Children.Add(valueText);
        // Текст ярлыка
        var labelText = new TextBlock
        {
            Text = label,
            Foreground = Brushes.Black,
            FontSize = 11, // Увеличен шрифт с 9 до 11
            VerticalAlignment = VerticalAlignment.Center
        };
        Canvas.SetLeft(labelText, left + 70); // Увеличен отступ
        Canvas.SetTop(labelText, top + (itemHeight - 14) / 2); // Центрируем по вертикали
        canvas.Children.Add(labelText);
    }

    // Новый метод для рисования только шкалы по оси X
    private void DrawXScales(Canvas canvas, double left, double right, double canvasWidth, double canvasHeight,
        double topMargin, double bottomMargin, double minTime, double maxTime)
    {
        // Шкала по оси X (время) - ЦЕНТРИРОВАНИЕ ТЕКСТА ОТНОСИТЕЛЬНО РИСОК
        for (var i = 0; i <= 10; i++)
        {
            var timeValue = minTime + i * (maxTime - minTime) / 10;
            var x = left + i * (right - left) / 10;
            var tick = new Line
            {
                X1 = x,
                Y1 = canvasHeight - bottomMargin,
                X2 = x,
                Y2 = canvasHeight - bottomMargin + 5,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            canvas.Children.Add(tick);
            var text = new TextBlock
            {
                Text = Math.Round(timeValue).ToString(),
                FontSize = 9,
                Foreground = Brushes.Black
            };
            // ЦЕНТРИРОВАНИЕ ТЕКСТА ОТНОСИТЕЛЬНО РИСКИ
            var formattedText = new FormattedText(
                text.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch),
                text.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1.0);
            var textWidth = formattedText.Width;
            Canvas.SetLeft(text, x - textWidth / 2); // Центрируем текст относительно риски
            Canvas.SetTop(text, canvasHeight - bottomMargin + 8);
            canvas.Children.Add(text);
        }
    }
}