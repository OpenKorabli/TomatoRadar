using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using TomatoRadar.Models;

namespace TomatoRadar.Utils.Converters
{
    internal class ContextMenuItemCheckOnWoWSNumbersIsEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return DependencyProperty.UnsetValue;
            }
            Player p = (value as Player)!;
            if (p.Name[..1] != ":" && p.ID != "-1")
            {
                return p.Server switch
                {
                    Server.RU => false,
                    Server.CN => false,
                    _ => true,
                };
            }
            else
            {
                return false;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
