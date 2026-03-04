using System.Windows;

namespace BatteryTracker.App.Views
{
    /// <summary>
    /// Логика взаимодействия для ConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : Window
    {
        public ConnectionWindow()
        {
            InitializeComponent();
        }
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // Закрываем окно с успехом
        }
    }
}


