using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xutils.ConsoleApp;

namespace monitor_queues
{
    public class AzureStorageQueueMonitor : IQueueMonitor
    {
        private readonly IColoredConsole _console;

        private const string colunas = "{0,60} {1,10} {2,7} {3,10} {4,14}";
        private readonly string DARKRED_WHITE = $"<background c=\"DarkRed\"><foreground c=\"White\">{colunas}</foreground></background>";
        private readonly string DARKGREEN_WHITE = $"<background c=\"DarkGreen\"><foreground c=\"White\">{colunas}</foreground></background>";
        private readonly string BLACK_RED = $"<background c=\"Black\"><foreground c=\"Red\">{colunas}</foreground></background>";
        private readonly string BLACK_BLUE = $"<background c=\"Black\"><foreground c=\"Blue\">{colunas}</foreground></background>";
        private readonly string BLACK_GREEN = $"<background c=\"Black\"><foreground c=\"Green\">{colunas}</foreground></background>";
        private readonly string BLACK_GRAY = $"<background c=\"Black\"><foreground c=\"Gray\">{colunas}</foreground></background>";
        private readonly string BLACK_WHITE = $"<background c=\"Black\"><foreground c=\"White\">{colunas}</foreground></background>";
        private readonly string CLEAR = "<background c=\"Black\"><foreground c=\"Gray\">{0}</foreground></background>";

        public AzureStorageQueueMonitor(IColoredConsole console)
        {
            _console = console;
        }

        public async Task<bool> Start(ConfigurationOptions config, CancellationToken cancellationToken)
        {

            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(config.ConnectionString);
            }
            catch (FormatException ex)
            {
                _console.ColoredWriteLine($"<r>Error parsing the connection string: <w>{ex.Message}</w></r>");
                return false;
            }

            Console.Clear();
            Console.Title = $"Azure Storage Queue Monitor{(!string.IsNullOrWhiteSpace(config.Title) ? $" -- {config.Title}" : "")}";
            Console.CursorVisible = false;

            Dictionary<string, QueueInfo> queueInfos = new Dictionary<string, QueueInfo>();

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            int iterations = 0;
            int previousCount = 0;
            Stopwatch sw = new Stopwatch();
            while (true)
            {
                await Task.Yield();
                if (cancellationToken.IsCancellationRequested)
                    return true;

                sw.Restart();
                iterations++;
                if (iterations == 50)
                {
                    iterations = 0;
                    previousCount = queueInfos.Count;
                    queueInfos.Clear();
                }
                List<CloudQueue> queues = queueClient.ListQueues().ToList();
                foreach (var queue in queues)
                {
                    queue.FetchAttributes();
                    string name = queue.Name;
                    int count = queue.ApproximateMessageCount ?? 0;
                    if (!queueInfos.ContainsKey(name))
                    {
                        if (count > 0)
                            queueInfos.Add(name, new QueueInfo { Name = name, FirstCount = count, CurrentCount = count });
                    }
                    else
                    {
                        var info = queueInfos[name];
                        if (count > info.FirstCount)
                        {
                            info.FirstCount = count;
                            info.IncreasedCount = true;
                        }
                        else
                        {
                            info.IncreasedCount = false;
                        }
                        info.DecreasedCount = count < info.CurrentCount;
                        info.CurrentCount = count;
                    }
                }
                Console.CursorVisible = false;
                Console.SetCursorPosition(0, 0);
                _console.ColoredWriteLine(BLACK_WHITE, "Queue", "Messages", "Msgs/s", "Elapsed", "Completion");
                _console.ColoredWriteLine(BLACK_WHITE, new string('=', 5), new string('=', 8), new string('=', 6), new string('=', 7), new string('=', 10));

                string colorPattern = "";
                foreach (string queue in queueInfos.Keys.OrderBy(k => k))
                {
                    if (queueInfos[queue].Name.EndsWith("-poison") && queueInfos[queue].IncreasedCount)
                    {
                        colorPattern = DARKRED_WHITE;
                    }
                    else if (queueInfos[queue].Name.EndsWith("-poison") && queueInfos[queue].DecreasedCount)
                    {
                        colorPattern = DARKGREEN_WHITE;
                    }
                    else if (queueInfos[queue].Name.EndsWith("-poison"))
                    {
                        colorPattern = BLACK_RED;
                    }
                    else if (queueInfos[queue].IncreasedCount)
                    {
                        colorPattern = BLACK_BLUE;
                    }
                    else if (queueInfos[queue].DecreasedCount)
                    {
                        colorPattern = BLACK_GREEN;
                    }
                    else
                    {
                        colorPattern = BLACK_GRAY;
                    }
                    _console.ColoredWriteLine(colorPattern, queueInfos[queue].Name, queueInfos[queue].CurrentCount.ToString(), queueInfos[queue].Speed, queueInfos[queue].RemainingTime, queueInfos[queue].EstimatedConclusion);
                }
                if (iterations == 0)
                {
                    for (int i = queueInfos.Count, l = previousCount + 2; i < l; i++)
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine(new string(' ', Console.BufferWidth - 1));
                        //_console.ColoredWriteLine(CLEAR, new string('.', Console.BufferWidth - 1));
                    }
                }
                Console.SetCursorPosition(0, 0);
                sw.Stop();
                long sleepInterval = config.Interval - sw.ElapsedMilliseconds;
                if (sleepInterval > 0)
                    await Task.Delay(config.Interval, cancellationToken);
            }
        }
    }
}
