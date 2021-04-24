using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;
using Xutils.ValidationAttributes;

namespace monitor_queues
{
    public class GroupConfiguration
    {
        public string GroupName { get; set; }
        public string[] Queues { get; set; }
        public int Order { get; set; }
    }

    public class ConfigurationOptions
    {
        private int _interval;
        private string pollingInterval;

        [Option(CommandOptionType.SingleValue, Template = "-t|--title",
                Description = "Optional. A title to be shown on the console window. Useful to differentiate environments or when using multiple instances.",
                ShowInHelpText = true, ValueName = "Title")]
        public string Title { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-cs|--connectionstring",
                Description = "Required, but can be omitted if supplied in configuration file. The Azure Storage connection string, in which the queues will be monitored.",
                ShowInHelpText = true, ValueName = "Connection String")]
        public string ConnectionString { get; set; }

        [RegularExpression(@"\d+", ErrorMessage = "The polling interval must be numeric.")]
        [Option(CommandOptionType.SingleValue, Template = "-i|--pollinginterval",
                Description = "Optional. The interval between the pollings of the queues, in milliseconds. Defaults to 2000 milliseconds if ommited.",
                ShowInHelpText = true, ValueName = "ms")]
        public string PollingInterval
        {
            get => pollingInterval;
            set
            {
                pollingInterval = value;
                int.TryParse(value, out _interval);
            }
        }

        [FileMustExist(Optional = true, ErrorMessage = "Config file not found")]
        [Option(CommandOptionType.SingleValue, Template = "-c|--config",
                Description = "Optional. Json configuration file containing the Title, ConnectionString and PollingInterval options. If this option is supplied, all other options are ignored.",
                ShowInHelpText = true, ValueName ="Json config path")]
        public string ConfigFile { get; set; }

        public int Interval { get => _interval; }

        [Option(CommandOptionType.SingleValue, Template = "-e|--showEmptyQueues",
                Description = "Optional. Indicates if empty queues must be shown. If not specified, only non-empty queues will be shown after the refresh.",
                ShowInHelpText = true, ValueName = "Show empty queues")]
        public bool ShowEmptyQueues { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-s|--orderByQueueSize",
                Description = "Optional. Indicates if the queues must be ordered by size, then by name. If not specified, the queues will be ordered by name only.",
                ShowInHelpText = true, ValueName = "Order queues by size, then by name")]
        public bool OrderByQueueSize { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-x|--exportCountData",
                Description = "Optional. Indicates if the queues counting must be exported over time. This is useful for plotting graphs os historical analysis.",
                ShowInHelpText = true, ValueName = "Export count data.")]
        public bool ExportCountData { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-p|--exportFolderPath",
                Description = "Optional. Indicates the folder path for count data exporting. If not specified, the current user documents folder shall be used. The file name pattern is 'azqmon_YYYYMMDDHHMMSS.csv.",
                ShowInHelpText = true, ValueName = "Export folder path.")]
        public string ExportPath { get; set; }

        [Option(CommandOptionType.MultipleValue, Template = "--ignore",
                Description = "Optional. Indicates a queue name pattern (regex) to ignore.",
                ShowInHelpText = true, ValueName = "Queue name to ignore.")]
        public string[] IgnoredQueues { get; set; }

        [Option(CommandOptionType.MultipleValue, Template = "--important",
                Description = "Optional. Indicates a queue name pattern (regex) to consider as important. Important queues are emphasized in exhibition.",
                ShowInHelpText = true, ValueName = "Important queue name.")]
        public string[] ImportantQueues { get; set; }

        public bool GroupQueues { get; set; }

        public GroupConfiguration[] QueueGroups { get; set; }
    }
}
