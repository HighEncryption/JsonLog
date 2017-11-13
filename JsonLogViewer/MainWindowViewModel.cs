namespace JsonLogViewer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Data;

    using JsonLog;

    using JsonLogViewer.Framework;

    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private PipeWatcher pipeWatcher;
        private LogWatcher logWatcher;

        public ObservableCollection<LogEntry> Entries { get; }

        public MainWindowViewModel()
        {
            this.Entries = new ObservableCollection<LogEntry>();

            string[] cmdArgs = Environment.GetCommandLineArgs();
            Dictionary<string, string> commandLineArgs = new Dictionary<string, string>();

            if (cmdArgs.Length == 2)
            {
                try
                {
                    if (File.Exists(cmdArgs[1]))
                    {
                        commandLineArgs.Add("file", cmdArgs[1]);
                    }
                }
                catch
                {
                    // Suppress failure
                }
            }
            else
            {
                commandLineArgs = CommandLineHelper.ParseCommandLineArgs(cmdArgs);
            }

            string closeOnExitPid;
            if (commandLineArgs.TryGetValue("closeOnExit", out closeOnExitPid))
            {
                int pid = int.Parse(closeOnExitPid);
                Task.Factory.StartNew(() =>
                {
                    this.Entries.Add(new LogEntry() { Message = "[JsonLogViewer] Will exit on PID " + pid + " termination." });

                    var process = Process.GetProcessById(pid);
                    process.WaitForExit();
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        App.Current.Shutdown();
                    });
                });
            }

            string pipeName;
            if (commandLineArgs.TryGetValue("pipe", out pipeName))
            {
                this.pipeWatcher = new PipeWatcher(pipeName);
                this.pipeWatcher.OnLog += (sender, args) =>
                {
                    App.DispatcherInvoke(() =>
                    {
                        if (args.IsValid)
                        {
                            this.Entries.Add(args.Entry);
                        }
                        else
                        {
                            this.Entries.Add(new LogEntry() { Message = "Error: " + args.ParseError });
                        }

                        CollectionViewSource.GetDefaultView(this.Entries).MoveCurrentToLast();
                    });
                };

                this.pipeWatcher.Start();
            }

            string logFile;
            if (commandLineArgs.TryGetValue("file", out logFile))
            {
                this.logWatcher = new LogWatcher(cmdArgs[1]);
                this.logWatcher.OnLog += (sender, args) =>
                {
                    App.DispatcherInvoke(() =>
                    {
                        if (args.IsValid)
                        {
                            this.Entries.Add(args.Entry);
                        }
                        else
                        {
                            this.Entries.Add(new LogEntry() { Message = "Error: " + args.ParseError });
                        }

                        CollectionViewSource.GetDefaultView(this.Entries).MoveCurrentToLast();
                    });
                };
                this.logWatcher.Start();
            }
        }

        public void Dispose()
        {
            if (this.pipeWatcher != null)
            {
                this.pipeWatcher.Dispose();
                this.pipeWatcher = null;
            }

            if (this.logWatcher != null)
            {
                this.logWatcher.Dispose();
                this.logWatcher = null;
            }
        }
    }
}