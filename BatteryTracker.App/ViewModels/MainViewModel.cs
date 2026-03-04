using BatteryTracker.Core.Models;
using BatteryTracker.Core.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BatteryTracker.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SystemController? _controller;
        private BatteryData _currentData = new BatteryData(0, 0, 0, DateTime.Now);
        private string _statusMessage = "Ожидание данных...";

        // 1. Данные для графиков
        public ObservableCollection<ObservablePoint> VoltageValues { get; } = new();
        public ObservableCollection<ObservablePoint> CurrentValues { get; } = new();

        // 2. Серии и Оси (обязательно свойства для корректного Binding в XAML)
        public ISeries[] CombinedSeries { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        // 3. Форматтер для оси X (вынесен в свойство)
        public Func<double, string> XFormatter { get; } = v =>
            v > 0 ? DateTime.FromOADate(v).ToString("HH:mm:ss") : "";

        /// <summary>
        /// Конструктор для работы (DI)
        /// </summary>
        public MainViewModel(SystemController controller) : this()
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            // Подписка на данные от контроллера
            _controller.NewDataReady += (data) =>
            {
                Application.Current.Dispatcher.Invoke(() => UpdateChartPoints(data));
            };

            _controller.ErrorMessage += (err) => StatusMessage = $"Ошибка: {err}";
        }

        /// <summary>
        /// Пустой конструктор для Дизайнера WPF (убирает ошибку "MainViewModel не существует")
        /// </summary>
        public MainViewModel()
        {
            // Настройка осей (теперь XAML не будет ругаться на типы)
            XAxes = new Axis[] {
                new Axis {
                    Labeler = XFormatter,
                    Name = "Время",
                    NameTextSize = 12
                }
            };

            YAxes = new Axis[] {
                new Axis {
                    Name = "Вольты (V)",
                    Position = LiveChartsCore.Measure.AxisPosition.Start,
                    NamePaint = new SolidColorPaint(SKColors.DarkGreen)
                },
                new Axis {
                    Name = "Амперы (A)",
                    Position = LiveChartsCore.Measure.AxisPosition.End,
                    ShowSeparatorLines = false,
                    NamePaint = new SolidColorPaint(SKColors.DarkRed)
                }
            };

            // Настройка серий
            CombinedSeries = new ISeries[] {
                new LineSeries<ObservablePoint> {
                    Values = VoltageValues,
                    Name = "Напряжение (V)",
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                    GeometrySize = 0,
                    Fill = null,
                    ScalesYAt = 0 // Привязка к первой оси Y
                },
                new LineSeries<ObservablePoint> {
                    Values = CurrentValues,
                    Name = "Ток (A)",
                    Stroke = new SolidColorPaint(SKColors.OrangeRed, 2),
                    GeometrySize = 0,
                    Fill = null,
                    ScalesYAt = 1 // Привязка ко второй оси Y
                }
            };
        }

        private void UpdateChartPoints(BatteryData data)
        {
            CurrentData = data;
            StatusMessage = $"Обновлено: {DateTime.Now:HH:mm:ss}";

            var x = data.Timestamp.ToOADate();
            VoltageValues.Add(new ObservablePoint(x, data.Voltage));
            CurrentValues.Add(new ObservablePoint(x, data.Current));

            // --- ЛОГИКА ОСЦИЛЛОГРАФА ---

            // Определяем ширину окна (например, последние 30 секунд)
            // 30 секунд в формате OADate (дни) = 30 / (24 * 3600)
            double windowWidth = 30.0 / (24 * 3600);

            // Берем текущую ось X (она у нас в массиве XAxes)
            var xAxis = XAxes[0];

            // Устанавливаем границы: Максимум — это текущая точка, Минимум — текущая минус окно
            xAxis.MaxLimit = x;
            xAxis.MinLimit = x - windowWidth;

            // Ограничиваем количество точек в памяти (например, до 500), чтобы не тормозило
            if (VoltageValues.Count > 500)
            {
                VoltageValues.RemoveAt(0);
                CurrentValues.RemoveAt(0);
            }
        }

        public void LoadLogFile()
        {
            var dialog = new OpenFileDialog
            {
                // Устанавливаем папку запуска по умолчанию
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                // Ставим CSV на первое место, чтобы не просил .txt
                Filter = "CSV Data Files (*.csv)|*.csv|Log Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = ".csv",
                Title = "Открыть данные BatteryTracker"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var lines = File.ReadAllLines(dialog.FileName);

                    // Очищаем старые точки перед загрузкой новых
                    VoltageValues.Clear();
                    CurrentValues.Clear();

                    foreach (var line in lines)
                    {
                        var cleanLine = line.Trim();
                        if (string.IsNullOrWhiteSpace(cleanLine)) continue;

                        var data = BatteryParser.Parse(cleanLine);
                        if (data != null)
                        {
                            var x = data.Timestamp.ToOADate();
                            VoltageValues.Add(new ObservablePoint(x, data.Voltage));
                            CurrentValues.Add(new ObservablePoint(x, data.Current));
                        }
                    }

                    // ВАЖНО: Сбрасываем лимиты осей (MinLimit/MaxLimit), 
                    // чтобы график автоматически масштабировался под загруженный файл,
                    // иначе логика "осциллографа" покажет пустоту.
                    if (XAxes != null && XAxes.Length > 0)
                    {
                        XAxes[0].MinLimit = null;
                        XAxes[0].MaxLimit = null;
                    }

                    StatusMessage = $"Файл загружен: {Path.GetFileName(dialog.FileName)} ({VoltageValues.Count} точек)";

                    // Принудительно уведомляем UI об обновлении серий
                    OnPropertyChanged(nameof(CombinedSeries));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка парсинга файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Ошибка при загрузке файла";
                }
            }
        }

        // --- Свойства с уведомлением об изменении ---

        public BatteryData CurrentData
        {
            get => _currentData ?? new BatteryData(0, 0, 0, DateTime.Now); // Защита от null
            set { _currentData = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }
    }
}