using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xutils.ConsoleApp;

namespace monitor_queues
{
    public class AzureStorageQueueMonitor : IQueueMonitor
    {
        private readonly IColoredConsole _console;

        private const string columnSizes = "{0,58}  {1,8}  {2,6}  {3,14}  {4,15}";
        private readonly string DARKRED_WHITE = $"<background c=\"DarkRed\"><foreground c=\"White\">{columnSizes}</foreground></background>";
        private readonly string DARKGREEN_WHITE = $"<background c=\"DarkGreen\"><foreground c=\"White\">{columnSizes}</foreground></background>";
        private readonly string BLACK_RED = $"<background c=\"Black\"><foreground c=\"Red\">{columnSizes}</foreground></background>";
        private readonly string BLACK_BLUE = $"<background c=\"Black\"><foreground c=\"Blue\">{columnSizes}</foreground></background>";
        private readonly string BLACK_GREEN = $"<background c=\"Black\"><foreground c=\"Green\">{columnSizes}</foreground></background>";
        private readonly string BLACK_GRAY = $"<background c=\"Black\"><foreground c=\"Gray\">{columnSizes}</foreground></background>";
        private readonly string BLACK_WHITE = $"<background c=\"Black\"><foreground c=\"White\">{columnSizes}</foreground></background>";

        public AzureStorageQueueMonitor(IColoredConsole console)
        {
            _console = console;
        }

        private int _previousQueueCount = 0;

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
                    queueInfos.Clear();
                }
                List<CloudQueue> queues = queueClient.ListQueues().ToList();
                foreach (var queue in queues)
                {
                    try
                    {
                        queue.FetchAttributes();

                    }
                    catch (StorageException sex) when (sex.Message.Contains("The specified queue does not exist"))
                    {
                        if (queueInfos.ContainsKey(queue.Name))
                        {
                            queueInfos.Remove(queue.Name);
                        }
                    }                    
                    
                    string name = queue.Name;
                    if (config.IgnoredQueues?.Any(i => Regex.IsMatch(name, i)) ?? false)
                        continue;

                    int count = queue.ApproximateMessageCount ?? 0;
                    if (!queueInfos.ContainsKey(name))
                    {
                        bool isImportant = false;
                        if (config.ImportantQueues?.Any(i => Regex.IsMatch(name, i)) ?? false)
                            isImportant = true;

                        string groupName = "NOT GROUPED";
                        int groupOrder = int.MaxValue;
                        if (config.GroupQueues)
                        {
                            foreach (var queueGroup in config.QueueGroups)
                            {
                                if (queueGroup.Queues.Any(q => Regex.IsMatch(name, q)))
                                {
                                    groupName = queueGroup.GroupName;
                                    groupOrder = queueGroup.Order;
                                    continue;
                                }
                            }
                        }

                        queueInfos.Add(name, new QueueInfo { 
                            Name = name, 
                            FirstCount = count, 
                            CurrentCount = count, 
                            IsImportant = isImportant,
                            GroupName = groupName,
                            GroupOrder = groupOrder
                        });
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
                PrintQueues(config, queueInfos, iterations);
                if (config.ExportCountData)
                {
                    if(_previousQueueCount != queueInfos.Count)
                    {
                        _exportFullPath = null;
                    }
                    ExportCountData(config, queueInfos);
                }
                _previousQueueCount = queueInfos.Count;
                sw.Stop();
                long sleepInterval = config.Interval - sw.ElapsedMilliseconds;
                if (sleepInterval > 0)
                    await Task.Delay(config.Interval, cancellationToken);
            }
        }

        private string _exportFullPath;

        private void ExportCountData(ConfigurationOptions config, Dictionary<string, QueueInfo> queueInfos)
        {
            if (_exportFullPath == null)
            {
                string exportPath;
                if (string.IsNullOrWhiteSpace(config.ExportPath))
                {
                    exportPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }
                else
                {
                    exportPath = config.ExportPath;
                }

                _exportFullPath = Path.Combine(exportPath, $"azqmon_{DateTime.Now.ToString("yyyyMMddhhmmss")}.csv");
            }

            var sb = new StringBuilder();
            var orderedQueueInfos = queueInfos.Values.Select(q => new { q.Name, q.CurrentCount })
                                                     .OrderBy(q => q.Name)
                                                     .ToArray();

            if (!File.Exists(_exportFullPath))
            {
                sb.Append("Date");
                foreach (var queueInfo in orderedQueueInfos)
                {
                    sb.Append($";{queueInfo.Name}");
                }
                sb.AppendLine();
            }
            sb.Append(DateTime.Now.ToString());
            foreach (var queueInfo in orderedQueueInfos)
            {
                sb.Append($";{queueInfo.CurrentCount}");
            }
            sb.AppendLine();
            File.AppendAllText(_exportFullPath, sb.ToString());
        }

        private void PrintQueues(ConfigurationOptions config, Dictionary<string, QueueInfo> queueInfos, int iterations)
        {
            Console.CursorVisible = false;
            if (iterations == 0)
            {
                Console.Clear();
            }

            Console.SetCursorPosition(0, 0);
            _console.ColoredWriteLine(BLACK_WHITE, "Queue", "Messages", "Msgs/s", "Remaining Time", "Est. Conclusion");
            _console.ColoredWriteLine(BLACK_WHITE, new string('=', 5), new string('=', 8), new string('=', 6), new string('=', 14), new string('=', 15));

            string colorPattern = "";

            var filteredQueueInfos = !config.ShowEmptyQueues ? queueInfos.Where(q => q.Value.FirstCount != 0 || q.Value.CurrentCount != 0)
                                                             : queueInfos;

            IOrderedEnumerable<KeyValuePair<string, QueueInfo>> orderedQueueInfos;
            if (config.GroupQueues)
            {
                orderedQueueInfos = filteredQueueInfos.OrderBy(q => q.Value.GroupOrder)
                                                      .ThenBy(q => q.Value.GroupName);
                if (config.OrderByQueueSize)
                {
                    orderedQueueInfos = orderedQueueInfos.ThenByDescending(q => q.Value.CurrentCount);
                }
                orderedQueueInfos = orderedQueueInfos.ThenBy(q => q.Key);
            }
            else
            {
                if (config.OrderByQueueSize)
                {
                    orderedQueueInfos = filteredQueueInfos.OrderByDescending(q => q.Value.CurrentCount)
                                                          .ThenBy(q => q.Key);
                }
                else
                {
                    orderedQueueInfos = filteredQueueInfos.OrderBy(q => q.Key);
                }
            }


            string previousGroupName = "";
            bool isFirstGroup = true;

            foreach (string queue in orderedQueueInfos.Select(q => q.Key))
            {
                if (queueInfos[queue].GroupName != previousGroupName)
                {
                    if (!isFirstGroup)
                    {
                        //"{0,58}  {1,8}  {2,6}  {3,14}  {4,15}"
                        _console.ColoredWriteLine(BLACK_WHITE, new string('\u00A0', 58)
                                                             , new string('\u00A0', 8)
                                                             , new string('\u00A0', 6)
                                                             , new string('\u00A0', 14)
                                                             , new string('\u00A0', 15));
                    }
                    _console.ColoredWriteLine(BLACK_WHITE, queueInfos[queue].GroupName.ToUpper(), "", "", "", "");
                    previousGroupName = queueInfos[queue].GroupName;
                    isFirstGroup = false;
                }

                if (queueInfos[queue].IsImportant && queueInfos[queue].IncreasedCount)
                {
                    colorPattern = DARKRED_WHITE;
                }
                else if (queueInfos[queue].IsImportant && queueInfos[queue].DecreasedCount)
                {
                    colorPattern = DARKGREEN_WHITE;
                }
                else if (queueInfos[queue].IsImportant)
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
            Console.SetCursorPosition(0, 0);
        }
    }
}