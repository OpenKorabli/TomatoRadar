using TomatoRadar.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using System;
using System.Collections.Generic;
using LiveChartsCore.Defaults;
using TomatoRadar.Utils.Converters;

namespace TomatoRadar.Utils
{
    static internal class ChartUtils
    {

        public static RectangularSection[] GetWinrateChartSections(Battlefield battlefield, int chartType)
        {
            List<RectangularSection> result = new();
            List<double[]> sections = battlefield.GetSectionsForPlot(chartType);
            if (sections.Count <= 1)
            {
                return result.ToArray();
            }
            for (int i = 0; i < sections.Count; i++)
            {
                result.Add(new RectangularSection
                {
                    Xi = sections[i][0],
                    Xj = sections[i][1],
                    Fill = i % 2 == 0 ? new SolidColorPaint { Color = new SKColor(240, 240, 240) } : new SolidColorPaint { Color = SKColors.White }
                });
            }
            return result.ToArray();
        }

        public static ISeries[] GetWinrateChartSeries(Battlefield battlefield, int chartType)
        {
            List<List<Player?>> playerListForPlot = battlefield.GetPlayersForPlot(chartType);
            return chartType switch
            {
                0 => GetWinrateChartColumnSeries(playerListForPlot),
                1 => GetWinrateChartColumnSeries(playerListForPlot),
                2 => GetWinrateChartColumnSeries(playerListForPlot),
                3 => GetWinrateChartColumnSeries(playerListForPlot),
                4 => GetWinrateChartLineSeries(playerListForPlot),
                5 => GetWinrateChartLineSeries(playerListForPlot),
                _ => GetWinrateChartLineSeries(playerListForPlot),
            };
        }

        private static ISeries[] GetWinrateChartLineSeries(List<List<Player?>> playerList)
        {
            return new ISeries[]
            {
                new LineSeries<Player?>
                {
                    Name = "Allies",
                    Values = playerList[0],
                    Mapping = (player, point) =>
                    {
                        point.PrimaryValue = (Properties.Settings.Default.WinrateTypeUsed == 0) ? player!.AccountWinrate : player!.WeightedWinrate;
                        point.SecondaryValue = player.PlotXPosition;
                    },
                    Stroke = new SolidColorPaint(new SKColor(71,227,165)) { StrokeThickness = 3 },
                    Fill = null,
                    GeometrySize = 12,
                    GeometryFill = new SolidColorPaint(new SKColor(71,227,165)),
                    GeometryStroke = null,
                    TooltipLabelFormatter = (chartPoint) => $"{chartPoint.Model!.ClanTag} {chartPoint.Model.Name}\r\n{new ShipTierConverter().Convert(chartPoint.Model.ShipTier,null,null,null)} {chartPoint.Model.ShipName}\r\n{chartPoint.PrimaryValue:p2}"
                },
                new LineSeries<Player?>
                {
                    Name = "Enemies",
                    Values = playerList[1],
                    Mapping = (player, point) =>
                    {
                        point.PrimaryValue = (Properties.Settings.Default.WinrateTypeUsed == 0) ? player!.AccountWinrate : player!.WeightedWinrate;
                        point.SecondaryValue = player.PlotXPosition;
                    },
                    Stroke = new SolidColorPaint(new SKColor(255,66,0)) { StrokeThickness = 3 },
                    Fill = null,
                    GeometrySize = 12,
                    GeometryFill = new SolidColorPaint(new SKColor(255,66,0)),
                    GeometryStroke = null,
                    TooltipLabelFormatter = (chartPoint) => $"{chartPoint.Model!.ClanTag} {chartPoint.Model.Name}\r\n{new ShipTierConverter().Convert(chartPoint.Model.ShipTier,null,null,null)} {chartPoint.Model.ShipName}\r\n{chartPoint.PrimaryValue:p2}",
                }
            };
        }

        private static ISeries[] GetWinrateChartColumnSeries(List<List<Player?>> playerList)
        {
            return new ISeries[]
            {
                new ColumnSeries<Player?>
                {
                    Values = playerList[0],
                    Mapping = (player, point) =>
                    {
                        point.PrimaryValue = (Properties.Settings.Default.WinrateTypeUsed == 0) ? player!.AccountWinrate : player!.WeightedWinrate;
                        point.SecondaryValue = player.PlotXPosition;
                    },
                    Stroke = null,
                    MaxBarWidth = 8,
                    Fill = new SolidColorPaint(new SKColor(71,227,165)),
                    TooltipLabelFormatter = (chartPoint) => $"{chartPoint.Model!.ClanTag} {chartPoint.Model.Name}\r\n{new ShipTierConverter().Convert(chartPoint.Model.ShipTier,null,null,null)} {chartPoint.Model.ShipName}\r\n{chartPoint.PrimaryValue:p2}",
                },
                new ColumnSeries<Player?>
                {
                    Values = playerList[1],
                    Mapping = (player, point) =>
                    {
                        point.PrimaryValue = (Properties.Settings.Default.WinrateTypeUsed == 0) ? player!.AccountWinrate : player!.WeightedWinrate;
                        point.SecondaryValue = player.PlotXPosition;
                    },
                    Stroke = null,
                    MaxBarWidth = 8,
                    Fill = new SolidColorPaint(new SKColor(255,66,0)),
                    TooltipLabelFormatter = (chartPoint) => $"{chartPoint.Model!.ClanTag} {chartPoint.Model.Name}\r\n{new ShipTierConverter().Convert(chartPoint.Model.ShipTier,null,null,null)} {chartPoint.Model.ShipName}\r\n{chartPoint.PrimaryValue:p2}",
                },
            };
        }

        public static ISeries[] GetKDEChartSeries(Battlefield battlefield)
        {
            List<ObservablePoint[]> KDEListForPlot = battlefield.GetPlayersKDEForPlot();

            ISeries[] chartSeriesWinrateKDE = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Name = "Allies",
                    Values = KDEListForPlot[0],
                    Stroke = new SolidColorPaint(new SKColor(71,227,165)) { StrokeThickness = 3 },
                    Fill = null,
                    GeometryFill = null,
                    GeometryStroke = null,
                    IsHoverable = false
                },
                new LineSeries<ObservablePoint>
                {
                    Name = "Enemies",
                    Values = KDEListForPlot[1],
                    Stroke = new SolidColorPaint(new SKColor(255,66,0)) { StrokeThickness = 3 },
                    Fill = null,
                    GeometryFill = null,
                    GeometryStroke = null,
                    IsHoverable = false
                }
            };
            return chartSeriesWinrateKDE;
        }
    }
}
