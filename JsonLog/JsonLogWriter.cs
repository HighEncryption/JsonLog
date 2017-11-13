namespace JsonLog
{
    public class JsonLogWriter : TextLogWriter
    {
        protected override string LogFileExtension => "jsonlog";

        protected override string FormatMessage(LogEntry entry)
        {
            return LogEntry.Serialize(entry);
        }
    }
}