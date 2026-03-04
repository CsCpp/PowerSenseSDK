📝 Статус проекта: BatteryTracker (v1.0)
1. Архитектура (Core Library — .NET Standard 2.1 / .NET 10):
Pattern: Dependency Injection (DI) через интерфейсы.
Hardware Layer: Интерфейс IBatteryMonitor (события RawLineReceived, ErrorOccurred).
BatterySerialMonitor: Реальный COM-порт с SerialSettings.
VirtualBatteryMonitor: Эмулятор, генерирует случайные данные $V,I,T раз в секунду.
Service Layer: BatteryParser (статический, поддержка 3 и 4 параметров, перегрузка с DateTime). SystemController — дирижер, связывает монитор и репозиторий.
Data Layer: FileRepository — построчная запись сырых данных с добавлением даты после $.
2. Интерфейс (WPF App — .NET 10):
Pattern: MVVM.
Base: ViewModelBase с реализацией INotifyPropertyChanged.
ViewModels:
MainViewModel: Подписан на NewDataReady, обновляет CurrentBatteryData.
ConnectionViewModel: Управляет списком портов (включая "VIRTUAL") и SerialSettings.
Views: MainWindow.xaml (разметка на Grid), ConnectionWindow.xaml.
3. Текущая задача:
Завершена настройка связки Hardware -> Controller -> ViewModel.
Проект переименован в BatteryTracker.App (WPF) и BatteryTracker.Core (Library), чтобы избежать конфликта имен с классом монитора.
Следующий шаг: Реализация расчета емкости (Ah) и статистики в контроллере.
4. Ссылка на репозиторий: https://github.com (ветка CsCppKotlin).
