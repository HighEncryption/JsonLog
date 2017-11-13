namespace JsonLog
{
    public interface ILogWriter
    {
        void LogInternal(LogEntry logEntry);

        bool IsInitialized { get; }

        bool IsFaulted { get; set; }

        void Shutdown();
    }

}