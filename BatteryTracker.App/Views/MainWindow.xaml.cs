using BatteryTracker.App.ViewModels;
using BatteryTracker.Core.Hardware;
using BatteryTracker.Core.Models;
using BatteryTracker.Core.Services;
using LiveChartsCore; // ОБЯЗАТЕЛЬНО: Добавляем ссылку на базовые типы
using LiveChartsCore.Measure; // Для ZoomAndPanMode и LegendPosition
using LiveChartsCore.SkiaSharpView.WPF;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BatteryTracker.App.Views
{
    public partial class MainWindow : Window
    {
        private SystemController? _controller;

        public MainWindow()
        {
            InitializeComponent();
            this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            OpenSettingsAndStart();
        }

        private void SetupChart()
        {
            // 1. Создаем график как dynamic, чтобы обойти строгую проверку версий LiveChartsCore
            dynamic chart = new CartesianChart();

            // 2. Настройка фона (WPF тип, тут проблем нет)
            chart.Background = new SolidColorBrush(Color.FromRgb(37, 37, 38));

            // 3. Настройка свойств через dynamic (теперь ошибки "Тип определен в сборке..." исчезнут)
            // Мы присваиваем значения напрямую, компилятор не будет проверять тип Paint/LegendPosition
            chart.LegendTextPaint = new SolidColorPaint(SKColors.WhiteSmoke);
            chart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Right;
            chart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X;

            // 4. Привязки через Binding
            // Используем явное указание свойств из WPF-контрола
            var seriesBinding = new Binding("CombinedSeries") { Source = this.DataContext };
            BindingOperations.SetBinding(chart, CartesianChart.SeriesProperty, seriesBinding);

            var xAxesBinding = new Binding("XAxes") { Source = this.DataContext };
            BindingOperations.SetBinding(chart, CartesianChart.XAxesProperty, xAxesBinding);

            var yAxesBinding = new Binding("YAxes") { Source = this.DataContext };
            BindingOperations.SetBinding(chart, CartesianChart.YAxesProperty, yAxesBinding);

            // 5. Помещаем в Border
            ChartHost.Child = chart;
        }

        // ... Остальные методы (OpenSettingsAndStart, OnClosing, OpenFile_Click) без изменений ...

        protected override void OnClosing(CancelEventArgs e)
        {
            _controller?.Dispose();
            base.OnClosing(e);
        }

        private void OpenSettingsAndStart()
        {
            var connVm = new ConnectionViewModel();
            var connWin = new ConnectionWindow { DataContext = connVm };

            if (connWin.ShowDialog() == true)
            {
                var settings = connVm.GetSettings();
                IBatteryMonitor monitor;

                if (settings.PortName == "VIRTUAL")
                    monitor = new VirtualBatteryMonitor();
                else
                {
                    if (string.IsNullOrEmpty(settings.PortName))
                    {
                        MessageBox.Show("Порт не выбран.");
                        this.Close();
                        return;
                    }
                    monitor = new BatterySerialMonitor(settings);
                }

                _controller = new SystemController(monitor, "BatteryLog");
                this.DataContext = new MainViewModel(_controller);
                SetupChart();
                _controller.Start();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm) vm.LoadLogFile();
        }
    }
}