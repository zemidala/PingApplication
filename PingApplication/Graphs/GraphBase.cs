using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PingApp.Graphs;

public abstract class GraphBase
{
    protected void DrawAxes(Canvas canvas, double left, double right, double top, double bottom)
    {
        // Проверяем корректность параметров
        if (left >= right || top >= bottom || left < 0 || top < 0) return;

        // Ось X (жирная линия) - горизонтальная линия внизу
        var xAxis = new Line
        {
            X1 = left,
            Y1 = bottom,
            X2 = right,
            Y2 = bottom,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        canvas.Children.Add(xAxis);

        // Ось Y (жирная линия) - вертикальная линия слева
        var yAxis = new Line
        {
            X1 = left,
            Y1 = top,
            X2 = left,
            Y2 = bottom,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        canvas.Children.Add(yAxis);

        // Стрелки на осях
        // Стрелка для оси X (вправо)
        var arrowX1 = new Line
        {
            X1 = right - 10,
            Y1 = bottom - 5,
            X2 = right,
            Y2 = bottom,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        canvas.Children.Add(arrowX1);

        var arrowX2 = new Line
        {
            X1 = right - 10,
            Y1 = bottom + 5,
            X2 = right,
            Y2 = bottom,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        canvas.Children.Add(arrowX2);

        // Стрелка для оси Y (вверх)
        var arrowY1 = new Line
        {
            X1 = left - 5,
            Y1 = top + 10,
            X2 = left,
            Y2 = top,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        canvas.Children.Add(arrowY1);

        var arrowY2 = new Line
        {
            X1 = left + 5,
            Y1 = top + 10,
            X2 = left,
            Y2 = top,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        canvas.Children.Add(arrowY2);
    }

    protected void DrawBackground(Canvas canvas, double width, double height)
    {
        // Проверяем корректность размеров
        if (width <= 0 || height <= 0) return;

        var background = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = Brushes.White
        };
        canvas.Children.Add(background);
    }
}