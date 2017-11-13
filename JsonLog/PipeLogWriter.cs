namespace JsonLog
{
    using System;
    using System.IO.Pipes;

    public class PipeLogWriter : ILogWriter, IDisposable
    {
        private NamedPipeServerStream pipeStream;
        private bool isInitialized;

        public string PipeName { get; private set; }

        public void LogInternal(LogEntry logEntry)
        {
            string jsonMessage = LogEntry.Serialize(logEntry);
            byte[] messageBytes = System.Text.Encoding.Unicode.GetBytes(jsonMessage);

            try
            {
                this.pipeStream.Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception)
            {
                // Caught an exception writing to the pipe. Close down the pipe server.
                this.pipeStream.Close();
                this.pipeStream = null;

                throw;
            }
        }

        public bool IsInitialized => this.isInitialized;

        public bool IsFaulted { get; set; }

        public void Shutdown()
        {
            this.pipeStream.Close();
            this.pipeStream = null;
        }

        public void StartInitialize()
        {
            this.PipeName = Guid.NewGuid().ToString();

            this.pipeStream = new NamedPipeServerStream(
                this.PipeName, 
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Message);
        }

        public void FinishInitialize()
        {
            this.pipeStream.WaitForConnection();

            this.isInitialized = true;

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