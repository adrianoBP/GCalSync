using System;
using System.IO;

namespace GCalSync.Helpers
{
    public class LoggerHelper
    {
        public static readonly string LogFolder = "Logs";

        public static void AddLog(string message, Severity severity, Exception ex = null)
        {
            if (!Directory.Exists(LogFolder))
                Directory.CreateDirectory(LogFolder);

            string filePath = $"{LogFolder}/{DateTime.UtcNow:yyyyMMdd}.log";
            string logMessage = $"{DateTime.UtcNow:HH:mm} [{severity}] - {message}\n";

            File.AppendAllText(filePath, logMessage + $"{(ex != null ? $"\nStackTrace: {ex.StackTrace}" : "")}");
        }

        public enum Severity
        {
            DEBUG,
            INFO,
            WARNING,
            ERROR
        }
    }
}
