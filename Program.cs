using SinOfSloth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

// Arguments mean an administrator command: install, status, delay 10, ...
if (args.Length > 0)
    return Admin.Run(args);

// No arguments: the Service Control Manager starts it this way.
// Run from a console, the same code runs in the foreground (Ctrl+C to stop).
Host.CreateDefaultBuilder()
    .UseWindowsService(options => options.ServiceName = Settings.ServiceName)
    .ConfigureServices(services =>
    {
        // Only inside the service: a console run keeps the normal Ctrl+C lifetime.
        if (!Environment.UserInteractive)
            services.Replace(ServiceDescriptor.Singleton<IHostLifetime, GuardLifetime>());

        services.AddHostedService<Guard>();
    })
    .Build()
    .Run();

return 0;
