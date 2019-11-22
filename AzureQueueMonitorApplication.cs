using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xutils.ConsoleApp;

namespace monitor_queues
{
    [Command(Name = "azqmon",
             Description = "Azure Storage Queues Monitor: Utility to monitor Azure Storage Queues",
             OptionsComparison = StringComparison.InvariantCultureIgnoreCase,
             ThrowOnUnexpectedArgument = false)]
    [HelpOption("-?")]
    public class AzureQueueMonitorApplication : ConfigurationOptions
    {
        private readonly IColoredConsole _console;
        private readonly IQueueMonitor _queueMonitor;
        private readonly CancellationTokenSource cts;

        public AzureQueueMonitorApplication(IColoredConsole console, IQueueMonitor queueMonitor)
        {
            _console = console;
            _queueMonitor = queueMonitor;
            cts = new CancellationTokenSource();
        }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            if(app.Options.All(o => o.Values.Count == 0))
            {
                app.ShowHelp();
                return -1;
            }

            ConfigurationOptions configurationOptions = this as ConfigurationOptions;

            if (!string.IsNullOrWhiteSpace(ConfigFile))
            {
                configurationOptions = JsonConvert.DeserializeObject<ConfigurationOptions>(File.ReadAllText(ConfigFile));
                if (string.IsNullOrWhiteSpace(this.ConnectionString) && string.IsNullOrWhiteSpace(configurationOptions.ConnectionString))
                {
                    app.ShowHint();
                    _console.ColoredWriteLine("<r>The required ConnectionString option was not supplied in configuration file</r>");
                    return -2;
                }
                if(!string.IsNullOrWhiteSpace(ConnectionString))
                    configurationOptions.ConnectionString = this.ConnectionString;
            }


            if(string.IsNullOrWhiteSpace(configurationOptions.ConnectionString))
            {
                app.ShowHint();
                _console.ColoredWriteLine("<r>The required Azure Storage connection string was not supplied neither in <w>--connectionstring</w> option, nor in the <w>--config</w> option supplied configuration file</r>");
                return -3;
            }

            if (string.IsNullOrWhiteSpace(configurationOptions.PollingInterval))
                configurationOptions.PollingInterval = "2000";

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var success = await _queueMonitor.Start(configurationOptions, cts.Token);
            Console.ResetColor();
            Console.CursorVisible = true;
            if(success)
                Console.Clear();
            return success ? 0 : -4;
        }
    }
}
