namespace JsonLogConsole
{
    using System;
    using System.Globalization;

    using JsonLog;

    class Program
    {
        static volatile object consoleLock = new object();

        static void Main(string[] args)
        {
            string file = args[0];

            LogWatcher watcher = new LogWatcher(file);
            watcher.OnLog += (sender, eventArgs) =>
            {
                if (eventArgs.IsValid)
                {
                    string line = string.Format(
                        CultureInfo.CurrentCulture,
                        "[{0,12}] [{3,3}] [{1,-5}] {2}",
                        eventArgs.Entry.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.CurrentCulture),
                        eventArgs.Entry.Level,
                        eventArgs.Entry.Message,
                        eventArgs.Entry.ThreadId);

                    if (eventArgs.Entry.Level == "ERROR")
                    {
                        WriteToConsole(line, ConsoleColor.Red);
                    }
                    else if (eventArgs.Entry.Level == "WARN")
                    {
                        WriteToConsole(line, ConsoleColor.Yellow);
                    }
                    else if (eventArgs.Entry.Level == "DEBUG" || eventArgs.Entry.Level == "VERB")
                    {
                        WriteToConsole(line, ConsoleColor.Gray);
                    }
                    else
                    {
                        WriteToConsole(line, ConsoleColor.White);
                    }
                }
                else
                {
                    WriteToConsole("Error: " + eventArgs.ParseError, ConsoleColor.Red);
                }
            };

            watcher.Start();

            Console.ReadLine();

            watcher.Start();
        }

        static void WriteToConsole(string line, ConsoleColor color)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(line);
                Console.ResetColor();
            }
        }

        /*
if (args.Length == 0)
{
    //using (FileStream stream = new FileStream(@"C:\NoIndex\log\test1.jsonlog", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
    //{
    //    stream.Seek(0, SeekOrigin.End);
    //    StreamWriter sw = new StreamWriter(stream);
    //    sw.AutoFlush = true;
    //    int i = 0;
    //    while (i < 1000)
    //    {
    //        LogEntry entry = new LogEntry()
    //        {
    //            Level = "INFO",
    //            Message = "This is test " + i,
    //            ThreadId = 1,
    //            Timestamp = DateTime.Now
    //        };

    //        string e = JsonConvert.SerializeObject(entry);
    //        //sw.WriteLine("Test " + DateTime.Now.ToString("s"));
    //        sw.WriteLine(e);
    //        sw.Flush();
    //        i++;

    //        if (i%20 == 0)
    //        {
    //            Thread.Sleep(500);
    //        }
    //        //Thread.Sleep(3000);
    //    }
    //}

    int i = 0;
    while (i < 1000)
    {
        LogEntry entry = new LogEntry()
        {
            Level = "INFO",
            Message = "This is test " + i,
            ThreadId = 1,
            Timestamp = DateTime.Now
        };

        string e = JsonConvert.SerializeObject(entry);
        //sw.WriteLine("Test " + DateTime.Now.ToString("s"));
        File.AppendAllLines(
            @"C:\NoIndex\log\test1.jsonlog",
            new List<string>() { e });
        //sw.WriteLine(e);
        //sw.Flush();
        i++;
        if (i%20 == 0)
        {
            Thread.Sleep(1000);
        }
    }
}
*/
    }
}
