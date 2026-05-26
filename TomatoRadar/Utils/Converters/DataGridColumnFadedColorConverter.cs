using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace TomatoRadar.Utils.Converters
{
    internal class DataGridColumnFadedColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as int?) switch
            {
                1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#66000000")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000")),
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
