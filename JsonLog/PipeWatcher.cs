namespace JsonLog
{
    using System;
    using System.IO.Pipes;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;

    public class PipeWatcher : IDisposable
    {
        private readonly CancellationTokenSource cancellation;
        private NamedPipeClientStream pipeStream;

        public event EventHandler<LogMessageEventArgs> OnLog;

        public PipeWatcher(string pipeName)
        {
            this.pipeStream = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut, 
                PipeOptions.None,
                TokenImpersonationLevel.Impersonation);

            this.cancellation = new CancellationTokenSource();
        }

        public void Start()
        {
            this.pipeStream.Connect();
            this.pipeStream.ReadMode = PipeTransmissionMode.Message;

            Task.Run(() =>
                {
                    while (!this.cancellation.IsCancellationRequested)
                    {
                        byte[] buffer = new byte[2048];
                        this.pipeStream.Read(buffer, 0, 2048);

                        if (this.pipeStream.IsConnected == false)
                        {
                            break;
                        }

                        if (this.pipeStream.IsMessageComplete)
                        {
                            LogMessageEventArgs args = new LogMessageEventArgs();
                            try
                            {
                                string jsonMessage = System.Text.Encoding.Unicode.GetString(buffer);
                                args.Entry = LogEntry.Parse(jsonMessage);
                                args.IsValid = true;
                            }
                            catch (Exception exception)
                            {
                                args.Entry = null;
                                args.IsValid = false;
                                args.ParseError = exception.Message;
                            }

                            this.OnLog?.Invoke(this, args);
                        }
                    }
                },
                this.cancellation.Token);
        }

        public void Dispose()
        {
            if (this.pipeStream != null)
            {
                this.pipeStream.Close();
                this.pipeStream = null;
            }
        }
    }
}