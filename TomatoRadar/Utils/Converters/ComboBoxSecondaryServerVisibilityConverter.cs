using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace TomatoRadar.Utils.Converters
{
    internal class ComboBoxSecondaryServerVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as bool?) switch
            {
                true => System.Windows.Visibility.Visible,
                _ => System.Windows.Visibility.Hidden,
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
