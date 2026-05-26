using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Compression;
using System.Security.Cryptography;
using TomatoRadar.Models;

namespace TomatoRadar.Utils
{
    static internal class SoftwareUpdateUtils
    {
        const string MainMetadataUrl = "https://dl.localizedkorabli.org/tomatoradar/app/metadata.json";

        static string softwareLatestVersion = "";
        static string softwareLatestDate = "";
        static string softwareLatestUrl = "";
        static string softwareLatestFileName = "";
        static string softwareLasestSHA256 = "";

        static string downloadDirectory = @".\Download";
        static string[] ignoredDirectoryList = { @".\Log", @".\Screenshot" };
        static string[] ignoredFileList = { @".\placement.config", @".\WatchList.json" };
        static string[] occupiedFileList = { @".\TomatoRadar.exe", @".\libSkiaSharp.dll" };

        public static async Task<bool> CheckForUpdates()
        {
            try
            {
                JObject mainMeta = JsonUtils.Parse(await NetworkUtils.HttpGet(MainMetadataUrl));

                if (!mainMeta["update_server_enabled"]!.Value<bool>())
                {
                    return false;
                }

                downloadDirectory = mainMeta["download_directory"]!.Value<string>()!;

                softwareLatestVersion = mainMeta["software_latest_version"]!.Value<string>()!;
                softwareLatestDate = mainMeta["software_latest_date"]!.Value<string>()!;
                softwareLatestUrl = mainMeta["software_latest_url"]!.Value<string>()!;
                softwareLatestFileName = softwareLatestUrl.Substring(softwareLatestUrl.LastIndexOf('/') + 1);
                softwareLasestSHA256 = mainMeta["software_latest_sha256"]!.Value<string>()!;

                ignoredDirectoryList = mainMeta["software_update_ignored_directory_list"]!.ToObject<string[]>()!;
                ignoredFileList = mainMeta["software_update_ignored_file_list"]!.ToObject<string[]>()!;
                occupiedFileList = mainMeta["software_update_occupied_file_list"]!.ToObject<string[]>()!;

                JObject shiplistMetaMap = (JObject)mainMeta["shiplist_metadata"]!;

                var shiplistEntries = new List<ShiplistEntry>();
                foreach (var kvp in shiplistMetaMap)
                {
                    JObject subMeta = JsonUtils.Parse(await NetworkUtils.HttpGet(kvp.Value!.Value<string>()!));
                    shiplistEntries.Add(new ShiplistEntry
                    {
                        Key = kvp.Key,
                        LatestVersion = subMeta["shiplist_latest_version"]!.Value<string>()!,
                        LatestDate = subMeta["shiplist_latest_date"]!.Value<string>()!,
                        LatestUrl = subMeta["shiplist_latest_url"]!.Value<string>()!,
                        LatestFileName = subMeta["shiplist_latest_url"]!.Value<string>()!.Substring(subMeta["shiplist_latest_url"]!.Value<string>()!.LastIndexOf('/') + 1),
                        LatestSha256 = subMeta["shiplist_latest_sha256"]!.Value<string>()!,
                        HashValidateEnabled = subMeta["shiplist_hash_validate_enabled"]!.Value<bool>(),
                    });
                }

                int softwareDateInt = Convert.ToInt32(softwareLatestDate);
                int localSoftwareDate = Convert.ToInt32(Properties.Settings.Default.SoftwareDate);

                bool anyShiplistNeedsUpdate = false;
                foreach (var entry in shiplistEntries)
                {
                    entry.LocalDate = Convert.ToInt32(ShipInfoUtils.GetShipInfoDate(entry.ReferenceServer));
                    if (Convert.ToInt32(entry.LatestDate) > entry.LocalDate)
                        anyShiplistNeedsUpdate = true;
                }

                if (softwareDateInt <= localSoftwareDate && !anyShiplistNeedsUpdate)
                {
                    return false;
                }

                if (softwareDateInt > localSoftwareDate)
                {
                    if (MessageBox.Show($"{Application.Current.FindResource("MsgBoxSoftwareUpdateFound") as string}\n{Application.Current.FindResource("MsgBoxCurrentVersion") as string} {Properties.Settings.Default.SoftwareVersion} ({Properties.Settings.Default.SoftwareDate})\n{Application.Current.FindResource("MsgBoxLatestVersion") as string} {softwareLatestVersion} ({softwareLatestDate})\n{Application.Current.FindResource("MsgBoxUpdateComfirm") as string}", Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        NotificationMessageUtils.CreateMessage(MessageType.INFO, Application.Current.FindResource("NotificationMessageSoftwareUpdateDownloading") as string);
                        Directory.CreateDirectory(downloadDirectory);
                        await NetworkUtils.HttpDownloadFile(softwareLatestUrl, $@"{downloadDirectory}\{softwareLatestFileName}");
                        if (mainMeta["software_hash_validate_enabled"]!.Value<bool>())
                        {
                            using SHA256 sha = SHA256.Create();
                            using FileStream fs = new($@"{downloadDirectory}\{softwareLatestFileName}", FileMode.Open);
                            fs.Position = 0;
                            if (BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "") != softwareLasestSHA256)
                            {
                                throw new FileFormatException("FileHashInvalid");
                            }
                        }
                        ZipFile.ExtractToDirectory($@"{downloadDirectory}\{softwareLatestFileName}", $@"{downloadDirectory}\", true);

                        foreach (string directoryname in Directory.GetDirectories($@"{downloadDirectory}\TomatoRadar\"))
                        {
                            string directoryDest = $".{directoryname.Substring(directoryname.LastIndexOf('\\'))}";
                            if (!ignoredDirectoryList.Contains(directoryDest))
                            {
                                Directory.Delete(directoryDest, true);
                                Directory.Move(directoryname, directoryDest);
                            }
                        }

                        foreach (string filename in occupiedFileList)
                        {
                            if (File.Exists(filename))
                            {
                                File.Move(filename, $"{filename}.bak", true);
                            }
                        }

                        foreach (string filename in Directory.GetFiles($@"{downloadDirectory}\TomatoRadar\"))
                        {
                            string fileDest = $".{filename.Substring(filename.LastIndexOf('\\'))}";
                            if (!ignoredFileList.Contains(fileDest))
                            {
                                LogUtils.WriteInfo($"filename:{filename}, filedest:{fileDest}");
                                File.Move(filename, fileDest, true);
                            }
                        }
                        NotificationMessageUtils.CreateMessage(MessageType.INFO, Application.Current.FindResource("NotificationMessageSoftwareUpdateComplete") as string);
                        MessageBox.Show(Application.Current.FindResource("MsgBoxSoftwareUpdateComplete") as string, Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.OK, MessageBoxImage.Information);
                        Application.Current.Shutdown();
                        return true;
                    }
                }

                foreach (var entry in shiplistEntries)
                {
                    if (Convert.ToInt32(entry.LatestDate) > entry.LocalDate)
                    {
                        await CheckForShiplistUpdate(entry);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogUtils.WriteError("", ex);
                _ = ex.Message switch
                {
                    "HttpRequestFailed" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, Application.Current.FindResource("NotificationMessageUpdateConnectionError") as string),
                    "FileHashInvalid" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, Application.Current.FindResource("NotificationMessageUpdateFileHashError") as string),
                    _ => NotificationMessageUtils.CreateMessage(MessageType.ERROR, Application.Current.FindResource("NotificationMessageOtherError") as string),
                };
                return true;
            }
            finally
            {
                if (Directory.Exists($"{downloadDirectory}"))
                {
                    Directory.Delete($"{downloadDirectory}", true);
                }
            }
        }

        private static async Task CheckForShiplistUpdate(ShiplistEntry entry)
        {
            string shiplistLabel = entry.Key.ToUpper();
            string currentVersionText = ShipInfoUtils.GetShipInfoVersion(entry.ReferenceServer);
            string currentDateText = ShipInfoUtils.GetShipInfoDate(entry.ReferenceServer);

            if (MessageBox.Show($"{Application.Current.FindResource("MsgBoxShiplistUpdateFound") as string} ({shiplistLabel})\n{Application.Current.FindResource("MsgBoxCurrentVersion") as string} {currentVersionText} ({currentDateText})\n{Application.Current.FindResource("MsgBoxLatestVersion") as string} {entry.LatestVersion} ({entry.LatestDate})\n{Application.Current.FindResource("MsgBoxUpdateComfirm") as string}", Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                NotificationMessageUtils.CreateMessage(MessageType.INFO, $"{Application.Current.FindResource("NotificationMessageShiplistUpdateDownloading") as string} ({shiplistLabel})");
                Directory.CreateDirectory(downloadDirectory);
                await NetworkUtils.HttpDownloadFile(entry.LatestUrl, $@"{downloadDirectory}\{entry.LatestFileName}");
                if (entry.HashValidateEnabled)
                {
                    using SHA256 sha = SHA256.Create();
                    using FileStream fs = new($@"{downloadDirectory}\{entry.LatestFileName}", FileMode.Open);
                    fs.Position = 0;
                    if (BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "") != entry.LatestSha256)
                    {
                        throw new FileFormatException("FileHashInvalid");
                    }
                }
                ZipFile.ExtractToDirectory($@"{downloadDirectory}\{entry.LatestFileName}", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Json\"), true);
                ShipInfoUtils.LoadShipInfoForServer(entry.ReferenceServer);
                NotificationMessageUtils.CreateMessage(MessageType.INFO, $"{Application.Current.FindResource("NotificationMessageShiplistUpdateComplete") as string} ({shiplistLabel})");
                MessageBox.Show($"{Application.Current.FindResource("MsgBoxShiplistUpdateComplete") as string} ({shiplistLabel})", Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public static void CleanOldVersionFiles()
        {
            foreach (string filename in occupiedFileList)
            {
                if (File.Exists($"{filename}.bak"))
                {
                    File.Delete($"{filename}.bak");
                }
            }
        }

        private class ShiplistEntry
        {
            public string Key { get; init; } = "";
            public string LatestVersion { get; init; } = "";
            public string LatestDate { get; init; } = "";
            public string LatestUrl { get; init; } = "";
            public string LatestFileName { get; init; } = "";
            public string LatestSha256 { get; init; } = "";
            public bool HashValidateEnabled { get; init; }
            public int LocalDate { get; set; }

            public Server ReferenceServer => Key switch
            {
                "wg" => Server.EU,
                "lesta" => Server.RU,
                _ => Server.EU,
            };
        }
    }
}
