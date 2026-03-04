using System;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace BatteryTracker.Core.Hardware
{
    public class VirtualBatteryMonitor : IBatteryMonitor
    {
        private CancellationTokenSource? _cts;
        private readonly Random _rnd = new Random();

        public event Action<string>? RawLineReceived;
        public event Action<string>? ErrorOccurred;

        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        public void Start()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            // Запускаем задачу и не ждем её завершения (Fire and forget)
            _ = Task.Run(() => SimulationLoop(_cts.Token));
        }

        private async Task SimulationLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 1. Генерируем случайные, но реалистичные данные
                    // Напряжение: 11.50 - 13.50 V
                    double v = 11.5 + _rnd.NextDouble() * 2.0;
                    // Ток: 0.00 - 5.00 A
                    double i = _rnd.NextDouble() * 5.0;
                    // Температура: 20.0 - 30.0 C
                    double t = 20.0 + _rnd.NextDouble() * 10.0;

                    // 2. Формируем строку с использованием точки (InvariantCulture)
                    // Формат: $12.45,1.23,24.5
                    string rawLine = string.Format(CultureInfo.InvariantCulture, "${0:F2},{1:F2},{2:F1}", v, i, t);

                    // 3. Уведомляем подписчиков (SystemController)
                    RawLineReceived?.Invoke(rawLine);

                    // 4. Ждем ровно 1 секунду
                    await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException) { /* Нормальный выход при Stop() */ }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Ошибка эмулятора: {ex.Message}");
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public void Dispose() => Stop();
    }
}