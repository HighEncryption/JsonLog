namespace JsonLog
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class LogMessageEventArgs : EventArgs
    {
        public LogEntry Entry { get; set; }

        public bool IsValid { get; set; }

        public string ParseError { get; set; }
    }

    public class LogWatcher : IDisposable
    {
        private readonly string path;
        private readonly string filename;

        private FileSystemWatcher watcher;
        private AutoResetEvent fileChangedEvent = new AutoResetEvent(false);
        private bool terminate;
        private Task task;
        private long currentPosition;
        private readonly StringBuilder sb = new StringBuilder();


        public LogWatcher(string logFilePath)
        {
            string fullPath = Path.GetFullPath(logFilePath);

            Debug.Assert(File.Exists(fullPath), "file does not exist as " + fullPath);

            this.path = Path.GetDirectoryName(fullPath);
            this.filename = Path.GetFileName(fullPath);

            Debug.Assert(this.path != null, "this.path != null");
        }

        public void Start()
        {
            this.fileChangedEvent.Reset();

            this.watcher = new FileSystemWatcher(this.path, this.filename);
            this.watcher.EnableRaisingEvents = true;
            this.watcher.Changed += (sender, args) =>
            {
                this.fileChangedEvent.Set();
            };

            this.task = Task.Factory.StartNew(this.Watch);
        }

        private void Watch()
        {
            FileStream stream = new FileStream(
                Path.Combine(this.path, this.filename), 
                FileMode.Open,
                FileAccess.Read, 
                FileShare.ReadWrite);

            using (stream)
            {
                if (this.currentPosition > 0)
                {
                    stream.Seek(this.currentPosition, SeekOrigin.Begin);
                }

                while (!this.terminate)
                {
                    int i = stream.ReadByte();
                    if (i == -1)
                    {
                        this.fileChangedEvent.WaitOne();
                        continue;
                    }

                    char ch = (char)i;
                    if (ch == '\n')
                    {
                        // Skip blank lines
                        if (this.sb.Length == 0)
                        {
                            continue;
                        }

                        LogMessageEventArgs args = new LogMessageEventArgs();
                        try
                        {
                            args.Entry = LogEntry.Parse(this.sb);
                            args.IsValid = true;
                        }
                        catch (Exception exception)
                        {
                            args.Entry = null;
                            args.IsValid = false;
                            args.ParseError = exception.Message;
                        }

                        this.OnLog?.Invoke(this, args);

                        this.sb.Clear();
                        continue;
                    }

                    if (ch != '\r')
                    {
                        this.sb.Append(ch);
                    }
                }

                this.currentPosition = stream.Position;
            }
        }

        public void Stop(int timeout = 5000)
        {
            this.watcher.EnableRaisingEvents = false;
            this.watcher.Dispose();
            this.watcher = null;

            this.terminate = true;
            this.fileChangedEvent.Set();

            this.task.Wait(timeout);
        }

        public event EventHandler<LogMessageEventArgs> OnLog;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (this.watcher != null)
                {
                    this.watcher.Dispose();
                    this.watcher = null;
                }

                if (this.fileChangedEvent != null)
                {
                    this.fileChangedEvent.Dispose();
                    this.fileChangedEvent = null;
                }
            }

            // free native resources if there are any.
        }
    }
}