using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SinOfSloth;

// The stock lifetime accepts stop requests from sc stop, Stop-Service and Services.msc, and the SCM never restarts a
// clean stop. This one refuses those requests, so the service can only end when Guard sees Disabled in config.json
// or when Windows shuts down. Killing the process is still an unexpected exit, which the SCM restarts.
public sealed class GuardLifetime : WindowsServiceLifetime
{
    public GuardLifetime(
        IHostEnvironment environment,
        IHostApplicationLifetime applicationLifetime,
        ILoggerFactory loggerFactory,
        IOptions<HostOptions> hostOptions,
        IOptions<WindowsServiceLifetimeOptions> optionsAccessor)
        : base(environment, applicationLifetime, loggerFactory, hostOptions, optionsAccessor)
    {
        CanStop = false;
        CanShutdown = true;
    }
}
