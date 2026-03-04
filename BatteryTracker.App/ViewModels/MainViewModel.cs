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
using System.Linq;

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

        // 2. Серии и Оси
        public ISeries[] CombinedSeries { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        // Краска для текста и сетки (Dark Theme)
        private static readonly SolidColorPaint WhitePaint = new(SKColors.WhiteSmoke);
        private static readonly SolidColorPaint GridPaint = new(SKColors.Gray.WithAlpha(40));

        // 3. Форматтер для оси X
        public Func<double, string> XFormatter { get; } = v =>
            v > 0 ? DateTime.FromOADate(v).ToString("HH:mm:ss") : "";

        /// <summary>
        /// Конструктор для DI
        /// </summary>
        public MainViewModel(SystemController controller) : this()
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));

            _controller.NewDataReady += (data) =>
            {
                Application.Current.Dispatcher.Invoke(() => UpdateChartPoints(data));
            };

            _controller.ErrorMessage += (err) => StatusMessage = $"Ошибка: {err}";
        }

        /// <summary>
        /// Основной конструктор (Настройка стилей Темной Темы)
        /// </summary>
        public MainViewModel()
        {
            // Настройка осей под темный фон
            XAxes = new Axis[] {
                new Axis {
                    Labeler = XFormatter,
                    Name = "Время",
                    NamePaint = WhitePaint,
                    LabelsPaint = WhitePaint,
                    SeparatorsPaint = GridPaint,
                    NameTextSize = 12
                }
            };

            YAxes = new Axis[] {
                new Axis {
                    Name = "Вольты (V)",
                    Position = LiveChartsCore.Measure.AxisPosition.Start,
                    NamePaint = new SolidColorPaint(SKColors.Cyan), // Контрастный циан
                    LabelsPaint = WhitePaint,
                    SeparatorsPaint = GridPaint
                },
                new Axis {
                    Name = "Амперы (A)",
                    Position = LiveChartsCore.Measure.AxisPosition.End,
                    ShowSeparatorLines = false,
                    NamePaint = new SolidColorPaint(SKColors.Gold), // Контрастный золотой
                    LabelsPaint = WhitePaint
                }
            };

            // Настройка серий
            CombinedSeries = new ISeries[] {
                new LineSeries<ObservablePoint> {
                    Values = VoltageValues,
                    Name = "Напряжение (V)",
                    Stroke = new SolidColorPaint(SKColors.Cyan, 2),
                    GeometrySize = 0,
                    Fill = null,
                    ScalesYAt = 0
                },
                new LineSeries<ObservablePoint> {
                    Values = CurrentValues,
                    Name = "Ток (A)",
                    Stroke = new SolidColorPaint(SKColors.Gold, 2),
                    GeometrySize = 0,
                    Fill = null,
                    ScalesYAt = 1
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

            // Логика осциллографа (30 сек)
            double windowWidth = 30.0 / (24 * 3600);
            var xAxis = XAxes[0];
            xAxis.MaxLimit = x;
            xAxis.MinLimit = x - windowWidth;

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
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Filter = "CSV Data Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = ".csv",
                Title = "Открыть данные BatteryTracker"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var lines = File.ReadAllLines(dialog.FileName);
                    VoltageValues.Clear();
                    CurrentValues.Clear();

                    foreach (var line in lines)
                    {
                        var data = BatteryParser.Parse(line);
                        if (data != null)
                        {
                            var x = data.Timestamp.ToOADate();
                            VoltageValues.Add(new ObservablePoint(x, data.Voltage));
                            CurrentValues.Add(new ObservablePoint(x, data.Current));
                        }
                    }

                    // Сброс лимитов для отображения всего файла
                    if (XAxes.Length > 0)
                    {
                        XAxes[0].MinLimit = null;
                        XAxes[0].MaxLimit = null;
                    }

                    StatusMessage = $"Загружено: {VoltageValues.Count} точек ({Path.GetFileName(dialog.FileName)})";
                    OnPropertyChanged(nameof(CombinedSeries));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public BatteryData CurrentData
        {
            get => _currentData;
            set { _currentData = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }
    }
}