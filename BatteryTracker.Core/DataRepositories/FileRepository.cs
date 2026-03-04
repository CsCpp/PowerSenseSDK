using BatteryTracker.Core.Models;
using BatteryTracker.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BatteryTracker.Core.DataRepositories
{
    public class FileRepository
    {
        private readonly string _logDirectory;
        // Заголовок теперь всегда с префиксом $ для совместимости с вашим BatteryParser
        private const string Header = "$DateTime,Voltage,Current,Temperature";

        public FileRepository(string logDirectory)
        {
            _logDirectory = logDirectory;
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private string GetCurrentFilePath()
        {
            // Формат имени файла: Log_2024-05-20.csv
            string fileName = $"Log_{DateTime.Now:yyyy-MM-dd}.csv";
            return Path.Combine(_logDirectory, fileName);
        }

        /// <summary>
        /// Записывает пачку строк в файл. Гарантирует наличие префикса $ и заголовка.
        /// </summary>
        public void WriteBatch(IEnumerable<string> lines)
        {
            if (lines == null || !lines.Any()) return;

            try
            {
                string currentPath = GetCurrentFilePath();
                bool fileExists = File.Exists(currentPath);

                // Если файла нет — создаем и пишем заголовок
                if (!fileExists)
                {
                    File.WriteAllText(currentPath, Header + Environment.NewLine);
                }

                // Гарантируем, что каждая строка начинается с '$'
                var formattedLines = lines
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => l.Trim().StartsWith("$") ? l.Trim() : "$" + l.Trim());

                File.AppendAllLines(currentPath, formattedLines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileRepository] Ошибка записи: {ex.Message}");
            }
        }

        /// <summary>
        /// Читает данные из текущего лога и превращает их в объекты BatteryData
        /// </summary>
        public List<BatteryData> ReadAll()
        {
            var dataList = new List<BatteryData>();
            string currentPath = GetCurrentFilePath();

            if (!File.Exists(currentPath)) return dataList;

            try
            {
                // Читаем все строки, кроме заголовка
                var lines = File.ReadLines(currentPath);
                foreach (string line in lines)
                {
                    // Пропускаем заголовок (даже если он с $)
                    if (line.Contains("DateTime")) continue;

                    var data = BatteryParser.Parse(line);
                    if (data != null)
                    {
                        dataList.Add(data);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileRepository] Ошибка чтения: {ex.Message}");
            }

            return dataList;
        }

        /// <summary>
        /// Дополнительный метод для сохранения ОДИНОЧНОГО объекта BatteryData (удобно для тестов)
        /// </summary>
        public void SaveSingle(BatteryData data)
        {
            string line = $"{data.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                          $"{data.Voltage.ToString(CultureInfo.InvariantCulture)}," +
                          $"{data.Current.ToString(CultureInfo.InvariantCulture)}," +
                          $"{data.Temperature.ToString(CultureInfo.InvariantCulture)}";

            WriteBatch(new[] { line });
        }
    }
}