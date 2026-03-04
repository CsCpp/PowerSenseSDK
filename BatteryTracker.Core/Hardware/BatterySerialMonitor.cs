using BatteryTracker.Core.Models;
using System;
using System.IO.Ports;

namespace BatteryTracker.Core.Hardware
{
    public class BatterySerialMonitor : IBatteryMonitor
    {
        private SerialPort? _serialPort;
        private readonly SerialSettings _settings;
       
        public event Action<string>? RawLineReceived;
        public event Action<string>? ErrorOccurred;

        // Передаем настройки порта при создании объекта
        public BatterySerialMonitor(SerialSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        // Теперь свойство IsRunning реализовано
        public bool IsRunning => _serialPort?.IsOpen ?? false;

        // Реализация метода Start БЕЗ параметров (согласно интерфейсу)
        public void Start()
        {
            if (IsRunning) return;
           
            // Конфигурируем порт из нашей сущности
            _serialPort = new SerialPort(_settings.PortName, _settings.BaudRate)
            {
                ReadTimeout = _settings.ReadTimeout
            };

            _serialPort.DataReceived += (s, e) =>
            {
                try
                {
                    string line = _serialPort.ReadLine();
                    RawLineReceived?.Invoke(line);
                }
                catch (TimeoutException)
                {
                    ErrorOccurred?.Invoke("Таймаут: данные не полные или МК молчит.");
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke($"Ошибка порта: {ex.Message}");
                }
            };

            try
            {
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Не удалось открыть {_settings.PortName}: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen) _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        public void Dispose() => Stop();
    }
}