using System.Collections;
using TomatoRadar.Models;

namespace TomatoRadar.Utils.Sorters
{
    internal class CustomSorterByShipTypeAndShipTierAndWinrateDescending : IComparer
    {
        public int Compare(object? x, object? y)
        {
            Player? a = x as Player;
            Player? b = y as Player;
            return a!.ShipType != b!.ShipType ? a.ShipType.CompareTo(b.ShipType) : a!.ShipTier != b!.ShipTier ? b.ShipTier - a.ShipTier : (Properties.Settings.Default.WinrateTypeUsed == 0) ? (a!.AccountWinrate <= b!.AccountWinrate ? 1 : -1) : (a!.WeightedWinrate <= b!.WeightedWinrate ? 1 : -1);
        }
    }
}
