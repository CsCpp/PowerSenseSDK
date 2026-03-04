using System;

namespace BatteryTracker.Core.Models
{
    public class BatteryData
    {
        public DateTime Timestamp { get; private set; }
        public double Voltage { get; private set; }
        public double Current { get; private set; }
        public double Temperature { get; private set; }

     
        public BatteryData(double voltage, double current, double temperature, DateTime timestamp)
        {
            Timestamp = timestamp;
            Voltage = voltage;
            Current = current;
            Temperature = temperature;
        }
      
        
    }
}
