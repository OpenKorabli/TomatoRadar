using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TomatoRadar.Models
{
    internal class Player : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string Name { get; set; }
        public string ID { get; set; }
        public Server Server { get; set; }
        public string Relation { get; set; }
        public string ShipID { get; set; }
        public string ShipName { get; set; }
        public string ShipType { get; set; }
        public int ShipTier { get; set; }
        public string ClanID { get; set; }
        public string ClanTag { get; set; }
        public bool IsHidden { get; set; }
        public double Wins { get; set; }
        public double Wins_Solo { get; set; }
        public double Wins_Div2 { get; set; }
        public double Wins_Div3 { get; set; }
        public double Battles { get; set; }
        public double Battles_Solo { get; set; }
        public double Battles_Div2 { get; set; }
        public double Battles_Div3 { get; set; }
        public double TotalExp { get; set; }
        public double TotalExp_Solo { get; set; }
        public double TotalExp_Div2 { get; set; }
        public double TotalExp_Div3 { get; set; }
        public double AvgExpPerBattle { get; set; }
        public double AvgExpPerBattle_Solo { get; set; }
        public double AvgExpPerBattle_Div2 { get; set; }
        public double AvgExpPerBattle_Div3 { get; set; }
        public double AccountWinrate { get; set; }
        public double AccountWinrate_Solo { get; set; }
        public double AccountWinrate_Div2 { get; set; }
        public double AccountWinrate_Div3 { get; set; }
        public double ShipWins { get; set; }
        public double ShipWins_Solo { get; set; }
        public double ShipWins_Div2 { get; set; }
        public double ShipWins_Div3 { get; set; }
        public double ShipBattles { get; set; }
        public double ShipBattles_Solo { get; set; }
        public double ShipBattles_Div2 { get; set; }
        public double ShipBattles_Div3 { get; set; }
        public double ShipTotalDmg { get; set; }
        public double ShipTotalDmg_Solo { get; set; }
        public double ShipTotalDmg_Div2 { get; set; }
        public double ShipTotalDmg_Div3 { get; set; }
        public double ShipAvgDmgPerBattle { get; set; }
        public double ShipAvgDmgPerBattle_Solo { get; set; }
        public double ShipAvgDmgPerBattle_Div2 { get; set; }
        public double ShipAvgDmgPerBattle_Div3 { get; set; }
        public double ShipTotalExp { get; set; }
        public double ShipTotalExp_Solo { get; set; }
        public double ShipTotalExp_Div2 { get; set; }
        public double ShipTotalExp_Div3 { get; set; }
        public double ShipAvgExpPerBattle { get; set; }
        public double ShipAvgExpPerBattle_Solo { get; set; }
        public double ShipAvgExpPerBattle_Div2 { get; set; }
        public double ShipAvgExpPerBattle_Div3 { get; set; }
        public double ShipWinrate { get; set; }
        public double ShipWinrate_Solo { get; set; }
        public double ShipWinrate_Div2 { get; set; }
        public double ShipWinrate_Div3 { get; set; }
        public double WeightedWinrate { get; set; }
        public double Karma { get; set; }

        private WatchStatus watchStatus;
        public WatchStatus WatchStatus { 
            get 
            {
                return watchStatus;
            } 
            set
            {
                watchStatus = value;
                NotifyPropertyChanged();
            }
        }
        public double PlotXPosition { get; set; }

        public Player(string Name, Server Server, string Relation, string ShipID)
        {
            this.Name = Name;
            this.Server = Server;
            ID = "-1";
            this.Relation = Relation;
            this.ShipID = ShipID;
            ShipName = "";
            ShipType = "";
            ShipTier = 0;
            ClanID = "-1";
            ClanTag = "";
            IsHidden = false;
            Wins = -1;
            Wins_Solo = -1;
            Wins_Div2 = -1;
            Wins_Div3 = -1;
            Battles = -1;
            Battles_Solo = -1;
            Battles_Div2 = -1;
            Battles_Div3 = -1;
            TotalExp = -1;
            TotalExp_Solo = -1;
            TotalExp_Div2 = -1;
            TotalExp_Div3 = -1;
            AvgExpPerBattle = -1;
            AvgExpPerBattle_Solo = -1;
            AvgExpPerBattle_Div2 = -1;
            AvgExpPerBattle_Div3 = -1;
            AccountWinrate = -1;
            AccountWinrate_Solo = -1;
            AccountWinrate_Div2 = -1;
            AccountWinrate_Div3 = -1;
            ShipWins = -1;
            ShipWins_Solo = -1;
            ShipWins_Div2 = -1;
            ShipWins_Div3 = -1;
            ShipBattles = -1;
            ShipBattles_Solo = -1;
            ShipBattles_Div2 = -1;
            ShipBattles_Div3 = -1;
            ShipTotalDmg = -1;
            ShipTotalDmg_Solo = -1;
            ShipTotalDmg_Div2 = -1;
            ShipTotalDmg_Div3 = -1;
            ShipAvgDmgPerBattle = -1;
            ShipAvgDmgPerBattle_Solo = -1;
            ShipAvgDmgPerBattle_Div2 = -1;
            ShipAvgDmgPerBattle_Div3 = -1;
            ShipTotalExp = -1;
            ShipTotalExp_Solo = -1;
            ShipTotalExp_Div2 = -1;
            ShipTotalExp_Div3 = -1;
            ShipAvgExpPerBattle = -1;
            ShipAvgExpPerBattle_Solo = -1;
            ShipAvgExpPerBattle_Div2 = -1;
            ShipAvgExpPerBattle_Div3 = -1;
            ShipWinrate = -1;
            ShipWinrate_Solo = -1;
            ShipWinrate_Div2 = -1;
            ShipWinrate_Div3 = -1;
            WeightedWinrate = -1;
            Karma = -1;
            WatchStatus = WatchStatus.NONE;
            PlotXPosition = -1;
        }

        public Player(string Name, string ID, Server Server, WatchStatus WatchStatus)
        {
            this.Name = Name;
            this.Server = Server;
            this.ID = ID;
            Relation = "-1";
            ShipID = "-1";
            ShipName = "";
            ShipType = "";
            ShipTier = 0;
            ClanID = "-1";
            ClanTag = "";
            IsHidden = false;
            Wins = -1;
            Wins_Solo = -1;
            Wins_Div2 = -1;
            Wins_Div3 = -1;
            Battles = -1;
            Battles_Solo = -1;
            Battles_Div2 = -1;
            Battles_Div3 = -1;
            TotalExp = -1;
            TotalExp_Solo = -1;
            TotalExp_Div2 = -1;
            TotalExp_Div3 = -1;
            AvgExpPerBattle = -1;
            AvgExpPerBattle_Solo = -1;
            AvgExpPerBattle_Div2 = -1;
            AvgExpPerBattle_Div3 = -1;
            AccountWinrate = -1;
            AccountWinrate_Solo = -1;
            AccountWinrate_Div2 = -1;
            AccountWinrate_Div3 = -1;
            ShipWins = -1;
            ShipWins_Solo = -1;
            ShipWins_Div2 = -1;
            ShipWins_Div3 = -1;
            ShipBattles = -1;
            ShipBattles_Solo = -1;
            ShipBattles_Div2 = -1;
            ShipBattles_Div3 = -1;
            ShipTotalDmg = -1;
            ShipTotalDmg_Solo = -1;
            ShipTotalDmg_Div2 = -1;
            ShipTotalDmg_Div3 = -1;
            ShipAvgDmgPerBattle = -1;
            ShipAvgDmgPerBattle_Solo = -1;
            ShipAvgDmgPerBattle_Div2 = -1;
            ShipAvgDmgPerBattle_Div3 = -1;
            ShipTotalExp = -1;
            ShipTotalExp_Solo = -1;
            ShipTotalExp_Div2 = -1;
            ShipTotalExp_Div3 = -1;
            ShipAvgExpPerBattle = -1;
            ShipAvgExpPerBattle_Solo = -1;
            ShipAvgExpPerBattle_Div2 = -1;
            ShipAvgExpPerBattle_Div3 = -1;
            ShipWinrate = -1;
            ShipWinrate_Solo = -1;
            ShipWinrate_Div2 = -1;
            ShipWinrate_Div3 = -1;
            WeightedWinrate = -1;
            Karma = -1;
            this.WatchStatus = WatchStatus;
            PlotXPosition = -1;
        }

        public string GetPlayerInfoStrForYuyukoApiPush()
        {
            return $@"{{""server"": ""{ServerExt.GetNameByServer(Server).ToLower()}"", ""accountId"": {ID}, ""userName"": ""{Name}"", ""shipId"": {ShipID}, ""hidden"": {IsHidden.ToString().ToLower()}, ""clanId"": {ClanID}, ""tag"": ""{ClanTag}"", ""relation"": {Relation}}}";
        }
        override public string ToString()
        {
            return $"Name={Name}, ID={ID}, Server={ServerExt.GetNameByServer(Server)}, Relation={Relation}, ShipID={ShipID}, ShipName={ShipName}, ShipType={ShipType}, ShipTier={ShipTier}, ClanID={ClanID}, ClanTag={ClanTag}, IsHidden={IsHidden}, Wins={Wins}, Wins_Solo={Wins_Solo}, Wins_Div2={Wins_Div2}, Wins_Div3={Wins_Div3}, Battles={Battles}, Battles_Solo={Battles_Solo}, Battles_Div2={Battles_Div2}, Battles_Div3={Battles_Div3}, TotalExp={TotalExp}, TotalExp_Solo={TotalExp_Solo}, TotalExp_Div2={TotalExp_Div2}, TotalExp_Div3={TotalExp_Div3}, AvgExpPerBattle={AvgExpPerBattle}, AvgExpPerBattle_Solo={AvgExpPerBattle_Solo}, AvgExpPerBattle_Div2={AvgExpPerBattle_Div2}, AvgExpPerBattle_Div3={AvgExpPerBattle_Div3}, AccountWinrate={AccountWinrate}, AccountWinrate_Solo={AccountWinrate_Solo}, AccountWinrate_Div2={AccountWinrate_Div2}, AccountWinrate_Div3={AccountWinrate_Div3}, ShipWins={ShipWins}, ShipWins_Solo={ShipWins_Solo}, ShipWins_Div2={ShipWins_Div2}, ShipWins_Div3={ShipWins_Div3}, ShipBattles={ShipBattles}, ShipBattles_Solo={ShipBattles_Solo}, ShipBattles_Div2={ShipBattles_Div2}, ShipBattles_Div3={ShipBattles_Div3}, ShipTotalDmg={ShipTotalDmg}, ShipTotalDmg_Solo={ShipTotalDmg_Solo}, ShipTotalDmg_Div2={ShipTotalDmg_Div2}, ShipTotalDmg_Div3={ShipTotalDmg_Div3}, ShipAvgDmgPerBattle={ShipAvgDmgPerBattle}, ShipAvgDmgPerBattle_Solo={ShipAvgDmgPerBattle_Solo}, ShipAvgDmgPerBattle_Div2={ShipAvgDmgPerBattle_Div2}, ShipAvgDmgPerBattle_Div3={ShipAvgDmgPerBattle_Div3}, ShipTotalExp={ShipTotalExp}, ShipTotalExp_Solo={ShipTotalExp_Solo}, ShipTotalExp_Div2={ShipTotalExp_Div2}, ShipTotalExp_Div3={ShipTotalExp_Div3}, ShipAvgExpPerBattle={ShipAvgExpPerBattle}, ShipAvgExpPerBattle_Solo={ShipAvgExpPerBattle_Solo}, ShipAvgExpPerBattle_Div2={ShipAvgExpPerBattle_Div2}, ShipAvgExpPerBattle_Div3={ShipAvgExpPerBattle_Div3}, ShipWinrate={ShipWinrate}, ShipWinrate_Solo={ShipWinrate_Solo}, ShipWinrate_Div2={ShipWinrate_Div2}, ShipWinrate_Div3={ShipWinrate_Div3}, WeightedWinrate={WeightedWinrate}, Karma={Karma}, WatchStatus={WatchStatusExt.GetNameByStatus(WatchStatus)}";
        }
    }
}
