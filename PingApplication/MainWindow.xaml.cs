using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using PingApp.Models;
using System.Linq;

namespace PingApp
{
    public partial class MainWindow : Window
    {
        // Данные для каждой вкладки
        private Dictionary<string, TabData> _tabData;
        private string _currentTab = "Ping1";
        private bool _isPinging = false;
        private DateTime _startTime;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTabData();
            InitializeHosts();
        }

        private void InitializeTabData()
        {
            _tabData = new Dictionary<string, TabData>
            {
                ["Ping1"] = new TabData { Hosts = new ObservableCollection<string>(), Results = new List<PingResult>() },
                ["Ping2"] = new TabData { Hosts = new ObservableCollection<string>(), Results = new List<PingResult>() },
                ["Ping3"] = new TabData { Hosts = new ObservableCollection<string>(), Results = new List<PingResult>() }
            };

            // Заполняем хосты для каждой вкладки
            foreach (var tab in _tabData.Values)
            {
                tab.Hosts.Add("yandex.ru");
                tab.Hosts.Add("google.com");
                tab.Hosts.Add("github.com");
            }
        }

        private void InitializeHosts()
        {
            HostComboBox.ItemsSource = _tabData[_currentTab].Hosts;
            HostComboBox.SelectedIndex = 0;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tabControl && tabControl.SelectedItem is TabItem selectedTab)
            {
                switch (selectedTab.Tag?.ToString())
                {
                    case "Settings":
                        PingContent.Visibility = Visibility.Collapsed;
                        SettingsContent.Visibility = Visibility.Visible;
                        break;
                    default:
                        PingContent.Visibility = Visibility.Visible;
                        SettingsContent.Visibility = Visibility.Collapsed;

                        // Обновляем текущую вкладку
                        _currentTab = selectedTab.Tag?.ToString() ?? "Ping1";
                        HostComboBox.ItemsSource = _tabData[_currentTab].Hosts;
                        HostComboBox.SelectedIndex = 0;

                        // Перерисовываем графики для новой вкладки
                        RedrawGraphs();
                        break;
                }
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPinging) return;

            string host = HostComboBox.Text;
            if (string.IsNullOrEmpty(host)) return;

            _isPinging = true;
            _startTime = DateTime.Now;
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;

            await StartPinging(host);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _isPinging = false;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }

        private async Task StartPinging(string host)
        {
            int packetSize = 32;
            int timeout = 5000;

            if (int.TryParse(PacketSizeTextBox.Text, out int size))
                packetSize = size;

            while (_isPinging)
            {
                try
                {
                    using (var ping = new Ping())
                    {
                        var buffer = new byte[packetSize];
                        var reply = await ping.SendPingAsync(host, timeout, buffer);

                        var result = new PingResult
                        {
                            Host = host,
                            Timestamp = DateTime.Now,
                            PacketSize = packetSize,
                            IsSuccess = reply.Status == IPStatus.Success,
                            RoundTripTime = reply.RoundtripTime,
                            IpAddress = reply.Address?.ToString(),
                            ErrorMessage = reply.Status != IPStatus.Success ? reply.Status.ToString() : null
                        };

                        Dispatcher.Invoke(() =>
                        {
                            _tabData[_currentTab].Results.Add(result);

                            if (_tabData[_currentTab].Results.Count > 100)
                                _tabData[_currentTab].Results.RemoveAt(0);

                            StatusTextBlock.Text = result.IsSuccess ?
                                $"Success: {result.RoundTripTime} ms from {result.IpAddress}" :
                                $"Failed: {result.ErrorMessage}";

                            TimeTextBlock.Text = $"Время: {(DateTime.Now - _startTime).ToString(@"mm\:ss")}";

                            // Рисуем графики
                            DrawGraphs(result);
                        });
                    }
                }
                catch (Exception ex)
                {
                    var result = new PingResult
                    {
                        Host = host,
                        Timestamp = DateTime.Now,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };

                    Dispatcher.Invoke(() =>
                    {
                        _tabData[_currentTab].Results.Add(result);
                        StatusTextBlock.Text = $"Error: {ex.Message}";
                    });
                }

                await Task.Delay(1000);
            }
        }

        private void DrawGraphs(PingResult result)
        {
            // Очищаем все графики
            MainPingCanvas.Children.Clear();
            ProgressCanvas.Children.Clear();
            StatisticsCanvas.Children.Clear();

            var results = _tabData[_currentTab].Results;
            if (results.Count == 0) return;

            // Рисуем основной график пинга
            DrawPingGraph(MainPingCanvas, results);

            // Рисуем график прогресса
            DrawProgressGraph(ProgressCanvas, results);

            // Рисуем график статистики
            DrawStatisticsGraph(StatisticsCanvas, results);
        }

        private void RedrawGraphs()
        {
            // Перерисовываем графики при смене вкладки
            MainPingCanvas.Children.Clear();
            ProgressCanvas.Children.Clear();
            StatisticsCanvas.Children.Clear();

            var results = _tabData[_currentTab].Results;
            if (results.Count == 0) return;

            // Рисуем основной график пинга
            DrawPingGraph(MainPingCanvas, results);

            // Рисуем график прогресса
            DrawProgressGraph(ProgressCanvas, results);

            // Рисуем график статистики
            DrawStatisticsGraph(StatisticsCanvas, results);
        }

        private void DrawPingGraph(Canvas canvas, List<PingResult> results)
        {
            if (results.Count == 0 || canvas.ActualWidth == 0 || canvas.ActualHeight == 0) return;

            var successfulResults = results.Where(r => r.IsSuccess).ToList();
            if (successfulResults.Count == 0) return;

            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            double margin = 40;

            // Находим минимум и максимум для масштабирования
            var times = successfulResults.Select(r => (double)r.RoundTripTime).ToList();
            if (times.Count == 0) return;

            double minTime = Math.Max(0, times.Min() - 5);
            double maxTime = times.Max() + 5;
            double timeRange = maxTime - minTime;
            if (timeRange == 0) timeRange = 1;

            // Рисуем белый фон
            var background = new Rectangle
            {
                Width = canvasWidth,
                Height = canvasHeight,
                Fill = Brushes.White
            };
            canvas.Children.Add(background);

            // Рисуем сетку
            DrawPingGrid(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin, successfulResults.Count, minTime, maxTime);

            // Рисуем четкие оси X и Y
            DrawAxes(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);

            // Рисуем линии графика
            for (int i = 1; i < successfulResults.Count; i++)
            {
                double x1 = margin + (i - 1) * (canvasWidth - 2 * margin) / Math.Max(successfulResults.Count - 1, 1);
                double y1 = canvasHeight - margin - (successfulResults[i - 1].RoundTripTime - minTime) * (canvasHeight - 2 * margin) / timeRange;

                double x2 = margin + i * (canvasWidth - 2 * margin) / Math.Max(successfulResults.Count - 1, 1);
                double y2 = canvasHeight - margin - (successfulResults[i].RoundTripTime - minTime) * (canvasHeight - 2 * margin) / timeRange;

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

            // Рисуем точки
            for (int i = 0; i < successfulResults.Count; i++)
            {
                double x = margin + i * (canvasWidth - 2 * margin) / Math.Max(successfulResults.Count - 1, 1);
                double y = canvasHeight - margin - (successfulResults[i].RoundTripTime - minTime) * (canvasHeight - 2 * margin) / timeRange;

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

            // Добавляем шкалы
            DrawPingGraphScales(canvas, margin, canvasWidth, canvasHeight, successfulResults.Count, minTime, maxTime);
        }

        private void DrawProgressGraph(Canvas canvas, List<PingResult> results)
        {
            if (results.Count == 0 || canvas.ActualWidth == 0 || canvas.ActualHeight == 0) return;

            var successfulResults = results.Where(r => r.IsSuccess).ToList();
            if (successfulResults.Count == 0) return;

            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            double margin = 40;

            // Вычисляем актуальную статистику
            var times = successfulResults.Select(r => (double)r.RoundTripTime).ToList();
            double minTime = Math.Max(0, times.Min() - 10);
            double maxTime = times.Max() + 10;
            double avgTime = times.Average();
            double currentTime = successfulResults.Last().RoundTripTime;
            double maxAllTime = times.Max();
            double minAllTime = times.Min();

            // Рисуем белый фон
            var background = new Rectangle
            {
                Width = canvasWidth,
                Height = canvasHeight,
                Fill = Brushes.White
            };
            canvas.Children.Add(background);

            // Рисуем сетку
            DrawProgressGrid(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin, minTime, maxTime);

            // Рисуем четкие оси X и Y
            DrawAxes(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);

            // Рисуем горизонтальные линии - ВСЕГДА перерисовываем их динамически
            double usableHeight = canvasHeight - 2 * margin;
            double lineHeightSpacing = usableHeight / 5;

            // Max линия (динамически обновляется)
            double maxY = margin + lineHeightSpacing;
            DrawHorizontalLineWithLabel(canvas, margin, canvasWidth - margin, maxY, Brushes.Red, $"Max: {maxAllTime:F0} мс");

            // Current линия (динамически обновляется)
            double currentY = margin + 2 * lineHeightSpacing;
            DrawHorizontalLineWithLabel(canvas, margin, canvasWidth - margin, currentY, Brushes.Blue, $"Current: {currentTime:F0} мс");

            // Average линия (динамически обновляется)
            double avgY = margin + 3 * lineHeightSpacing;
            DrawHorizontalLineWithLabel(canvas, margin, canvasWidth - margin, avgY, Brushes.Green, $"Average: {avgTime:F1} мс");

            // Min линия (динамически обновляется)
            double minY = margin + 4 * lineHeightSpacing;
            DrawHorizontalLineWithLabel(canvas, margin, canvasWidth - margin, minY, Brushes.Orange, $"Min: {minAllTime:F0} мс");

            // Добавляем шкалы
            DrawProgressGraphScales(canvas, margin, canvasWidth, canvasHeight, minTime, maxTime);
        }

        private void DrawStatisticsGraph(Canvas canvas, List<PingResult> results)
        {
            if (results.Count == 0 || canvas.ActualWidth == 0 || canvas.ActualHeight == 0) return;

            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            double margin = 40;

            int successCount = results.Count(r => r.IsSuccess);
            int failCount = results.Count(r => !r.IsSuccess);
            int totalCount = results.Count;

            // Рисуем белый фон
            var background = new Rectangle
            {
                Width = canvasWidth,
                Height = canvasHeight,
                Fill = Brushes.White
            };
            canvas.Children.Add(background);

            // Рисуем сетку
            DrawStatisticsGrid(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin, Math.Max(successCount, failCount));

            // Рисуем четкие оси X и Y
            DrawAxes(canvas, margin, canvasWidth - margin, margin, canvasHeight - margin);

            // Рисуем столбчатую диаграмму
            double barWidth = (canvasWidth - 3 * margin) / 3;

            // Успешные пинги
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

            // Неуспешные пинги
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

            // Добавляем подписи
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

            // Добавляем шкалы
            DrawStatisticsGraphScales(canvas, margin, canvasWidth, canvasHeight, Math.Max(successCount, failCount));
        }

        private void DrawAxes(Canvas canvas, double left, double right, double top, double bottom)
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
            // Стрелка на оси X
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

            // Стрелка на оси Y
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

        private void DrawPingGrid(Canvas canvas, double left, double right, double top, double bottom, int pingCount, double minTime, double maxTime)
        {
            // Вертикальные линии сетки с шагом по пингам
            double width = right - left;
            int maxLines = Math.Min(20, pingCount); // Ограничиваем количество линий
            int step = Math.Max(1, pingCount / maxLines);

            for (int i = 0; i <= pingCount; i += step)
            {
                if (pingCount > 0)
                {
                    double x = left + i * width / Math.Max(pingCount, 1);
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
            }

            // Горизонтальные линии сетки по времени
            double height = bottom - top;
            int timeSteps = 8;

            for (int i = 0; i <= timeSteps; i++)
            {
                double y = top + i * height / timeSteps;
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

        private void DrawProgressGrid(Canvas canvas, double left, double right, double top, double bottom, double minTime, double maxTime)
        {
            // Вертикальные линии сетки по времени
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

            // Горизонтальные линии сетки
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

        private void DrawStatisticsGrid(Canvas canvas, double left, double right, double top, double bottom, int maxValue)
        {
            // Вертикальные линии сетки (2 столбца)
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

            // Горизонтальные линии сетки по количеству
            double height = bottom - top;
            int maxScale = (int)Math.Ceiling(Math.Max(maxValue, 5) / 5.0) * 5;
            if (maxScale == 0) maxScale = 5;
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

        private void DrawHorizontalLineWithLabel(Canvas canvas, double x1, double x2, double y, Brush color, string label)
        {
            var line = new Line
            {
                X1 = x1,
                Y1 = y,
                X2 = x2,
                Y2 = y,
                Stroke = color,
                StrokeThickness = 2
            };
            canvas.Children.Add(line);

            // Добавляем подпись
            var text = new TextBlock
            {
                Text = label,
                Foreground = color,
                FontSize = 12
            };
            Canvas.SetLeft(text, x2 - 120);
            Canvas.SetTop(text, y - 15);
            canvas.Children.Add(text);
        }

        private void DrawPingGraphScales(Canvas canvas, double margin, double canvasWidth, double canvasHeight, int pingCount, double minTime, double maxTime)
        {
            // Шкала по оси X (количество пингов)
            int step = Math.Max(1, pingCount / 10);
            for (int i = 0; i <= pingCount; i += step)
            {
                double x = margin + i * (canvasWidth - 2 * margin) / Math.Max(pingCount, 1);

                var tick = new Line
                {
                    X1 = x,
                    Y1 = canvasHeight - margin,
                    X2 = x,
                    Y2 = canvasHeight - margin + 5,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2 // Более толстые деления
                };
                canvas.Children.Add(tick);

                var text = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(text, x - 5);
                Canvas.SetTop(text, canvasHeight - margin + 8);
                canvas.Children.Add(text);
            }

            // Шкала по оси Y (время в мс)
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
                    StrokeThickness = 2 // Более толстые деления
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

            // Подписи осей
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

        private void DrawProgressGraphScales(Canvas canvas, double margin, double canvasWidth, double canvasHeight, double minTime, double maxTime)
        {
            // Шкала по оси X (время в мс)
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
                    StrokeThickness = 2 // Более толстые деления
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

            // Подписи осей
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

        private void DrawStatisticsGraphScales(Canvas canvas, double margin, double canvasWidth, double canvasHeight, int maxValue)
        {
            // Шкала по оси Y (количество пингов)
            int maxScale = (int)Math.Ceiling(Math.Max(maxValue, 5) / 5.0) * 5;
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
                    StrokeThickness = 2 // Более толстые деления
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

            // Подписи осей и столбцов
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

            // Подписи столбцов
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

    // Вспомогательный класс для хранения данных каждой вкладки
    public class TabData
    {
        public ObservableCollection<string> Hosts { get; set; }
        public List<PingResult> Results { get; set; }
    }
}