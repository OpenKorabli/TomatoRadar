using System;

namespace TomatoRadar.Models
{
    public enum WatchStatus
    {
        NONE,
        POSITIVE,
        NEGATIVE,
        CHEATER
    }

    public static class WatchStatusExt
    {
        public static string GetNameByStatus(WatchStatus status)
        {
            return status switch
            {
                WatchStatus.NONE => "None",
                WatchStatus.POSITIVE => "Positive",
                WatchStatus.NEGATIVE => "Negative",
                WatchStatus.CHEATER => "Cheater",
                _ => "None",
            };
        }

        public static WatchStatus GetStatusByName(string name)
        {
            return name switch
            {
                "None" => WatchStatus.NONE,
                "Positive" => WatchStatus.POSITIVE,
                "Negative" => WatchStatus.NEGATIVE,
                "Cheater" => WatchStatus.CHEATER,
                _ => WatchStatus.NONE,
            };
        }
    }
}
