using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace TomatoRadar.Utils.Converters
{
    internal class LabelGamePathIsSetColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as string) switch
            {
                "" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FE0E00")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#318000")),
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
