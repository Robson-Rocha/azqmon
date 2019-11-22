using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;
using Xutils.ValidationAttributes;

namespace monitor_queues
{
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
        [Option(CommandOptionType.SingleValue, Template = "-p|--pollinginterval",
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
                Description = "Optional. Json configuration file containing the Title, ConnectionString and PollingInterval options. If this option is supplied, all other options are ignored.", ValueName ="Json config path")]
        public string ConfigFile { get; set; }

        public int Interval { get => _interval; }
    }
}
