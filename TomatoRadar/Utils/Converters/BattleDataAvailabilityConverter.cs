using System;
using System.Windows.Data;
using System.Globalization;

namespace TomatoRadar.Utils.Converters
{
    internal class BattleDataAvailabilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value as double? >= 0)
            {
                return value;
            }
            else
            {
                return "-";//hidden or bot player
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
