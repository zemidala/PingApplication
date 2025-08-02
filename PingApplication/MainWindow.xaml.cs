using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using PingApp.Graphs;
using PingApp.Models;

namespace PingApp;

public partial class MainWindow : Window
{
    private string _currentTab = "Ping1";
    private bool _isPinging;

    // Графики
    private PingGraph _pingGraph;
    private ProgressGraph _progressGraph;
    private DateTime _startTime;

    private StatisticsGraph _statisticsGraph;

    // Данные для каждой вкладки
    private Dictionary<string, TabData> _tabData;

    public MainWindow()
    {
        InitializeComponent();
        InitializeGraphs();
        InitializeTabData();
        InitializeHosts();
    }

    private void InitializeGraphs()
    {
        _pingGraph = new PingGraph();
        _progressGraph = new ProgressGraph();
        _statisticsGraph = new StatisticsGraph();
    }

    private void InitializeTabData()
    {
        _tabData = new Dictionary<string, TabData>
        {
            ["Ping1"] = new() { Hosts = new ObservableCollection<string>(), Results = new List<PingResult>() },
            ["Ping2"] = new() { Hosts = new ObservableCollection<string>(), Results = new List<PingResult>() },
            ["Ping3"] = new() { Hosts = new ObservableCollection<string>(), Results = new List<PingResult>() }
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
                    HostComboBox.ItemsSource = _tabData[_currentTab].Hosts;
                    HostComboBox.SelectedIndex = 0;

                    RedrawGraphs();
                    break;
            }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPinging) return;

        var host = HostComboBox.Text;
        if (string.IsNullOrEmpty(host)) return;

        _isPinging = true;
        _startTime = DateTime.Now;
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;

        // Обновляем отображаемый адрес
        CurrentHostTextBlock.Text = $"{host}";

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
        var packetSize = 32;
        var timeout = 5000;

        if (int.TryParse(PacketSizeTextBox.Text, out var size))
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

                        StatusTextBlock.Text = result.IsSuccess
                            ? $"Success: {result.RoundTripTime} ms from {result.IpAddress}"
                            : $"Failed: {result.ErrorMessage}";

                        TimeTextBlock.Text = $"Время: {(DateTime.Now - _startTime).ToString(@"mm\:ss")}";

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
        var results = _tabData[_currentTab].Results;
        if (results.Count == 0) return;

        _pingGraph.Draw(MainPingCanvas, results);
        _progressGraph.Draw(ProgressCanvas, results);
        _statisticsGraph.Draw(StatisticsCanvas, results);
    }

    private void RedrawGraphs()
    {
        var results = _tabData[_currentTab].Results;
        if (results.Count == 0) return;

        _pingGraph.Draw(MainPingCanvas, results);
        _progressGraph.Draw(ProgressCanvas, results);
        _statisticsGraph.Draw(StatisticsCanvas, results);
    }
}

public class TabData
{
    public ObservableCollection<string> Hosts { get; set; }
    public List<PingResult> Results { get; set; }
}