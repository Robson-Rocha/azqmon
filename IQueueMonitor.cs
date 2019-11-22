using System.Threading;
using System.Threading.Tasks;

namespace monitor_queues
{
    public interface IQueueMonitor
    {
        Task<bool> Start(ConfigurationOptions config, CancellationToken cancellationToken);
    }
}