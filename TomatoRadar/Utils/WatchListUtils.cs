using TomatoRadar.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace TomatoRadar.Utils
{
    static internal class WatchListUtils
    {
        public static void CreateNewWatchList(string filename)
        {
            using FileStream fs = new(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            using StreamWriter sw = new(fs);
            JObject JObjectWatchList = JsonUtils.Parse("{\"RU\":{},\"EU\":{},\"NA\":{},\"ASIA\":{},\"CN\":{}}");
            sw.WriteLine(JsonConvert.SerializeObject(JObjectWatchList, Formatting.Indented));
        }

        public static JObject ReadWatchList(string filename)
        {
            if (!File.Exists(filename))
            {
                CreateNewWatchList(filename);
            }
            using FileStream fs = new(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using StreamReader sr = new(fs);
            string strWatchList = sr.ReadToEnd();
            return JsonUtils.Parse(strWatchList);
        }

        public static void SaveWatchList(Player p, string filename)
        {
            JObject JObjectWatchList = ReadWatchList(filename);

            if (JObjectWatchList[ServerExt.GetNameByServer(p.Server)]!.SelectToken(p.ID) != null)
            {
                if (p.WatchStatus == WatchStatus.NONE)
                {
                    JObject? JObjectToUpdate = JObjectWatchList[ServerExt.GetNameByServer(p.Server)] as JObject;
                    JObjectToUpdate!.Remove(p.ID);
                }
                else
                {
                    JObjectWatchList[ServerExt.GetNameByServer(p.Server)]![p.ID]!["status"] = WatchStatusExt.GetNameByStatus(p.WatchStatus);
                }
            }
            else
            {
                JObject JObjectPlayer = JsonUtils.Parse($"{{\"name\": \"{p.Name}\",\"status\": \"{WatchStatusExt.GetNameByStatus(p.WatchStatus)}\"}}");
                JObject? JObjectToUpdate = JObjectWatchList[ServerExt.GetNameByServer(p.Server)] as JObject;
                JObjectToUpdate!.Add(p.ID, JObjectPlayer);
            }
            using FileStream fs = new(filename, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            using StreamWriter sw = new(fs);
            sw.WriteLine(JsonConvert.SerializeObject(JObjectWatchList, Formatting.Indented));
        }
    }
}
