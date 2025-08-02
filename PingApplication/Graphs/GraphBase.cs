using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PingApp.Graphs
{
    public abstract class GraphBase
    {
        protected void DrawAxes(Canvas canvas, double left, double right, double top, double bottom)
        {
            // Ось X (жирная линия)
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

            // Ось Y (жирная линия)
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
            var background = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = Brushes.White
            };
            canvas.Children.Add(background);
        }
    }
}