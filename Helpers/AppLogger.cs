using Serilog;
using System;
using System.IO;

namespace Netplwiz.Helpers
{
    public static class AppLogger
    {
        private static ILogger? _logger;

        public static ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    Initialize();
                }
                return _logger!;
            }
        }

        public static void Initialize()
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Netplwiz",
                "logs",
                "netplwiz-.log");

            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            _logger.Information("Logger initialized. Log path: {LogPath}", logPath);
        }
    }
}
