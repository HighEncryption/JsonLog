namespace AzureLogger
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using JsonLog;

    using Microsoft.WindowsAzure.Storage.Table;

    public class AzureLogWriter : ILogWriter
    {
        private volatile object logEntryListLock = new object();

        private CloudTable table;
        private string partitionKey;
        private long lastRowKey;

        private string source;
        private string sourceInstance;

#if false
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private List<LogEntry> logEntryList = new List<LogEntry>();
        private Task uploadTask;


        private async void UploadToAzure()
        {
            while (!this.cancellationTokenSource.IsCancellationRequested)
            {
                await this.ProcessLogEntries();

                await Task.Delay(1000);
            }

            await this.ProcessLogEntries();
        }

        private async Task ProcessLogEntries()
        {
            List<LogEntry> currentLogEntries = null;

            lock (this.logEntryListLock)
            {
                if (this.logEntryList.Any())
                {
                    currentLogEntries = this.logEntryList;
                    this.logEntryList = new List<LogEntry>();
                }
            }

            if (currentLogEntries != null)
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (LogEntry logEntry in currentLogEntries)
                {
                    //long rowKey = DateTime.UtcNow.ToFileTime();
                    //if (rowKey <= this.lastRowKey)
                    //{
                    //    rowKey = this.lastRowKey + 1;
                    //}

                    LogEntryEntity entity = LogEntryEntity.FromLogEntry(logEntry);
                    entity.PartitionKey = this.partitionKey;
                    //entity.RowKey = rowKey.ToString();
                    entity.RowKey = Guid.NewGuid().ToString("N");
                    entity.Timestamp = DateTime.UtcNow;
                    entity.Role = this.source;
                    entity.RoleInstance = this.sourceInstance;
                    entity.Pid = Process.GetCurrentProcess().Id;

                    //batchOperation.Add(TableOperation.Insert(entity));
                    batchOperation.Insert(entity);
                    //await this.table.ExecuteAsync(TableOperation.Insert(entity));

                    //this.lastRowKey = rowKey;
                }

                await this.table.ExecuteBatchAsync(batchOperation);
            }
        }

        public async Task LogInternalAsync(LogEntry logEntry)
        {
            lock (this.logEntryListLock)
            {
                this.logEntryList.Add(logEntry);
            }
        }

#endif

        public void LogInternal(LogEntry logEntry)
        {
            long rowKey;
            lock (this.logEntryListLock)
            {
                rowKey = DateTime.UtcNow.ToFileTime();
                if (rowKey <= this.lastRowKey)
                {
                    rowKey = this.lastRowKey + 1;
                }

                this.lastRowKey = rowKey;
            }

            LogEntryEntity entity = LogEntryEntity.FromLogEntry(logEntry);
            entity.PartitionKey = this.partitionKey;
            entity.RowKey = rowKey.ToString();
            entity.Timestamp = DateTime.UtcNow;
            entity.Role = this.source;
            entity.RoleInstance = this.sourceInstance;
            entity.Pid = Process.GetCurrentProcess().Id;

            this.table.Execute(TableOperation.Insert(entity));
            //await this.table.ExecuteAsync(TableOperation.Insert(entity));
        }

        //public async Task LogInternalAsync(LogEntry logEntry)
        //{
        //    long rowKey;
        //    lock (this.logEntryListLock)
        //    {
        //        rowKey = DateTime.UtcNow.ToFileTime();
        //        if (rowKey <= this.lastRowKey)
        //        {
        //            rowKey = this.lastRowKey + 1;
        //        }

        //        this.lastRowKey = rowKey;
        //    }

        //    LogEntryEntity entity = LogEntryEntity.FromLogEntry(logEntry);
        //    entity.PartitionKey = this.partitionKey;
        //    entity.RowKey = rowKey.ToString();
        //    entity.Timestamp = DateTime.UtcNow;
        //    entity.Role = this.source;
        //    entity.RoleInstance = this.sourceInstance;
        //    entity.Pid = Process.GetCurrentProcess().Id;

        //    await this.table.ExecuteAsync(TableOperation.Insert(entity));
        //}

        public bool IsInitialized { get; private set; }

        public bool IsFaulted { get; set; }

        public void Shutdown()
        {
#if false
            this.cancellationTokenSource.Cancel();

            this.uploadTask.Wait(5000);
#endif
        }

        public void Initialize(CloudTable cloudTable, string logSource, string logSourceInstance)
        {
            if (cloudTable == null)
            {
                throw new ArgumentNullException(nameof(cloudTable));
            }

            if (string.IsNullOrWhiteSpace(logSource))
            {
                throw new ArgumentNullException(nameof(logSource));
            }

            if (string.IsNullOrWhiteSpace(logSourceInstance))
            {
                throw new ArgumentNullException(nameof(logSourceInstance));
            }

            // Partition key is the timestamp of when the logger was initialize (aka session ID)
            this.partitionKey = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            this.source = logSource;
            this.sourceInstance = logSourceInstance;

            this.table = cloudTable;

#if false
            this.uploadTask = new Task(this.UploadToAzure);
            this.uploadTask.Start();
#endif

            this.IsInitialized = true;
        }
    }

    public class LogEntryEntity : TableEntity
    {
        [IgnoreProperty]
        public DateTime LogTimestamp { get; set; }

        public string Role { get; set; }

        public string RoleInstance { get; set; }

        public int Pid { get; set; }

        public int Tid { get; set; }

        public string LevelName { get; set; }

        public int Level { get; set; }

        public string Message { get; set; }

        internal static LogEntryEntity FromLogEntry(LogEntry logEntry)
        {
            LogEntryEntity entity = new LogEntryEntity
            {
                LogTimestamp = logEntry.Timestamp,
                LevelName = logEntry.Level,
                Tid = logEntry.ThreadId,
                Message = logEntry.Message
            };

            switch (logEntry.Level)
            {
                case "ERROR":
                    entity.Level = 0;
                    break;
                case "WARN":
                    entity.Level = 1;
                    break;
                case "INFO":
                    entity.Level = 2;
                    break;
                case "VERB":
                    entity.Level = 3;
                    break;
                case "DEBUG":
                    entity.Level = 4;
                    break;
                default:
                    entity.Level = -1;
                    break;
            }

            return entity;
        }
    }
}
