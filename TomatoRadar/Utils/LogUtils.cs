using System;

namespace TomatoRadar.Utils
{
    internal class LogUtils
    {
        public static log4net.ILog log_info = log4net.LogManager.GetLogger("loginfo");

        public static void SetLogLevel(log4net.Core.Level level)
        {
            ((log4net.Repository.Hierarchy.Logger)log_info.Logger).Level = level;
        }
        public static void WriteDebug(string debug)
        {
            if (log_info.IsDebugEnabled)
            {
                log_info.Debug(debug);
            }
        }
        public static void WriteInfo(string info)
        {
            if (log_info.IsInfoEnabled)
            {
                log_info.Info(info);
            }
        }
        public static void WriteError(string error, Exception ex)
        {
            if (log_info.IsErrorEnabled)
            {
                log_info.Error(error, ex);
            }
        }
    }
}
