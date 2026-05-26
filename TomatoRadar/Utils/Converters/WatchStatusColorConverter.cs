using TomatoRadar.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TomatoRadar.Utils.Converters
{
    internal class WatchStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as WatchStatus?) switch
            {
                WatchStatus.NONE => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000")),
                WatchStatus.POSITIVE => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A00DC5")),
                WatchStatus.NEGATIVE => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FE0E00")),
                WatchStatus.CHEATER => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF96CA")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000")),
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}