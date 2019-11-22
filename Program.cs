using System;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Xutils.ConsoleApp;

namespace monitor_queues
{

    class Program
    {
        static void Main(string[] args)
        {
            var console = new ColoredConsole();
            var services = new ServiceCollection()
                           .AddSingleton<IColoredConsole, ColoredConsole>(s => console)
                           .AddSingleton<IQueueMonitor, AzureStorageQueueMonitor>()
                           .BuildServiceProvider();

            var app = new CommandLineApplication<AzureQueueMonitorApplication>();
            app.Conventions.UseDefaultConventions()
                           .UseConstructorInjection(services);

            try
            {
                app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                console.ColoredWriteLine($"<r>{ex.Message}</r>");
            }
            catch (Exception ex)
            {
                Console.ResetColor();
                Console.CursorVisible = true;
                Console.Clear();
                console.ColoredWriteLine($"<r>An unexpected error ocurred: <w>{ex.Message}</w></r>");
            }
        }
    }
}
