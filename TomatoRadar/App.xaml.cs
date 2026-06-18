using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using RestoreWindowPlace;

namespace TomatoRadar
{
    public partial class App : Application
    {
        public static readonly string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TomatoRadar");
        public static readonly string ShipInfoDirectory = Path.Combine(DataDirectory, "ShipInfo");

        public static ProgressBar? DownloadProgressBar { get; set; }

        public WindowPlace WindowPlace { get; }

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { File.AppendAllText(Path.Combine(DataDirectory, "Log", "Crash.txt"), $"{DateTimeOffset.Now}: {(e.ExceptionObject as Exception)?.ToString()}\n"); } catch { }
            };
            this.DispatcherUnhandledException += (s, e) =>
            {
                try { File.AppendAllText(Path.Combine(DataDirectory, "Log", "Crash.txt"), $"{DateTimeOffset.Now}: {e.Exception}\n"); } catch { }
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try { File.AppendAllText(Path.Combine(DataDirectory, "Log", "Crash.txt"), $"{DateTimeOffset.Now}: {e.Exception}\n"); } catch { }
            };

            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(Path.Combine(DataDirectory, "Log"));
            Directory.CreateDirectory(ShipInfoDirectory);

            SeedShipInfo("ships_wg.json");
            SeedShipInfo("ships_lesta.json");

            this.WindowPlace = new WindowPlace(Path.Combine(DataDirectory, "placement.config"));

            var fileInfo = new FileInfo("log4net.config");
            log4net.Config.XmlConfigurator.Configure(fileInfo);
            var repo = log4net.LogManager.GetRepository();
            var appender = repo.GetAppenders().FirstOrDefault(a => a.Name == "InfoAppender") as log4net.Appender.RollingFileAppender;
            if (appender != null)
            {
                appender.File = Path.Combine(DataDirectory, "Log", "Log.txt");
                appender.ActivateOptions();
            }
        }

        private static void SeedShipInfo(string fileName)
        {
            string targetPath = Path.Combine(ShipInfoDirectory, fileName);
            if (File.Exists(targetPath))
                return;

            string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Json", fileName);
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, targetPath);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            this.WindowPlace.Save();
        }
    }
}
