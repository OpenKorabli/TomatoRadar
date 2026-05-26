using System;
using System.Globalization;
using System.Windows.Data;
using TomatoRadar.Models;

namespace TomatoRadar.Utils.Converters
{
    internal class WatchStatusIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as WatchStatus?) switch
            {
                WatchStatus.NONE => "",
                WatchStatus.POSITIVE => Properties.Settings.Default.WatchIcon,
                WatchStatus.NEGATIVE => Properties.Settings.Default.WatchIcon,
                WatchStatus.CHEATER => Properties.Settings.Default.WatchIcon,
                _ => "",
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
