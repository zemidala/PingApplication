using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PingApp.Interfaces;
using PingApp.Models;

namespace PingApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IPingService _pingService;
        private readonly IPingStatisticsService _statisticsService;

        private string _selectedHost;
        private int _packetSize = 32;
        private int _pingInterval = 1000;
        private int _timeout = 5000;
        private bool _isPinging;
        private string _statusMessage;
        private DateTime _startTime;
        private TimeSpan _elapsedTime;
        private string _newHost;

        // Коллекции для хостов
        public ObservableCollection<string> AvailableHosts { get; set; }
        public ObservableCollection<PingResult> PingResults { get; set; }

        // Команды
        public ICommand StartPingCommand { get; set; }
        public ICommand StopPingCommand { get; set; }
        public ICommand AddHostCommand { get; set; }
        public ICommand RemoveHostCommand { get; set; }

        public MainViewModel(IPingService pingService, IPingStatisticsService statisticsService)
        {
            _pingService = pingService;
            _statisticsService = statisticsService;

            InitializeCollections();
            InitializeCommands();
            SetupEventHandlers();
            InitializeDefaultHosts();
        }

        private void InitializeCollections()
        {
            AvailableHosts = new ObservableCollection<string>();
            PingResults = new ObservableCollection<PingResult>();
        }

        private void InitializeDefaultHosts()
        {
            // Добавляем предустановленные хосты
            AvailableHosts.Add("yandex.ru");
            AvailableHosts.Add("google.com");
            AvailableHosts.Add("github.com");
        }

        private void InitializeCommands()
        {
            StartPingCommand = new RelayCommand(StartPing, CanStartPing);
            StopPingCommand = new RelayCommand(StopPing, CanStopPing);
            AddHostCommand = new RelayCommand(AddHost, CanAddHost);
            RemoveHostCommand = new RelayCommand<string>(RemoveHost);
        }

        private void SetupEventHandlers()
        {
            _pingService.PingCompleted += OnPingCompleted;
        }

        private void OnPingCompleted(object sender, PingEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _statisticsService.AddPingResult(e.Host, e.Result);
                PingResults.Add(e.Result);

                if (PingResults.Count > 100)
                    PingResults.RemoveAt(0);

                UpdateStatusMessage(e.Result);
                UpdateElapsedTime();
            });
        }

        private void StartPing()
        {
            if (string.IsNullOrEmpty(SelectedHost)) return;

            _isPinging = true;
            _startTime = DateTime.Now;
            OnPropertyChanged(nameof(IsPinging));

            _pingService.StartContinuousPing(SelectedHost, PacketSize, PingInterval, Timeout);
        }

        private void StopPing()
        {
            if (string.IsNullOrEmpty(SelectedHost)) return;

            _pingService.StopContinuousPing(SelectedHost);
            _isPinging = false;
            OnPropertyChanged(nameof(IsPinging));
        }

        private bool CanStartPing() => !IsPinging && !string.IsNullOrEmpty(SelectedHost);
        private bool CanStopPing() => IsPinging;

        private void AddHost()
        {
            if (!string.IsNullOrEmpty(NewHost) && !AvailableHosts.Contains(NewHost))
            {
                AvailableHosts.Add(NewHost);
                NewHost = string.Empty;
                OnPropertyChanged(nameof(NewHost));
            }
        }

        private bool CanAddHost() => !string.IsNullOrEmpty(NewHost);

        private void RemoveHost(string host)
        {
            if (!string.IsNullOrEmpty(host) && AvailableHosts.Contains(host))
            {
                AvailableHosts.Remove(host);
            }
        }

        private void UpdateStatusMessage(PingResult result)
        {
            if (result.IsSuccess)
            {
                StatusMessage = $"Received {result.PacketSize} bytes from {result.IpAddress} in {result.RoundTripTime} ms";
            }
            else
            {
                StatusMessage = $"Ping failed: {result.ErrorMessage}";
            }
        }

        private void UpdateElapsedTime()
        {
            ElapsedTime = DateTime.Now - _startTime;
        }

        // Свойства с уведомлением об изменениях
        public string SelectedHost
        {
            get => _selectedHost;
            set
            {
                _selectedHost = value;
                OnPropertyChanged();
                ((RelayCommand)StartPingCommand).NotifyCanExecuteChanged();
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

        public bool IsPinging
        {
            get => _isPinging;
            set
            {
                _isPinging = value;
                OnPropertyChanged();
                ((RelayCommand)StartPingCommand).NotifyCanExecuteChanged();
                ((RelayCommand)StopPingCommand).NotifyCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan ElapsedTime
        {
            get => _elapsedTime;
            set
            {
                _elapsedTime = value;
                OnPropertyChanged();
            }
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}