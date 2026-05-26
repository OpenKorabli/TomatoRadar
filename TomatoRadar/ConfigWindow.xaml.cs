using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using TomatoRadar.Models;
using TomatoRadar.Utils;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace TomatoRadar
{
    partial class ConfigWindow : Window
    {
        private void LoadSettings()
        {
            ComboBoxGamePath.Text = Properties.Settings.Default.GamePath;
            TxtOutputTextTemplateGeneralStatistics.Text = Properties.Settings.Default.OutputTextTemplateGeneralStatistics;
            TxtOutputTextTemplateParticularPlayerStatistics.Text = Properties.Settings.Default.OutputTextTemplateParticularPlayerStatistics;
            ChkBoxSecondaryServerEnabled.IsChecked = Properties.Settings.Default.SecondaryServerEnabled;
            ComboBoxSecondaryServer.SelectedValue = Properties.Settings.Default.SecondaryServer;
            SliderMaximumRetryAttemptsOnError.Value = Properties.Settings.Default.MaximumRetryAttemptsOnError;
            ComboBoxWinrateTypeSelect.SelectedIndex = Properties.Settings.Default.WinrateTypeUsed;
            ComboBoxColorStyle.SelectedIndex = Properties.Settings.Default.ColorStyle;
            TxtTomatoIcon.Text = Properties.Settings.Default.TomatoIcon;
            TxtUnicumIcon.Text = Properties.Settings.Default.UnicumIcon;
            TxtHiddenIcon.Text = Properties.Settings.Default.HiddenIcon;
            TxtWatchIcon.Text = Properties.Settings.Default.WatchIcon;
            SliderPlayerColumnFontSize.Value = Properties.Settings.Default.PlayerColumnFontSize;
            SliderStatisticsColumnFontSize.Value = Properties.Settings.Default.StatisticsColumnFontSize;
            SliderDetailedStatisticsFontSize.Value = Properties.Settings.Default.DetailedStatisticsFontSize;
            SliderOutputTextFontSize.Value = Properties.Settings.Default.OutputTextFontSize;
            ComboBoxAccountWinrateVisibility.SelectedIndex = Properties.Settings.Default.AccountWinrateVisibility;
            ComboBoxWeightedWinrateVisibility.SelectedIndex = Properties.Settings.Default.WeightedWinrateVisibility;
            ComboBoxShipWinrateVisibility.SelectedIndex = Properties.Settings.Default.ShipWinrateVisibility;
            ComboBoxAccountAvgExpVisibility.SelectedIndex = Properties.Settings.Default.AccountAvgExpVisibility;
            ComboBoxShipAvgExpVisibility.SelectedIndex = Properties.Settings.Default.ShipAvgExpVisibility;
            ComboBoxShipAvgDmgVisibility.SelectedIndex = Properties.Settings.Default.ShipAvgDmgVisibility;
            ComboBoxTagVisibility.SelectedIndex = Properties.Settings.Default.TagVisibility;
            ChkBoxShortMode.IsChecked = Properties.Settings.Default.OutputTextShortMode;
            ChkBoxExcludeYourself.IsChecked = Properties.Settings.Default.OutputTextExcludeSelf;
            ChkBoxTextOutputUnlocked.Checked -= ChkBoxTextOutputUnlocked_Checked;
            ChkBoxTextOutputUnlocked.IsChecked = Properties.Settings.Default.OutputTextUnlock;
            ChkBoxTextOutputUnlocked.Checked += ChkBoxTextOutputUnlocked_Checked;
            ChkBoxAutoCopy.IsChecked = Properties.Settings.Default.OutputTextAutoCopy && Properties.Settings.Default.OutputTextUnlock;
            ComboBoxAPITypeDefault.SelectedValue = Properties.Settings.Default.APITypeSelectionDefault;
            ComboBoxAPITypeRU.SelectedValue = Properties.Settings.Default.APITypeSelectionRU;
            ComboBoxAPITypeCN.SelectedValue = Properties.Settings.Default.APITypeSelectionCN;
            ChkBoxEnableYuyukoAPIPush.IsChecked = Properties.Settings.Default.YuyukoAPIPushEnabled;
            ChkBoxEnableDebugMode.IsChecked = Properties.Settings.Default.DebugMode;
            TxtTomatoWinrateThreshold.Text = Properties.Settings.Default.TomatoWinrateThreshold.ToString("f1");
            TxtUnicumWinrateThreshold.Text = Properties.Settings.Default.UnicumWinrateThreshold.ToString("f1");
            TxtTomatoBattleCountThreshold.Text = Properties.Settings.Default.TomatoBattleCountThreshold.ToString();
            TxtUnicumBattleCountThreshold.Text = Properties.Settings.Default.UnicumBattleCountThreshold.ToString();
            TxtWeightedWinrateAccountSoloWeightMultiplier.Text = Properties.Settings.Default.WeightedWinrateAccountSoloWeightMultiplier.ToString("f1");
            TxtWeightedWinrateAccountDiv2WeightMultiplier.Text = Properties.Settings.Default.WeightedWinrateAccountDiv2WeightMultiplier.ToString("f1");
            TxtWeightedWinrateAccountDiv3WeightMultiplier.Text = Properties.Settings.Default.WeightedWinrateAccountDiv3WeightMultiplier.ToString("f1");
            TxtWeightedWinrateShipMaxWeight.Text = Properties.Settings.Default.WeightedWinrateShipMaxWeight.ToString("f1");
            TxtWeightedWinrateShipBattlesAtMaxWeight.Text = Properties.Settings.Default.WeightedWinrateShipBattlesAtMaxWeight.ToString();
            TxtDelimiter.Text = Properties.Settings.Default.OutputTextDelimiter;
            ComboBoxServer.SelectedValue = Properties.Settings.Default.Server;
            PopulateShipNameLanguagePriorityList();
            ChkBoxCheckForUpdatesOnStartup.IsChecked = Properties.Settings.Default.CheckForUpdatesOnStartup;
            LabelShipListVersionDateStr.Content = $"{ShipInfoUtils.GetShipInfoVersion(Server.EU)} ({ShipInfoUtils.GetShipInfoDate(Server.EU)})";
        }

        private int SaveSettings()
        {
            try
            {
                if (double.TryParse(TxtTomatoWinrateThreshold.Text, out double TomatoWinrateThreshold) & double.TryParse(TxtUnicumWinrateThreshold.Text, out double UnicumWinrateThreshold) & int.TryParse(TxtTomatoBattleCountThreshold.Text, out int TomatoBattleCountThreshold) & int.TryParse(TxtUnicumBattleCountThreshold.Text, out int UnicumBattleCountThreshold) & double.TryParse(TxtWeightedWinrateAccountSoloWeightMultiplier.Text, out double WeightedWinrateAccountSoloWeightMultiplier) & double.TryParse(TxtWeightedWinrateAccountDiv2WeightMultiplier.Text, out double WeightedWinrateAccountDiv2WeightMultiplier) & double.TryParse(TxtWeightedWinrateAccountDiv3WeightMultiplier.Text, out double WeightedWinrateAccountDiv3WeightMultiplier) & double.TryParse(TxtWeightedWinrateShipMaxWeight.Text, out double WeightedWinrateShipMaxWeight) & int.TryParse(TxtWeightedWinrateShipBattlesAtMaxWeight.Text, out int WeightedWinrateShipBattlesAtMaxWeight))
                {
                    if (TomatoWinrateThreshold < 0 || TomatoWinrateThreshold > 100 || UnicumWinrateThreshold < 0 || UnicumWinrateThreshold > 100 || TomatoBattleCountThreshold < 0 || UnicumBattleCountThreshold < 0 || WeightedWinrateAccountSoloWeightMultiplier < 0 || WeightedWinrateAccountSoloWeightMultiplier > 10000 || WeightedWinrateAccountDiv2WeightMultiplier < 0 || WeightedWinrateAccountDiv2WeightMultiplier > 10000 || WeightedWinrateAccountDiv3WeightMultiplier < 0 || WeightedWinrateAccountDiv3WeightMultiplier > 10000 || WeightedWinrateAccountSoloWeightMultiplier + WeightedWinrateAccountDiv2WeightMultiplier + WeightedWinrateAccountDiv3WeightMultiplier == 0 || WeightedWinrateShipMaxWeight < 0 || WeightedWinrateShipMaxWeight > 100 || WeightedWinrateShipBattlesAtMaxWeight < 0)
                    {
                        return -1;
                    }

                    if (!Directory.Exists(ComboBoxGamePath.Text) || (!File.Exists($@"{ComboBoxGamePath.Text}\WorldOfWarships.exe") && !File.Exists($@"{ComboBoxGamePath.Text}\Korabli.exe")))
                    {
                        if (System.Windows.MessageBox.Show(TryFindResource("MsgBoxGamePathErrorConfirmation") as string, TryFindResource("MsgBoxConfirmation") as string, MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
                        {
                            return -2;
                        }
                    }

                    Properties.Settings.Default.GamePath = ComboBoxGamePath.Text;
                    Properties.Settings.Default.OutputTextTemplateGeneralStatistics = TxtOutputTextTemplateGeneralStatistics.Text;
                    Properties.Settings.Default.OutputTextTemplateParticularPlayerStatistics = TxtOutputTextTemplateParticularPlayerStatistics.Text;
                    Properties.Settings.Default.SecondaryServerEnabled = ChkBoxSecondaryServerEnabled.IsChecked ?? false;
                    Properties.Settings.Default.SecondaryServer = ComboBoxSecondaryServer.SelectedValue.ToString();
                    Properties.Settings.Default.MaximumRetryAttemptsOnError = Convert.ToInt32(SliderMaximumRetryAttemptsOnError.Value);
                    Properties.Settings.Default.WinrateTypeUsed = ComboBoxWinrateTypeSelect.SelectedIndex;
                    Properties.Settings.Default.ColorStyle = ComboBoxColorStyle.SelectedIndex;
                    Properties.Settings.Default.TomatoIcon = TxtTomatoIcon.Text;
                    Properties.Settings.Default.UnicumIcon = TxtUnicumIcon.Text;
                    Properties.Settings.Default.HiddenIcon = TxtHiddenIcon.Text;
                    Properties.Settings.Default.WatchIcon = TxtWatchIcon.Text;
                    Properties.Settings.Default.PlayerColumnFontSize = SliderPlayerColumnFontSize.Value;
                    Properties.Settings.Default.StatisticsColumnFontSize = SliderStatisticsColumnFontSize.Value;
                    Properties.Settings.Default.DetailedStatisticsFontSize = SliderDetailedStatisticsFontSize.Value;
                    Properties.Settings.Default.OutputTextFontSize = SliderOutputTextFontSize.Value;
                    Properties.Settings.Default.AccountWinrateVisibility = ComboBoxAccountWinrateVisibility.SelectedIndex;
                    Properties.Settings.Default.WeightedWinrateVisibility = ComboBoxWeightedWinrateVisibility.SelectedIndex;
                    Properties.Settings.Default.ShipWinrateVisibility = ComboBoxShipWinrateVisibility.SelectedIndex;
                    Properties.Settings.Default.AccountAvgExpVisibility = ComboBoxAccountAvgExpVisibility.SelectedIndex;
                    Properties.Settings.Default.ShipAvgExpVisibility = ComboBoxShipAvgExpVisibility.SelectedIndex;
                    Properties.Settings.Default.ShipAvgDmgVisibility = ComboBoxShipAvgDmgVisibility.SelectedIndex;
                    Properties.Settings.Default.TagVisibility = ComboBoxTagVisibility.SelectedIndex;
                    Properties.Settings.Default.OutputTextShortMode = ChkBoxShortMode.IsChecked ?? false;
                    Properties.Settings.Default.OutputTextExcludeSelf = ChkBoxExcludeYourself.IsChecked ?? false;
                    Properties.Settings.Default.OutputTextAutoCopy = ChkBoxAutoCopy.IsChecked ?? false;
                    Properties.Settings.Default.APITypeSelectionDefault = ComboBoxAPITypeDefault.SelectedValue.ToString();
                    Properties.Settings.Default.APITypeSelectionRU = ComboBoxAPITypeRU.SelectedValue.ToString();
                    Properties.Settings.Default.APITypeSelectionCN = ComboBoxAPITypeCN.SelectedValue.ToString();
                    Properties.Settings.Default.YuyukoAPIPushEnabled = ChkBoxEnableYuyukoAPIPush.IsChecked ?? false;
                    Properties.Settings.Default.DebugMode = ChkBoxEnableDebugMode.IsChecked ?? false;
                    Properties.Settings.Default.TomatoWinrateThreshold = TomatoWinrateThreshold;
                    Properties.Settings.Default.UnicumWinrateThreshold = UnicumWinrateThreshold;
                    Properties.Settings.Default.TomatoBattleCountThreshold = TomatoBattleCountThreshold;
                    Properties.Settings.Default.UnicumBattleCountThreshold = UnicumBattleCountThreshold;
                    Properties.Settings.Default.WeightedWinrateAccountSoloWeightMultiplier = WeightedWinrateAccountSoloWeightMultiplier;
                    Properties.Settings.Default.WeightedWinrateAccountDiv2WeightMultiplier = WeightedWinrateAccountDiv2WeightMultiplier;
                    Properties.Settings.Default.WeightedWinrateAccountDiv3WeightMultiplier = WeightedWinrateAccountDiv3WeightMultiplier;
                    Properties.Settings.Default.WeightedWinrateShipMaxWeight = WeightedWinrateShipMaxWeight;
                    Properties.Settings.Default.WeightedWinrateShipBattlesAtMaxWeight = WeightedWinrateShipBattlesAtMaxWeight;
                    Properties.Settings.Default.OutputTextDelimiter = TxtDelimiter.Text;
                    Properties.Settings.Default.Server = ComboBoxServer.SelectedValue.ToString();
                    Properties.Settings.Default.CheckForUpdatesOnStartup = ChkBoxCheckForUpdatesOnStartup.IsChecked ?? false;
                    Properties.Settings.Default.OutputTextUnlock = ChkBoxTextOutputUnlocked.IsChecked ?? false;
                    Properties.Settings.Default.Save();
                    if (Properties.Settings.Default.DebugMode)
                    {
                        LogUtils.SetLogLevel(log4net.Core.Level.Debug);
                    }
                    else
                    {
                        LogUtils.SetLogLevel(log4net.Core.Level.Info);
                    }
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                LogUtils.WriteError("", ex);
                return -1;
            }
        }

        private void RefreshWatchList()
        {
            JObject JObjectWatchList = WatchListUtils.ReadWatchList(@".\WatchList.json");

            List<Player> WatchListPositive = new();
            List<Player> WatchListNegative = new();
            List<Player> WatchListCheater = new();
            foreach (JProperty JPropertyServer in JObjectWatchList.Children())
            {
                if (JPropertyServer.Value.HasValues)
                {
                    foreach (JProperty JPropertyPlayer in JPropertyServer.Value.Children())
                    {
                        WatchStatus status = WatchStatusExt.GetStatusByName(JPropertyPlayer.Value["status"]!.Value<string>()!);
                        if (status == WatchStatus.POSITIVE)
                        {
                            WatchListPositive.Add(new Player(JPropertyPlayer.Value["name"]!.Value<string>()!, JPropertyPlayer.Name, ServerExt.GetServerByName(JPropertyServer.Name), WatchStatusExt.GetStatusByName(JPropertyPlayer.Value["status"]!.Value<string>()!)));
                        }
                        else if (status == WatchStatus.NEGATIVE)
                        {
                            WatchListNegative.Add(new Player(JPropertyPlayer.Value["name"]!.Value<string>()!, JPropertyPlayer.Name, ServerExt.GetServerByName(JPropertyServer.Name), WatchStatusExt.GetStatusByName(JPropertyPlayer.Value["status"]!.Value<string>()!)));
                        }
                        else if (status == WatchStatus.CHEATER)
                        {
                            WatchListCheater.Add(new Player(JPropertyPlayer.Value["name"]!.Value<string>()!, JPropertyPlayer.Name, ServerExt.GetServerByName(JPropertyServer.Name), WatchStatusExt.GetStatusByName(JPropertyPlayer.Value["status"]!.Value<string>()!)));
                        }
                    }
                }
            }
            DataGridWatchListPositive.ItemsSource = WatchListPositive;
            DataGridWatchListNegative.ItemsSource = WatchListNegative;
            DataGridWatchListCheater.ItemsSource = WatchListCheater;
        }

        private void AutoDetectGamePath()
        {
            ComboBoxGamePath.Items.Clear();
            RegistryKey hkcuUninstall = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall")!;
            RegistryKey hklmUninstall = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall")!;

            foreach (string subKeyName in hkcuUninstall.GetSubKeyNames())
            {
                RegistryKey key = hkcuUninstall.OpenSubKey(subKeyName)!;
                string publisher = (key.GetValue("Publisher") ?? "").ToString()!;
                if (publisher == "Wargaming.net" || publisher == "Wargaming Group Limited" || publisher == "360.cn" || publisher == "Lesta Games")
                {
                    string installLocation = (key.GetValue("InstallLocation") ?? "").ToString()!;
                    if (Directory.Exists(installLocation))
                    {
                        string exeName = (publisher == "Lesta Games") ? "Korabli.exe" : "WorldOfWarships.exe";
                        if (File.Exists($@"{installLocation}\{exeName}"))
                        {
                            ComboBoxGamePath.Items.Add(installLocation);
                        }
                    }
                }
            }
            foreach (string subKeyName in hklmUninstall.GetSubKeyNames())
            {
                RegistryKey key = hklmUninstall.OpenSubKey(subKeyName)!;
                string publisher = (key.GetValue("Publisher") ?? "").ToString()!;
                if (publisher == "Wargaming.net" || publisher == "Wargaming Group Limited" || publisher == "360.cn" || publisher == "Lesta Games")
                {
                    string installLocation = (key.GetValue("InstallLocation") ?? "").ToString()!;
                    if (Directory.Exists(installLocation))
                    {
                        string exeName = (publisher == "Lesta Games") ? "Korabli.exe" : "WorldOfWarships.exe";
                        if (File.Exists($@"{installLocation}\{exeName}"))
                        {
                            ComboBoxGamePath.Items.Add(installLocation);
                        }
                    }
                }
            }
        }

        public ConfigWindow()
        {
            InitializeComponent();
            LoadSettings();
            AutoDetectGamePath();
            RefreshWatchList();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ComboBoxGamePath.Text = dialog.SelectedPath;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            int result = SaveSettings();
            if (result == 0)
            {
                this.Close();
            }
            else if (result == -1)
            {
                System.Windows.MessageBox.Show(TryFindResource("MsgBoxInputError") as string, TryFindResource("MsgBoxError") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

        }

        private void BtnDefault_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();
            LoadSettings();
        }

        private void ConfigWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LoadSettings();
        }

        private void ChkBoxTextOutputUnlocked_Checked(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show(TryFindResource("MsgBoxOutputTextboxUnlockConfirmation") as string, TryFindResource("MsgBoxConfirmation") as string, MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
            {
                ChkBoxTextOutputUnlocked.IsChecked = false;
            };
        }

        private void ChkBoxTextOutputUnlocked_Unchecked(object sender, RoutedEventArgs e)
        {
            ChkBoxAutoCopy.IsChecked = false;
        }

        private void BtnInsertGeneralStatistics_Click(object sender, RoutedEventArgs e)
        {
            int tempIndex = TxtOutputTextTemplateGeneralStatistics.CaretIndex;
            TxtOutputTextTemplateGeneralStatistics.Text = TxtOutputTextTemplateGeneralStatistics.Text.Insert(tempIndex, $"{{{ComboBoxTagInsertGeneralStatistics.SelectedValue}}}");
            TxtOutputTextTemplateGeneralStatistics.CaretIndex = tempIndex;
        }

        private void BtnInsertParticularPlayerStatistics_Click(object sender, RoutedEventArgs e)
        {
            int tempIndex = TxtOutputTextTemplateParticularPlayerStatistics.CaretIndex;
            TxtOutputTextTemplateParticularPlayerStatistics.Text = TxtOutputTextTemplateParticularPlayerStatistics.Text.Insert(tempIndex, $"{{{ComboBoxTagInsertParticularPlayerStatistics.SelectedValue}}}");
            TxtOutputTextTemplateParticularPlayerStatistics.CaretIndex = tempIndex;
        }


        private void ContextMenuAddToWatchListPositive_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.POSITIVE;
            WatchListUtils.SaveWatchList(p, @".\WatchList.json");
            RefreshWatchList();
        }

        private void ContextMenuAddToWatchListNegative_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.NEGATIVE;
            WatchListUtils.SaveWatchList(p, @".\WatchList.json");
            RefreshWatchList();
        }

        private void ContextMenuAddToWatchListCheater_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.CHEATER;
            WatchListUtils.SaveWatchList(p, @".\WatchList.json");
            RefreshWatchList();
        }

        private void ContextMenuRemoveFromWatchList_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.NONE;
            WatchListUtils.SaveWatchList(p, @".\WatchList.json");
            RefreshWatchList();
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

        private void PopulateShipNameLanguagePriorityList()
        {
            string priority = Properties.Settings.Default.ShipNameLanguage;
            List<Language> languages = LanguageExt.ParsePriority(priority);
            bool isAuto = priority == "AUTO";

            ListBoxShipNameLanguagePriority.Items.Clear();
            foreach (Language lang in languages)
            {
                if (lang == Models.Language.AUTO)
                    continue;
                ListBoxShipNameLanguagePriority.Items.Add(new ListItem
                {
                    Content = System.Windows.Application.Current.FindResource($"ComboBoxItemLanguageName{LanguageExt.GetNameByLanguage(lang)}"),
                    Value = LanguageExt.GetNameByLanguage(lang),
                });
            }

            BtnShipNameLanguageAuto.IsEnabled = !isAuto;
        }

        private void PersistShipNameLanguagePriority()
        {
            List<Language> priority = GetCurrentPriorityFromListBox();
            Properties.Settings.Default.ShipNameLanguage = LanguageExt.FormatPriority(priority);
        }

        private List<Language> GetCurrentPriorityFromListBox()
        {
            List<Language> languages = new();
            foreach (ListItem item in ListBoxShipNameLanguagePriority.Items)
            {
                languages.Add(LanguageExt.GetLanguageByName(item.Value.ToString()!));
            }
            return languages;
        }

        private void BtnShipNameLanguageUp_Click(object sender, RoutedEventArgs e)
        {
            int idx = ListBoxShipNameLanguagePriority.SelectedIndex;
            if (idx > 0)
            {
                object item = ListBoxShipNameLanguagePriority.Items[idx];
                ListBoxShipNameLanguagePriority.Items.RemoveAt(idx);
                ListBoxShipNameLanguagePriority.Items.Insert(idx - 1, item);
                ListBoxShipNameLanguagePriority.SelectedIndex = idx - 1;
                PersistShipNameLanguagePriority();
            }
        }

        private void BtnShipNameLanguageDown_Click(object sender, RoutedEventArgs e)
        {
            int idx = ListBoxShipNameLanguagePriority.SelectedIndex;
            if (idx >= 0 && idx < ListBoxShipNameLanguagePriority.Items.Count - 1)
            {
                object item = ListBoxShipNameLanguagePriority.Items[idx];
                ListBoxShipNameLanguagePriority.Items.RemoveAt(idx);
                ListBoxShipNameLanguagePriority.Items.Insert(idx + 1, item);
                ListBoxShipNameLanguagePriority.SelectedIndex = idx + 1;
                PersistShipNameLanguagePriority();
            }
        }

        private void BtnResetTomatoIcon_Click(object sender, RoutedEventArgs e)
        {
            TxtTomatoIcon.Text = "🍅";
            Properties.Settings.Default.TomatoIcon = TxtTomatoIcon.Text;
        }

        private void BtnResetUnicumIcon_Click(object sender, RoutedEventArgs e)
        {
            TxtUnicumIcon.Text = "🏅";
            Properties.Settings.Default.UnicumIcon = TxtUnicumIcon.Text;
        }

        private void BtnResetHiddenIcon_Click(object sender, RoutedEventArgs e)
        {
            TxtHiddenIcon.Text = "🚫";
            Properties.Settings.Default.HiddenIcon = TxtHiddenIcon.Text;
        }

        private void BtnResetWatchIcon_Click(object sender, RoutedEventArgs e)
        {
            TxtWatchIcon.Text = "⚠️";
            Properties.Settings.Default.WatchIcon = TxtWatchIcon.Text;
        }

        private void BtnShipNameLanguageAuto_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ShipNameLanguage = "AUTO";
            PopulateShipNameLanguagePriorityList();
        }

        private async void BtnCheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            BtnCheckForUpdates.IsEnabled = false;
            if (await SoftwareUpdateUtils.CheckForUpdates() == false)
            {
                System.Windows.MessageBox.Show(System.Windows.Application.Current.FindResource("MsgBoxUpdatesNotFound") as string, System.Windows.Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            BtnCheckForUpdates.IsEnabled = true;
            LabelShipListVersionDateStr.Content = $"{ShipInfoUtils.GetShipInfoVersion(Server.EU)} ({ShipInfoUtils.GetShipInfoDate(Server.EU)})";
        }
    }
}
