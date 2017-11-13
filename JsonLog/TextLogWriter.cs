namespace JsonLog
{
    using System;
    using System.Globalization;
    using System.IO;

    public class TextLogWriter : ILogWriter
    {
        private string traceLogFileName;
        private string traceLogFilePath;

        private volatile object logFileLock = new object();

        protected virtual string LogFileExtension => "log";

        public string LogFilePath => this.traceLogFilePath;

        public void LogInternal(LogEntry logEntry)
        {
            string line = this.FormatMessage(logEntry);

            lock (this.logFileLock)
            {
                File.AppendAllLines(this.traceLogFilePath, new[] { line });
            }
        }

        public bool IsInitialized { get; private set; }

        public bool IsFaulted { get; set; }

        public void Shutdown()
        {
        }

        public void Initialize(string logDir)
        {
            this.Initialize(logDir, "trace");
        }

        public void Initialize(string logDir, string filePrefix)
        {
            DateTime logTime = DateTime.Now;

            this.traceLogFileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}.{2}",
                filePrefix,
                logTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture),
                this.LogFileExtension);

            this.traceLogFilePath = Path.Combine(logDir, this.traceLogFileName);

            this.IsInitialized = true;
        }

        protected virtual string FormatMessage(LogEntry entry)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "[{0,12}] [{3,3}] [{1,-5}] {2}",
                entry.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.CurrentCulture),
                entry.Level,
                entry.Message,
                entry.ThreadId);
        }
    }
}