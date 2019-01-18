using System.Threading;
using System.Threading.Tasks;

namespace Thor.Hosting.GenericHost.FunctionalTest
{
    internal class MyBackgroundService : BackgroundServiceBase
    {
        protected override Task OnExecuteAsync(CancellationToken stoppingToken)
        {
            // Do some work
            return Task.CompletedTask;
        }
    }
}
