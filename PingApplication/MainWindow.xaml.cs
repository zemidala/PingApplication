using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PingApp.Graphs;
using PingApp.Models;

namespace PingApp;

public partial class MainWindow : Window
{
    private string _currentTab = "Ping1";
    private bool _isPinging;

    // Словари для хранения состояния пинга каждой вкладки
    private Dictionary<string, bool> _isPingingDict;
    private Dictionary<string, DateTime> _startTimes;

    // Данные для каждой вкладки
    private Dictionary<string, TabData> _tabData;

    // Таймеры для каждой вкладки
    private Dictionary<string, DispatcherTimer> _timers;

    public MainWindow()
    {
        InitializeComponent();
        InitializeTabData();
        InitializeHosts();
        InitializePingingStates();

        // Инициализируем графики для текущей вкладки при запуске
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Инициализируем пустые графики для всех вкладок после загрузки окна
        InitializeAllGraphsWithGrids();
    }

    private void InitializeTabData()
    {
        _tabData = new Dictionary<string, TabData>
        {
            ["Ping1"] = new()
            {
                Hosts = new ObservableCollection<string>(),
                Results = new List<PingResult>(),
                PingGraph = new PingGraph(),
                ProgressGraph = new ProgressGraph(),
                StatisticsGraph = new StatisticsGraph()
            },
            ["Ping2"] = new()
            {
                Hosts = new ObservableCollection<string>(),
                Results = new List<PingResult>(),
                PingGraph = new PingGraph(),
                ProgressGraph = new ProgressGraph(),
                StatisticsGraph = new StatisticsGraph()
            },
            ["Ping3"] = new()
            {
                Hosts = new ObservableCollection<string>(),
                Results = new List<PingResult>(),
                PingGraph = new PingGraph(),
                ProgressGraph = new ProgressGraph(),
                StatisticsGraph = new StatisticsGraph()
            }
        };

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

    private void InitializePingingStates()
    {
        _isPingingDict = new Dictionary<string, bool>
        {
            ["Ping1"] = false,
            ["Ping2"] = false,
            ["Ping3"] = false
        };

        _startTimes = new Dictionary<string, DateTime>
        {
            ["Ping1"] = DateTime.Now,
            ["Ping2"] = DateTime.Now,
            ["Ping3"] = DateTime.Now
        };

        _timers = new Dictionary<string, DispatcherTimer>();
    }

    private void InitializeAllGraphsWithGrids()
    {
        // Сохраняем текущую вкладку
        var originalTab = _currentTab;

        // Инициализируем графики для всех вкладок
        foreach (var tabName in new[] { "Ping1", "Ping2", "Ping3" })
        {
            var tabData = _tabData[tabName];
            var results = new List<PingResult>(); // Пустой список

            // Временно устанавливаем текущую вкладку для корректной отрисовки
            _currentTab = tabName;

            // Инициализируем графики с пустыми данными (они нарисуют сетку и оси)
            tabData.PingGraph.Draw(MainPingCanvas, results);
            tabData.ProgressGraph.Draw(ProgressCanvas, results);
            tabData.StatisticsGraph.Draw(StatisticsCanvas, results);
        }

        // Восстанавливаем оригинальную вкладку
        _currentTab = originalTab;

        // Перерисовываем графики для текущей вкладки
        RedrawGraphs();
    }

    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tabControl && tabControl.SelectedItem is TabItem selectedTab)
            switch (selectedTab.Tag?.ToString())
            {
                case "Settings":
                    PingContent.Visibility = Visibility.Collapsed;
                    SettingsContent.Visibility = Visibility.Visible;
                    break;
                default:
                    PingContent.Visibility = Visibility.Visible;
                    SettingsContent.Visibility = Visibility.Collapsed;

                    _currentTab = selectedTab.Tag?.ToString() ?? "Ping1";

                    // Обновляем источник данных для ComboBox
                    HostComboBox.ItemsSource = _tabData[_currentTab].Hosts;
                    HostComboBox.SelectedIndex = 0;

                    // Обновляем состояние кнопок
                    UpdateButtonStates();

                    // Обновляем отображаемый адрес хоста
                    UpdateCurrentHostText();

                    // Перерисовываем графики для новой вкладки
                    RedrawGraphs();
                    break;
            }
    }

    private void UpdateButtonStates()
    {
        var isPinging = _isPingingDict[_currentTab];
        StartButton.IsEnabled = !isPinging;
        StopButton.IsEnabled = isPinging;
    }

    private void UpdateCurrentHostText()
    {
        var currentTabData = _tabData[_currentTab];
        var hostText = "График пинга";

        if (currentTabData.Results.Count > 0)
        {
            var lastResult = currentTabData.Results.LastOrDefault();
            if (lastResult != null)
                hostText = $"{lastResult.Host}";
        }

        CurrentHostTextBlock.Text = hostText;
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPingingDict[_currentTab]) return;

        var host = HostComboBox.Text;
        if (string.IsNullOrEmpty(host)) return;

        _isPingingDict[_currentTab] = true;
        _startTimes[_currentTab] = DateTime.Now;

        UpdateButtonStates();

        // Обновляем отображаемый адрес
        CurrentHostTextBlock.Text = $"{host}";

        await StartPinging(_currentTab, host, _startTimes[_currentTab]);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _isPingingDict[_currentTab] = false;
        UpdateButtonStates();

        // Останавливаем таймер, если он есть
        if (_timers.ContainsKey(_currentTab))
        {
            _timers[_currentTab].Stop();
            _timers.Remove(_currentTab);
        }
    }

    private async Task StartPinging(string tabName, string host, DateTime startTime)
    {
        var packetSize = 32;
        var timeout = 5000;

        if (int.TryParse(PacketSizeTextBox.Text, out var size))
            packetSize = size;

        // Создаем таймер для периодического пинга
        var timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        _timers[tabName] = timer;

        timer.Tick += async (s, e) =>
        {
            if (!_isPingingDict[tabName]) return;

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

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (_tabData.ContainsKey(tabName))
                        {
                            _tabData[tabName].Results.Add(result);

                            // Ограничиваем количество результатов для оптимизации памяти
                            if (_tabData[tabName].Results.Count > 1000)
                                _tabData[tabName].Results.RemoveAt(0);

                            UpdateStatusText(result);
                            UpdateTimeText(startTime);

                            // Рисуем графики только если это текущая вкладка
                            if (_currentTab == tabName) DrawGraphs(result);
                        }
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_tabData.ContainsKey(tabName))
                    {
                        _tabData[tabName].Results.Add(result);
                        UpdateStatusText(result);

                        // Рисуем графики только если это текущая вкладка
                        if (_currentTab == tabName) DrawGraphs(result);
                    }
                });
            }
        };

        // Запускаем первый пинг сразу
        timer.Start();
        timer_Tick(timer, EventArgs.Empty);
    }

    private async void timer_Tick(object sender, EventArgs e)
    {
        // Этот метод будет вызван через await для первого пинга
    }

    private void UpdateStatusText(PingResult result)
    {
        var statusText = result.IsSuccess
            ? $"Success: {result.RoundTripTime} ms from {result.IpAddress}"
            : $"Failed: {result.ErrorMessage}";

        StatusTextBlock.Text = statusText;
    }

    private void UpdateTimeText(DateTime startTime)
    {
        var timeText = $"Время: {(DateTime.Now - startTime).ToString(@"mm\:ss")}";
        TimeTextBlock.Text = timeText;
    }

    private void DrawGraphs(PingResult result)
    {
        var currentTabData = _tabData[_currentTab];
        var results = currentTabData.Results;

        currentTabData.PingGraph.Draw(MainPingCanvas, results);
        currentTabData.ProgressGraph.Draw(ProgressCanvas, results);
        currentTabData.StatisticsGraph.Draw(StatisticsCanvas, results);
    }

    private void RedrawGraphs()
    {
        var currentTabData = _tabData[_currentTab];
        var results = currentTabData.Results;

        currentTabData.PingGraph.Draw(MainPingCanvas, results);
        currentTabData.ProgressGraph.Draw(ProgressCanvas, results);
        currentTabData.StatisticsGraph.Draw(StatisticsCanvas, results);
    }
}

public class TabData
{
    public ObservableCollection<string> Hosts { get; set; }

    public List<PingResult> Results { get; set; }

    // Новые свойства для графиков
    public PingGraph PingGraph { get; set; }
    public ProgressGraph ProgressGraph { get; set; }
    public StatisticsGraph StatisticsGraph { get; set; }
}