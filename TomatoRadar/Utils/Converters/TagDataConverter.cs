using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace TomatoRadar.Utils.Converters
{
    internal class TagDataConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value[0] == DependencyProperty.UnsetValue || value[1] == DependencyProperty.UnsetValue || value[2] == DependencyProperty.UnsetValue || value[3] == DependencyProperty.UnsetValue)
            {
                return DependencyProperty.UnsetValue;
            }
            double? winrate = (Properties.Settings.Default.WinrateTypeUsed == 0) ? value[0] as double? : value[1] as double?;

            double? battles = value[2] as double?;
            bool isHidden = (bool)value[3];

            if (winrate < 0 || isHidden)
            {
                return Properties.Settings.Default.HiddenIcon;
            }
            else if (winrate <= Properties.Settings.Default.TomatoWinrateThreshold / 100 && battles >= Properties.Settings.Default.TomatoBattleCountThreshold)
            {
                return Properties.Settings.Default.TomatoIcon;
            }
            else if (winrate > Properties.Settings.Default.UnicumWinrateThreshold / 100 && battles >= Properties.Settings.Default.UnicumBattleCountThreshold)
            {
                return Properties.Settings.Default.UnicumIcon;
            }
            else
            {
                return "";
            }
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
