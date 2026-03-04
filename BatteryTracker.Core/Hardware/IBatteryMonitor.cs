using System;

namespace BatteryTracker.Core.Hardware
{
    public interface IBatteryMonitor : IDisposable
    {
        // Событие для передачи сырой строки
        event Action<string> RawLineReceived;

        // Событие для передачи ошибок порта/эмулятора
        event Action<string> ErrorOccurred;

        bool IsRunning { get; }

        // Универсальные методы управления
        void Start();
        void Stop();
    }
}