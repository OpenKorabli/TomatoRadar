using Newtonsoft.Json.Linq;
using System;
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
        static string softwareLatestVersion = "";
        static string softwareLatestDate = "";
        static string softwareLatestUrl = "";
        static string softwareLatestFileName = "";
        static string softwareLasestSHA256 = "";
        static string shiplistLatestVersion = "";
        static string shiplistLatestDate = "";
        static string shiplistLatestUrl = "";
        static string shiplistLatestFileName = "";
        static string shiplistLasestSHA256 = "";

        static string downloadDirectory = @".\Download";
        static string[] ignoredDirectoryList = { @".\Log", @".\Screenshot" };
        static string[] ignoredFileList = { @".\placement.config", @".\WatchList.json" };
        static string[] occupiedFileList = { @".\TomatoRadar.exe", @".\libSkiaSharp.dll" };

        public static async Task<bool> CheckForUpdates()
        {
            try
            {
                JObject JObjectUpdateInfo = JsonUtils.Parse(await NetworkUtils.HttpGet("http://lxdev.org/aperadar/updateinfo/"));

                if (!JObjectUpdateInfo["update_server_enabled"]!.Value<bool>())
                {
                    return false;
                }

                downloadDirectory = JObjectUpdateInfo["download_directory"]!.Value<string>()!;

                softwareLatestVersion = JObjectUpdateInfo["software_latest_version"]!.Value<string>()!;
                softwareLatestDate = JObjectUpdateInfo["software_latest_date"]!.Value<string>()!;
                softwareLatestUrl = JObjectUpdateInfo["software_latest_url"]!.Value<string>()!;
                softwareLatestFileName = softwareLatestUrl.Substring(softwareLatestUrl.LastIndexOf('/') + 1);
                softwareLasestSHA256 = JObjectUpdateInfo["software_latest_sha256"]!.Value<string>()!;


                ignoredDirectoryList = JObjectUpdateInfo["software_update_ignored_directory_list"]!.ToObject<string[]>()!;
                ignoredFileList = JObjectUpdateInfo["software_update_ignored_file_list"]!.ToObject<string[]>()!;
                occupiedFileList = JObjectUpdateInfo["software_update_occupied_file_list"]!.ToObject<string[]>()!;

                shiplistLatestVersion = JObjectUpdateInfo["shiplist_latest_version"]!.Value<string>()!;
                shiplistLatestDate = JObjectUpdateInfo["shiplist_latest_date"]!.Value<string>()!;
                shiplistLatestUrl = JObjectUpdateInfo["shiplist_latest_url"]!.Value<string>()!;
                shiplistLatestFileName = shiplistLatestUrl.Substring(shiplistLatestUrl.LastIndexOf('/') + 1);
                shiplistLasestSHA256 = JObjectUpdateInfo["shiplist_latest_sha256"]!.Value<string>()!;

                if (Convert.ToInt32(softwareLatestDate) <= Convert.ToInt32(Properties.Settings.Default.SoftwareDate) && Convert.ToInt32(shiplistLatestDate) <= Convert.ToInt32(ShipInfoUtils.GetShipInfoDate(Server.EU)))
                {
                    return false;
                }

                if (Convert.ToInt32(softwareLatestDate) > Convert.ToInt32(Properties.Settings.Default.SoftwareDate))
                {
                    if (MessageBox.Show($"{Application.Current.FindResource("MsgBoxSoftwareUpdateFound") as string}\n{Application.Current.FindResource("MsgBoxCurrentVersion") as string} {Properties.Settings.Default.SoftwareVersion} ({Properties.Settings.Default.SoftwareDate})\n{Application.Current.FindResource("MsgBoxLatestVersion") as string} {softwareLatestVersion} ({softwareLatestDate})\n{Application.Current.FindResource("MsgBoxUpdateComfirm") as string}", Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        NotificationMessageUtils.CreateMessage(MessageType.INFO, Application.Current.FindResource("NotificationMessageSoftwareUpdateDownloading") as string);
                        Directory.CreateDirectory(downloadDirectory);
                        await NetworkUtils.HttpDownloadFile(softwareLatestUrl, $@"{downloadDirectory}\{softwareLatestFileName}");
                        if (JObjectUpdateInfo["software_hash_validate_enabled"]!.Value<bool>())
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
                if (Convert.ToInt32(shiplistLatestDate) > Convert.ToInt32(ShipInfoUtils.GetShipInfoDate(Server.EU)))
                {
                    if (MessageBox.Show($"{Application.Current.FindResource("MsgBoxShiplistUpdateFound") as string}\n{Application.Current.FindResource("MsgBoxCurrentVersion") as string} {ShipInfoUtils.GetShipInfoVersion(Server.EU)} ({ShipInfoUtils.GetShipInfoDate(Server.EU)})\n{Application.Current.FindResource("MsgBoxLatestVersion") as string} {shiplistLatestVersion} ({shiplistLatestDate})\n{Application.Current.FindResource("MsgBoxUpdateComfirm") as string}", Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        NotificationMessageUtils.CreateMessage(MessageType.INFO, Application.Current.FindResource("NotificationMessageShiplistUpdateDownloading") as string);
                        Directory.CreateDirectory(downloadDirectory);
                        await NetworkUtils.HttpDownloadFile(shiplistLatestUrl, $@"{downloadDirectory}\{shiplistLatestFileName}");
                        if (JObjectUpdateInfo["shiplist_hash_validate_enabled"]!.Value<bool>())
                        {
                            using SHA256 sha = SHA256.Create();
                            using FileStream fs = new($@"{downloadDirectory}\{shiplistLatestFileName}", FileMode.Open);
                            fs.Position = 0;
                            if (BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "") != shiplistLasestSHA256)
                            {
                                throw new FileFormatException("FileHashInvalid");
                            }
                        }
                        ZipFile.ExtractToDirectory($@"{downloadDirectory}\{shiplistLatestFileName}", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Json\"), true);
                        ShipInfoUtils.LoadShipInfoForServer(Server.EU);
                        NotificationMessageUtils.CreateMessage(MessageType.INFO, Application.Current.FindResource("NotificationMessageShiplistUpdateComplete") as string);
                        MessageBox.Show(Application.Current.FindResource("MsgBoxShiplistUpdateComplete") as string, Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}
