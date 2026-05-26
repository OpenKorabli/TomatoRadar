using TomatoRadar.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;

namespace TomatoRadar.Utils
{
    static internal class ShipInfoUtils
    {
        private static readonly Dictionary<Server, JObject> _shipInfoByServer = new();
        private static readonly Dictionary<Server, string> _shipInfoPathByServer = new();

        public static void RegisterShipInfoFile(Server server, string filename)
        {
            _shipInfoPathByServer[server] = filename;
        }

        public static void LoadShipInfoForServer(Server server)
        {
            string filename = _shipInfoPathByServer[server];
            using FileStream fs = new(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using StreamReader sr = new(fs);
            string ShipInfoStr = sr.ReadToEnd();
            _shipInfoByServer[server] = JsonUtils.Parse(ShipInfoStr);
        }

        private static JObject GetShipInfoForServer(Server server)
        {
            if (_shipInfoByServer.TryGetValue(server, out var info))
            {
                return info;
            }
            if (_shipInfoByServer.TryGetValue(Server.EU, out info))
            {
                return info;
            }
            if (_shipInfoByServer.TryGetValue(Server.NA, out info))
            {
                return info;
            }
            if (_shipInfoByServer.TryGetValue(Server.ASIA, out info))
            {
                return info;
            }
            throw new InvalidOperationException("No ship info loaded for any server");
        }

        public static string GetShipInfoVersion(Server server)
        {
            return GetShipInfoForServer(server)["version"]!.Value<string>()!;
        }

        public static string GetShipInfoDate(Server server)
        {
            return GetShipInfoForServer(server)["date"]!.Value<string>()!;
        }

        public static string GetShipNameByID(string ID, List<Language> languagePriority, Server server)
        {
            string? strUnknownShip = Application.Current.FindResource("StringUnknownShip") as string;
            JObject shipInfo = GetShipInfoForServer(server);
            if (((JObject)shipInfo["ships"]!).ContainsKey(ID))
            {
                foreach (Language lang in languagePriority)
                {
                    string? field = lang switch
                    {
                        Language.ZH_CN => "name_zh-cn",
                        Language.EN_US => "name_en-us",
                        _ => null,
                    };
                    if (field == null)
                        continue;
                    JToken? token = shipInfo["ships"]![ID]![field];
                    if (token != null && !string.IsNullOrEmpty(token.Value<string>()))
                        return token.Value<string>()!;
                }
                return shipInfo["ships"]![ID]!["name_en-us"]!.Value<string>()!;
            }
            else
            {
                return strUnknownShip!;
            }
        }

        public static string GetShipTypeByID(string ID, Server server)
        {
            JObject shipInfo = GetShipInfoForServer(server);
            if (((JObject)shipInfo["ships"]!).ContainsKey(ID))
            {
                return shipInfo["ships"]![ID]!["type"]!.Value<string>()!;
            }
            else
            {
                return "Unknown";
            }
        }

        public static int GetShipTierByID(string ID, Server server)
        {
            JObject shipInfo = GetShipInfoForServer(server);
            if (((JObject)shipInfo["ships"]!).ContainsKey(ID))
            {
                return Convert.ToInt32(shipInfo["ships"]![ID]!["tier"]!);
            }
            else
            {
                return 0;
            }
        }
    }
}
