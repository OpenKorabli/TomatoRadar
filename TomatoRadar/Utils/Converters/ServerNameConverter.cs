using TomatoRadar.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace TomatoRadar.Utils.Converters
{
    internal class ServerNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ServerExt.GetNameByServer((Server)value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
