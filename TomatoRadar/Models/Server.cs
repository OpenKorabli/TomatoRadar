using System;
using System.IO;

namespace TomatoRadar.Models
{
    public enum Server
    {
        AUTO,
        RU,
        EU,
        NA,
        ASIA,
        CN,
    }
    public static class ServerExt
    {
        public static Server AutoDetectServer(string filename)
        {
            try
            {
                using FileStream fs = new(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using StreamReader sr = new(fs);
                string clientRunnerLog = sr.ReadToEnd();
                int realmIndex = clientRunnerLog.LastIndexOf("Selected realm: ") + 16;
                string realm = clientRunnerLog.Substring(realmIndex, clientRunnerLog.IndexOf('\n', realmIndex) - realmIndex);
                return realm switch
                {
                    "ru" => Server.RU,
                    "eu" => Server.EU,
                    "na" => Server.NA,
                    "asia" => Server.ASIA,
                    "cn" => Server.CN,
                    _ => throw new ArgumentException(),
                };
            }
            catch (Exception ex)
            {
                throw new Exception("ServerAutoDetectionFailed", ex);
            }
        }
        public static Server GetServerByName(string name)
        {
            return name switch
            {
                "AUTO" => Server.AUTO,
                "RU" => Server.RU,
                "EU" => Server.EU,
                "NA" => Server.NA,
                "ASIA" => Server.ASIA,
                "CN" => Server.CN,
                _ => throw new ArgumentException(),
            };
        }
        public static string GetNameByServer(Server server)
        {
            return server switch
            {
                Server.AUTO => "AUTO",
                Server.RU => "RU",
                Server.EU => "EU",
                Server.NA => "NA",
                Server.ASIA => "ASIA",
                Server.CN => "CN",
                _ => throw new ArgumentException(),
            };
        }

        public static string GetFullUrlStringByServer(Server server)
        {
            return server switch
            {
                Server.RU => "korabli.su",
                Server.EU => "worldofwarships.eu",
                Server.NA => "worldofwarships.com",
                Server.ASIA => "worldofwarships.asia",
                Server.CN => "wowsgame.cn",
                _ => throw new ArgumentException(),
            };
        }

        public static string GetWoWSNumbersUrlStringByServer(Server server)
        {
            return server switch
            {
                Server.EU => "https://wows-numbers.com",
                Server.NA => "https://na.wows-numbers.com",
                Server.ASIA => "https://asia.wows-numbers.com",
                _ => throw new ArgumentException(),
            };
        }

        public static string GetProshipsUrlByPlayerId(string playerId)
        {
            return $"https://proships.su/stat/ru/p/{playerId}";
        }
    }
}
