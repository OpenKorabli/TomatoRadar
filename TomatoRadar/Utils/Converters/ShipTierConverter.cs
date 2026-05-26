using System;
using System.Windows.Data;
using System.Globalization;

namespace TomatoRadar.Utils.Converters
{
    internal class ShipTierConverter : IValueConverter
    {
        public object Convert(object value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            return value switch
            {
                0 => "??",
                1 => "Ⅰ",
                2 => "Ⅱ",
                3 => "Ⅲ",
                4 => "Ⅳ",
                5 => "Ⅴ",
                6 => "Ⅵ",
                7 => "Ⅶ",
                8 => "Ⅷ",
                9 => "Ⅸ",
                10 => "Ⅹ",
                11 => "★",
                _ => "??",
            };
        }
        public object ConvertBack(object value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}
