using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using TomatoRadar.Models;

namespace TomatoRadar.Utils
{
    static internal class FileUtils
    {
        public const string SCRIPTS_DIR = @"Resources\scripts";

        private static DateTimeOffset latestFileWriteTime = DateTimeOffset.MinValue;
        private static string? lastKorabliReplayPath;
        private static long lastKorabliReplaySize;

        public static string GetLatestTempArenaInfoFile(bool requireFileToBeNewer)
        {
            if (Properties.Settings.Default.GamePath == "")
            {
                return "";
            }

            string[] tempArenaInfoPath = Array.Empty<string>();
            if (Directory.Exists($@"{Properties.Settings.Default.GamePath}\replays\"))
            {
                var allFiles = Directory.GetFiles($@"{Properties.Settings.Default.GamePath}\replays\", "tempArenaInfo.json", SearchOption.AllDirectories).ToList();
                var korabliFiles = Directory.GetFiles($@"{Properties.Settings.Default.GamePath}\replays\", "temp.korablireplay", SearchOption.AllDirectories);
                allFiles.AddRange(korabliFiles);
                tempArenaInfoPath = allFiles.ToArray();
            }

            if (tempArenaInfoPath.Length == 0)
            {
                LogUtils.WriteDebug("tempArenaInfo file not found. ");
                latestFileWriteTime = DateTimeOffset.MinValue;
                lastKorabliReplayPath = null;
                lastKorabliReplaySize = 0;
                return "";
            }
            else
            {
                foreach (string path in tempArenaInfoPath)
                {
                    LogUtils.WriteDebug($"tempArenaInfo file path={path}");
                }
            }

            DateTimeOffset dt = DateTimeOffset.MinValue;
            string latestFileName = "";
            foreach (string filename in tempArenaInfoPath)
            {
                FileInfo fi = new(filename);

                if (fi.LastWriteTime > dt)
                {
                    dt = fi.LastWriteTime;
                    latestFileName = filename;
                }
            }

            if (latestFileName.EndsWith(".korablireplay"))
            {
                if (latestFileName == lastKorabliReplayPath)
                {
                    long currentSize = new FileInfo(latestFileName).Length;
                    if (currentSize > lastKorabliReplaySize)
                    {
                        LogUtils.WriteDebug($"KorabliReplay file grew from {lastKorabliReplaySize} to {currentSize} bytes");
                        lastKorabliReplaySize = currentSize;
                        return latestFileName;
                    }
                    return "";
                }

                lastKorabliReplayPath = latestFileName;
                lastKorabliReplaySize = new FileInfo(latestFileName).Length;
                LogUtils.WriteDebug($"KorabliReplay file detected: {lastKorabliReplayPath}, size={lastKorabliReplaySize}");
            }

            bool result = (dt > latestFileWriteTime);
            latestFileWriteTime = dt;

            if (result || !requireFileToBeNewer)
            {
                return latestFileName;
            }
            else
            {
                return "";
            }
        }

        public static JObject ReadTempArenaInfoFile(string filename)
        {
            string tempArenaInfo;
            using FileStream fs = new(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using StreamReader sr = new(fs);
            byte[] buffer = new byte[4];

            int firstByte = sr.Peek();

            if (firstByte == 0x7B)
            {
                tempArenaInfo = sr.ReadToEnd();
            }
            else if (firstByte == 0x12)
            {
                fs.Seek(0, SeekOrigin.Begin);
                fs.Read(buffer, 0, 4);
                if (Enumerable.SequenceEqual(buffer, new byte[] { 0x12, 0x32, 0x34, 0x11 }))
                {
                    fs.Seek(8, SeekOrigin.Begin);
                    fs.Read(buffer, 0, 4);
                    sr.DiscardBufferedData();
                    tempArenaInfo = sr.ReadToEnd()[..BitConverter.ToInt32(buffer, 0)];
                }
                else
                {
                    throw new FileFormatException("FileFormatIncorrect");
                }
            }
            else
            {
                throw new FileFormatException("FileFormatIncorrect");
            }

            LogUtils.WriteInfo($"tempArenaInfo:{tempArenaInfo}");
            try
            {
                return JsonUtils.Parse(tempArenaInfo);
            }
            catch (Exception ex)
            {
                throw new FileFormatException("FileFormatIncorrect", ex);
            }
        }

        // RU temp.korablireplay parsing (KorabliReplayReader.ReadKorabliReplay):
        //   Path A: Replay header [12 32 34 11] → Block 1 JSON playersPublicInfo
        //   Path B: Arena info binary → zlib → MessagePack entities (name + shipId + teamId)
        public static JObject? GetPlayerListJObject(Server server, string filename)
        {
            if (server == Server.RU)
            {
                return KorabliReplayReader.ReadKorabliReplay(filename);
            }
            return ReadTempArenaInfoFile(filename);
        }

        public static int ReadFogOfWarFlagFromKorabliReplayDir(string korabliReplayPath)
        {
            try
            {
                string? dir = Path.GetDirectoryName(korabliReplayPath);
                if (string.IsNullOrEmpty(dir))
                    return 0;
                string jsonPath = Path.Combine(dir, "tempArenaInfo.json");
                if (!File.Exists(jsonPath))
                    return 0;
                JObject json = ReadTempArenaInfoFile(jsonPath);
                return json["isFogOfWar"]?.Value<int>() ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
