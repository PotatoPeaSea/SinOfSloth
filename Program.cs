using LolGuard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// The Service Control Manager starts LolGuard.exe with no arguments.
// Run from a console, the same code runs in the foreground (Ctrl+C to stop).
Host.CreateDefaultBuilder()
    .UseWindowsService(options => options.ServiceName = Settings.ServiceName)
    .ConfigureServices(services => services.AddHostedService<Guard>())
    .Build()
    .Run();
