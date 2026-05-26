using TomatoRadar.Models;
using System.Collections;

namespace TomatoRadar.Utils.Sorters
{
    internal class CustomSorterByShipTypeAndWinrateDescending : IComparer
    {
        public int Compare(object? x, object? y)
        {
            Player? a = x as Player;
            Player? b = y as Player;
            return a!.ShipType != b!.ShipType ? a.ShipType.CompareTo(b.ShipType) : (Properties.Settings.Default.WinrateTypeUsed == 0) ? (a!.AccountWinrate <= b!.AccountWinrate ? 1 : -1) : (a!.WeightedWinrate <= b!.WeightedWinrate ? 1 : -1);
        }
    }
}
