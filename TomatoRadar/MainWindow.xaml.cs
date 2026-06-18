using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.IO;
using System.Diagnostics;
using System.Windows.Threading;

using TomatoRadar.Models;
using TomatoRadar.Utils;
using TomatoRadar.Utils.Sorters;

using Newtonsoft.Json.Linq;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Globalization;

namespace TomatoRadar
{
    partial class MainWindow : Window
    {

        private void SwitchLanguage(Language language)
        {
            Application.Current.Resources.MergedDictionaries[0] = LanguageExt.GetResourceDictionaryByLanguage(language);
            LabelGamePathIsSetOrNot.GetBindingExpression(ContentProperty).UpdateTarget();

            ComboBoxLanguage.SelectionChanged -= ComboBoxLanguage_SelectionChanged;
            ComboBoxLanguage.Items.Clear();
            ComboBoxLanguage.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemLanguageAuto"), Value = "AUTO" });
            ComboBoxLanguage.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemLanguageNameEN_US"), Value = "EN_US" });
            ComboBoxLanguage.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemLanguageNameZH_CN"), Value = "ZH_CN" });
            ComboBoxLanguage.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemLanguageNameRU_RU"), Value = "RU_RU" });
            ComboBoxLanguage.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemLanguageNameJA_JP"), Value = "JA_JP" });
            ComboBoxLanguage.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemLanguageNameZH_TW"), Value = "ZH_TW" });
            ComboBoxLanguage.SelectedValue = Properties.Settings.Default.Language;
            ComboBoxLanguage.SelectionChanged += ComboBoxLanguage_SelectionChanged;

            ComboBoxChartType.SelectionChanged -= ComboBoxChartType_SelectionChanged;
            ComboBoxChartType.Items.Clear();
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeShipTypeAndShipTierAndWinrateDescending"), Value = "0" });
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeShipTierAndShipTypeAndWinrateDescending"), Value = "1" });
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeShipTypeAndWinrateDescending"), Value = "2" });
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeShipTierAndWinrateDescending"), Value = "3" });
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeWinrateDescending"), Value = "4" });
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeWinrateAscending"), Value = "5" });
            ComboBoxChartType.SelectedValue = Properties.Settings.Default.WinrateChartType;
            ComboBoxChartType.SelectionChanged += ComboBoxChartType_SelectionChanged;

            ComboBoxMirrored.SelectionChanged -= ComboBoxMirrored_SelectionChanged;
            ComboBoxMirrored.Items.Clear();
            ComboBoxMirrored.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemMirroredDisplayDisabled"), Value = "False" });
            ComboBoxMirrored.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemMirroredDisplayEnabled"), Value = "True" });
            ComboBoxMirrored.SelectedValue = Properties.Settings.Default.EnemiesDisplayMirrored.ToString();
            ComboBoxMirrored.SelectionChanged += ComboBoxMirrored_SelectionChanged;

            ComboBoxSortBy.SelectionChanged -= ComboBoxSortBy_SelectionChanged;
            ComboBoxSortBy.Items.Clear();
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByShipTypeAndShipTierAndWinrateDescending"), Value = "0" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByShipTierAndShipTypeAndWinrateDescending"), Value = "1" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByShipTypeAndWinrateDescending"), Value = "2" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByShipTierAndWinrateDescending"), Value = "3" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByWinrateDescending"), Value = "4" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByWinrateAscending"), Value = "5" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByBattlesDescending"), Value = "6" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByBattlesAscending"), Value = "7" });
            ComboBoxSortBy.SelectedValue = Properties.Settings.Default.PlayerListSortBy;
            ComboBoxSortBy.SelectionChanged += ComboBoxSortBy_SelectionChanged;

            ComboBoxPlayerNamesVisibility.SelectionChanged -= ComboBoxPlayerNamesVisibility_SelectionChanged;
            ComboBoxPlayerNamesVisibility.Items.Clear();
            ComboBoxPlayerNamesVisibility.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemPlayerNamesVisible"), Value = "True" });
            ComboBoxPlayerNamesVisibility.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemPlayerNamesHidden"), Value = "False" });
            ComboBoxPlayerNamesVisibility.SelectedValue = Properties.Settings.Default.PlayerNamesVisibility.ToString();
            ComboBoxPlayerNamesVisibility.SelectionChanged += ComboBoxPlayerNamesVisibility_SelectionChanged;
        }

        private void SwitchSorting(int sorting)
        {
            if (this.DataContext is not Battlefield battlefield)
            {
                return;
            }
            ListCollectionView? alliesCollectionView = CollectionViewSource.GetDefaultView(battlefield.Allies) as ListCollectionView;
            ListCollectionView? enemiesCollectionView = CollectionViewSource.GetDefaultView(battlefield.Enemies) as ListCollectionView;

            alliesCollectionView!.CustomSort = enemiesCollectionView!.CustomSort = sorting switch
            {
                0 => new CustomSorterByShipTypeAndShipTierAndWinrateDescending(),
                1 => new CustomSorterByShipTierAndShipTypeAndWinrateDescending(),
                2 => new CustomSorterByShipTypeAndWinrateDescending(),
                3 => new CustomSorterByShipTierAndWinrateDescending(),
                4 => new CustomSorterByWinrateDescending(),
                5 => new CustomSorterByWinrateAscending(),
                6 => new CustomSorterByBattlesDescending(),
                7 => new CustomSorterByBattlesAscending(),
                _ => new CustomSorterByShipTypeAndShipTierAndWinrateDescending(),
            };
        }

        private void SwitchWinrateChartType(int chartType)
        {
            if (this.DataContext is not Battlefield battlefield)
            {
                return;
            }
            WinrateChart.Series = ChartUtils.GetWinrateChartSeries(battlefield, chartType);
            WinrateChart.Sections = ChartUtils.GetWinrateChartSections(battlefield, chartType);
        }

        private void RefreshDataGridColumns(bool mirrored)
        {
            DataGridAlliesList.Columns.Clear();
            DataGridAlliesList.Columns.Add(TryFindResource("AlliesNameColumn") as DataGridTemplateColumn);
            DataGridAlliesList.Columns.Add(TryFindResource("AlliesStatisticsColumn") as DataGridTemplateColumn);
            DataGridAlliesList.Columns.Add(TryFindResource("AlliesTagColumn") as DataGridTemplateColumn);
            if (mirrored)
            {
                DataGridEnemiesList.Columns.Clear();
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesTagColumn") as DataGridTemplateColumn);
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesStatisticsColumnMirrored") as DataGridTemplateColumn);
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesNameColumnMirrored") as DataGridTemplateColumn);
            }
            else
            {
                DataGridEnemiesList.Columns.Clear();
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesNameColumn") as DataGridTemplateColumn);
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesStatisticsColumn") as DataGridTemplateColumn);
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesTagColumn") as DataGridTemplateColumn);
            }
        }

        private void ForceUpdateDataGridColumnWidth()
        {
            foreach (DataGridColumn col in DataGridAlliesList.Columns)
            {
                col.Width = 0;
            }
            foreach (DataGridColumn col in DataGridEnemiesList.Columns)
            {
                col.Width = 0;
            }
            DataGridAlliesList.UpdateLayout();
            DataGridEnemiesList.UpdateLayout();
            foreach (DataGridColumn col in DataGridAlliesList.Columns)
            {
                col.Width = DataGridLength.Auto;
            }
            foreach (DataGridColumn col in DataGridEnemiesList.Columns)
            {
                col.Width = DataGridLength.Auto;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.DebugMode)
            {
                LogUtils.SetLogLevel(log4net.Core.Level.Debug);
            }
            else
            {
                LogUtils.SetLogLevel(log4net.Core.Level.Info);
            }

            LogUtils.WriteDebug("Debug Mode");
            LogUtils.WriteInfo($"SoftwareVersion: {Properties.Settings.Default.SoftwareVersion}");
            LogUtils.WriteInfo($"SoftwareDate: {Properties.Settings.Default.SoftwareDate}");

            LogUtils.WriteInfo($"OS Version: {Environment.OSVersion.VersionString}");
            Version currentVersion = Environment.OSVersion.Version;
            if (currentVersion.CompareTo(new Version("6.2")) < 0)
            {
                LogUtils.WriteInfo($"OS is Windows 7 or earlier. Some emoji chars may be displayed incorrectly.");
            }

            if (!Properties.Settings.Default.SettingsUpgradeDone)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.SettingsUpgradeDone = true;
            }

            try
            {
                string oldApiType = (string)Properties.Settings.Default["APITypeSelection"];
                if (!string.IsNullOrEmpty(oldApiType) && Properties.Settings.Default.APITypeSelectionDefault == "VORTEX")
                {
                    Properties.Settings.Default.APITypeSelectionDefault = oldApiType;
                }
            }
            catch { }

            //solve old version settings migration problem
            try
            {
                LanguageExt.GetLanguageByName(Properties.Settings.Default.Language);
            }
            catch
            {
                Properties.Settings.Default.Language = LanguageExt.GetNameByLanguage(Models.Language.AUTO);
            }

            try
            {
                LanguageExt.ParsePriority(Properties.Settings.Default.ShipNameLanguage);
            }
            catch
            {
                Properties.Settings.Default.ShipNameLanguage = LanguageExt.GetNameByLanguage(Models.Language.AUTO);
            }

            try
            {
                ServerExt.GetServerByName(Properties.Settings.Default.Server);
            }
            catch
            {
                Properties.Settings.Default.Server = ServerExt.GetNameByServer(Server.AUTO);
            }

            try
            {
                ServerExt.GetServerByName(Properties.Settings.Default.SecondaryServer);
            }
            catch
            {
                Properties.Settings.Default.SecondaryServer = ServerExt.GetNameByServer(Server.RU);
            }

            Properties.Settings.Default.Save();

            ((App)Application.Current).WindowPlace.Register(this);

            NotificationMessageUtils.InitializeNotificationMessageDataGrid(DataGridNotificationMessages);
            App.DownloadProgressBar = ProgressBarDownload;
            NetworkUtils.InitializeHttpClient();

            string shipInfoPath = Path.Combine(App.ShipInfoDirectory, "ships_wg.json");
            string shipInfoPathLesta = Path.Combine(App.ShipInfoDirectory, "ships_lesta.json");
            ShipInfoUtils.RegisterShipInfoFile(Server.EU, shipInfoPath);
            ShipInfoUtils.RegisterShipInfoFile(Server.NA, shipInfoPath);
            ShipInfoUtils.RegisterShipInfoFile(Server.ASIA, shipInfoPath);
            ShipInfoUtils.RegisterShipInfoFile(Server.CN, shipInfoPath);
            ShipInfoUtils.RegisterShipInfoFile(Server.RU, shipInfoPathLesta);
            ShipInfoUtils.LoadShipInfoForServer(Server.EU);
            ShipInfoUtils.LoadShipInfoForServer(Server.RU);
            LogUtils.WriteInfo($"ShipInfoFileVersion: {ShipInfoUtils.GetShipInfoVersion(Server.EU)}");
            LogUtils.WriteInfo($"ShipInfoFileDate: {ShipInfoUtils.GetShipInfoDate(Server.EU)}");
            LogUtils.WriteInfo($"ShipInfoFileVersion(RU): {ShipInfoUtils.GetShipInfoVersion(Server.RU)}");
            LogUtils.WriteInfo($"ShipInfoFileDate(RU): {ShipInfoUtils.GetShipInfoDate(Server.RU)}");


            SwitchLanguage(LanguageExt.GetLanguageByName(Properties.Settings.Default.Language));

            if (Properties.Settings.Default.CheckForUpdatesOnStartup)
            {
                NotificationMessageUtils.CreateMessage(MessageType.INFO, FindResource("NotificationMessageCheckingForUpdates") as string);
                SoftwareUpdateUtils.CheckForUpdates().ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        NotificationMessageUtils.CreateMessage(MessageType.INFO, FindResource("NotificationMessageWaitingForBattle") as string);
                    });
                }, TaskScheduler.Default);
            }
            else
            {
                NotificationMessageUtils.CreateMessage(MessageType.INFO, FindResource("NotificationMessageWaitingForBattle") as string);
            }
            WinrateChart.TooltipTextPaint = new SolidColorPaint { Color = SKColors.Black, FontFamily = WinrateChart.FontFamily.Source };
            WinrateChart.XAxes = new Axis[] { new Axis { IsVisible = false } };
            WinrateChart.YAxes = new Axis[] { new Axis { Labeler = d => { return d.ToString("p1"); }, CrosshairPaint = new SolidColorPaint(SKColors.Gray) } };
            KDEChart.XAxes = new Axis[] { new Axis { Labeler = d => { return d.ToString("p1"); }, SeparatorsPaint = new SolidColorPaint(SKColors.LightGray), CrosshairPaint = new SolidColorPaint(SKColors.Gray) } };
            KDEChart.YAxes = new Axis[] { new Axis { IsVisible = false } };

            RefreshDataGridColumns(Properties.Settings.Default.EnemiesDisplayMirrored);

            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();
            LogUtils.WriteInfo("Timer Start");
        }

        private bool _dataLoadInProgress;
        private string _currentBattleFilePath = "";
        private string _korabliReplayPath = "";
        private long _currentBattleFileSize;
        private bool _isFogOfWarBattle;
        private bool _incrementalScanInProgress;
        private readonly HashSet<string> _knownVehicleNames = new();
        private string _lastBattleFilePath = "";

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_dataLoadInProgress)
                    return;

                if (DataContext != null)
                {
                    if (_currentBattleFilePath != "" && File.Exists(_currentBattleFilePath))
                    {
                        long currentSize;
                        if (_korabliReplayPath != "")
                            currentSize = new FileInfo(_korabliReplayPath).Length;
                        else
                            currentSize = new FileInfo(_currentBattleFilePath).Length;

                        if (_korabliReplayPath == "" && currentSize < _currentBattleFileSize && currentSize < 50000)
                        {
                            LogUtils.WriteInfo($"Battle ended (file shrunk from {_currentBattleFileSize} to {currentSize} bytes).");
                            if (Properties.Settings.Default.ClearPlayerListAfterBattle)
                            {
                                ResetBattle();
                            }
                            else
                            {
                                _lastBattleFilePath = _currentBattleFilePath;
                                _currentBattleFilePath = "";
                                _korabliReplayPath = "";
                                _currentBattleFileSize = 0;
                                _isFogOfWarBattle = false;
                                _knownVehicleNames.Clear();
                            }
                            return;
                        }

                        if (!_incrementalScanInProgress && currentSize > _currentBattleFileSize)
                        {
                            _currentBattleFileSize = currentSize;
                            _ = IncrementalScanAsync();
                        }
                        return;
                    }

                    if (_currentBattleFilePath != "" && !File.Exists(_currentBattleFilePath))
                    {
                        LogUtils.WriteInfo("Battle ended (temp file deleted).");
                        if (Properties.Settings.Default.ClearPlayerListAfterBattle)
                        {
                            ResetBattle();
                        }
                        else
                        {
                            _lastBattleFilePath = _currentBattleFilePath;
                            _currentBattleFilePath = "";
                            _korabliReplayPath = "";
                            _currentBattleFileSize = 0;
                            _isFogOfWarBattle = false;
                            _knownVehicleNames.Clear();
                        }
                    }

                    if (_currentBattleFilePath == "")
                    {
                        string nextFileName = FileUtils.GetLatestTempArenaInfoFile(true);
                        if (nextFileName != "" && nextFileName != _lastBattleFilePath)
                        {
                            ResetBattle();
                            _currentBattleFilePath = nextFileName;
                            _currentBattleFileSize = new FileInfo(nextFileName).Length;
                            _dataLoadInProgress = true;
                            ReadPlayersListAndGetDataFromServer(nextFileName);
                        }
                    }
                    return;
                }

                string latestFileName = FileUtils.GetLatestTempArenaInfoFile(true);
                if (latestFileName != "")
                {
                    _currentBattleFilePath = latestFileName;
                    _currentBattleFileSize = new FileInfo(latestFileName).Length;
                    _dataLoadInProgress = true;
                    ReadPlayersListAndGetDataFromServer(latestFileName);
                }
            }
            catch (Exception ex)
            {
                LogUtils.WriteError("Timer_Tick crashed", ex);
            }
        }

        private void ResetBattle()
        {
            DataContext = null;
            _currentBattleFilePath = "";
            _korabliReplayPath = "";
            _currentBattleFileSize = 0;
            _isFogOfWarBattle = false;
            _knownVehicleNames.Clear();
            WinrateChart.Series = Array.Empty<ISeries>();
            KDEChart.Series = Array.Empty<ISeries>();
            TxtOutputText.Text = "";
            NotificationMessageUtils.CreateMessage(MessageType.INFO, FindResource("NotificationMessageWaitingForBattle") as string);
        }

        private async void ReadPlayersListAndGetDataFromServer(string filename)
        {
            LogUtils.WriteInfo("Reading Players List");
            LogUtils.WriteInfo($"gamePath={Properties.Settings.Default.GamePath}");
            LogUtils.WriteInfo($"filename={filename}");

            int maximumRetryAttempts = Properties.Settings.Default.MaximumRetryAttemptsOnError;
            const int delayTimeBetweenRetryAttempts = 1000;
            const int delayTimeBetweenHttpRequestsBaseValue = 20;
            const int delayTimeBetweenHttpRequestsAdditionalDelayPerRetryAttempt = 10;

            for (int i = 0; i <= maximumRetryAttempts; i++)
            {
                try
                {
                    BtnRefresh.IsEnabled = false;
                    BtnOpen.IsEnabled = false;

                    int delayTimeBetweenHttpRequests = delayTimeBetweenHttpRequestsBaseValue + i * delayTimeBetweenHttpRequestsAdditionalDelayPerRetryAttempt;

                    Server server = ServerExt.GetServerByName(Properties.Settings.Default.Server);

                    LogUtils.WriteInfo($"server={ServerExt.GetNameByServer(server)}");
                    if (server == Server.AUTO)
                    {
                        server = ServerExt.AutoDetectServer($@"{Properties.Settings.Default.GamePath}\profile\clientrunner.log");
                        LogUtils.WriteInfo($"detectedServer={ServerExt.GetNameByServer(server)}");
                    }

                    //secondary server: enemy players come from different server, for cross-server CW only
                    Server secondaryServer = ServerExt.GetServerByName(Properties.Settings.Default.SecondaryServer);
                    LogUtils.WriteInfo($"secondaryServer={ServerExt.GetNameByServer(secondaryServer)}");

                    string dataPath = filename;
                    if (server == Server.RU)
                    {
                        string? dir = Path.GetDirectoryName(filename);
                        if (dir != null)
                        {
                            string korabliPath = Path.Combine(dir, "temp.korablireplay");
                            if (File.Exists(korabliPath))
                            {
                                _korabliReplayPath = korabliPath;
                                dataPath = korabliPath;
                                _currentBattleFileSize = new FileInfo(korabliPath).Length;
                            }
                        }
                    }

                    JObject JObjectWatchList = WatchListUtils.ReadWatchList(Path.Combine(App.DataDirectory, "WatchList.json"));
                    JObject? JObjectTempArenaInfo = FileUtils.GetPlayerListJObject(server, dataPath);
                    if (JObjectTempArenaInfo == null)
                        throw new FileFormatException("PlayerListJObjectIsNull");

                    string battleType = JObjectTempArenaInfo["matchGroup"]!.Value<string>()!;
                    DateTimeOffset battleStartTime = DateTimeOffset.ParseExact(JObjectTempArenaInfo["dateTime"]!.Value<string>()!, "dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture);

                    int playerCount = JObjectTempArenaInfo["vehicles"]!.Count();
                    LogUtils.WriteInfo($"playerCount={playerCount}");

                    List<Player> playerList;
                    List<Task<List<Player>>> taskList = new();

                    NotificationMessageUtils.CreateMessage(MessageType.INFO, FindResource("NotificationMessageRetrivingData") as string);

                    APIType effectiveApiType = ApiUtils.GetApiTypeForServer(server);

                    if (effectiveApiType == APIType.VORTEX)
                    {
                        if (server == Server.RU)
                        {
                            if (Properties.Settings.Default.SecondaryServerEnabled)
                            {
                                taskList.Add(ApiUtils.VortexApiGetPlayersStatisticsRU(playerCount, 1, JObjectTempArenaInfo, server, delayTimeBetweenHttpRequests));
                                taskList.Add(ApiUtils.VortexApiGetPlayersStatisticsRU(playerCount, 2, JObjectTempArenaInfo, secondaryServer, delayTimeBetweenHttpRequests));
                            }
                            else
                            {
                                taskList.Add(ApiUtils.VortexApiGetPlayersStatisticsRU(playerCount, 0, JObjectTempArenaInfo, server, delayTimeBetweenHttpRequests));
                            }
                        }
                        else
                        {
                            if (Properties.Settings.Default.SecondaryServerEnabled)
                            {
                                taskList.Add(ApiUtils.VortexApiGetPlayersStatistics(playerCount, 1, JObjectTempArenaInfo, server, delayTimeBetweenHttpRequests));
                                taskList.Add(ApiUtils.VortexApiGetPlayersStatistics(playerCount, 2, JObjectTempArenaInfo, secondaryServer, delayTimeBetweenHttpRequests));
                            }
                            else
                            {
                                taskList.Add(ApiUtils.VortexApiGetPlayersStatistics(playerCount, 0, JObjectTempArenaInfo, server, delayTimeBetweenHttpRequests));
                            }
                        }
                    }
                    else if (effectiveApiType == APIType.LESTA_PUBLIC)
                    {
                        if (Properties.Settings.Default.SecondaryServerEnabled)
                        {
                            taskList.Add(ApiUtils.LestaPublicApiGetPlayersStatistics(playerCount, 1, JObjectTempArenaInfo, server, delayTimeBetweenHttpRequests));
                            taskList.Add(ApiUtils.LestaPublicApiGetPlayersStatistics(playerCount, 2, JObjectTempArenaInfo, secondaryServer, delayTimeBetweenHttpRequests));
                        }
                        else
                        {
                            taskList.Add(ApiUtils.LestaPublicApiGetPlayersStatistics(playerCount, 0, JObjectTempArenaInfo, server, delayTimeBetweenHttpRequests));
                        }
                    }
                    else
                    {
                        bool useYuyukoProxy = (effectiveApiType == APIType.WG_PUBLIC_WITH_YUYUKO_PROXY);
                        if (Properties.Settings.Default.SecondaryServerEnabled)
                        {
                            taskList.Add(ApiUtils.WgPublicApiGetPlayersStatistics(playerCount, 1, JObjectTempArenaInfo, server, delayTimeBetweenHttpRequests, useYuyukoProxy));
                            taskList.Add(ApiUtils.WgPublicApiGetPlayersStatistics(playerCount, 2, JObjectTempArenaInfo, secondaryServer, delayTimeBetweenHttpRequests, useYuyukoProxy));
                        }
                        else
                        {
                            taskList.Add(ApiUtils.WgPublicApiGetPlayersStatistics(playerCount, 0, JObjectTempArenaInfo, server, delayTimeBetweenHttpRequests, useYuyukoProxy));
                        }
                    }

                    await Task.WhenAll(taskList);

                    playerList = taskList[0].Result;
                    if (Properties.Settings.Default.SecondaryServerEnabled)
                    {
                        playerList = playerList.Concat(taskList[1].Result).ToList();
                    }

                    //check if player is on the watchlist
                    foreach (Player p in playerList)
                    {
                        if (p.ID != "-1" && JObjectWatchList[ServerExt.GetNameByServer(p.Server)]!.SelectToken(p.ID) != null)
                        {
                            p.WatchStatus = WatchStatusExt.GetStatusByName(JObjectWatchList[ServerExt.GetNameByServer(p.Server)]![p.ID]!["status"]!.Value<string>()!);
                        }
                    }

                    //battlefield is the main model containing ally and enemy player list
                    Battlefield battlefield = new(battleType, battleStartTime, playerList);
                    this.DataContext = battlefield;

                    //fog of war detection: when enabled, enemy players are gradually revealed
                    _knownVehicleNames.Clear();
                    foreach (var v in JObjectTempArenaInfo["vehicles"]!)
                    {
                        if (v["id"]!.Value<int>() > 30)
                        {
                            _knownVehicleNames.Add(v["name"]!.Value<string>()!);
                        }
                    }
                    _isFogOfWarBattle = server == Server.RU
                        ? FileUtils.ReadFogOfWarFlagFromKorabliReplayDir(_korabliReplayPath) == 1
                        : (JObjectTempArenaInfo["isFogOfWar"]?.Value<int>() ?? 0) == 1;
                    LogUtils.WriteInfo($"isFogOfWar={_isFogOfWarBattle}");

                    //push feature under dev
                    if (Properties.Settings.Default.YuyukoAPIPushEnabled)
                    {
                        ApiUtils.YuyukoApiPushBattlefieldInfo(battlefield);
                    }

                    TxtOutputText.Text = TextUtils.GenerateGeneralStatisticsOutputText(battlefield);
                    if (Properties.Settings.Default.OutputTextAutoCopy && Properties.Settings.Default.OutputTextUnlock)
                    {
                        Clipboard.SetDataObject(TxtOutputText.Text);
                    }

                    SwitchSorting(Properties.Settings.Default.PlayerListSortBy);
                    SwitchWinrateChartType(Properties.Settings.Default.WinrateChartType);
                    KDEChart.Series = ChartUtils.GetKDEChartSeries(battlefield);

                    NotificationMessageUtils.CreateMessage(MessageType.INFO, FindResource("NotificationMessageDataRetrieved") as string);
                    return;
                }
                catch (Exception ex)
                {
                    LogUtils.WriteError("", ex);
                    _ = ex.Message switch
                    {
                        "FileFormatIncorrect" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, FindResource("NotificationMessageFileError") as string),
                        "ServerAutoDetectionFailed" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, FindResource("NotificationMessageServerAutoDetectionFailed") as string),
                        "HttpRequestFailed" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, FindResource("NotificationMessageConnectionError") as string),
                        "JsonStringNotValid" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, FindResource("NotificationMessageJsonError") as string),
                        "PlayerListJObjectIsNull" => NotificationMessageUtils.CreateMessage(MessageType.INFO, FindResource("NotificationMessageRetrivingData") as string),
                        _ => NotificationMessageUtils.CreateMessage(MessageType.ERROR, FindResource("NotificationMessageOtherError") as string),
                    };
                    if (ex.Message == "FileFormatIncorrect" || ex.Message == "ServerAutoDetectionFailed")
                    {
                        return;
                    }
                    else
                    {
                        if (i < maximumRetryAttempts)
                        {
                            await Task.Delay(delayTimeBetweenRetryAttempts);
                            NotificationMessageUtils.CreateMessage(MessageType.INFO, $"{FindResource("NotificationMessageRetrying")}{i + 1}{FindResource("NotificationMessageAttempt")}");
                        }
                        else if (maximumRetryAttempts > 0)
                        {
                            NotificationMessageUtils.CreateMessage(MessageType.INFO, FindResource("NotificationMessageMaximumAttemptsReached") as string);
                        }
                    }
                }
                finally
                {
                    _dataLoadInProgress = false;
                    BtnRefresh.IsEnabled = true;
                    BtnOpen.IsEnabled = true;
                }
            }
        }

        private async Task IncrementalScanAsync()
        {
            _incrementalScanInProgress = true;
            try
            {
                string filename = _currentBattleFilePath;
                if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
                    return;

                string dataPath = filename;
                if (_korabliReplayPath != "")
                    dataPath = _korabliReplayPath;

                Server server = ServerExt.GetServerByName(Properties.Settings.Default.Server);
                if (server == Server.AUTO)
                {
                    server = ServerExt.AutoDetectServer($@"{Properties.Settings.Default.GamePath}\profile\clientrunner.log");
                }

                JObject? JObjectTempArenaInfo = FileUtils.GetPlayerListJObject(server, dataPath);
                if (JObjectTempArenaInfo == null)
                    return;

                JArray vehicles = (JArray)JObjectTempArenaInfo["vehicles"]!;
                List<JToken> newVehicleTokens = new();
                foreach (var v in vehicles)
                {
                    string vehicleName = v["name"]!.Value<string>()!;
                    if (!_knownVehicleNames.Contains(vehicleName) && v["id"]!.Value<int>() > 30)
                    {
                        _knownVehicleNames.Add(vehicleName);
                        newVehicleTokens.Add(v);
                    }
                }

                if (newVehicleTokens.Count == 0)
                    return;

                var newVehicleNames = string.Join(", ", newVehicleTokens.Select(t => t["name"]!.Value<string>()));
                LogUtils.WriteInfo($"IncrementalScan: {newVehicleTokens.Count} new vehicles detected: {newVehicleNames}");

                JObject filteredJObject = new()
                {
                    ["vehicles"] = new JArray(newVehicleTokens),
                    ["matchGroup"] = JObjectTempArenaInfo["matchGroup"],
                    ["dateTime"] = JObjectTempArenaInfo["dateTime"],
                };

                int newPlayerCount = newVehicleTokens.Count;
                const int delayTimeBetweenHttpRequests = 20;

                APIType effectiveApiType = ApiUtils.GetApiTypeForServer(server);
                List<Task<List<Player>>> taskList = new();

                if (effectiveApiType == APIType.VORTEX)
                {
                    if (server == Server.RU)
                    {
                        taskList.Add(ApiUtils.VortexApiGetPlayersStatisticsRU(newPlayerCount, 0, filteredJObject, server, delayTimeBetweenHttpRequests));
                    }
                    else
                    {
                        taskList.Add(ApiUtils.VortexApiGetPlayersStatistics(newPlayerCount, 0, filteredJObject, server, delayTimeBetweenHttpRequests));
                    }
                }
                else if (effectiveApiType == APIType.LESTA_PUBLIC)
                {
                    taskList.Add(ApiUtils.LestaPublicApiGetPlayersStatistics(newPlayerCount, 0, filteredJObject, server, delayTimeBetweenHttpRequests));
                }
                else
                {
                    bool useYuyukoProxy = (effectiveApiType == APIType.WG_PUBLIC_WITH_YUYUKO_PROXY);
                    taskList.Add(ApiUtils.WgPublicApiGetPlayersStatistics(newPlayerCount, 0, filteredJObject, server, delayTimeBetweenHttpRequests, useYuyukoProxy));
                }

                await Task.WhenAll(taskList);
                List<Player> newPlayerList = taskList[0].Result;

                if (newPlayerList.Count > 0 && DataContext is Battlefield battlefield)
                {
                    JObject JObjectWatchList = WatchListUtils.ReadWatchList(Path.Combine(App.DataDirectory, "WatchList.json"));
                    foreach (Player p in newPlayerList)
                    {
                        if (p.ID != "-1" && JObjectWatchList[ServerExt.GetNameByServer(p.Server)]?.SelectToken(p.ID) != null)
                        {
                            p.WatchStatus = WatchStatusExt.GetStatusByName(JObjectWatchList[ServerExt.GetNameByServer(p.Server)]![p.ID]!["status"]!.Value<string>()!);
                        }
                    }

                    battlefield.AddPlayers(newPlayerList);

                    SwitchSorting(Properties.Settings.Default.PlayerListSortBy);
                    SwitchWinrateChartType(Properties.Settings.Default.WinrateChartType);
                    KDEChart.Series = ChartUtils.GetKDEChartSeries(battlefield);
                    TxtOutputText.Text = TextUtils.GenerateGeneralStatisticsOutputText(battlefield);

                    object tmp = DataContext;
                    DataContext = null;
                    DataContext = tmp;

                    var newShipNames = string.Join(", ", newPlayerList.Take(5).Select(p => p.ShipName));
                    if (newPlayerList.Count > 5) newShipNames += "...";
                    NotificationMessageUtils.CreateMessage(MessageType.INFO, string.Format((string)FindResource("NotificationMessageNewPlayersRevealed"), newPlayerList.Count, newShipNames));
                }
            }
            catch (Exception ex)
            {
                LogUtils.WriteError("IncrementalScan error", ex);
            }
            finally
            {
                _incrementalScanInProgress = false;
            }
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            ConfigWindow configWindow = new()
            {
                Owner = this
            };
            configWindow.ShowDialog();
            ForceUpdateDataGridColumnWidth();
        }

        private void ComboBoxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.Language = ComboBoxLanguage.SelectedValue.ToString()!;
            Properties.Settings.Default.Save();
            SwitchLanguage(LanguageExt.GetLanguageByName(Properties.Settings.Default.Language));
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            string latestFileName = FileUtils.GetLatestTempArenaInfoFile(false);
            if (latestFileName != "")
            {
                ReadPlayersListAndGetDataFromServer(latestFileName);
            }
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new();
            dialog.Filter = "*.json, *.wowsreplay, *.korablireplay|*.json;*.wowsreplay;*.korablireplay";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DataContext = null;
                WinrateChart.Series = Array.Empty<ISeries>();
                KDEChart.Series = Array.Empty<ISeries>();
                TxtOutputText.Text = "";
                _currentBattleFilePath = dialog.FileName;
                _currentBattleFileSize = new FileInfo(dialog.FileName).Length;
                _dataLoadInProgress = true;
                ReadPlayersListAndGetDataFromServer(dialog.FileName);
            }
        }

        private void ContextMenuCheckOnWoWSNumbers_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            Process.Start("explorer.exe", $"{ServerExt.GetWoWSNumbersUrlStringByServer(p!.Server)}/player/{p.ID}%2C{p.Name}/");
        }

        private void ContextMenuCheckOnProships_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            Process.Start("explorer.exe", ServerExt.GetProshipsUrlByPlayerId(p!.ID));
        }

        private void ContextMenuCheckOnWoWSOfficialSite_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            Process.Start("explorer.exe", $"https://profile.{ServerExt.GetFullUrlStringByServer(p!.Server)}/statistics/{p.ID}/");
        }

        private void BtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            int gridH = (int)MainWindowGrid.ActualHeight;
            int gridW = (int)MainWindowGrid.ActualWidth;
            RenderTargetBitmap bitmap = new(gridW, gridH, 96, 96, PixelFormats.Default);
            Rect rect = new(0, 0, gridW, gridH);
            DrawingVisual drawingVisual = new();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Background, null, rect);
            }
            bitmap.Render(drawingVisual);
            bitmap.Render(MainWindowGrid);
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            string datetimestr = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss_ffff");
            if (!Directory.Exists(Path.Combine(App.DataDirectory, "Screenshot")))
            {
                Directory.CreateDirectory(Path.Combine(App.DataDirectory, "Screenshot"));
            }
            string filePath = Path.Combine(App.DataDirectory, "Screenshot", $"{datetimestr}.png");
            using FileStream fs = new(filePath, FileMode.Create);
            encoder.Save(fs);
            NotificationMessageUtils.CreateMessage(MessageType.INFO, $"{FindResource("NotificationMessageScreenshotIsSavedTo") as string}{filePath}{FindResource("NotificationMessagePeriod") as string}");
        }

        //drag and drop a replay file on the window to open it
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                object? files = e.Data.GetData(DataFormats.FileDrop);
                if (files is not null)
                {
                    if ((files as string[])!.Length == 1)
                    {
                        ReadPlayersListAndGetDataFromServer((files as string[])![0]);
                    }
                }
            }
        }

        private void ContextMenuCopyPlayerStatistics_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            Clipboard.SetDataObject(TextUtils.GenerateParticularPlayerStatisticsOutputText(p!));
        }

        private void ContextMenuAddToWatchListPositive_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.POSITIVE;
            WatchListUtils.SaveWatchList(p, Path.Combine(App.DataDirectory, "WatchList.json"));
        }

        private void ContextMenuAddToWatchListNegative_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.NEGATIVE;
            WatchListUtils.SaveWatchList(p, Path.Combine(App.DataDirectory, "WatchList.json"));
        }

        private void ContextMenuAddToWatchListCheater_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.CHEATER;
            WatchListUtils.SaveWatchList(p, Path.Combine(App.DataDirectory, "WatchList.json"));
        }

        private void ContextMenuRemoveFromWatchList_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.NONE;
            WatchListUtils.SaveWatchList(p, Path.Combine(App.DataDirectory, "WatchList.json"));
        }

        //the ToolTipPlayerDetails.LayoutTransform is bind to the ViewboxPlayerList.Tag
        //so the tooltip will scale to fit the size of the datagrid when changing window size
        //a dumb way but it works
        private void ViewboxPlayerList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (VisualTreeHelper.GetChild(ViewboxPlayerList, 0) is ContainerVisual cv)
            {
                ViewboxPlayerList.Tag = cv.Transform;
            }
        }

        private void ComboBoxChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.WinrateChartType = Convert.ToInt32(ComboBoxChartType.SelectedValue);
            Properties.Settings.Default.Save();
            SwitchWinrateChartType(Properties.Settings.Default.WinrateChartType);
        }

        private void ComboBoxMirrored_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.EnemiesDisplayMirrored = Convert.ToBoolean(ComboBoxMirrored.SelectedValue);
            Properties.Settings.Default.Save();
            RefreshDataGridColumns(Properties.Settings.Default.EnemiesDisplayMirrored);
        }

        private void ComboBoxSortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.PlayerListSortBy = Convert.ToInt32(ComboBoxSortBy.SelectedValue);
            Properties.Settings.Default.Save();
            SwitchSorting(Properties.Settings.Default.PlayerListSortBy);
        }

        private void ComboBoxPlayerNamesVisibility_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.PlayerNamesVisibility = Convert.ToBoolean(ComboBoxPlayerNamesVisibility.SelectedValue);
            Properties.Settings.Default.Save();

            //force the datagrid to refresh. a dumb way but it works
            object tmpDataContext = this.DataContext;
            this.DataContext = null;
            this.DataContext = tmpDataContext;
        }

        private void HyperLinkTomatoRadarWebsite_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start("explorer.exe", e.Uri.AbsoluteUri);
        }
    }
}
