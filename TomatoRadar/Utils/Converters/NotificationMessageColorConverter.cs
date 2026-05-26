using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using TomatoRadar.Models;

namespace TomatoRadar.Utils.Converters
{
    class NotificationMessageColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as MessageType?) switch
            {
                MessageType.ERROR => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000")),
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
