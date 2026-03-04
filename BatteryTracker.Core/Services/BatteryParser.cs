using BatteryTracker.Core.Models;
using System;
using System.Globalization;

namespace BatteryTracker.Core.Services
{
    public static class BatteryParser
    {
        private const char StartChar = '$';
        private const char Separator = ',';
        private static readonly NumberStyles Style = NumberStyles.Any;
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Быстрая проверка: начинается ли строка с '$' и содержит ли нужное кол-во разделителей
        /// </summary>
        public static bool IsValid(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            string trimmed = input.Trim();
            if (!trimmed.StartsWith(StartChar.ToString())) return false;

            // Считаем запятые: должно быть 2 (для 3 параметров) или 3 (для 4 параметров)
            var parts = trimmed.Substring(1).Split(Separator);
            return parts.Length == 3 || parts.Length == 4;
        }

        public static BatteryData? Parse(string input, DateTime? dateTime = null)
        {
            if (!IsValid(input)) return null;

            DateTime targetDate = dateTime ?? DateTime.Now;
            var parts = input.Trim().Substring(1).Split(Separator);

            // Вариант 1: 3 параметра (U, I, T) -> $12.2,45,65
            if (parts.Length == 3)
            {
                if (TryParseBatteryFields(parts, 0, out double u, out double i, out double t))
                    return new BatteryData(u, i, t, targetDate);
            }
            // Вариант 2: 4 параметра (DateTime, U, I, T) -> $2024-05-20 12:00:00,12.2,45,65
            else if (parts.Length == 4)
            {
                if (DateTime.TryParse(parts[0], Culture, DateTimeStyles.None, out DateTime d) &&
                    TryParseBatteryFields(parts, 1, out double u, out double i, out double t))
                {
                    return new BatteryData(u, i, t, d);
                }
            }

            return null;
        }

        // Вспомогательный метод, чтобы не повторять double.TryParse 
        private static bool TryParseBatteryFields(string[] parts, int startIndex, out double u, out double i, out double t)
        {
            u = i = t = 0;
            return double.TryParse(parts[startIndex], Style, Culture, out u) &&
                   double.TryParse(parts[startIndex + 1], Style, Culture, out i) &&
                   double.TryParse(parts[startIndex + 2], Style, Culture, out t);
        }
    }
}