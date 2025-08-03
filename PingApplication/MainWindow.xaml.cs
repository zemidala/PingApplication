using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using PingApp.Graphs;
using PingApp.Models;
using PingApp.ViewModels;

namespace PingApp;

public partial class MainWindow : Window
{
    // ViewModel для настроек
    private readonly SettingsViewModel _settingsViewModel;
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
        _settingsViewModel = new SettingsViewModel();
        _settingsViewModel.PropertyChanged += SettingsViewModel_PropertyChanged;
        InitializeTabData();
        InitializeHosts();
        InitializePingingStates();
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
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

            // Инициализируем графики с пустыми данными
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

    // Методы для работы с настройками
    private void SettingsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeViewItem item) ShowSettingsPanel(item.Tag?.ToString());
    }

    private void ShowSettingsPanel(string category)
    {
        SettingsPanel.Children.Clear();

        switch (category)
        {
            case "Startup":
                SettingsTitle.Text = "Запуск и закрытие программы";
                ShowStartupSettings();
                break;
            case "Shortcuts":
                SettingsTitle.Text = "Ярлыки";
                ShowShortcutsSettings();
                break;
            case "PingPanel":
                SettingsTitle.Text = "Панель пингования";
                ShowPingPanelSettings();
                break;
            case "PingConfig":
                SettingsTitle.Text = "Настройки пингования";
                ShowPingConfigSettings();
                break;
            case "PingList":
                SettingsTitle.Text = "Список пингования";
                ShowPingListSettings();
                break;
            case "Logs":
                SettingsTitle.Text = "Логи";
                ShowLogsSettings();
                break;
            default:
                SettingsTitle.Text = "Настройки";
                break;
        }
    }

    private void ShowStartupSettings()
    {
        // Создаем стек панель для настроек запуска
        var startupPanel = new StackPanel();

        // Старт программы вместе с Windows
        var autoStartPanel = new StackPanel
            { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 15) };
        var autoStartCheckBox = new CheckBox
        {
            Content = "Старт программы вместе с Windows (Автозагрузка)",
            IsChecked = _settingsViewModel.AutoStartWindows,
            Margin = new Thickness(0, 0, 10, 0)
        };
        autoStartCheckBox.Checked += (s, e) => _settingsViewModel.AutoStartWindows = true;
        autoStartCheckBox.Unchecked += (s, e) => _settingsViewModel.AutoStartWindows = false;
        autoStartPanel.Children.Add(autoStartCheckBox);
        startupPanel.Children.Add(autoStartPanel);

        // При старте, сворачивать в "трей"
        var minimizeToTrayPanel = new StackPanel
            { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 15) };
        var minimizeToTrayCheckBox = new CheckBox
        {
            Content = "При старте, сворачивать в \"трей\" (к часам)",
            IsChecked = _settingsViewModel.MinimizeOnStart,
            Margin = new Thickness(0, 0, 10, 0)
        };
        minimizeToTrayCheckBox.Checked += (s, e) => _settingsViewModel.MinimizeOnStart = true;
        minimizeToTrayCheckBox.Unchecked += (s, e) => _settingsViewModel.MinimizeOnStart = false;
        minimizeToTrayPanel.Children.Add(minimizeToTrayCheckBox);
        startupPanel.Children.Add(minimizeToTrayPanel);

        // При нажатии кнопку "Закрыть", сворачивать в "Трей"
        var closeToTrayPanel = new StackPanel
            { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 15) };
        var closeToTrayCheckBox = new CheckBox
        {
            Content = "При нажатии кнопку \"Закрыть\" (Х), сворачивать в \"Трей\" (к часам)",
            IsChecked = _settingsViewModel.ConfirmExit,
            Margin = new Thickness(0, 0, 10, 0)
        };
        closeToTrayCheckBox.Checked += (s, e) => _settingsViewModel.ConfirmExit = true;
        closeToTrayCheckBox.Unchecked += (s, e) => _settingsViewModel.ConfirmExit = false;
        closeToTrayPanel.Children.Add(closeToTrayCheckBox);
        startupPanel.Children.Add(closeToTrayPanel);

        SettingsPanel.Children.Add(startupPanel);
    }

    private void ShowShortcutsSettings()
    {
        // Создаем бордер для секции настроек
        var border = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Создаем стек панель для настроек ярлыков
        var shortcutsPanel = new StackPanel();

        // Поместить ярлык в меню "Быстрый запуск"
        var quickLaunchPanel = new StackPanel
            { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 10) };
        var quickLaunchCheckBox = new CheckBox
        {
            Content = "Поместить ярлык в меню \"Быстрый запуск\"",
            IsChecked = false, // Значение по умолчанию
            Margin = new Thickness(0, 0, 10, 0)
        };
        quickLaunchPanel.Children.Add(quickLaunchCheckBox);
        shortcutsPanel.Children.Add(quickLaunchPanel);

        // Поместить ярлык в меню "Программы"
        var programsMenuPanel = new StackPanel
            { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 10) };
        var programsMenuCheckBox = new CheckBox
        {
            Content = "Поместить ярлык в меню \"Программы\"",
            IsChecked = false, // Значение по умолчанию
            Margin = new Thickness(0, 0, 10, 0)
        };
        programsMenuPanel.Children.Add(programsMenuCheckBox);
        shortcutsPanel.Children.Add(programsMenuPanel);

        // Поместить ярлык на "рабочий стол"
        var desktopPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 10) };
        var desktopCheckBox = new CheckBox
        {
            Content = "Поместить ярлык на \"рабочий стол\"",
            IsChecked = false, // Значение по умолчанию
            Margin = new Thickness(0, 0, 10, 0)
        };
        desktopPanel.Children.Add(desktopCheckBox);
        shortcutsPanel.Children.Add(desktopPanel);

        border.Child = shortcutsPanel;
        SettingsPanel.Children.Add(border);
    }

    private void ShowPingPanelSettings()
    {
        // Создаем бордер для секции настроек
        var border = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Создаем стек панель для настроек панели пингования
        var pingPanelSettings = new StackPanel();

        // Число графических панелей пингования
        var panelCountPanel = new StackPanel
            { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 10) };
        var panelCountLabel = new TextBlock
        {
            Text = "Число графических панелей пингования:",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            Width = 250
        };

        var panelCountComboBox = new ComboBox { Width = 60 };
        for (var i = 1; i <= 5; i++) panelCountComboBox.Items.Add(i.ToString());
        panelCountComboBox.SelectedIndex = 2; // По умолчанию 3

        panelCountPanel.Children.Add(panelCountLabel);
        panelCountPanel.Children.Add(panelCountComboBox);
        pingPanelSettings.Children.Add(panelCountPanel);

        border.Child = pingPanelSettings;
        SettingsPanel.Children.Add(border);
    }

    private void ShowPingConfigSettings()
    {
        // Создаем бордер для секции настроек
        var border = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Создаем стек панель для настроек пингования
        var pingConfigPanel = new StackPanel();

        // Интервал пингования
        var intervalPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
        var intervalLabel = new TextBlock
        {
            Text = "Интервал пингования:",
            Margin = new Thickness(0, 0, 0, 10)
        };
        intervalPanel.Children.Add(intervalLabel);

        // Улучшенный вид для интервала пингования
        var intervalInputPanel = new StackPanel { Orientation = Orientation.Horizontal };

        // TextBox для ввода значения
        var intervalValue = new TextBox
        {
            Text = "1000",
            Width = 60,
            HorizontalContentAlignment = HorizontalAlignment.Right,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        // Панель для кнопок со стрелками (сделаем её компактной)
        var intervalUpDownPanel = new UniformGrid
        {
            Rows = 2,
            Columns = 1,
            Width = 20,
            Margin = new Thickness(5, 0, 0, 0)
        };

        // Кнопка "вверх" с уменьшенным размером и упрощенным видом
        var intervalUpButton = new Button
        {
            Content = "▲",
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 1), // Небольшой отступ между кнопками
            FontSize = 8 // Уменьшенный шрифт для стрелки
        };

        // Кнопка "вниз" с уменьшенным размером и упрощенным видом
        var intervalDownButton = new Button
        {
            Content = "▼",
            Padding = new Thickness(0),
            Margin = new Thickness(0, 1, 0, 0), // Небольшой отступ между кнопками
            FontSize = 8 // Уменьшенный шрифт для стрелки
        };

        // Обработчики событий для кнопок
        intervalUpButton.Click += (s, e) =>
        {
            if (int.TryParse(intervalValue.Text, out var value) && value < 10000)
                intervalValue.Text = (value + 100).ToString();
        };

        intervalDownButton.Click += (s, e) =>
        {
            if (int.TryParse(intervalValue.Text, out var value) && value > 100)
                intervalValue.Text = (value - 100).ToString();
        };

        intervalUpDownPanel.Children.Add(intervalUpButton);
        intervalUpDownPanel.Children.Add(intervalDownButton);

        intervalInputPanel.Children.Add(intervalValue);
        intervalInputPanel.Children.Add(intervalUpDownPanel);

        var intervalUnitLabel = new TextBlock
        {
            Text = "мс",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };
        intervalInputPanel.Children.Add(intervalUnitLabel);

        intervalPanel.Children.Add(intervalInputPanel);
        pingConfigPanel.Children.Add(intervalPanel);

        // Пинговать без остановки
        var continuousPingPanel = new StackPanel
            { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 15) };
        var continuousPingCheckBox = new CheckBox
        {
            Content = "Пинговать без остановки",
            IsChecked = true, // По умолчанию включен
            Margin = new Thickness(0, 0, 10, 0)
        };
        continuousPingPanel.Children.Add(continuousPingCheckBox);
        pingConfigPanel.Children.Add(continuousPingPanel);

        // Пингов на страницу (аналогично улучшим внешний вид)
        var pingsPerPagePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
        var pingsPerPageLabel = new TextBlock
        {
            Text = "Пингов на страницу:",
            Margin = new Thickness(0, 0, 0, 10)
        };
        pingsPerPagePanel.Children.Add(pingsPerPageLabel);

        var pingsPerPageInputPanel = new StackPanel { Orientation = Orientation.Horizontal };
        var pingsPerPageValue = new TextBox
        {
            Text = "30",
            Width = 60,
            HorizontalContentAlignment = HorizontalAlignment.Right,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var pingsPerPageUpDownPanel = new UniformGrid
        {
            Rows = 2,
            Columns = 1,
            Width = 20,
            Margin = new Thickness(5, 0, 0, 0)
        };

        var pingsPerPageUpButton = new Button
        {
            Content = "▲",
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 1),
            FontSize = 8
        };

        var pingsPerPageDownButton = new Button
        {
            Content = "▼",
            Padding = new Thickness(0),
            Margin = new Thickness(0, 1, 0, 0),
            FontSize = 8
        };

        pingsPerPageUpButton.Click += (s, e) =>
        {
            if (int.TryParse(pingsPerPageValue.Text, out var value) && value < 100)
                pingsPerPageValue.Text = (value + 1).ToString();
        };

        pingsPerPageDownButton.Click += (s, e) =>
        {
            if (int.TryParse(pingsPerPageValue.Text, out var value) && value > 1)
                pingsPerPageValue.Text = (value - 1).ToString();
        };

        pingsPerPageUpDownPanel.Children.Add(pingsPerPageUpButton);
        pingsPerPageUpDownPanel.Children.Add(pingsPerPageDownButton);

        pingsPerPageInputPanel.Children.Add(pingsPerPageValue);
        pingsPerPageInputPanel.Children.Add(pingsPerPageUpDownPanel);

        pingsPerPagePanel.Children.Add(pingsPerPageInputPanel);
        pingConfigPanel.Children.Add(pingsPerPagePanel);

        border.Child = pingConfigPanel;
        SettingsPanel.Children.Add(border);
    }

    private void ShowPingListSettings()
    {
        // Создаем бордер для секции настроек
        var border = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Создаем стек панель для списка пингования
        var pingListPanel = new StackPanel();

        // Форма добавления
        var addFormPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var addressInput = new TextBox
        {
            Width = 200,
            Text = "Введите адрес...",
            Foreground = Brushes.Gray
        };

        addressInput.GotFocus += (s, e) =>
        {
            if (addressInput.Text == "Введите адрес...")
            {
                addressInput.Text = "";
                addressInput.Foreground = Brushes.Black;
            }
        };

        addressInput.LostFocus += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(addressInput.Text))
            {
                addressInput.Text = "Введите адрес...";
                addressInput.Foreground = Brushes.Gray;
            }
        };

        var addButton = new Button { Content = "Добавить", Width = 80, Margin = new Thickness(10, 0, 0, 0) };
        var removeButton = new Button { Content = "Удалить", Width = 80, Margin = new Thickness(10, 0, 0, 0) };
        var updateButton = new Button { Content = "Обновить", Width = 80, Margin = new Thickness(10, 0, 0, 0) };

        addFormPanel.Children.Add(addressInput);
        addFormPanel.Children.Add(addButton);
        addFormPanel.Children.Add(removeButton);
        addFormPanel.Children.Add(updateButton);

        pingListPanel.Children.Add(addFormPanel);

        // Список адресов
        var addressesListBox = new ListBox
        {
            Height = 150,
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Добавляем примеры адресов
        addressesListBox.Items.Add("yandex.ru");
        addressesListBox.Items.Add("google.com");
        addressesListBox.Items.Add("github.com");

        pingListPanel.Children.Add(addressesListBox);

        border.Child = pingListPanel;
        SettingsPanel.Children.Add(border);
    }

    private void ShowLogsSettings()
    {
        // Создаем бордер для секции настроек
        var border = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Создаем стек панель для настроек логов
        var logsPanel = new StackPanel();

        // Запись результатов пингования
        var logCheckboxPanel = new StackPanel
            { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 15) };
        var logCheckBox = new CheckBox
        {
            Content = "Записывать результаты в лог файлы",
            IsChecked = _settingsViewModel.SaveLogs,
            Margin = new Thickness(0, 0, 10, 0)
        };
        logCheckBox.Checked += (s, e) => _settingsViewModel.SaveLogs = true;
        logCheckBox.Unchecked += (s, e) => _settingsViewModel.SaveLogs = false;
        logCheckboxPanel.Children.Add(logCheckBox);
        logsPanel.Children.Add(logCheckboxPanel);

        // Путь к лог файлам
        var pathPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
        var pathLabel = new TextBlock
        {
            Text = "Путь к лог файлам:",
            Margin = new Thickness(0, 0, 0, 10)
        };
        pathPanel.Children.Add(pathLabel);

        var pathInputPanel = new StackPanel { Orientation = Orientation.Horizontal };
        var pathTextBox = new TextBox
        {
            Text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            Width = 300,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var browseButton = new Button { Content = "Обзор...", Width = 80 };

        pathInputPanel.Children.Add(pathTextBox);
        pathInputPanel.Children.Add(browseButton);

        pathPanel.Children.Add(pathInputPanel);
        logsPanel.Children.Add(pathPanel);

        border.Child = logsPanel;
        SettingsPanel.Children.Add(border);
    }

    private void SettingsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // Здесь можно добавить логику реагирования на изменения настроек
    }

    private void ApplySettings_Click(object sender, RoutedEventArgs e)
    {
        // Здесь будет логика применения настроек
        MessageBox.Show("Настройки применены");
    }

    private void CancelSettings_Click(object sender, RoutedEventArgs e)
    {
        // Здесь будет логика отмены изменений
        MessageBox.Show("Изменения отменены");
    }

    private void DefaultSettings_Click(object sender, RoutedEventArgs e)
    {
        // Здесь будет логика сброса настроек по умолчанию
        MessageBox.Show("Настройки сброшены к значениям по умолчанию");
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