namespace BatteryTracker.Core.Models
{
    /// <summary>
    /// Настройки COMпорта
    /// </summary>
    public class SerialSettings
    {
        // Имя порта (например, "COM3")
        public string PortName { get; set; } = string.Empty;

        // Скорость передачи (стандарт 9600)
        public int BaudRate { get; set; } = 9600;

        // Таймаут чтения (в мс), чтобы ReadLine не висел вечно
        public int ReadTimeout { get; set; } = 2000;

        // Можно добавить еще параметры, если МК требует специфики:
        // public int DataBits { get; set; } = 8;
        // public StopBits StopBits { get; set; } = StopBits.One;
    }
}
