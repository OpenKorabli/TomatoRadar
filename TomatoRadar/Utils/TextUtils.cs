using TomatoRadar.Models;
using System.Windows;

namespace TomatoRadar.Utils
{
    static internal class TextUtils
    {
        private static void ProcessTextOutputPlayerList(ref string list, int count, string strNone, string strDelimter)
        {
            if (count > 0)
            {
                list = list.Remove(list.LastIndexOf(strDelimter));
            }
            if (list == "")
            {
                list = strNone;
            }
        }

        public static string GenerateGeneralStatisticsOutputText(Battlefield battlefield)
        {
            string? strNone = Application.Current.FindResource("StringNone") as string;
            string strDelimter = Properties.Settings.Default.OutputTextDelimiter;
            if (strDelimter == "")
            {
                strDelimter = " ";
            }

            string allyHiddenList = "";
            string allyTomatoList = "";
            string allyUnicumList = "";
            string allyPositiveWatchList = "";
            string allyNegativeWatchList = "";
            string allyCheaterWatchList = "";
            int allyHiddenCount = 0;
            int allyTomatoCount = 0;
            int allyUnicumCount = 0;
            int allyPositiveWatchCount = 0;
            int allyNegativeWatchCount = 0;
            int allyCheaterWatchCount = 0;

            foreach (Player p in battlefield.Allies)
            {
                double winrate = (Properties.Settings.Default.WinrateTypeUsed == 0) ? p.AccountWinrate : p.WeightedWinrate;
                if (p.Relation == "0" && Properties.Settings.Default.OutputTextExcludeSelf)
                {
                    continue;
                }
                if (p.WatchStatus == WatchStatus.POSITIVE)
                {
                    if (Properties.Settings.Default.OutputTextShortMode && battlefield.Allies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                    {
                        allyPositiveWatchList = $"{allyPositiveWatchList}{p.ShipName}{strDelimter}";
                    }
                    else
                    {
                        allyPositiveWatchList = $"{allyPositiveWatchList}{p.ShipName}({p.Name}){strDelimter}";
                    }
                    allyPositiveWatchCount++;
                }
                if (p.WatchStatus == WatchStatus.NEGATIVE)
                {
                    if (Properties.Settings.Default.OutputTextShortMode && battlefield.Allies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                    {
                        allyNegativeWatchList = $"{allyNegativeWatchList}{p.ShipName}{strDelimter}";
                    }
                    else
                    {
                        allyNegativeWatchList = $"{allyNegativeWatchList}{p.ShipName}({p.Name}){strDelimter}";
                    }
                    allyNegativeWatchCount++;
                }
                if (p.WatchStatus == WatchStatus.CHEATER)
                {
                    if (Properties.Settings.Default.OutputTextShortMode && battlefield.Allies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                    {
                        allyCheaterWatchList = $"{allyCheaterWatchList}{p.ShipName}{strDelimter}";
                    }
                    else
                    {
                        allyCheaterWatchList = $"{allyCheaterWatchList}{p.ShipName}({p.Name}){strDelimter}";
                    }
                    allyCheaterWatchCount++;
                }
                if (p.IsHidden)
                {
                    if (Properties.Settings.Default.OutputTextShortMode && battlefield.Allies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                    {
                        allyHiddenList = $"{allyHiddenList}{p.ShipName}{strDelimter}";
                    }
                    else
                    {
                        allyHiddenList = $"{allyHiddenList}{p.ShipName}({p.Name}){strDelimter}";
                    }
                    allyHiddenCount++;
                }
                if (!p.IsHidden)
                {
                    if (winrate >= 0 && winrate <= Properties.Settings.Default.TomatoWinrateThreshold / 100 && p.Battles >= Properties.Settings.Default.TomatoBattleCountThreshold)
                    {
                        if (Properties.Settings.Default.OutputTextShortMode && battlefield.Allies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                        {
                            allyTomatoList = $"{allyTomatoList}{p.ShipName}{strDelimter}";
                        }
                        else
                        {
                            allyTomatoList = $"{allyTomatoList}{p.ShipName}({p.Name}){strDelimter}";
                        }
                        allyTomatoCount++;
                    }
                    if (winrate > Properties.Settings.Default.UnicumWinrateThreshold / 100 && p.Battles >= Properties.Settings.Default.UnicumBattleCountThreshold)
                    {
                        if (Properties.Settings.Default.OutputTextShortMode && battlefield.Allies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                        {
                            allyUnicumList = $"{allyUnicumList}{p.ShipName}{strDelimter}";
                        }
                        else
                        {
                            allyUnicumList = $"{allyUnicumList}{p.ShipName}({p.Name}){strDelimter}";
                        }
                        allyUnicumCount++;
                    }
                }
            }

            ProcessTextOutputPlayerList(ref allyHiddenList, allyHiddenCount, strNone!, strDelimter);
            ProcessTextOutputPlayerList(ref allyTomatoList, allyTomatoCount, strNone!, strDelimter);
            ProcessTextOutputPlayerList(ref allyUnicumList, allyUnicumCount, strNone!, strDelimter);
            ProcessTextOutputPlayerList(ref allyPositiveWatchList, allyPositiveWatchCount, strNone!, strDelimter);
            ProcessTextOutputPlayerList(ref allyNegativeWatchList, allyNegativeWatchCount, strNone!, strDelimter);
            ProcessTextOutputPlayerList(ref allyCheaterWatchList, allyCheaterWatchCount, strNone!, strDelimter);

            string enemyHiddenList = "";
            string enemyTomatoList = "";
            string enemyUnicumList = "";
            string enemyPositiveWatchList = "";
            string enemyNegativeWatchList = "";
            string enemyCheaterWatchList = "";
            int enemyHiddenCount = 0;
            int enemyTomatoCount = 0;
            int enemyUnicumCount = 0;
            int enemyPositiveWatchCount = 0;
            int enemyNegativeWatchCount = 0;
            int enemyCheaterWatchCount = 0;

            foreach (Player p in battlefield.Enemies)
            {
                double winrate = (Properties.Settings.Default.WinrateTypeUsed == 0) ? p.AccountWinrate : p.WeightedWinrate;
                if (p.WatchStatus == WatchStatus.POSITIVE)
                {
                    if (Properties.Settings.Default.OutputTextShortMode && battlefield.Enemies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                    {
                        enemyPositiveWatchList = $"{enemyPositiveWatchList}{p.ShipName}{strDelimter}";
                    }
                    else
                    {
                        enemyPositiveWatchList = $"{enemyPositiveWatchList}{p.ShipName}({p.Name}){strDelimter}";
                    }
                    enemyPositiveWatchCount++;
                }
                if (p.WatchStatus == WatchStatus.NEGATIVE)
                {
                    if (Properties.Settings.Default.OutputTextShortMode && battlefield.Enemies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                    {
                        enemyNegativeWatchList = $"{enemyNegativeWatchList}{p.ShipName}{strDelimter}";
                    }
                    else
                    {
                        enemyNegativeWatchList = $"{enemyNegativeWatchList}{p.ShipName}({p.Name}){strDelimter}";
                    }
                    enemyNegativeWatchCount++;
                }
                if (p.WatchStatus == WatchStatus.CHEATER)
                {
                    if (Properties.Settings.Default.OutputTextShortMode && battlefield.Enemies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                    {
                        enemyCheaterWatchList = $"{enemyCheaterWatchList}{p.ShipName}{strDelimter}";
                    }
                    else
                    {
                        enemyCheaterWatchList = $"{enemyCheaterWatchList}{p.ShipName}({p.Name}){strDelimter}";
                    }
                    enemyCheaterWatchCount++;
                }
                if (p.IsHidden)
                {
                    if (Properties.Settings.Default.OutputTextShortMode && battlefield.Enemies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                    {
                        enemyHiddenList = $"{enemyHiddenList}{p.ShipName}{strDelimter}";
                    }
                    else
                    {
                        enemyHiddenList = $"{enemyHiddenList}{p.ShipName}({p.Name}){strDelimter}";
                    }
                    enemyHiddenCount++;
                }
                if (!p.IsHidden)
                {
                    if (winrate >= 0 && winrate <= Properties.Settings.Default.TomatoWinrateThreshold / 100 && p.Battles >= Properties.Settings.Default.TomatoBattleCountThreshold)
                    {
                        if (Properties.Settings.Default.OutputTextShortMode && battlefield.Enemies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                        {
                            enemyTomatoList = $"{enemyTomatoList}{p.ShipName}{strDelimter}";
                        }
                        else
                        {
                            enemyTomatoList = $"{enemyTomatoList}{p.ShipName}({p.Name}){strDelimter}";
                        }
                        enemyTomatoCount++;
                    }
                    if (winrate > Properties.Settings.Default.UnicumWinrateThreshold / 100 && p.Battles >= Properties.Settings.Default.UnicumBattleCountThreshold)
                    {
                        if (Properties.Settings.Default.OutputTextShortMode && battlefield.Enemies.FindAll(x => x.ShipName == p.ShipName).Count <= 1)
                        {
                            enemyUnicumList = $"{enemyUnicumList}{p.ShipName}{strDelimter}";
                        }
                        else
                        {
                            enemyUnicumList = $"{enemyUnicumList}{p.ShipName}({p.Name}){strDelimter}";
                        }
                        enemyUnicumCount++;
                    }
                }
            }

            ProcessTextOutputPlayerList(ref enemyHiddenList, enemyHiddenCount, strNone!, strDelimter);
            ProcessTextOutputPlayerList(ref enemyTomatoList, enemyTomatoCount, strNone!, strDelimter);
            ProcessTextOutputPlayerList(ref enemyUnicumList, enemyUnicumCount, strNone!, strDelimter);
            ProcessTextOutputPlayerList(ref enemyPositiveWatchList, enemyPositiveWatchCount, strNone!, strDelimter);
            ProcessTextOutputPlayerList(ref enemyNegativeWatchList, enemyNegativeWatchCount, strNone!, strDelimter);
            ProcessTextOutputPlayerList(ref enemyCheaterWatchList, enemyCheaterWatchCount, strNone!, strDelimter);

            string outputText = Properties.Settings.Default.OutputTextTemplateGeneralStatistics;
            outputText = outputText.Replace("{TomatoWinrateThreshold}", (Properties.Settings.Default.TomatoWinrateThreshold / 100).ToString("p1"));
            outputText = outputText.Replace("{UnicumWinrateThreshold}", (Properties.Settings.Default.UnicumWinrateThreshold / 100).ToString("p1"));
            outputText = outputText.Replace("{AllyHiddenList}", allyHiddenList);
            outputText = outputText.Replace("{AllyTomatoList}", allyTomatoList);
            outputText = outputText.Replace("{AllyUnicumList}", allyUnicumList);
            outputText = outputText.Replace("{AllyPositiveWatchList}", allyPositiveWatchList);
            outputText = outputText.Replace("{AllyNegativeWatchList}", allyNegativeWatchList);
            outputText = outputText.Replace("{AllyCheaterWatchList}", allyCheaterWatchList);
            outputText = outputText.Replace("{EnemyHiddenList}", enemyHiddenList);
            outputText = outputText.Replace("{EnemyTomatoList}", enemyTomatoList);
            outputText = outputText.Replace("{EnemyUnicumList}", enemyUnicumList);
            outputText = outputText.Replace("{EnemyPositiveWatchList}", enemyPositiveWatchList);
            outputText = outputText.Replace("{EnemyNegativeWatchList}", enemyNegativeWatchList);
            outputText = outputText.Replace("{EnemyCheaterWatchList}", enemyCheaterWatchList);
            outputText = outputText.Replace("{AllyHiddenCount}", allyHiddenCount.ToString());
            outputText = outputText.Replace("{AllyTomatoCount}", allyTomatoCount.ToString());
            outputText = outputText.Replace("{AllyUnicumCount}", allyUnicumCount.ToString());
            outputText = outputText.Replace("{AllyPositiveWatchCount}", allyPositiveWatchCount.ToString());
            outputText = outputText.Replace("{AllyNegativeWatchCount}", allyNegativeWatchCount.ToString());
            outputText = outputText.Replace("{AllyCheaterWatchCount}", allyCheaterWatchCount.ToString());
            outputText = outputText.Replace("{EnemyHiddenCount}", enemyHiddenCount.ToString());
            outputText = outputText.Replace("{EnemyTomatoCount}", enemyTomatoCount.ToString());
            outputText = outputText.Replace("{EnemyUnicumCount}", enemyUnicumCount.ToString());
            outputText = outputText.Replace("{EnemyPositiveWatchCount}", enemyPositiveWatchCount.ToString());
            outputText = outputText.Replace("{EnemyNegativeWatchCount}", enemyNegativeWatchCount.ToString());
            outputText = outputText.Replace("{EnemyCheaterWatchCount}", enemyCheaterWatchCount.ToString());
            outputText = outputText.Replace("{AllyAvgAccountWinrate}", battlefield.AllyAvgAccountWinrate.ToString("p2"));
            outputText = outputText.Replace("{EnemyAvgAccountWinrate}", battlefield.EnemyAvgAccountWinrate.ToString("p2"));
            outputText = outputText.Replace("{AllyAvgWeightedWinrate}", battlefield.AllyAvgWeightedWinrate.ToString("p2"));
            outputText = outputText.Replace("{EnemyAvgWeightedWinrate}", battlefield.EnemyAvgWeightedWinrate.ToString("p2"));
            outputText = outputText.Replace("{AllyAvgBattles}", battlefield.AllyAvgBattleCount.ToString("f1"));
            outputText = outputText.Replace("{EnemyAvgBattles}", battlefield.EnemyAvgBattleCount.ToString("f1"));

            //split text every 140 char
            for (int i = outputText.Length / 140; i > 0; i--)
            {
                outputText = outputText.Insert(i * 140, "\n\n");
            }

            return outputText;
        }

        public static string GenerateParticularPlayerStatisticsOutputText(Player p)
        {
            string OutputText = Properties.Settings.Default.OutputTextTemplateParticularPlayerStatistics;
            OutputText = OutputText.Replace("{PlayerName}", p.Name);
            OutputText = OutputText.Replace("{ShipName}", p.ShipName);
            OutputText = OutputText.Replace("{ClanTag}", p.ClanTag);

            OutputText = OutputText.Replace("{AccountBattles}", p.Battles < 0 ? " - " : p.Battles.ToString());
            OutputText = OutputText.Replace("{ShipBattles}", p.ShipBattles < 0 ? " - " : p.ShipBattles.ToString());
            OutputText = OutputText.Replace("{AccountWinrate}", p.AccountWinrate < 0 ? " - " : p.AccountWinrate.ToString("p2"));
            OutputText = OutputText.Replace("{WeightedWinrate}", p.WeightedWinrate < 0 ? " - " : p.WeightedWinrate.ToString("p2"));
            OutputText = OutputText.Replace("{ShipWinrate}", p.ShipWinrate < 0 ? " - " : p.ShipWinrate.ToString("p2"));
            OutputText = OutputText.Replace("{ShipAvgDmg}", p.ShipAvgDmgPerBattle < 0 ? " - " : p.ShipAvgDmgPerBattle.ToString("f0"));
            return OutputText;
        }
    }
}
