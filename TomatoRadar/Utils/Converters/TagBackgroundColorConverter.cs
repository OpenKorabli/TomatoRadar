using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace TomatoRadar.Utils.Converters
{
    internal class TagBackgroundColorConverter : IMultiValueConverter
    {
        private static Color HSL2RGB(double h, double sl, double l)
        {
            double v;
            double r, g, b;
            r = l;
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int)h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }
            return Color.FromRgb(System.Convert.ToByte(r * 255.0f), System.Convert.ToByte(g * 255.0f), System.Convert.ToByte(b * 255.0f));
        }

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value[0] == DependencyProperty.UnsetValue || value[1] == DependencyProperty.UnsetValue)
            {
                return DependencyProperty.UnsetValue;
            }
            double? winrate = (Properties.Settings.Default.WinrateTypeUsed == 0) ? value[0] as double? : value[1] as double?;
            Color bgcolor;
            bgcolor = Properties.Settings.Default.ColorStyle switch
            {
                0 => (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                1 => winrate! switch
                {
                    > 0.6 => (Color)ColorConverter.ConvertFromString("#D042F3"),
                    > 0.52 => (Color)ColorConverter.ConvertFromString("#318000"),
                    > 0.47 => (Color)ColorConverter.ConvertFromString("#FFC71F"),
                    >= 0 => (Color)ColorConverter.ConvertFromString("#FE0E00"),
                    _ => (Color)ColorConverter.ConvertFromString("#CCCCCC"),
                },
                2 => winrate switch
                {
                    > 0.65 => (Color)ColorConverter.ConvertFromString("#A00DC5"),
                    > 0.6 => (Color)ColorConverter.ConvertFromString("#D042F3"),
                    > 0.56 => (Color)ColorConverter.ConvertFromString("#02C9B3"),
                    > 0.54 => (Color)ColorConverter.ConvertFromString("#318000"),
                    > 0.52 => (Color)ColorConverter.ConvertFromString("#44B300"),
                    > 0.49 => (Color)ColorConverter.ConvertFromString("#FFC71F"),
                    > 0.47 => (Color)ColorConverter.ConvertFromString("#FE7903"),
                    >= 0 => (Color)ColorConverter.ConvertFromString("#FE0E00"),
                    _ => (Color)ColorConverter.ConvertFromString("#CCCCCC"),
                },
                3 => winrate switch
                {
                    > 0.65 => HSL2RGB(0.8, 1, 0.5),
                    > 0.47 => HSL2RGB((double)((winrate - 0.47) / 0.18 * 0.8), 1, 0.5),
                    >= 0 => HSL2RGB(0, 1, 0.5),
                    _ => (Color)ColorConverter.ConvertFromString("#CCCCCC"),
                },
                _ => (Color)ColorConverter.ConvertFromString("#FFFFFF"),
            };
            return new SolidColorBrush(bgcolor);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
