using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace PingApp.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private bool _animateCharts = true;
    private bool _autoScaleCharts = true;
    private bool _autoStartWindows;
    private bool _confirmExit = true;
    private string _logFormat = "CSV";
    private string _logPath = @"C:\ping_logs\";
    private bool _minimizeOnStart;
    private string _newHost;
    private int _packetSize = 32;
    private int _pingInterval = 1000;
    private bool _saveLogs = true;
    private string _theme = "Светлая";
    private int _timeout = 5000;

    public SettingsViewModel()
    {
        InitializeCollections();
        InitializeCommands();
        LoadDefaultSettings();
    }

    public ObservableCollection<string> AvailableHosts { get; set; }
    public ObservableCollection<string> Themes { get; set; }
    public ObservableCollection<string> LogFormats { get; set; }

    public ICommand AddHostCommand { get; set; }
    public ICommand RemoveHostCommand { get; set; }
    public ICommand SaveSettingsCommand { get; set; }
    public ICommand CancelSettingsCommand { get; set; }
    public ICommand BrowseLogPathCommand { get; set; }

    // Свойства с уведомлением об изменениях
    public string NewHost
    {
        get => _newHost;
        set
        {
            _newHost = value;
            OnPropertyChanged();
            ((RelayCommand)AddHostCommand).NotifyCanExecuteChanged();
        }
    }

    public bool AutoStartWindows
    {
        get => _autoStartWindows;
        set
        {
            _autoStartWindows = value;
            OnPropertyChanged();
        }
    }

    public bool MinimizeOnStart
    {
        get => _minimizeOnStart;
        set
        {
            _minimizeOnStart = value;
            OnPropertyChanged();
        }
    }

    public bool ConfirmExit
    {
        get => _confirmExit;
        set
        {
            _confirmExit = value;
            OnPropertyChanged();
        }
    }

    public string Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            OnPropertyChanged();
        }
    }

    public bool AutoScaleCharts
    {
        get => _autoScaleCharts;
        set
        {
            _autoScaleCharts = value;
            OnPropertyChanged();
        }
    }

    public bool AnimateCharts
    {
        get => _animateCharts;
        set
        {
            _animateCharts = value;
            OnPropertyChanged();
        }
    }

    public int PingInterval
    {
        get => _pingInterval;
        set
        {
            _pingInterval = value;
            OnPropertyChanged();
        }
    }

    public int Timeout
    {
        get => _timeout;
        set
        {
            _timeout = value;
            OnPropertyChanged();
        }
    }

    public int PacketSize
    {
        get => _packetSize;
        set
        {
            _packetSize = value;
            OnPropertyChanged();
        }
    }

    public bool SaveLogs
    {
        get => _saveLogs;
        set
        {
            _saveLogs = value;
            OnPropertyChanged();
        }
    }

    public string LogPath
    {
        get => _logPath;
        set
        {
            _logPath = value;
            OnPropertyChanged();
        }
    }

    public string LogFormat
    {
        get => _logFormat;
        set
        {
            _logFormat = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void InitializeCollections()
    {
        AvailableHosts = new ObservableCollection<string>
        {
            "google.com",
            "yandex.ru",
            "github.com",
            "microsoft.com"
        };

        Themes = new ObservableCollection<string>
        {
            "Светлая",
            "Темная",
            "Синяя"
        };

        LogFormats = new ObservableCollection<string>
        {
            "CSV",
            "JSON",
            "XML"
        };
    }

    private void InitializeCommands()
    {
        AddHostCommand = new RelayCommand(AddHost, CanAddHost);
        RemoveHostCommand = new RelayCommand<string>(RemoveHost);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        CancelSettingsCommand = new RelayCommand(CancelSettings);
        BrowseLogPathCommand = new RelayCommand(BrowseLogPath);
    }

    private void LoadDefaultSettings()
    {
        AutoStartWindows = false;
        MinimizeOnStart = false;
        ConfirmExit = true;
        Theme = "Светлая";
        AutoScaleCharts = true;
        AnimateCharts = true;
        PingInterval = 1000;
        Timeout = 5000;
        PacketSize = 32;
        SaveLogs = true;
        LogPath = @"C:\ping_logs\";
        LogFormat = "CSV";
    }

    private void AddHost()
    {
        if (!string.IsNullOrEmpty(NewHost) && !AvailableHosts.Contains(NewHost))
        {
            AvailableHosts.Add(NewHost);
            NewHost = string.Empty;
            OnPropertyChanged(nameof(NewHost));
        }
    }

    private bool CanAddHost()
    {
        return !string.IsNullOrEmpty(NewHost);
    }

    private void RemoveHost(string host)
    {
        if (!string.IsNullOrEmpty(host) && AvailableHosts.Contains(host)) AvailableHosts.Remove(host);
    }

    private void SaveSettings()
    {
        // Здесь будет логика сохранения настроек
    }

    private void CancelSettings()
    {
        // Здесь будет логика отмены изменений
    }

    private void BrowseLogPath()
    {
        // Здесь будет логика выбора пути для логов
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}