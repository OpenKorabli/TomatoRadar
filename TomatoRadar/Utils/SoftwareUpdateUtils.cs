using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using TomatoRadar.Models;

namespace TomatoRadar.Utils
{
    static internal class SoftwareUpdateUtils
    {
        const string MainMetadataUrl = "https://localizedkorabli.org/metadata/tomatoradar/metadata.json";

        static string DownloadDirectory => Path.Combine(App.DataDirectory, "Download");

        private static bool _skipDirectoryCleanup;

        public static async Task<bool> CheckForUpdates()
        {
            try
            {
                JObject mainMeta = JsonUtils.Parse(await NetworkUtils.HttpGet(MainMetadataUrl));

                if (!mainMeta["update_server_enabled"]!.Value<bool>())
                {
                    return false;
                }

                string softwareLatestVersion = mainMeta["software_latest_version"]!.Value<string>()!;
                string softwareLatestDate = mainMeta["software_latest_date"]!.Value<string>()!;
                string softwareLatestUrl = mainMeta["software_latest_url"]!.Value<string>()!;
                string softwareLatestFileName = softwareLatestUrl.Substring(softwareLatestUrl.LastIndexOf('/') + 1);
                string softwareLatestSha256 = mainMeta["software_latest_sha256"]!.Value<string>()!;

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

                long softwareLatestBuild = Convert.ToInt64(softwareLatestDate);
                long localAppBuild = Convert.ToInt64(Properties.Settings.Default.SoftwareDate);

                bool anyShiplistNeedsUpdate = false;
                foreach (var entry in shiplistEntries)
                {
                    entry.LocalDate = Convert.ToInt32(ShipInfoUtils.GetShipInfoDate(entry.ReferenceServer));
                    if (Convert.ToInt32(entry.LatestDate) > entry.LocalDate)
                        anyShiplistNeedsUpdate = true;
                }

                if (softwareLatestBuild <= localAppBuild && !anyShiplistNeedsUpdate)
                {
                    return false;
                }

                if (softwareLatestBuild > localAppBuild)
                {
                    if (MessageBox.Show($"{Application.Current.FindResource("MsgBoxSoftwareUpdateFound") as string}\n{Application.Current.FindResource("MsgBoxCurrentVersion") as string} {Properties.Settings.Default.SoftwareVersion} ({Properties.Settings.Default.SoftwareDate})\n{Application.Current.FindResource("MsgBoxLatestVersion") as string} {softwareLatestVersion} ({softwareLatestDate})\n{Application.Current.FindResource("MsgBoxUpdateComfirm") as string}", Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        NotificationMessageUtils.CreateMessage(MessageType.INFO, Application.Current.FindResource("NotificationMessageSoftwareUpdateDownloading") as string);
                        Directory.CreateDirectory(DownloadDirectory);
                        string installerPath = Path.Combine(DownloadDirectory, softwareLatestFileName);

                        var progress = new Progress<double>(p =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (App.DownloadProgressBar != null)
                                {
                                    App.DownloadProgressBar.Visibility = Visibility.Visible;
                                    App.DownloadProgressBar.Value = p;
                                }
                            });
                        });

                        await NetworkUtils.HttpDownloadFile(softwareLatestUrl, installerPath, progress);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (App.DownloadProgressBar != null)
                                App.DownloadProgressBar.Visibility = Visibility.Collapsed;
                        });

                        string computedHash;
                        using (SHA256 sha = SHA256.Create())
                        using (FileStream fs = new(installerPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            computedHash = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
                        }
                        if (computedHash != softwareLatestSha256)
                        {
                            LogUtils.WriteError($"Installer hash mismatch: expected {softwareLatestSha256}, actual {computedHash}", new FileFormatException("FileHashInvalid"));
                            throw new FileFormatException("FileHashInvalid");
                        }

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = installerPath,
                            UseShellExecute = true,
                            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                        });

                        _skipDirectoryCleanup = true;
                        Environment.Exit(0);
                    }
                }

                foreach (var entry in shiplistEntries)
                {
                    if (Convert.ToInt32(entry.LatestDate) > entry.LocalDate)
                    {
                        try
                        {
                            await CheckForShiplistUpdate(entry);
                        }
                        catch (Exception ex)
                        {
                            LogUtils.WriteError($"Shiplist update failed for {entry.Key}", ex);
                            if (ex is FileFormatException)
                            {
                                NotificationMessageUtils.CreateMessage(MessageType.ERROR, $"{entry.Key.ToUpper()} {Application.Current.FindResource("NotificationMessageUpdateFileHashError") as string}");
                            }
                        }
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
                if (!_skipDirectoryCleanup && Directory.Exists(DownloadDirectory))
                {
                    Directory.Delete(DownloadDirectory, true);
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
                Directory.CreateDirectory(DownloadDirectory);
                await NetworkUtils.HttpDownloadFile(entry.LatestUrl, $@"{DownloadDirectory}\{entry.LatestFileName}");
                if (entry.HashValidateEnabled)
                {
                    string computedHash;
                    using (SHA256 sha = SHA256.Create())
                    using (FileStream fs = new($@"{DownloadDirectory}\{entry.LatestFileName}", FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        computedHash = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
                    }
                    if (computedHash != entry.LatestSha256)
                    {
                        LogUtils.WriteError($"Shiplist hash mismatch for {entry.Key}: expected {entry.LatestSha256}, actual {computedHash}", new FileFormatException("FileHashInvalid"));
                        throw new FileFormatException("FileHashInvalid");
                    }
                }
                ZipFile.ExtractToDirectory($@"{DownloadDirectory}\{entry.LatestFileName}", App.ShipInfoDirectory, true);
                ShipInfoUtils.LoadShipInfoForServer(entry.ReferenceServer);
                NotificationMessageUtils.CreateMessage(MessageType.INFO, $"{Application.Current.FindResource("NotificationMessageShiplistUpdateComplete") as string} ({shiplistLabel})");
                MessageBox.Show($"{Application.Current.FindResource("MsgBoxShiplistUpdateComplete") as string} ({shiplistLabel})", Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.OK, MessageBoxImage.Information);
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
