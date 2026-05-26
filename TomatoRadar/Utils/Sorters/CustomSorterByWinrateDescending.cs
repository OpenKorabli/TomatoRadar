using System.Collections;
using TomatoRadar.Models;

namespace TomatoRadar.Utils.Sorters
{
    internal class CustomSorterByWinrateDescending : IComparer
    {
        public int Compare(object? x, object? y)
        {
            Player? a = x as Player;
            Player? b = y as Player;
            return (Properties.Settings.Default.WinrateTypeUsed == 0) ? (a!.AccountWinrate <= b!.AccountWinrate ? 1 : -1) : (a!.WeightedWinrate <= b!.WeightedWinrate ? 1 : -1);
        }
    }
}
