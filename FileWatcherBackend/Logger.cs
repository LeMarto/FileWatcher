using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FileWatcherBackend
{
    public static class Logger
    {
        #region Event Log Constants
        public const string LOG_SOURCE = "FileWatcherService";
        public const string LOG_NAME = "Application";
        #endregion
        private static EventLog _eventLog;
        public static void Log(string message)
        {
            if (_eventLog == null)
                InitEventLog();

            Log(message, EventLogEntryType.Information);
        }
        public static void Log(string message, EventLogEntryType type)
        {
            if (_eventLog == null)
                InitEventLog();

            string entryTypeText = "";

            if (type == EventLogEntryType.Information)
                entryTypeText = "Information";
            else if (type == EventLogEntryType.Warning)
                entryTypeText = "Warning";
            else if (type == EventLogEntryType.Error)
                entryTypeText = "Error";
            else if (type == EventLogEntryType.FailureAudit)
                entryTypeText = "FailureAudit";
            else if (type == EventLogEntryType.SuccessAudit)
                entryTypeText = "SuccessAudit";

            Console.WriteLine(entryTypeText + ": " + message);
            _eventLog.WriteEntry(message, type);
        }
        private static void InitEventLog()
        {
            if (!EventLog.SourceExists(LOG_SOURCE))
                EventLog.CreateEventSource(LOG_SOURCE, LOG_NAME);

            _eventLog = new EventLog();
            _eventLog.Source = LOG_SOURCE;
            _eventLog.Log = LOG_NAME;
        }
    }

}
