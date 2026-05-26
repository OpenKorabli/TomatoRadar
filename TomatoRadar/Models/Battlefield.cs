using TomatoRadar.Utils;
using LiveChartsCore.Defaults;
using System;
using System.Collections.Generic;

namespace TomatoRadar.Models
{
    internal class Battlefield
    {
        public string BattleType { get; set; }
        public DateTimeOffset BattleStartTime { get; set; }
        public double AllyAvgAccountWinrate { get; set; }
        public double EnemyAvgAccountWinrate { get; set; }
        public double AllyAvgWeightedWinrate { get; set; }
        public double EnemyAvgWeightedWinrate { get; set; }
        public double AllyAvgBattleCount { get; set; }
        public double EnemyAvgBattleCount { get; set; }

        public List<Player> Allies { get; set; }
        public List<Player> Enemies { get; set; }

        private int allyPlayersWithValidWinrate;
        private int enemyPlayersWithValidWinrate;
        private int maxTeamPlayersCount;

        private int SortPlayersByWinrateAscending(Player? a, Player? b)
        {
            double va = (Properties.Settings.Default.WinrateTypeUsed == 0) ? a!.AccountWinrate : a!.WeightedWinrate;
            double vb = (Properties.Settings.Default.WinrateTypeUsed == 0) ? b!.AccountWinrate : b!.WeightedWinrate;
            return va.CompareTo(vb);
        }

        private int SortPlayersByWinrateDescending(Player? a, Player? b)
        {
            double va = (Properties.Settings.Default.WinrateTypeUsed == 0) ? a!.AccountWinrate : a!.WeightedWinrate;
            double vb = (Properties.Settings.Default.WinrateTypeUsed == 0) ? b!.AccountWinrate : b!.WeightedWinrate;
            return vb.CompareTo(va);
        }

        private int SortPlayersByShipTypeAndWinrateDescending(Player? a, Player? b)
        {
            return a!.ShipType != b!.ShipType ? a.ShipType.CompareTo(b.ShipType) : SortPlayersByWinrateDescending(a, b);
        }

        private int SortPlayersByShipTypeAndShipTierAndWinrateDescending(Player? a, Player? b)
        {
            return a!.ShipType != b!.ShipType ? a.ShipType.CompareTo(b.ShipType) : a!.ShipTier != b!.ShipTier ? b.ShipTier - a.ShipTier : SortPlayersByWinrateDescending(a, b);
        }

        private int SortPlayersByShipTierAndWinrateDescending(Player? a, Player? b)
        {
            return a!.ShipTier != b!.ShipTier ? b.ShipTier - a.ShipTier : SortPlayersByWinrateDescending(a, b);
        }

        private int SortPlayersByShipTierAndShipTypeAndWinrateDescending(Player? a, Player? b)
        {
            return a!.ShipTier != b!.ShipTier ? b.ShipTier - a.ShipTier : a.ShipType != b.ShipType ? a.ShipType.CompareTo(b.ShipType) : SortPlayersByWinrateDescending(a, b);
        }

        private void SortPlayers(List<Player?> playerList, int sortBy)
        {
            playerList.Sort(sortBy switch
            {
                0 => SortPlayersByShipTypeAndShipTierAndWinrateDescending,
                1 => SortPlayersByShipTierAndShipTypeAndWinrateDescending,
                2 => SortPlayersByShipTypeAndWinrateDescending,
                3 => SortPlayersByShipTierAndWinrateDescending,
                4 => SortPlayersByWinrateDescending,
                5 => SortPlayersByWinrateAscending,
                _ => throw new ArgumentException(),
            });
        }

        private static bool ComparePlayers(Player a, Player b, int compareBy)
        {
            return compareBy switch
            {
                0 => a.ShipType == b.ShipType && a.ShipTier == b.ShipTier,
                1 => a.ShipType == b.ShipType && a.ShipTier == b.ShipTier,
                2 => a.ShipType == b.ShipType,
                3 => a.ShipTier == b.ShipTier,
                4 => throw new ArgumentException(),
                5 => throw new ArgumentException(),
                _ => throw new ArgumentException(),
            };
        }
        private static ObservablePoint[] KernelDensityEstimation(double[] data, double MAX, double MIN, double sigma, int nsteps)
        {
            // based on ksandric's code
            // https://gist.github.com/ksandric/e91860143f1dd378645c01d518ddf013

            ObservablePoint[] result = new ObservablePoint[nsteps];
            double[] x = new double[nsteps], y = new double[nsteps];

            int N = data.Length;

            x[0] = MIN;
            for (int i = 1; i < nsteps; i++)
            {
                x[i] = x[i - 1] + ((MAX - MIN) / nsteps);
            }

            double c = 1.0 / (Math.Sqrt(2 * Math.PI * sigma * sigma));
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < nsteps; j++)
                {
                    y[j] = y[j] + 1.0 / N * c * Math.Exp(-(data[i] - x[j]) * (data[i] - x[j]) / (2 * sigma * sigma));
                }
            }

            for (int i = 0; i < nsteps; i++)
            {
                result[i] = new ObservablePoint(x[i], y[i]);
            }
            return result;
        }

        public List<double[]> GetSectionsForPlot(int sortBy)
        {
            List<double[]> result = new();
            List<Player?> alliesList = new(Allies);
            List<Player?> enemiesList = new(Enemies);
            if (sortBy == 4 || sortBy == 5 || enemiesList.Count != 0 && alliesList.Count != enemiesList.Count)
            {
                return result;
            }
            SortPlayers(alliesList, sortBy);
            SortPlayers(enemiesList, sortBy);
            double[] section = { -0.5, 0 };
            for (int i = 0; i < alliesList.Count - 1; i++)
            {
                if (!ComparePlayers(alliesList[i]!, alliesList[i + 1]!, sortBy) || enemiesList.Count != 0 && !ComparePlayers(enemiesList[i]!, enemiesList[i + 1]!, sortBy))
                {
                    section[1] = i + 0.5;
                    result.Add(new double[] { section[0], section[1] });
                    section[0] = i + 0.5;
                }
            }
            section[1] = alliesList.Count - 0.5;
            result.Add(section);
            return result;
        }

        public List<List<Player?>> GetPlayersForPlot(int sortBy)
        {
            List<Player?> alliesList = new(Allies);
            SortPlayers(alliesList, sortBy);
            for (int i = 0; i < alliesList.Count; i++)
            {
                if (alliesList[i]!.AccountWinrate >= 0)
                {
                    alliesList[i]!.PlotXPosition = sortBy switch
                    {
                        0 => i,
                        1 => i,
                        2 => i,
                        3 => i,
                        4 => allyPlayersWithValidWinrate == 1 ? (maxTeamPlayersCount - 1) / 2.0 : (i + maxTeamPlayersCount - Allies.Count) * 1.0 * (maxTeamPlayersCount - 1) / (allyPlayersWithValidWinrate - 1),
                        5 => allyPlayersWithValidWinrate == 1 ? (maxTeamPlayersCount - 1) / 2.0 : (i + allyPlayersWithValidWinrate - Allies.Count) * 1.0 * (maxTeamPlayersCount - 1) / (allyPlayersWithValidWinrate - 1),
                        _ => throw new ArgumentException(),
                    };
                }
                else
                {
                    alliesList[i] = null;
                }
            }
            List<Player?> enemiesList = new(Enemies);
            SortPlayers(enemiesList, sortBy);
            for (int i = 0; i < enemiesList.Count; i++)
            {
                if (enemiesList[i]!.AccountWinrate >= 0)
                {
                    enemiesList[i]!.PlotXPosition = sortBy switch
                    {
                        0 => i,
                        1 => i,
                        2 => i,
                        3 => i,
                        4 => enemyPlayersWithValidWinrate == 1 ? (maxTeamPlayersCount - 1) / 2.0 : (i + maxTeamPlayersCount - Enemies.Count) * 1.0 * (maxTeamPlayersCount - 1) / (enemyPlayersWithValidWinrate - 1),
                        5 => enemyPlayersWithValidWinrate == 1 ? (maxTeamPlayersCount - 1) / 2.0 : (i + enemyPlayersWithValidWinrate - Enemies.Count) * 1.0 * (maxTeamPlayersCount - 1) / (enemyPlayersWithValidWinrate - 1),
                        _ => throw new ArgumentException(),
                    };
                }
                else
                {
                    enemiesList[i] = null;
                }
            }
            List<List<Player?>> result = new()
            {
                alliesList,
                enemiesList
            };
            return result;
        }

        public List<ObservablePoint[]> GetPlayersKDEForPlot()
        {
            double maxWinrate = 0;
            double minWinrate = 1;
            List<double> dataAllies = new();
            List<double> dataEnemies = new();
            foreach (Player p in Allies)
            {
                if (p.AccountWinrate >= 0)
                {
                    double winrate = (Properties.Settings.Default.WinrateTypeUsed == 0) ? p.AccountWinrate : p.WeightedWinrate;
                    if (winrate > maxWinrate)
                    {
                        maxWinrate = winrate;
                    }
                    if (winrate < minWinrate)
                    {
                        minWinrate = winrate;
                    }
                    dataAllies.Add(winrate);
                }
            }
            foreach (Player p in Enemies)
            {
                if (p.AccountWinrate >= 0)
                {
                    double winrate = (Properties.Settings.Default.WinrateTypeUsed == 0) ? p.AccountWinrate : p.WeightedWinrate;
                    if (winrate > maxWinrate)
                    {
                        maxWinrate = winrate;
                    }
                    if (winrate < minWinrate)
                    {
                        minWinrate = winrate;
                    }
                    dataEnemies.Add(winrate);
                }
            }
            double winrateSpan = maxWinrate - minWinrate;
            maxWinrate += winrateSpan * 0.3;
            if (maxWinrate > 1)
            {
                maxWinrate = 1;
            }
            minWinrate -= winrateSpan * 0.3;
            if (minWinrate < 0)
            {
                minWinrate = 0;
            }
            List<ObservablePoint[]> result = new();
            if (dataAllies.Count > 0)
            {
                result.Add(KernelDensityEstimation(dataAllies.ToArray(), maxWinrate, minWinrate, 0.025, 100));
            }
            else
            {
                result.Add(Array.Empty<ObservablePoint>());
            }
            if (dataEnemies.Count > 0)
            {
                result.Add(KernelDensityEstimation(dataEnemies.ToArray(), maxWinrate, minWinrate, 0.025, 100));
            }
            else
            {
                result.Add(Array.Empty<ObservablePoint>());
            }
            return result;
        }

        public string GetBattlefieldInfoStrForYuyukoApiPush()
        {
            string playerList = "";
            foreach (Player p in Allies)
            {
                playerList = $"{playerList}{p.GetPlayerInfoStrForYuyukoApiPush()}, ";
            }
            foreach (Player p in Enemies)
            {
                playerList = $"{playerList}{p.GetPlayerInfoStrForYuyukoApiPush()}, ";
            }
            playerList = playerList.Remove(playerList.Length - 2, 2);
            return $@"{{""battleType"": ""{BattleType}"", ""time"": ""{BattleStartTime.ToUnixTimeMilliseconds()}"", ""infoList"": [{playerList}]}}";
        }

        public Battlefield(string battleType, DateTimeOffset battleStartTime, List<Player> playerList)
        {
            BattleType = battleType;
            BattleStartTime = battleStartTime;

            Allies = new List<Player>();
            Enemies = new List<Player>();

            allyPlayersWithValidWinrate = 0;
            enemyPlayersWithValidWinrate = 0;

            AllyAvgAccountWinrate = 0;
            AllyAvgWeightedWinrate = 0;
            AllyAvgBattleCount = 0;

            foreach (Player p in playerList)
            {
                var shipLanguagePriority = LanguageExt.ParsePriority(Properties.Settings.Default.ShipNameLanguage);
                p.ShipName = ShipInfoUtils.GetShipNameByID(p.ShipID, shipLanguagePriority, p.Server);
                p.ShipType = ShipInfoUtils.GetShipTypeByID(p.ShipID, p.Server);
                p.ShipTier = ShipInfoUtils.GetShipTierByID(p.ShipID, p.Server);

                if (p.Relation == "0" || p.Relation == "1")
                {
                    Allies.Add(p);
                    if (p.AccountWinrate >= 0)
                    {
                        allyPlayersWithValidWinrate++;
                    }
                }
                else
                {
                    Enemies.Add(p);
                    if (p.AccountWinrate >= 0)
                    {
                        enemyPlayersWithValidWinrate++;
                    }
                }
            }

            maxTeamPlayersCount = Allies.Count > Enemies.Count ? Allies.Count : Enemies.Count;

            for (int i = 0; i < Allies.Count; i++)
            {
                if (Allies[i].AccountWinrate >= 0)
                {
                    AllyAvgAccountWinrate += Allies[i].AccountWinrate;
                    AllyAvgWeightedWinrate += Allies[i].WeightedWinrate;
                    AllyAvgBattleCount += Allies[i].Battles;
                }
            }
            AllyAvgAccountWinrate /= allyPlayersWithValidWinrate;
            AllyAvgWeightedWinrate /= allyPlayersWithValidWinrate;
            AllyAvgBattleCount /= allyPlayersWithValidWinrate;

            EnemyAvgAccountWinrate = 0;
            EnemyAvgWeightedWinrate = 0;
            EnemyAvgBattleCount = 0;

            for (int i = 0; i < Enemies.Count; i++)
            {
                if (Enemies[i].AccountWinrate >= 0)
                {
                    EnemyAvgAccountWinrate += Enemies[i].AccountWinrate;
                    EnemyAvgWeightedWinrate += Enemies[i].WeightedWinrate;
                    EnemyAvgBattleCount += Enemies[i].Battles;
                }
            }

            EnemyAvgAccountWinrate /= enemyPlayersWithValidWinrate;
            EnemyAvgWeightedWinrate /= enemyPlayersWithValidWinrate;
            EnemyAvgBattleCount /= enemyPlayersWithValidWinrate;

            Allies.Sort(SortPlayersByShipTypeAndShipTierAndWinrateDescending);
            Enemies.Sort(SortPlayersByShipTypeAndShipTierAndWinrateDescending);

            foreach (Player p in Allies)
            {
                LogUtils.WriteDebug($"player:{p}");
            }
            foreach (Player p in Enemies)
            {
                LogUtils.WriteDebug($"player:{p}");
            }
        }

        public void AddPlayers(List<Player> newPlayers)
        {
            foreach (Player p in newPlayers)
            {
                var shipLanguagePriority = LanguageExt.ParsePriority(Properties.Settings.Default.ShipNameLanguage);
                p.ShipName = ShipInfoUtils.GetShipNameByID(p.ShipID, shipLanguagePriority, p.Server);
                p.ShipType = ShipInfoUtils.GetShipTypeByID(p.ShipID, p.Server);
                p.ShipTier = ShipInfoUtils.GetShipTierByID(p.ShipID, p.Server);

                if (p.Relation == "0" || p.Relation == "1")
                {
                    Allies.Add(p);
                    if (p.AccountWinrate >= 0)
                    {
                        allyPlayersWithValidWinrate++;
                    }
                }
                else
                {
                    Enemies.Add(p);
                    if (p.AccountWinrate >= 0)
                    {
                        enemyPlayersWithValidWinrate++;
                    }
                }
            }

            maxTeamPlayersCount = Allies.Count > Enemies.Count ? Allies.Count : Enemies.Count;

            AllyAvgAccountWinrate = 0;
            AllyAvgWeightedWinrate = 0;
            AllyAvgBattleCount = 0;
            for (int i = 0; i < Allies.Count; i++)
            {
                if (Allies[i].AccountWinrate >= 0)
                {
                    AllyAvgAccountWinrate += Allies[i].AccountWinrate;
                    AllyAvgWeightedWinrate += Allies[i].WeightedWinrate;
                    AllyAvgBattleCount += Allies[i].Battles;
                }
            }
            if (allyPlayersWithValidWinrate > 0)
            {
                AllyAvgAccountWinrate /= allyPlayersWithValidWinrate;
                AllyAvgWeightedWinrate /= allyPlayersWithValidWinrate;
                AllyAvgBattleCount /= allyPlayersWithValidWinrate;
            }

            EnemyAvgAccountWinrate = 0;
            EnemyAvgWeightedWinrate = 0;
            EnemyAvgBattleCount = 0;
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (Enemies[i].AccountWinrate >= 0)
                {
                    EnemyAvgAccountWinrate += Enemies[i].AccountWinrate;
                    EnemyAvgWeightedWinrate += Enemies[i].WeightedWinrate;
                    EnemyAvgBattleCount += Enemies[i].Battles;
                }
            }
            if (enemyPlayersWithValidWinrate > 0)
            {
                EnemyAvgAccountWinrate /= enemyPlayersWithValidWinrate;
                EnemyAvgWeightedWinrate /= enemyPlayersWithValidWinrate;
                EnemyAvgBattleCount /= enemyPlayersWithValidWinrate;
            }

            LogUtils.WriteInfo($"FogOfWar: Added {newPlayers.Count} new players. Allies={Allies.Count}, Enemies={Enemies.Count}");
        }
    }
}
