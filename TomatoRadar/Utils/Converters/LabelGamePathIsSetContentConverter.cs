using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace TomatoRadar.Utils.Converters
{
    internal class LabelGamePathIsSetContentConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as string) switch
            {
                "" => Application.Current.FindResource("LabelGamePathIsNotSet") as string,
                _ => Application.Current.FindResource("LabelGamePathIsSet") as string,
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
