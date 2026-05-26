using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore;

namespace TomatoRadar.Utils.Converters
{
    internal class WeightedWinratePieChartWeightConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            string? soloStr = Application.Current.FindResource("LabelWeightedWinratePieChartSolo") as string;
            string? div2Str = Application.Current.FindResource("LabelWeightedWinratePieChartDiv2") as string;
            string? div3Str = Application.Current.FindResource("LabelWeightedWinratePieChartDiv3") as string;
            string? shipStr = Application.Current.FindResource("LabelWeightedWinratePieChartShip") as string;
            string? fontFamily = value[4] as string;
            if (Double.TryParse(value[0] as string, out double soloWeight) && Double.TryParse(value[1] as string, out double div2Weight) && Double.TryParse(value[2] as string, out double div3Weight) && Double.TryParse(value[3] as string, out double shipMaxWeight))
            {
                return new ISeries[]
                {
                    new PieSeries<double>
                    {
                        Name = soloStr,
                        Values = new List<double> { soloWeight },
                        Fill = new SolidColorPaint(new SKColor(4280391411u)),
                        DataLabelsSize = 10,
                        DataLabelsPaint = new SolidColorPaint{ Color = SKColors.Black, FontFamily = fontFamily },
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = p => $"{soloStr}\r\n{p.StackedValue!.Share:P1}",
                        IsHoverable = false,
                    },
                    new PieSeries<double>
                    {
                        Name = div2Str,
                        Values = new List<double> { div2Weight },
                        Fill = new SolidColorPaint(new SKColor(4294198070u)),
                        DataLabelsSize = 10,
                        DataLabelsPaint = new SolidColorPaint{ Color = SKColors.Black, FontFamily = fontFamily },
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = p => $"{div2Str}\r\n{p.StackedValue!.Share:P1}",
                        IsHoverable = false,
                    },
                    new PieSeries<double>
                    {
                        Name = div3Str,
                        Values = new List<double> { div3Weight },
                        Fill = new SolidColorPaint(new SKColor(4287349578u)),
                        DataLabelsSize = 10,
                        DataLabelsPaint = new SolidColorPaint{ Color = SKColors.Black, FontFamily = fontFamily },
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = p => $"{div3Str}\r\n{p.StackedValue!.Share:P1}",
                        IsHoverable = false,
                    },
                    new PieSeries<double>
                    {
                        Name = shipStr,
                        Values = new List<double> { (soloWeight + div2Weight + div3Weight) * (1 / (1 - shipMaxWeight / 100) - 1) },
                        Fill = new SolidColorPaint(new SKColor(4278238420u)),
                        DataLabelsSize = 10,
                        DataLabelsPaint = new SolidColorPaint{ Color = SKColors.Black, FontFamily = fontFamily },
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = p => $"{shipStr}\r\n{p.StackedValue!.Share:P1}",
                        IsHoverable = false,
                    }
                };
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}