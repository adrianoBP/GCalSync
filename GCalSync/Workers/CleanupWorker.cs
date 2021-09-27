using GCalSync.Helpers;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace GCalSync.Workers
{
    public class CleanupWorker
    {
        public void FullCleanup()
        {
            Directory.Delete(@"F:\Projects\GCalSync\GCalSync\GCalSync\from_acc_token", true);
            Directory.Delete(@"F:\Projects\GCalSync\GCalSync\GCalSync\to_acc_token", true);
        }

        public void ClearFromAccount()
        {
            Directory.Delete(@"F:\Projects\GCalSync\GCalSync\GCalSync\from_acc_token", true);
        }

        public void ClearToAccount()
        {
            Directory.Delete(@"F:\Projects\GCalSync\GCalSync\GCalSync\to_acc_token", true);
        }

        public void ClearLogs()
        {
            if (!Directory.Exists(LoggerHelper.LogFolder))
                return;

            DirectoryInfo logDirectory = new(LoggerHelper.LogFolder);

            var logFiles = logDirectory.GetFiles().ToList();

            foreach(var logFile in logFiles)
            {
                DateTime fileDate = DateTime.ParseExact(logFile.Name.Replace(".log", ""), "yyyyMMdd", CultureInfo.InvariantCulture);

                if ((DateTime.UtcNow - fileDate).TotalDays > 30)
                    logFile.Delete();
            }
        }
    }
}
