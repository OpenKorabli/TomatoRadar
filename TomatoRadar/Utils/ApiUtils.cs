using TomatoRadar.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace TomatoRadar.Utils
{
    static internal class ApiUtils
    {
        private static double CalcWeightedWinrate(double accountWinrateSolo, double accountBattlesSolo, double accountWinrateDiv2, double accountBattlesDiv2, double accountWinrateDiv3, double accountBattlesDiv3, double shipWinrate, double shipBattles)
        {
            double accountSoloWeight = accountBattlesSolo * Properties.Settings.Default.WeightedWinrateAccountSoloWeightMultiplier;
            double accountDiv2Weight = accountBattlesDiv2 * Properties.Settings.Default.WeightedWinrateAccountDiv2WeightMultiplier;
            double accountDiv3Weight = accountBattlesDiv3 * Properties.Settings.Default.WeightedWinrateAccountDiv3WeightMultiplier;
            double accountWeightedWinrate = (accountWinrateSolo * accountSoloWeight + accountWinrateDiv2 * accountDiv2Weight + accountWinrateDiv3 * accountDiv3Weight) / (accountSoloWeight + accountDiv2Weight + accountDiv3Weight);
            double shipWeight = ((shipBattles >= Properties.Settings.Default.WeightedWinrateShipBattlesAtMaxWeight) ? Properties.Settings.Default.WeightedWinrateShipMaxWeight : Properties.Settings.Default.WeightedWinrateShipMaxWeight * shipBattles / Properties.Settings.Default.WeightedWinrateShipBattlesAtMaxWeight) / 100;
            return accountWeightedWinrate * (1 - shipWeight) + shipWinrate * shipWeight;
        }

        public static APIType GetApiTypeForServer(Server server)
        {
            if (server == Server.RU)
            {
                return APITypeExt.GetAPITypeByName(Properties.Settings.Default.APITypeSelectionRU);
            }
            if (server == Server.CN)
            {
                return APITypeExt.GetAPITypeByName(Properties.Settings.Default.APITypeSelectionCN);
            }
            return APITypeExt.GetAPITypeByName(Properties.Settings.Default.APITypeSelectionDefault);
        }

        // TODO: Lesta Public API currently mirrors WG Public API structure. Application ID and URL paths may need updating.
        public async static Task<List<Player>> LestaPublicApiGetPlayersStatistics(int playerCount, int relationFilter, JObject JObjectPlayers, Server server, int delayTimeBetweenHttpRequests)
        {
            const string LESTA_PUBLIC_API_APPLICATION_ID = "447ec579e994976e39dec0e7d0bac644";

            LogUtils.WriteInfo("Lesta Public API");
            string serverUrlString = ServerExt.GetFullUrlStringByServer(server);
            List<Player> playerList = new();

            for (int i = 0; i < playerCount; i++)
            {
                if (JObjectPlayers["vehicles"]![i]!["id"]!.Value<int>() > 30)
                {
                    int relation = JObjectPlayers["vehicles"]![i]!["relation"]!.Value<int>();
                    if (relationFilter == 0 || relationFilter == 1 && relation <= 1 || relationFilter == 2 && relation > 1)
                    {
                        playerList.Add(new Player(JObjectPlayers["vehicles"]![i]!["name"]!.Value<string>()!, server, JObjectPlayers["vehicles"]![i]!["relation"]!.Value<string>()!, JObjectPlayers["vehicles"]![i]!["shipId"]!.Value<string>()!));
                    }
                }
            }

            string playerNameList = "";
            string requestUrl;
            string responseBodyAsText;

            foreach (Player p in playerList)
            {
                if (p.Name[..1] != ":")
                {
                    playerNameList = playerNameList + p.Name + "%2C";
                }
            }

            playerNameList = playerNameList.Remove(playerNameList.Length - 3);
            LogUtils.WriteDebug($"playerNameList={playerNameList}");

            requestUrl = $"https://api.{serverUrlString}/wows/account/list/?application_id={LESTA_PUBLIC_API_APPLICATION_ID}&type=exact&search={playerNameList}";

            responseBodyAsText = await NetworkUtils.HttpGet(requestUrl);
            await Task.Delay(delayTimeBetweenHttpRequests);

            LogUtils.WriteDebug($"LestaPublicApiGetPlayersID Response:{responseBodyAsText}");
            JObject JObjectLestaPublicApiPlayersIDList = JsonUtils.Parse(responseBodyAsText);
            if (JObjectLestaPublicApiPlayersIDList["status"]!.Value<string>() != "ok")
            {
                return new List<Player>();
            }
            int dataExistPlayerCount = Convert.ToInt32(JObjectLestaPublicApiPlayersIDList["meta"]!["count"]!);
            string playerIdList = "";
            LogUtils.WriteDebug($"dataExistPlayerCount={dataExistPlayerCount}");
            for (int i = 0; i < dataExistPlayerCount; i++)
            {
                Player? p = playerList.Find(a => a.Name == JObjectLestaPublicApiPlayersIDList["data"]![i]!["nickname"]!.Value<string>()!);
                if (p != null)
                {
                    p.ID = JObjectLestaPublicApiPlayersIDList["data"]![i]!["account_id"]!.Value<string>()!;
                    playerIdList = playerIdList + JObjectLestaPublicApiPlayersIDList["data"]![i]!["account_id"]!.Value<string>() + "%2C";
                }
            }

            if (playerIdList == "")
            {
                return new List<Player>();
            }

            playerIdList = playerIdList.Remove(playerIdList.Length - 3);

            LogUtils.WriteDebug($"playerIdList={playerIdList}");

            requestUrl = $"https://api.{serverUrlString}/wows/account/info/?application_id={LESTA_PUBLIC_API_APPLICATION_ID}&extra=statistics.pvp_solo%2Cstatistics.pvp_div2%2Cstatistics.pvp_div3&fields=hidden_profile%2Cstatistics.pvp.wins%2Cstatistics.pvp.battles%2Cstatistics.pvp_solo.wins%2Cstatistics.pvp_solo.battles%2Cstatistics.pvp_div2.wins%2Cstatistics.pvp_div2.battles%2Cstatistics.pvp_div3.wins%2Cstatistics.pvp_div3.battles&account_id={playerIdList}";

            responseBodyAsText = await NetworkUtils.HttpGet(requestUrl);
            await Task.Delay(delayTimeBetweenHttpRequests);
            LogUtils.WriteDebug($"LestaPublicApiGetPlayersAccountData Response:{responseBodyAsText}");
            JObject JObjectLestaPublicApiPlayersAccountDataList = JsonUtils.Parse(responseBodyAsText);

            requestUrl = $"https://api.{serverUrlString}/wows/clans/accountinfo/?application_id={LESTA_PUBLIC_API_APPLICATION_ID}&extra=clan&fields=clan_id%2Cclan.tag&account_id={playerIdList}";
            responseBodyAsText = await NetworkUtils.HttpGet(requestUrl);
            await Task.Delay(delayTimeBetweenHttpRequests);
            LogUtils.WriteDebug($"LestaPublicApiGetPlayersClanData Response:{responseBodyAsText}");
            JObject JObjectLestaPublicApiPlayersClanDataList = JsonUtils.Parse(responseBodyAsText);

            foreach (Player p in playerList)
            {

                if (p.Name[..1] != ":" && p.ID != "-1")
                {
                    if (JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!.HasValues && JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["hidden_profile"]!.Value<string>() != "true" && JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!.HasValues)
                    {
                        if (JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp"]!.HasValues)
                        {
                            p.Wins = JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp"]!["wins"]!.Value<double>();
                            p.Battles = JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp"]!["battles"]!.Value<double>();
                            if (p.Battles == 0)
                            {
                                p.AccountWinrate = 0;
                            }
                            else
                            {
                                p.AccountWinrate = p.Wins / p.Battles;
                            }
                        }
                        else
                        {
                            p.Wins = 0;
                            p.Battles = 0;
                            p.AccountWinrate = 0;
                        }

                        if (JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_solo"]!.HasValues)
                        {
                            p.Wins_Solo = JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_solo"]!["wins"]!.Value<double>();
                            p.Battles_Solo = JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_solo"]!["battles"]!.Value<double>();

                            if (p.Battles_Solo == 0)
                            {
                                p.AccountWinrate_Solo = 0;
                            }
                            else
                            {
                                p.AccountWinrate_Solo = p.Wins_Solo / p.Battles_Solo;
                            }
                        }
                        else
                        {
                            p.Wins_Solo = 0;
                            p.Battles_Solo = 0;
                            p.AccountWinrate_Solo = 0;
                        }


                        if (JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div2"]!.HasValues)
                        {
                            p.Wins_Div2 = JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div2"]!["wins"]!.Value<double>();
                            p.Battles_Div2 = JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div2"]!["battles"]!.Value<double>();

                            if (p.Battles_Div2 == 0)
                            {
                                p.AccountWinrate_Div2 = 0;
                            }
                            else
                            {
                                p.AccountWinrate_Div2 = p.Wins_Div2 / p.Battles_Div2;
                            }
                        }
                        else
                        {
                            p.Wins_Div2 = 0;
                            p.Battles_Div2 = 0;
                            p.AccountWinrate_Div2 = 0;
                        }

                        if (JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div3"]!.HasValues)
                        {
                            p.Wins_Div3 = JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div3"]!["wins"]!.Value<double>();
                            p.Battles_Div3 = JObjectLestaPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div3"]!["battles"]!.Value<double>();

                            if (p.Battles_Div3 == 0)
                            {
                                p.AccountWinrate_Div3 = 0;
                            }
                            else
                            {
                                p.AccountWinrate_Div3 = p.Wins_Div3 / p.Battles_Div3;
                            }

                        }
                        else
                        {
                            p.Wins_Div3 = 0;
                            p.Battles_Div3 = 0;
                            p.AccountWinrate_Div3 = 0;
                        }
                    }
                    else
                    {
                        p.IsHidden = true;
                    }

                    if (JObjectLestaPublicApiPlayersClanDataList["data"]![p.ID]!.HasValues && JObjectLestaPublicApiPlayersClanDataList["data"]![p.ID]!["clan"]!.HasValues)
                    {
                        p.ClanID = JObjectLestaPublicApiPlayersClanDataList["data"]![p.ID]!["clan_id"]!.Value<string>()!;
                        p.ClanTag = $"[{JObjectLestaPublicApiPlayersClanDataList["data"]![p.ID]!["clan"]!["tag"]!.Value<string>()}]";
                    }
                }
            }

            List<Task<string>> taskListLestaPublicApiGetPlayersShipsData = new();

            foreach (Player p in playerList)
            {
                if (p.Name[..1] != ":" && p.ID != "-1")
                {
                    requestUrl = $"https://api.{serverUrlString}/wows/ships/stats/?application_id={LESTA_PUBLIC_API_APPLICATION_ID}&extra=pvp_solo%2Cpvp_div2%2Cpvp_div3&fields=pvp.wins%2Cpvp.battles%2Cpvp.damage_dealt%2Cpvp_solo.wins%2Cpvp_solo.battles%2Cpvp_solo.damage_dealt%2Cpvp_div2.wins%2Cpvp_div2.battles%2Cpvp_div2.damage_dealt%2Cpvp_div3.wins%2Cpvp_div3.battles%2Cpvp_div3.damage_dealt&account_id={p.ID}&ship_id={p.ShipID}";
                    taskListLestaPublicApiGetPlayersShipsData.Add(NetworkUtils.HttpGet(requestUrl));
                    await Task.Delay(delayTimeBetweenHttpRequests);
                }
            }

            await Task.WhenAll(taskListLestaPublicApiGetPlayersShipsData);

            foreach (Player p in playerList)
            {
                if (p.Name[..1] != ":" && p.ID != "-1")
                {
                    LogUtils.WriteDebug($"LestaPublicApiGetPlayersShipsData Response:{taskListLestaPublicApiGetPlayersShipsData[0].Result}");
                    JObject JObjectLestaPublicApiPlayerShipsData = JsonUtils.Parse(taskListLestaPublicApiGetPlayersShipsData[0].Result);
                    taskListLestaPublicApiGetPlayersShipsData.RemoveAt(0);

                    if (JObjectLestaPublicApiPlayerShipsData["status"]!.Value<string>() == "ok" && JObjectLestaPublicApiPlayerShipsData["meta"]!["count"]!.Value<string>() == "1" && JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]!.HasValues)
                    {
                        p.ShipWins = JObjectLestaPublicApiPlayerShipsData!["data"]![p.ID]![0]!["pvp"]!["wins"]!.Value<double>();
                        p.ShipBattles = JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp"]!["battles"]!.Value<double>();
                        p.ShipTotalDmg = JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp"]!["damage_dealt"]!.Value<double>();
                        if (p.ShipBattles == 0)
                        {
                            p.ShipWinrate = 0;
                            p.ShipAvgDmgPerBattle = 0;
                            p.WeightedWinrate = p.AccountWinrate;
                        }
                        else
                        {
                            p.ShipWinrate = p.ShipWins / p.ShipBattles;
                            p.ShipAvgDmgPerBattle = p.ShipTotalDmg / p.ShipBattles;
                            p.WeightedWinrate = CalcWeightedWinrate(p.AccountWinrate_Solo, p.Battles_Solo, p.AccountWinrate_Div2, p.Battles_Div2, p.AccountWinrate_Div3, p.Battles_Div3, p.ShipWinrate, p.ShipBattles);
                        }

                        p.ShipWins_Solo = JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_solo"]!["wins"]!.Value<double>();
                        p.ShipBattles_Solo = JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_solo"]!["battles"]!.Value<double>();
                        p.ShipTotalDmg_Solo = JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_solo"]!["damage_dealt"]!.Value<double>();
                        if (p.ShipBattles_Solo == 0)
                        {
                            p.ShipWinrate_Solo = 0;
                            p.ShipAvgDmgPerBattle_Solo = 0;
                        }
                        else
                        {
                            p.ShipWinrate_Solo = p.ShipWins_Solo / p.ShipBattles_Solo;
                            p.ShipAvgDmgPerBattle_Solo = p.ShipTotalDmg_Solo / p.ShipBattles_Solo;
                        }

                        p.ShipWins_Div2 = JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div2"]!["wins"]!.Value<double>();
                        p.ShipBattles_Div2 = JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div2"]!["battles"]!.Value<double>();
                        p.ShipTotalDmg_Div2 = JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div2"]!["damage_dealt"]!.Value<double>();
                        if (p.ShipBattles_Div2 == 0)
                        {
                            p.ShipWinrate_Div2 = 0;
                            p.ShipAvgDmgPerBattle_Div2 = 0;
                        }
                        else
                        {
                            p.ShipWinrate_Div2 = p.ShipWins_Div2 / p.ShipBattles_Div2;
                            p.ShipAvgDmgPerBattle_Div2 = p.ShipTotalDmg_Div2 / p.ShipBattles_Div2;
                        }

                        p.ShipWins_Div3 = JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div3"]!["wins"]!.Value<double>();
                        p.ShipBattles_Div3 = JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div3"]!["battles"]!.Value<double>();
                        p.ShipTotalDmg_Div3 = JObjectLestaPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div3"]!["damage_dealt"]!.Value<double>();
                        if (p.ShipBattles_Div3 == 0)
                        {
                            p.ShipWinrate_Div3 = 0;
                            p.ShipAvgDmgPerBattle_Div3 = 0;
                        }
                        else
                        {
                            p.ShipWinrate_Div3 = p.ShipWins_Div3 / p.ShipBattles_Div3;
                            p.ShipAvgDmgPerBattle_Div3 = p.ShipTotalDmg_Div3 / p.ShipBattles_Div3;
                        }
                    }
                    else
                    {
                        if (p.IsHidden == false)
                        {
                            p.ShipWins = 0;
                            p.ShipBattles = 0;
                            p.ShipTotalDmg = 0;
                            p.ShipAvgDmgPerBattle = 0;
                            p.ShipWinrate = 0;
                            p.WeightedWinrate = p.AccountWinrate;
                        }
                    }
                    LogUtils.WriteDebug($"player:{p}");
                }
            }
            return playerList;
        }

        public async static Task<List<Player>> WgPublicApiGetPlayersStatistics(int playerCount, int relationFilter, JObject JObjectPlayers, Server server, int delayTimeBetweenHttpRequests, bool useYuyukoProxy)
        {
            const string WG_PUBLIC_API_APPLICATION_ID = "447ec579e994976e39dec0e7d0bac644";
            const string YUYUKO_PROXY_URL = "dev-proxy.wows.shinoaki.com:7700/dev";

            LogUtils.WriteInfo("WG Public API");
            if(useYuyukoProxy)
            {
                LogUtils.WriteInfo("Yuyuko Proxy Enabled");
            }
            string yuyukoServerString = ServerExt.GetNameByServer(server).ToLower();
            string serverUrlString = ServerExt.GetFullUrlStringByServer(server);
            List<Player> playerList = new();

            for (int i = 0; i < playerCount; i++)
            {
                if (JObjectPlayers["vehicles"]![i]!["id"]!.Value<int>() > 30) //exclude bots in operation and convoy mode
                {
                    int relation = JObjectPlayers["vehicles"]![i]!["relation"]!.Value<int>();
                    if (relationFilter == 0 || relationFilter == 1 && relation <= 1 || relationFilter == 2 && relation > 1)
                    {
                        playerList.Add(new Player(JObjectPlayers["vehicles"]![i]!["name"]!.Value<string>()!, server, JObjectPlayers["vehicles"]![i]!["relation"]!.Value<string>()!, JObjectPlayers["vehicles"]![i]!["shipId"]!.Value<string>()!));
                    }
                }
            }

            string playerNameList = "";
            string requestUrl;
            string responseBodyAsText;

            foreach (Player p in playerList)
            {
                if (p.Name[..1] != ":")
                {
                    playerNameList = playerNameList + p.Name + "%2C";
                }
            }

            playerNameList = playerNameList.Remove(playerNameList.Length - 3);
            LogUtils.WriteDebug($"playerNameList={playerNameList}");

            if (useYuyukoProxy)
            {
                requestUrl = $"https://{YUYUKO_PROXY_URL}/wows/search/{yuyukoServerString}/?type=exact&search={playerNameList}";
            }
            else
            {
                requestUrl = $"https://api.{serverUrlString}/wows/account/list/?application_id={WG_PUBLIC_API_APPLICATION_ID}&type=exact&search={playerNameList}";
            }

            responseBodyAsText = await NetworkUtils.HttpGet(requestUrl);
            await Task.Delay(delayTimeBetweenHttpRequests);

            LogUtils.WriteDebug($"WgPublicApiGetPlayersID Response:{responseBodyAsText}");
            JObject JObjectWgPublicApiPlayersIDList = JsonUtils.Parse(responseBodyAsText);
            if (JObjectWgPublicApiPlayersIDList["status"]!.Value<string>() != "ok")
            {
                return new List<Player>();
            }
            int dataExistPlayerCount = Convert.ToInt32(JObjectWgPublicApiPlayersIDList["meta"]!["count"]!);
            string playerIdList = "";
            LogUtils.WriteDebug($"dataExistPlayerCount={dataExistPlayerCount}");
            for (int i = 0; i < dataExistPlayerCount; i++)
            {
                Player? p = playerList.Find(a => a.Name == JObjectWgPublicApiPlayersIDList["data"]![i]!["nickname"]!.Value<string>()!);
                if (p != null)
                {
                    p.ID = JObjectWgPublicApiPlayersIDList["data"]![i]!["account_id"]!.Value<string>()!;
                    playerIdList = playerIdList + JObjectWgPublicApiPlayersIDList["data"]![i]!["account_id"]!.Value<string>() + "%2C";
                }
            }

            if (playerIdList == "")
            {
                return new List<Player>();
            }

            playerIdList = playerIdList.Remove(playerIdList.Length - 3);

            LogUtils.WriteDebug($"playerIdList={playerIdList}");

            if (useYuyukoProxy)
            {
                requestUrl = $"https://{YUYUKO_PROXY_URL}/wows/account/info/{yuyukoServerString}/?extra=statistics.pvp_solo%2Cstatistics.pvp_div2%2Cstatistics.pvp_div3&fields=hidden_profile%2Cstatistics.pvp.wins%2Cstatistics.pvp.battles%2Cstatistics.pvp_solo.wins%2Cstatistics.pvp_solo.battles%2Cstatistics.pvp_div2.wins%2Cstatistics.pvp_div2.battles%2Cstatistics.pvp_div3.wins%2Cstatistics.pvp_div3.battles&account_id={playerIdList}";
            }
            else 
            {
                requestUrl = $"https://api.{serverUrlString}/wows/account/info/?application_id={WG_PUBLIC_API_APPLICATION_ID}&extra=statistics.pvp_solo%2Cstatistics.pvp_div2%2Cstatistics.pvp_div3&fields=hidden_profile%2Cstatistics.pvp.wins%2Cstatistics.pvp.battles%2Cstatistics.pvp_solo.wins%2Cstatistics.pvp_solo.battles%2Cstatistics.pvp_div2.wins%2Cstatistics.pvp_div2.battles%2Cstatistics.pvp_div3.wins%2Cstatistics.pvp_div3.battles&account_id={playerIdList}";
            }

            responseBodyAsText = await NetworkUtils.HttpGet(requestUrl);
            await Task.Delay(delayTimeBetweenHttpRequests);
            LogUtils.WriteDebug($"WgPublicApiGetPlayersAccountData Response:{responseBodyAsText}");
            JObject JObjectWgPublicApiPlayersAccountDataList = JsonUtils.Parse(responseBodyAsText);

            if (useYuyukoProxy)
            {
                requestUrl = $"https://{YUYUKO_PROXY_URL}/wows/clans/accountinfo/{yuyukoServerString}/?extra=clan&fields=clan_id%2Cclan.tag&account_id={playerIdList}";
            }
            else
            {
                requestUrl = $"https://api.{serverUrlString}/wows/clans/accountinfo/?application_id={WG_PUBLIC_API_APPLICATION_ID}&extra=clan&fields=clan_id%2Cclan.tag&account_id={playerIdList}";
            }
            responseBodyAsText = await NetworkUtils.HttpGet(requestUrl);
            await Task.Delay(delayTimeBetweenHttpRequests);
            LogUtils.WriteDebug($"WgPublicApiGetPlayersClanData Response:{responseBodyAsText}");
            JObject JObjectWgPublicApiPlayersClanDataList = JsonUtils.Parse(responseBodyAsText);

            foreach (Player p in playerList)
            {

                if (p.Name[..1] != ":" && p.ID != "-1")
                {
                    if (JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!.HasValues && JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["hidden_profile"]!.Value<string>() != "true" && JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!.HasValues)
                    {
                        if (JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp"]!.HasValues)
                        {
                            p.Wins = JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp"]!["wins"]!.Value<double>();
                            p.Battles = JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp"]!["battles"]!.Value<double>();
                            if (p.Battles == 0)
                            {
                                p.AccountWinrate = 0;
                            }
                            else
                            {
                                p.AccountWinrate = p.Wins / p.Battles;
                            }
                        }
                        else
                        {
                            p.Wins = 0;
                            p.Battles = 0;
                            p.AccountWinrate = 0;
                        }

                        if (JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_solo"]!.HasValues)
                        {
                            p.Wins_Solo = JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_solo"]!["wins"]!.Value<double>();
                            p.Battles_Solo = JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_solo"]!["battles"]!.Value<double>();

                            if (p.Battles_Solo == 0)
                            {
                                p.AccountWinrate_Solo = 0;
                            }
                            else
                            {
                                p.AccountWinrate_Solo = p.Wins_Solo / p.Battles_Solo;
                            }
                        }
                        else
                        {
                            p.Wins_Solo = 0;
                            p.Battles_Solo = 0;
                            p.AccountWinrate_Solo = 0;
                        }


                        if (JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div2"]!.HasValues)
                        {
                            p.Wins_Div2 = JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div2"]!["wins"]!.Value<double>();
                            p.Battles_Div2 = JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div2"]!["battles"]!.Value<double>();

                            if (p.Battles_Div2 == 0)
                            {
                                p.AccountWinrate_Div2 = 0;
                            }
                            else
                            {
                                p.AccountWinrate_Div2 = p.Wins_Div2 / p.Battles_Div2;
                            }
                        }
                        else
                        {
                            p.Wins_Div2 = 0;
                            p.Battles_Div2 = 0;
                            p.AccountWinrate_Div2 = 0;
                        }

                        if (JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div3"]!.HasValues)
                        {
                            p.Wins_Div3 = JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div3"]!["wins"]!.Value<double>();
                            p.Battles_Div3 = JObjectWgPublicApiPlayersAccountDataList["data"]![p.ID]!["statistics"]!["pvp_div3"]!["battles"]!.Value<double>();

                            if (p.Battles_Div3 == 0)
                            {
                                p.AccountWinrate_Div3 = 0;
                            }
                            else
                            {
                                p.AccountWinrate_Div3 = p.Wins_Div3 / p.Battles_Div3;
                            }

                        }
                        else
                        {
                            p.Wins_Div3 = 0;
                            p.Battles_Div3 = 0;
                            p.AccountWinrate_Div3 = 0;
                        }
                    }
                    else
                    {
                        p.IsHidden = true;
                    }

                    if (JObjectWgPublicApiPlayersClanDataList["data"]![p.ID]!.HasValues && JObjectWgPublicApiPlayersClanDataList["data"]![p.ID]!["clan"]!.HasValues)
                    {
                        p.ClanID = JObjectWgPublicApiPlayersClanDataList["data"]![p.ID]!["clan_id"]!.Value<string>()!;
                        p.ClanTag = $"[{JObjectWgPublicApiPlayersClanDataList["data"]![p.ID]!["clan"]!["tag"]!.Value<string>()}]";
                    }
                }
            }

            List<Task<string>> taskListWgPublicApiGetPlayersShipsData = new();
            List<Task<string>> taskListVortexApiGetPlayersAccountData = new();

            foreach (Player p in playerList)
            {
                if (p.Name[..1] != ":" && p.ID != "-1")
                {
                    if (useYuyukoProxy)
                    {
                        requestUrl = $"https://{YUYUKO_PROXY_URL}/wows/ships/stats/{yuyukoServerString}/?extra=pvp_solo%2Cpvp_div2%2Cpvp_div3&fields=pvp.wins%2Cpvp.battles%2Cpvp.damage_dealt%2Cpvp_solo.wins%2Cpvp_solo.battles%2Cpvp_solo.damage_dealt%2Cpvp_div2.wins%2Cpvp_div2.battles%2Cpvp_div2.damage_dealt%2Cpvp_div3.wins%2Cpvp_div3.battles%2Cpvp_div3.damage_dealt&account_id={p.ID}&ship_id={p.ShipID}";
                    }
                    else
                    {
                        requestUrl = $"https://api.{serverUrlString}/wows/ships/stats/?application_id={WG_PUBLIC_API_APPLICATION_ID}&extra=pvp_solo%2Cpvp_div2%2Cpvp_div3&fields=pvp.wins%2Cpvp.battles%2Cpvp.damage_dealt%2Cpvp_solo.wins%2Cpvp_solo.battles%2Cpvp_solo.damage_dealt%2Cpvp_div2.wins%2Cpvp_div2.battles%2Cpvp_div2.damage_dealt%2Cpvp_div3.wins%2Cpvp_div3.battles%2Cpvp_div3.damage_dealt&account_id={p.ID}&ship_id={p.ShipID}";
                    }
                    taskListWgPublicApiGetPlayersShipsData.Add(NetworkUtils.HttpGet(requestUrl));
                    await Task.Delay(delayTimeBetweenHttpRequests);
                    taskListVortexApiGetPlayersAccountData.Add(NetworkUtils.HttpGet($"https://vortex.{serverUrlString}/api/accounts/{p.ID}/"));
                    await Task.Delay(delayTimeBetweenHttpRequests);
                }
            }

            await Task.WhenAll(taskListWgPublicApiGetPlayersShipsData.Concat(taskListVortexApiGetPlayersAccountData));

            foreach (Player p in playerList)
            {
                if (p.Name[..1] != ":" && p.ID != "-1")
                {
                    LogUtils.WriteDebug($"WgPublicApiGetPlayersShipsData Response:{taskListWgPublicApiGetPlayersShipsData[0].Result}");
                    JObject JObjectWgPublicApiPlayerShipsData = JsonUtils.Parse(taskListWgPublicApiGetPlayersShipsData[0].Result);
                    taskListWgPublicApiGetPlayersShipsData.RemoveAt(0);

                    //karma data is not available in WG Public API so get it by Vortex API
                    //temporarily disabled to prevent connection issues
                    /*
                    LogUtils.WriteDebug($"VortexApiGetPlayersAccountData Response:{taskListVortexApiGetPlayersAccountData[0].Result}");
                    JObject JObjectVortexApiPlayerAccountData = JsonUtils.Parse(taskListVortexApiGetPlayersAccountData[0].Result);
                    taskListVortexApiGetPlayersAccountData.RemoveAt(0);
                    */

                    if (JObjectWgPublicApiPlayerShipsData["status"]!.Value<string>() == "ok" && JObjectWgPublicApiPlayerShipsData["meta"]!["count"]!.Value<string>() == "1" && JObjectWgPublicApiPlayerShipsData["data"]![p.ID]!.HasValues)
                    {
                        p.ShipWins = JObjectWgPublicApiPlayerShipsData!["data"]![p.ID]![0]!["pvp"]!["wins"]!.Value<double>();
                        p.ShipBattles = JObjectWgPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp"]!["battles"]!.Value<double>();
                        p.ShipTotalDmg = JObjectWgPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp"]!["damage_dealt"]!.Value<double>();
                        if (p.ShipBattles == 0)
                        {
                            p.ShipWinrate = 0;
                            p.ShipAvgDmgPerBattle = 0;
                            p.WeightedWinrate = p.AccountWinrate;
                        }
                        else
                        {
                            p.ShipWinrate = p.ShipWins / p.ShipBattles;
                            p.ShipAvgDmgPerBattle = p.ShipTotalDmg / p.ShipBattles;
                            p.WeightedWinrate = CalcWeightedWinrate(p.AccountWinrate_Solo, p.Battles_Solo, p.AccountWinrate_Div2, p.Battles_Div2, p.AccountWinrate_Div3, p.Battles_Div3, p.ShipWinrate, p.ShipBattles);
                        }

                        p.ShipWins_Solo = JObjectWgPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_solo"]!["wins"]!.Value<double>();
                        p.ShipBattles_Solo = JObjectWgPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_solo"]!["battles"]!.Value<double>();
                        p.ShipTotalDmg_Solo = JObjectWgPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_solo"]!["damage_dealt"]!.Value<double>();
                        if (p.ShipBattles_Solo == 0)
                        {
                            p.ShipWinrate_Solo = 0;
                            p.ShipAvgDmgPerBattle_Solo = 0;
                        }
                        else
                        {
                            p.ShipWinrate_Solo = p.ShipWins_Solo / p.ShipBattles_Solo;
                            p.ShipAvgDmgPerBattle_Solo = p.ShipTotalDmg_Solo / p.ShipBattles_Solo;
                        }

                        p.ShipWins_Div2 = JObjectWgPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div2"]!["wins"]!.Value<double>();
                        p.ShipBattles_Div2 = JObjectWgPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div2"]!["battles"]!.Value<double>();
                        p.ShipTotalDmg_Div2 = JObjectWgPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div2"]!["damage_dealt"]!.Value<double>();
                        if (p.ShipBattles_Div2 == 0)
                        {
                            p.ShipWinrate_Div2 = 0;
                            p.ShipAvgDmgPerBattle_Div2 = 0;
                        }
                        else
                        {
                            p.ShipWinrate_Div2 = p.ShipWins_Div2 / p.ShipBattles_Div2;
                            p.ShipAvgDmgPerBattle_Div2 = p.ShipTotalDmg_Div2 / p.ShipBattles_Div2;
                        }

                        p.ShipWins_Div3 = JObjectWgPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div3"]!["wins"]!.Value<double>();
                        p.ShipBattles_Div3 = JObjectWgPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div3"]!["battles"]!.Value<double>();
                        p.ShipTotalDmg_Div3 = JObjectWgPublicApiPlayerShipsData["data"]![p.ID]![0]!["pvp_div3"]!["damage_dealt"]!.Value<double>();
                        if (p.ShipBattles_Div3 == 0)
                        {
                            p.ShipWinrate_Div3 = 0;
                            p.ShipAvgDmgPerBattle_Div3 = 0;
                        }
                        else
                        {
                            p.ShipWinrate_Div3 = p.ShipWins_Div3 / p.ShipBattles_Div3;
                            p.ShipAvgDmgPerBattle_Div3 = p.ShipTotalDmg_Div3 / p.ShipBattles_Div3;
                        }
                    }
                    else
                    {
                        if (p.IsHidden == false)
                        {
                            p.ShipWins = 0;
                            p.ShipBattles = 0;
                            p.ShipTotalDmg = 0;
                            p.ShipAvgDmgPerBattle = 0;
                            p.ShipWinrate = 0;
                            p.WeightedWinrate = p.AccountWinrate;
                        }
                    }
                    //karma data is not available in WG Public API so get it by Vortex API
                    //temporarily disabled to prevent connection issues
                    /*
                    if (JObjectVortexApiPlayerAccountData["status"]!.Value<string>() == "ok" && JObjectVortexApiPlayerAccountData["data"]![p.ID]!.HasValues && JObjectVortexApiPlayerAccountData["data"]![p.ID]!.SelectToken("hidden_profile") == null && JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!.HasValues)
                    {
                        p.Karma = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["basic"]!["karma"]!.Value<double>();
                    }
                    */
                    LogUtils.WriteDebug($"player:{p}");
                }
            }
            return playerList;
        }

        public async static Task<List<Player>> VortexApiGetPlayersStatistics(int playerCount, int relationFilter, JObject JObjectPlayers, Server server, int delayTimeBetweenHttpRequests)
        {
            return await VortexApiGetPlayersStatisticsCore(playerCount, relationFilter, JObjectPlayers, server, delayTimeBetweenHttpRequests);
        }

        // TODO: RU Vortex API response structure may diverge from other servers. Currently identical to default.
        public async static Task<List<Player>> VortexApiGetPlayersStatisticsRU(int playerCount, int relationFilter, JObject JObjectPlayers, Server server, int delayTimeBetweenHttpRequests)
        {
            return await VortexApiGetPlayersStatisticsCore(playerCount, relationFilter, JObjectPlayers, server, delayTimeBetweenHttpRequests);
        }

        private async static Task<List<Player>> VortexApiGetPlayersStatisticsCore(int playerCount, int relationFilter, JObject JObjectPlayers, Server server, int delayTimeBetweenHttpRequests)
        {
            LogUtils.WriteInfo("Vortex API");
            string serverUrlString = ServerExt.GetFullUrlStringByServer(server);
            List<Player> playerList = new();

            for (int i = 0; i < playerCount; i++)
            {
                if (JObjectPlayers["vehicles"]![i]!["id"]!.Value<int>() > 30) //exclude bots in operation and convoy mode
                {
                    int relation = JObjectPlayers["vehicles"]![i]!["relation"]!.Value<int>();
                    if (relationFilter == 0 || relationFilter == 1 && relation <= 1 || relationFilter == 2 && relation > 1)
                    {
                        playerList.Add(new Player(JObjectPlayers["vehicles"]![i]!["name"]!.Value<string>()!, server, JObjectPlayers["vehicles"]![i]!["relation"]!.Value<string>()!, JObjectPlayers["vehicles"]![i]!["shipId"]!.Value<string>()!));
                    }
                }
            }

            List<Task<string>> taskListVortexApiGetPlayerID = new();

            foreach (Player p in playerList)
            {
                if (p.Name[..1] != ":")
                {
                    taskListVortexApiGetPlayerID.Add(NetworkUtils.HttpGet($"https://vortex.{serverUrlString}/api/accounts/search/{Uri.EscapeDataString(p.Name)}"));
                    await Task.Delay(delayTimeBetweenHttpRequests);
                }
            }

            await Task.WhenAll(taskListVortexApiGetPlayerID);

            foreach (Player p in playerList)
            {
                if (p.Name[..1] != ":")
                {
                    LogUtils.WriteDebug($"VortexApiGetPlayerID Response:{taskListVortexApiGetPlayerID[0].Result}");
                    JObject JObjectVortexApiPlayerID = JsonUtils.Parse(taskListVortexApiGetPlayerID[0].Result);
                    taskListVortexApiGetPlayerID.RemoveAt(0);
                    if (JObjectVortexApiPlayerID["status"]!.Value<string>() == "ok" && JObjectVortexApiPlayerID["data"]!.HasValues && JObjectVortexApiPlayerID["data"]![0]!["name"]!.Value<string>()! == p.Name)
                    {
                        p.ID = JObjectVortexApiPlayerID["data"]![0]!["spa_id"]!.Value<string>()!;
                    }
                }
            }

            List<Task<string>> taskListVortexApiGetPlayersAccountData = new();
            List<Task<string>> taskListVortexApiGetPlayersClanData = new();
            List<Task<string>> taskListVortexApiGetPlayersShipsData = new();
            List<Task<string>> taskListVortexApiGetPlayersShipsSoloData = new();
            List<Task<string>> taskListVortexApiGetPlayersShipsDiv2Data = new();
            List<Task<string>> taskListVortexApiGetPlayersShipsDiv3Data = new();

            foreach (Player p in playerList)
            {
                if (p.Name[..1] != ":" && p.ID != "-1")
                {
                    taskListVortexApiGetPlayersAccountData.Add(NetworkUtils.HttpGet($"https://vortex.{serverUrlString}/api/accounts/{p.ID}/"));
                    await Task.Delay(delayTimeBetweenHttpRequests);
                    taskListVortexApiGetPlayersClanData.Add(NetworkUtils.HttpGet($"https://vortex.{serverUrlString}/api/accounts/{p.ID}/clans/"));
                    await Task.Delay(delayTimeBetweenHttpRequests);
                    taskListVortexApiGetPlayersShipsData.Add(NetworkUtils.HttpGet($"https://vortex.{serverUrlString}/api/accounts/{p.ID}/ships/{p.ShipID}/pvp/"));
                    await Task.Delay(delayTimeBetweenHttpRequests);
                    taskListVortexApiGetPlayersShipsSoloData.Add(NetworkUtils.HttpGet($"https://vortex.{serverUrlString}/api/accounts/{p.ID}/ships/{p.ShipID}/pvp_solo/"));
                    await Task.Delay(delayTimeBetweenHttpRequests);
                    taskListVortexApiGetPlayersShipsDiv2Data.Add(NetworkUtils.HttpGet($"https://vortex.{serverUrlString}/api/accounts/{p.ID}/ships/{p.ShipID}/pvp_div2/"));
                    await Task.Delay(delayTimeBetweenHttpRequests);
                    taskListVortexApiGetPlayersShipsDiv3Data.Add(NetworkUtils.HttpGet($"https://vortex.{serverUrlString}/api/accounts/{p.ID}/ships/{p.ShipID}/pvp_div3/"));
                    await Task.Delay(delayTimeBetweenHttpRequests);
                }
            }

            await Task.WhenAll(taskListVortexApiGetPlayersAccountData.Concat(taskListVortexApiGetPlayersClanData.Concat(taskListVortexApiGetPlayersShipsData.Concat(taskListVortexApiGetPlayersShipsSoloData.Concat(taskListVortexApiGetPlayersShipsDiv2Data.Concat(taskListVortexApiGetPlayersShipsDiv3Data))))));

            foreach (Player p in playerList)
            {

                if (p.Name[..1] != ":" && p.ID != "-1")
                {
                    LogUtils.WriteDebug($"VortexApiGetPlayersAccountData Response:{taskListVortexApiGetPlayersAccountData[0].Result}");
                    JObject JObjectVortexApiPlayerAccountData = JsonUtils.Parse(taskListVortexApiGetPlayersAccountData[0].Result);
                    taskListVortexApiGetPlayersAccountData.RemoveAt(0);

                    LogUtils.WriteDebug($"VortexApiGetPlayersClanData Response:{taskListVortexApiGetPlayersClanData[0].Result}");
                    JObject JObjectVortexApiPlayerClanData = JsonUtils.Parse(taskListVortexApiGetPlayersClanData[0].Result);
                    taskListVortexApiGetPlayersClanData.RemoveAt(0);

                    LogUtils.WriteDebug($"VortexApiGetPlayersShipsData Response:{taskListVortexApiGetPlayersShipsData[0].Result}");
                    JObject JObjectVortexApiPlayerShipsData = JsonUtils.Parse(taskListVortexApiGetPlayersShipsData[0].Result);
                    taskListVortexApiGetPlayersShipsData.RemoveAt(0);

                    LogUtils.WriteDebug($"VortexApiGetPlayersShipsSoloData Response:{taskListVortexApiGetPlayersShipsSoloData[0].Result}");
                    JObject JObjectVortexApiPlayerShipsSoloData = JsonUtils.Parse(taskListVortexApiGetPlayersShipsSoloData[0].Result);
                    taskListVortexApiGetPlayersShipsSoloData.RemoveAt(0);

                    LogUtils.WriteDebug($"VortexApiGetPlayersShipsDiv2Data Response:{taskListVortexApiGetPlayersShipsDiv2Data[0].Result}");
                    JObject JObjectVortexApiPlayerShipsDiv2Data = JsonUtils.Parse(taskListVortexApiGetPlayersShipsDiv2Data[0].Result);
                    taskListVortexApiGetPlayersShipsDiv2Data.RemoveAt(0);

                    LogUtils.WriteDebug($"VortexApiGetPlayersShipsDiv3Data Response:{taskListVortexApiGetPlayersShipsDiv3Data[0].Result}");
                    JObject JObjectVortexApiPlayerShipsDiv3Data = JsonUtils.Parse(taskListVortexApiGetPlayersShipsDiv3Data[0].Result);
                    taskListVortexApiGetPlayersShipsDiv3Data.RemoveAt(0);

                    if (JObjectVortexApiPlayerAccountData["status"]!.Value<string>() == "ok" && JObjectVortexApiPlayerAccountData["data"]![p.ID]!.HasValues && JObjectVortexApiPlayerAccountData["data"]![p.ID]!.SelectToken("hidden_profile") == null && JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!.HasValues)
                    {
                        p.Karma = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["basic"]!["karma"]!.Value<double>();
                        if (JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp"]!.HasValues)
                        {
                            p.Wins = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp"]!["wins"]!.Value<double>();
                            p.Battles = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp"]!["battles_count"]!.Value<double>();
                            p.TotalExp = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp"]!["original_exp"]!.Value<double>();
                            if (p.Battles == 0)
                            {
                                p.AccountWinrate = 0;
                                p.AvgExpPerBattle = 0;
                            }
                            else
                            {
                                p.AccountWinrate = p.Wins / p.Battles;
                                p.AvgExpPerBattle = p.TotalExp / p.Battles;
                            }
                        }
                        else
                        {
                            p.Wins = 0;
                            p.Battles = 0;
                            p.AccountWinrate = 0;
                            p.TotalExp = 0;
                            p.AvgExpPerBattle = 0;
                        }
                        if (JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_solo"]!.HasValues)
                        {
                            p.Wins_Solo = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_solo"]!["wins"]!.Value<double>();
                            p.Battles_Solo = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_solo"]!["battles_count"]!.Value<double>();
                            p.TotalExp_Solo = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_solo"]!["original_exp"]!.Value<double>();
                            if (p.Battles_Solo == 0)
                            {
                                p.AccountWinrate_Solo = 0;
                                p.AvgExpPerBattle_Solo = 0;
                            }
                            else
                            {
                                p.AccountWinrate_Solo = p.Wins_Solo / p.Battles_Solo;
                                p.AvgExpPerBattle_Solo = p.TotalExp_Solo / p.Battles_Solo;
                            }
                        }
                        else
                        {
                            p.Wins_Solo = 0;
                            p.Battles_Solo = 0;
                            p.AccountWinrate_Solo = 0;
                            p.TotalExp_Solo = 0;
                            p.AvgExpPerBattle_Solo = 0;
                        }
                        if (JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_div2"]!.HasValues)
                        {
                            p.Wins_Div2 = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_div2"]!["wins"]!.Value<double>();
                            p.Battles_Div2 = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_div2"]!["battles_count"]!.Value<double>();
                            p.TotalExp_Div2 = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_div2"]!["original_exp"]!.Value<double>();
                            if (p.Battles_Div2 == 0)
                            {
                                p.AccountWinrate_Div2 = 0;
                                p.AvgExpPerBattle_Div2 = 0;
                            }
                            else
                            {
                                p.AccountWinrate_Div2 = p.Wins_Div2 / p.Battles_Div2;
                                p.AvgExpPerBattle_Div2 = p.TotalExp_Div2 / p.Battles_Div2;
                            }
                        }
                        else
                        {
                            p.Wins_Div2 = 0;
                            p.Battles_Div2 = 0;
                            p.AccountWinrate_Div2 = 0;
                            p.TotalExp_Div2 = 0;
                            p.AvgExpPerBattle_Div2 = 0;
                        }
                        if (JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_div3"]!.HasValues)
                        {
                            p.Wins_Div3 = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_div3"]!["wins"]!.Value<double>();
                            p.Battles_Div3 = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_div3"]!["battles_count"]!.Value<double>();
                            p.TotalExp_Div3 = JObjectVortexApiPlayerAccountData["data"]![p.ID]!["statistics"]!["pvp_div3"]!["original_exp"]!.Value<double>();
                            if (p.Battles_Div3 == 0)
                            {
                                p.AccountWinrate_Div3 = 0;
                                p.AvgExpPerBattle_Div3 = 0;
                            }
                            else
                            {
                                p.AccountWinrate_Div3 = p.Wins_Div3 / p.Battles_Div3;
                                p.AvgExpPerBattle_Div3 = p.TotalExp_Div3 / p.Battles_Div3;
                            }
                        }
                        else
                        {
                            p.Wins_Div3 = 0;
                            p.Battles_Div3 = 0;
                            p.AccountWinrate_Div3 = 0;
                            p.TotalExp_Div3 = 0;
                            p.AvgExpPerBattle_Div3 = 0;
                        }
                    }
                    else
                    {
                        p.IsHidden = true;
                    }

                    if (JObjectVortexApiPlayerClanData["status"]!.Value<string>() == "ok" && JObjectVortexApiPlayerClanData["data"]!["clan"]!.HasValues)
                    {
                        p.ClanID = JObjectVortexApiPlayerClanData["data"]!["clan_id"]!.Value<string>()!;
                        p.ClanTag = $"[{JObjectVortexApiPlayerClanData["data"]!["clan"]!["tag"]!.Value<string>()}]";
                    }

                    if (p.IsHidden == false)
                    {
                        if (JObjectVortexApiPlayerShipsData["status"]!.Value<string>() == "ok" && JObjectVortexApiPlayerShipsData["data"]![p.ID]!.HasValues && JObjectVortexApiPlayerShipsData["data"]![p.ID]!.SelectToken("hidden_profile") == null && JObjectVortexApiPlayerShipsData["data"]![p.ID]!["statistics"]!.HasValues && JObjectVortexApiPlayerShipsData["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp"]!.HasValues)
                        {
                            p.ShipWins = JObjectVortexApiPlayerShipsData["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp"]!["wins"]!.Value<double>();
                            p.ShipBattles = JObjectVortexApiPlayerShipsData["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp"]!["battles_count"]!.Value<double>();
                            p.ShipTotalDmg = JObjectVortexApiPlayerShipsData["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp"]!["damage_dealt"]!.Value<double>();
                            p.ShipTotalExp = JObjectVortexApiPlayerShipsData["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp"]!["original_exp"]!.Value<double>();
                            if (p.ShipBattles == 0)
                            {
                                p.ShipWinrate = 0;
                                p.ShipAvgDmgPerBattle = 0;
                                p.ShipAvgExpPerBattle = 0;
                                p.WeightedWinrate = p.AccountWinrate;
                            }
                            else
                            {
                                p.ShipWinrate = p.ShipWins / p.ShipBattles;
                                p.ShipAvgDmgPerBattle = p.ShipTotalDmg / p.ShipBattles;
                                p.ShipAvgExpPerBattle = p.ShipTotalExp / p.ShipBattles;
                                p.WeightedWinrate = CalcWeightedWinrate(p.AccountWinrate_Solo, p.Battles_Solo, p.AccountWinrate_Div2, p.Battles_Div2, p.AccountWinrate_Div3, p.Battles_Div3, p.ShipWinrate, p.ShipBattles);
                            }
                        }
                        else
                        {
                            p.ShipWins = 0;
                            p.ShipBattles = 0;
                            p.ShipTotalDmg = 0;
                            p.ShipAvgDmgPerBattle = 0;
                            p.ShipTotalExp = 0;
                            p.ShipAvgExpPerBattle = 0;
                            p.ShipWinrate = 0;
                            p.WeightedWinrate = p.AccountWinrate;
                        }
                        if (JObjectVortexApiPlayerShipsSoloData["status"]!.Value<string>() == "ok" && JObjectVortexApiPlayerShipsSoloData["data"]![p.ID]!.HasValues && JObjectVortexApiPlayerShipsSoloData["data"]![p.ID]!.SelectToken("hidden_profile") == null && JObjectVortexApiPlayerShipsSoloData["data"]![p.ID]!["statistics"]!.HasValues && JObjectVortexApiPlayerShipsSoloData["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_solo"]!.HasValues)
                        {
                            p.ShipWins_Solo = JObjectVortexApiPlayerShipsSoloData["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_solo"]!["wins"]!.Value<double>();
                            p.ShipBattles_Solo = JObjectVortexApiPlayerShipsSoloData["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_solo"]!["battles_count"]!.Value<double>();
                            p.ShipTotalDmg_Solo = JObjectVortexApiPlayerShipsSoloData["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_solo"]!["damage_dealt"]!.Value<double>();
                            p.ShipTotalExp_Solo = JObjectVortexApiPlayerShipsSoloData["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_solo"]!["original_exp"]!.Value<double>();
                            if (p.ShipBattles_Solo == 0)
                            {
                                p.ShipWinrate_Solo = 0;
                                p.ShipAvgDmgPerBattle_Solo = 0;
                                p.ShipAvgExpPerBattle_Solo = 0;
                            }
                            else
                            {
                                p.ShipWinrate_Solo = p.ShipWins_Solo / p.ShipBattles_Solo;
                                p.ShipAvgDmgPerBattle_Solo = p.ShipTotalDmg_Solo / p.ShipBattles_Solo;
                                p.ShipAvgExpPerBattle_Solo = p.ShipTotalExp_Solo / p.ShipBattles_Solo;
                            }
                        }
                        else
                        {
                            p.ShipWins_Solo = 0;
                            p.ShipBattles_Solo = 0;
                            p.ShipTotalDmg_Solo = 0;
                            p.ShipAvgDmgPerBattle_Solo = 0;
                            p.ShipTotalExp_Solo = 0;
                            p.ShipAvgExpPerBattle_Solo = 0;
                            p.ShipWinrate_Solo = 0;
                        }
                        if (JObjectVortexApiPlayerShipsDiv2Data["status"]!.Value<string>() == "ok" && JObjectVortexApiPlayerShipsDiv2Data["data"]![p.ID]!.HasValues && JObjectVortexApiPlayerShipsDiv2Data["data"]![p.ID]!.SelectToken("hidden_profile") == null && JObjectVortexApiPlayerShipsDiv2Data["data"]![p.ID]!["statistics"]!.HasValues && JObjectVortexApiPlayerShipsDiv2Data["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_div2"]!.HasValues)
                        {
                            p.ShipWins_Div2 = JObjectVortexApiPlayerShipsDiv2Data["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_div2"]!["wins"]!.Value<double>();
                            p.ShipBattles_Div2 = JObjectVortexApiPlayerShipsDiv2Data["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_div2"]!["battles_count"]!.Value<double>();
                            p.ShipTotalDmg_Div2 = JObjectVortexApiPlayerShipsDiv2Data["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_div2"]!["damage_dealt"]!.Value<double>();
                            p.ShipTotalExp_Div2 = JObjectVortexApiPlayerShipsDiv2Data["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_div2"]!["original_exp"]!.Value<double>();
                            if (p.ShipBattles_Div2 == 0)
                            {
                                p.ShipWinrate_Div2 = 0;
                                p.ShipAvgDmgPerBattle_Div2 = 0;
                                p.ShipAvgExpPerBattle_Div2 = 0;
                            }
                            else
                            {
                                p.ShipWinrate_Div2 = p.ShipWins_Div2 / p.ShipBattles_Div2;
                                p.ShipAvgDmgPerBattle_Div2 = p.ShipTotalDmg_Div2 / p.ShipBattles_Div2;
                                p.ShipAvgExpPerBattle_Div2 = p.ShipTotalExp_Div2 / p.ShipBattles_Div2;
                            }
                        }
                        else
                        {
                            p.ShipWins_Div2 = 0;
                            p.ShipBattles_Div2 = 0;
                            p.ShipTotalDmg_Div2 = 0;
                            p.ShipAvgDmgPerBattle_Div2 = 0;
                            p.ShipTotalExp_Div2 = 0;
                            p.ShipAvgExpPerBattle_Div2 = 0;
                            p.ShipWinrate_Div2 = 0;
                        }
                        if (JObjectVortexApiPlayerShipsDiv3Data["status"]!.Value<string>() == "ok" && JObjectVortexApiPlayerShipsDiv3Data["data"]![p.ID]!.HasValues && JObjectVortexApiPlayerShipsDiv3Data["data"]![p.ID]!.SelectToken("hidden_profile") == null && JObjectVortexApiPlayerShipsDiv3Data["data"]![p.ID]!["statistics"]!.HasValues && JObjectVortexApiPlayerShipsDiv3Data["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_div3"]!.HasValues)
                        {
                            p.ShipWins_Div3 = JObjectVortexApiPlayerShipsDiv3Data["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_div3"]!["wins"]!.Value<double>();
                            p.ShipBattles_Div3 = JObjectVortexApiPlayerShipsDiv3Data["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_div3"]!["battles_count"]!.Value<double>();
                            p.ShipTotalDmg_Div3 = JObjectVortexApiPlayerShipsDiv3Data["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_div3"]!["damage_dealt"]!.Value<double>();
                            p.ShipTotalExp_Div3 = JObjectVortexApiPlayerShipsDiv3Data["data"]![p.ID]!["statistics"]![p.ShipID]!["pvp_div3"]!["original_exp"]!.Value<double>();
                            if (p.ShipBattles_Div3 == 0)
                            {
                                p.ShipWinrate_Div3 = 0;
                                p.ShipAvgDmgPerBattle_Div3 = 0;
                                p.ShipAvgExpPerBattle_Div3 = 0;
                            }
                            else
                            {
                                p.ShipWinrate_Div3 = p.ShipWins_Div3 / p.ShipBattles_Div3;
                                p.ShipAvgDmgPerBattle_Div3 = p.ShipTotalDmg_Div3 / p.ShipBattles_Div3;
                                p.ShipAvgExpPerBattle_Div3 = p.ShipTotalExp_Div3 / p.ShipBattles_Div3;
                            }
                        }
                        else
                        {
                            p.ShipWins_Div3 = 0;
                            p.ShipBattles_Div3 = 0;
                            p.ShipTotalDmg_Div3 = 0;
                            p.ShipAvgDmgPerBattle_Div3 = 0;
                            p.ShipTotalExp_Div3 = 0;
                            p.ShipAvgExpPerBattle_Div3 = 0;
                            p.ShipWinrate_Div3 = 0;
                        }
                    }
                }
                LogUtils.WriteDebug($"player:{p}");
            }
            return playerList;
        }

        public async static void YuyukoApiPushBattlefieldInfo(Battlefield battlefield)
        {
            string pushStr = battlefield.GetBattlefieldInfoStrForYuyukoApiPush();
            try
            {
                await NetworkUtils.HttpPost("https://dev-proxy.wows.shinoaki.com:7700/upload/wows/game/player", pushStr, "application/json");
            }
            catch
            {

            }
        }
    }
}
