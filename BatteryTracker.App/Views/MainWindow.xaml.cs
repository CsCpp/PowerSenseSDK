using BatteryTracker.App.ViewModels;
using BatteryTracker.Core.Hardware;
using BatteryTracker.Core.Models;
using BatteryTracker.Core.Services;
using LiveChartsCore.SkiaSharpView.WPF;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace BatteryTracker.App.Views
{
    public partial class MainWindow : Window
    {
        private SystemController? _controller;

        public MainWindow()
        {
            InitializeComponent();

            // Запускаем процесс выбора порта при старте
            OpenSettingsAndStart();
        }

        private void SetupChart()
        {
            // Используем динамику, чтобы обойти проблемы с версиями сборок в XAML-компиляторе
            dynamic chart = new LiveChartsCore.SkiaSharpView.WPF.CartesianChart();

            // Явно указываем Source = DataContext, чтобы график сразу "увидел" серии и оси
            chart.SetBinding(LiveChartsCore.SkiaSharpView.WPF.CartesianChart.SeriesProperty,
                new Binding("CombinedSeries") { Source = this.DataContext });

            chart.SetBinding(LiveChartsCore.SkiaSharpView.WPF.CartesianChart.XAxesProperty,
                new Binding("XAxes") { Source = this.DataContext });

            chart.SetBinding(LiveChartsCore.SkiaSharpView.WPF.CartesianChart.YAxesProperty,
                new Binding("YAxes") { Source = this.DataContext });

            // Настройка зума (X = 1) и позиции легенды (Top = 1) через перечисления
            chart.ZoomMode = (LiveChartsCore.Measure.ZoomAndPanMode)1;
            chart.LegendPosition = (LiveChartsCore.Measure.LegendPosition)1;

            // Помещаем созданный график в Border, который описан в XAML
            ChartHost.Child = chart;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Метод Dispose внутри себя вызывает Stop() и сохраняет остатки логов
            _controller?.Dispose();
            base.OnClosing(e);
        }

        private void OpenSettingsAndStart()
        {
            var connVm = new ConnectionViewModel();
            var connWin = new ConnectionWindow { DataContext = connVm };

            // Если пользователь нажал "Connect" в окне настроек
            if (connWin.ShowDialog() == true)
            {
                var settings = connVm.GetSettings();
                IBatteryMonitor monitor;

                // Выбор типа монитора: Виртуальный или Реальный COM-порт
                if (settings.PortName == "VIRTUAL")
                {
                    monitor = new VirtualBatteryMonitor();
                }
                else
                {
                    if (string.IsNullOrEmpty(settings.PortName))
                    {
                        MessageBox.Show("Порт не выбран. Работа будет остановлена.");
                        this.Close();
                        return;
                    }
                    monitor = new BatteryTracker.Core.Hardware.BatterySerialMonitor(settings);
                }

                // Инициализируем контроллер (Core слой)
                _controller = new SystemController(monitor, settings.PortName + "_LogData");

                // 1. Устанавливаем DataContext для MVVM привязок
                this.DataContext = new MainViewModel(_controller);

                // 2. ВАЖНО: Вызываем создание графика только ПОСЛЕ установки DataContext
                SetupChart();

                // 3. Запуск процесса получения и записи данных
                _controller.Start();
            }
            else
            {
                // Если окно настроек просто закрыли — выходим из приложения
                if (!this.IsLoaded) this.Close();
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            // Передаем управление методу загрузки во ViewModel
            if (DataContext is MainViewModel vm)
            {
                vm.LoadLogFile();
            }
        }
    }
}