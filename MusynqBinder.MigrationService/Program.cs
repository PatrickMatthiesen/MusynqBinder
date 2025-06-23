using ConcertTracker.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusynqBinder.MigrationService;
using MusynqBinder.Web.Data;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<MusicDbContext>("musynqbinder");
builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb");

builder.Services.AddHostedService<Worker<MusicDbContext>>();
builder.Services.AddHostedService<Worker<ApplicationDbContext>>();

// the CompletionTracker service will wait for all migration services to complete before stopping the application
builder.Services.AddHostedService<CompletionTracker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => {
        tracing.AddSource(Worker<MusicDbContext>.ActivitySourceName);
        tracing.AddSource(Worker<ApplicationDbContext>.ActivitySourceName);
    });

builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromMinutes(1);
});

var host = builder.Build();
await host.RunAsync();


public class CompletionTracker(
    IHostApplicationLifetime hostApplicationLifetime, 
    IServiceProvider serviceProvider,
    ILogger<CompletionTracker> logger) : BackgroundService {
    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        var migrationServices = serviceProvider.GetServices<IHostedService>()
            .Where(s => s.GetType().IsGenericType && s.GetType().GetGenericTypeDefinition() == typeof(Worker<>))
            .Cast<BackgroundService>()
            .ToList();
        logger.LogInformation("All migration services: {migrationServices}", string.Join(", ", migrationServices.Select(s => s.GetType().Name)));

        while(migrationServices is not []) {
            if (stoppingToken.IsCancellationRequested) {
                logger.LogInformation("Cancellation requested, stopping application.");
                hostApplicationLifetime.StopApplication();
                return Task.CompletedTask;
            }
            foreach (var service in migrationServices.ToArray()) {
                var task = service.ExecuteTask;
                if (task == null) continue; // Skip if the service has not started yet
                if (task.IsCompleted)
                {
                    logger.LogInformation("Migration service {serviceName} has completed.", service.GetType().Name);
                    migrationServices.Remove(service);
                }
            }
            logger.LogInformation("Waiting for all migration services to complete...");
            Thread.Sleep(200); // Sleep for a bit to avoid busy-waiting
        }
        logger.LogInformation("All migration services have completed. Stopping application.");
        hostApplicationLifetime.StopApplication();
        return Task.CompletedTask;
    }
}