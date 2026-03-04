using BatteryTracker.Core.DataRepositories;
using BatteryTracker.Core.Hardware;
using BatteryTracker.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BatteryTracker.Core.Services
{
    public class SystemController : IDisposable
    {
        private readonly IBatteryMonitor _monitor;
        private readonly FileRepository _fileRepo;

        private readonly List<string> _diskBuffer = new List<string>();
        private readonly object _lock = new object();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _isDisposed = false;

        public event Action<BatteryData>? NewDataReady;
        public event Action<string>? ErrorMessage;

        public SystemController(IBatteryMonitor monitor, string logPath)
        {
            _fileRepo = new FileRepository(logPath);
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

            _monitor.RawLineReceived += HandleIncomingRawLine;
            _monitor.ErrorOccurred += (msg) => ErrorMessage?.Invoke(msg);

            Task.Run(() => BackgroundWriteLoop(_cts.Token));
        }

        private void HandleIncomingRawLine(string rawLine)
        {
            if (!BatteryParser.IsValid(rawLine)) return;

            var now = DateTime.Now;
            string cleanData = rawLine.Trim().TrimStart('$');
            string logLine = $"{now:yyyy-MM-dd HH:mm:ss},{cleanData}";

            lock (_lock)
            {
                _diskBuffer.Add(logLine);
            }

            var data = BatteryParser.Parse(rawLine, now);
            if (data != null)
            {
                NewDataReady?.Invoke(data);
            }
        }

        private async Task BackgroundWriteLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Используем Task.Delay с токеном — он сразу пробросит исключение при отмене
                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                    await FlushBufferAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Это нормально при закрытии
            }
            finally
            {
                // Принудительный финальный сброс
                await FlushBufferAsync();
            }
        }

        private async Task FlushBufferAsync()
        {
            List<string> toWrite;
            lock (_lock)
            {
                if (_diskBuffer.Count == 0) return;
                toWrite = _diskBuffer.ToList();
                _diskBuffer.Clear();
            }

            // Если мы уже в процессе Dispose, Task.Run может не успеть.
            // Поэтому вызываем метод репозитория напрямую.
            _fileRepo.WriteBatch(toWrite);
            await Task.CompletedTask;
        }

        public void Start() => _monitor.Start();

        public void Stop()
        {
            _monitor.Stop();
            _cts.Cancel();
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;
            Stop(); // Останавливаем монитор и цикл

            // Ждем завершения записи (финальный Flush в блоке finally цикла успеет отработать)
            // Но для 100% гарантии вызываем еще раз синхронно
            List<string> finalData;
            lock (_lock)
            {
                finalData = _diskBuffer.ToList();
                _diskBuffer.Clear();
            }
            if (finalData.Count > 0) _fileRepo.WriteBatch(finalData);

            _cts.Dispose();
        }
    }
}