using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using BatteryTracker.Core.Models;

namespace BatteryTracker.App.ViewModels
{
    public class ConnectionViewModel : ViewModelBase
    {
        public ObservableCollection<string> AvailablePorts { get; } = new();
        public ObservableCollection<int> BaudRates { get; } = new() { 9600, 19200, 38400, 57600, 115200 };

        private string? _selectedPort;
        public string? SelectedPort
        {
            get => _selectedPort;
            set { _selectedPort = value; OnPropertyChanged(); }
        }

        private int _selectedBaudRate = 9600;
        public int SelectedBaudRate
        {
            get => _selectedBaudRate;
            set { _selectedBaudRate = value; OnPropertyChanged(); }
        }

        public ConnectionViewModel()
        {
            RefreshPorts();
        }

        public void RefreshPorts()
        {
            AvailablePorts.Clear();

            // 1. Добавляем реальные порты из системы
            var ports = SerialPort.GetPortNames();
            foreach (var p in ports) AvailablePorts.Add(p);

            // 2. Добавляем наш виртуальный порт для имитации
            if (!AvailablePorts.Contains("VIRTUAL"))
            {
                AvailablePorts.Add("VIRTUAL");
            }

            // Выбираем первый по умолчанию, если есть
            SelectedPort = AvailablePorts.FirstOrDefault();
        }

        // Метод для получения готовых настроек
        public SerialSettings GetSettings()
        {
            return new SerialSettings
            {
                PortName = SelectedPort ?? string.Empty,
                BaudRate = SelectedBaudRate
            };
        }
    }
}