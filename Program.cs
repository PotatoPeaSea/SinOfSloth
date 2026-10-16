using LolGuard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Arguments mean an administrator command: install, status, delay 10, ...
if (args.Length > 0)
    return Admin.Run(args);

// No arguments: the Service Control Manager starts it this way.
// Run from a console, the same code runs in the foreground (Ctrl+C to stop).
Host.CreateDefaultBuilder()
    .UseWindowsService(options => options.ServiceName = Settings.ServiceName)
    .ConfigureServices(services => services.AddHostedService<Guard>())
    .Build()
    .Run();

return 0;
